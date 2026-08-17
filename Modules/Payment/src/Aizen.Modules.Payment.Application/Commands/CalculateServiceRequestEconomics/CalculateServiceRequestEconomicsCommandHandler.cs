using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Application.Gateway;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Application.Commands.CalculateServiceRequestEconomics;

[DocumentationInfo("Calculate SR payment economics command handler (BE-P8)",
    "Runs the §19.9 combiner then, on an approving decision, persists the immutable snapshot, initiates the escrow " +
    "(gross = CustomerTotal, split = ProviderNet), links transaction.EconomicsSnapshotId, and bumps applied-count once. " +
    "Idempotent via IdempotencyKey — a retry returns the existing snapshot/transaction and never double-applies.")]
public sealed class CalculateServiceRequestEconomicsCommandHandler
    : AizenCommandHandler<CalculateServiceRequestEconomicsCommand, CalculateServiceRequestEconomicsRemoteCallResponse>
{
    private readonly ServiceRequestPaymentEconomicsCalculationService _calc;
    private readonly IPaymentEconomicsSnapshotRepository              _snapshots;
    private readonly IPaymentTransactionRepository                    _transactions;
    private readonly ICommissionRuleRepository                        _commissionRules;
    private readonly IProviderPaymentProfileRepository                _profiles;
    private readonly PaymentGatewayResolver                           _gatewayResolver;
    private readonly CustomerBenefitBudgetService                     _budgetService;
    private readonly CommissionBenefitEntitlementService              _entitlementService;
    private readonly ProviderNegativeBalanceService                   _negativeBalance;
    private readonly FinancialLedgerPostingService                    _ledgerPosting;
    private readonly ILineProfitProtectionEvaluationLogRepository     _lineProtectionLogs;   // BE-S9
    private readonly IOptions<PaymentAuthModeOptions>                 _authModeOptions;
    private readonly ILogger<CalculateServiceRequestEconomicsCommandHandler> _logger;

    public CalculateServiceRequestEconomicsCommandHandler(
        ServiceRequestPaymentEconomicsCalculationService calc,
        IPaymentEconomicsSnapshotRepository              snapshots,
        IPaymentTransactionRepository                    transactions,
        ICommissionRuleRepository                        commissionRules,
        IProviderPaymentProfileRepository                profiles,
        PaymentGatewayResolver                           gatewayResolver,
        CustomerBenefitBudgetService                     budgetService,
        CommissionBenefitEntitlementService              entitlementService,
        ProviderNegativeBalanceService                   negativeBalance,
        FinancialLedgerPostingService                    ledgerPosting,
        ILineProfitProtectionEvaluationLogRepository     lineProtectionLogs,
        IOptions<PaymentAuthModeOptions>                 authModeOptions,
        ILogger<CalculateServiceRequestEconomicsCommandHandler> logger)
    {
        _calc               = calc;
        _snapshots          = snapshots;
        _transactions       = transactions;
        _commissionRules    = commissionRules;
        _profiles           = profiles;
        _gatewayResolver    = gatewayResolver;
        _budgetService      = budgetService;
        _entitlementService = entitlementService;
        _negativeBalance    = negativeBalance;
        _ledgerPosting      = ledgerPosting;
        _lineProtectionLogs = lineProtectionLogs;
        _authModeOptions    = authModeOptions;
        _logger             = logger;
    }

    public override async Task<CalculateServiceRequestEconomicsRemoteCallResponse?> Handle(
        CalculateServiceRequestEconomicsCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // ── Idempotency (§8): an existing transaction for this key → return it, no second snapshot / MarkApplied ──
        var existing = await _transactions.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Duplicate P8 economics blocked. IdempotencyKey={Key}", request.IdempotencyKey);
            decimal feeGross = 0m;
            if (existing.EconomicsSnapshotId is { } snapId)
            {
                var snap = await _snapshots.GetByIdAsync(snapId, ct);
                feeGross = snap?.PlatformFeeGrossAmountSnapshot ?? 0m;
            }
            return new CalculateServiceRequestEconomicsRemoteCallResponse
            {
                Decision              = ProfitProtectionDecisionState.Approved,
                TransactionId         = existing.Id,
                EconomicsSnapshotId   = existing.EconomicsSnapshotId,
                CustomerTotalAmount   = existing.GrossAmount,
                ProviderNetTotal      = existing.NetPayoutAmount,
                PlatformFeeGross      = feeGross,
                TransactionCommission = existing.CommissionAmount,
            };
        }

        // ── BE-I1 split-eligibility gate (P9 prerequisite): recipient MUST have a split-eligible sub-merchant, else no
        //    escrow/snapshot — SR acceptance blocks (funds must never land on the main merchant via a null sub-merchant). ──
        var eligibility = await _profiles.GetSplitEligibilityAsync(request.ProviderProfileId, ct);
        if (!eligibility.IsSplitEligible)
        {
            _logger.LogWarning(
                "P8 blocked — provider {ProviderId} not split-eligible (onboarding {Status}) for SR {SrId} Offer {OfferId}.",
                request.ProviderProfileId, eligibility.OnboardingStatus, request.ServiceRequestId, request.OfferId);
            return new CalculateServiceRequestEconomicsRemoteCallResponse
            {
                Decision              = ProfitProtectionDecisionState.Rejected,
                ProviderSplitEligible = false,
                Reason                = eligibility.HasProfile
                    ? $"Provider sub-merchant not split-eligible (onboarding status: {eligibility.OnboardingStatus})."
                    : "Provider has no payment profile — sub-merchant not onboarded.",
            };
        }

        // ── BE-P10 §7.4 negative-balance acceptance gate: a provider whose negative balance is over the policy limit is
        //    blocked from new work until it is recovered (auto-offset from payouts, or admin manual adjustment). ──
        if (await _negativeBalance.IsOverLimitAsync(request.ProviderProfileId, request.CurrencyCode, ct))
        {
            _logger.LogWarning(
                "P8 blocked — provider {ProviderId} negative balance over limit for SR {SrId} Offer {OfferId}.",
                request.ProviderProfileId, request.ServiceRequestId, request.OfferId);
            return new CalculateServiceRequestEconomicsRemoteCallResponse
            {
                Decision = ProfitProtectionDecisionState.Rejected,
                Reason   = "Provider negative balance exceeds the allowed limit — resolve outstanding balance before accepting new work.",
            };
        }

        // ── §19.9 combiner (pure calc; loads plan/rules/fee/policy) ──
        var result = await _calc.CalculateAsync(request, ct);
        var core   = result.Core;

        // ── §19.9-12 gate: Rejected/ConfigurationError → NO snapshot, NO escrow. Return the decision. ──
        if (!core.CanProceed || core.Snapshot is null)
        {
            // ── BE-S9 (§20.12): a LINE-level failure is audited to its own insert-only log (mirrors P5's non-Approved log;
            //    no snapshot is written on failure). The transaction-level path is unchanged. ──
            if (core.LineProtectionFailed && core.LineProtectionResults is { Count: > 0 })
            {
                var eval = new LineProfitProtectionEvaluation(
                    Passed: false, State: core.Decision, Lines: core.LineProtectionResults,
                    PrimaryErrorCode: core.LineProtectionErrorCode, Reason: core.Reason);
                await _lineProtectionLogs.AddAsync(LineProfitProtectionEvaluationLogEntity.Create(
                    request.ServiceRequestId, request.OfferId, request.CurrencyCode, policyId: null, eval, DateTime.UtcNow), ct);
                await _lineProtectionLogs.SaveChangesAsync(ct);
                _logger.LogWarning(
                    "P8 blocked by S9 line-level profit protection. SR={SrId} Offer={OfferId} Decision={Decision} Code={Code} — {Reason}",
                    request.ServiceRequestId, request.OfferId, core.Decision, core.LineProtectionErrorCode, core.Reason);
            }

            return new CalculateServiceRequestEconomicsRemoteCallResponse
            {
                Decision               = core.Decision,
                Reason                 = core.Reason,
                ResolvedProviderPlanId = result.ResolvedProviderPlanId,
                CustomerTotalAmount    = core.CustomerTotalAmount,
                ProviderNetTotal       = core.ProviderNetTotal,
                PlatformFeeGross       = core.PlatformFeeGross,
                TransactionCommission  = core.TransactionCommission,
            };
        }

        // ── Persist the immutable snapshot first to materialise its Id (atomic — the handler is transactional) ──
        var snapshot = core.Snapshot;
        await _snapshots.AddAsync(snapshot, ct);
        await _snapshots.SaveChangesAsync(ct);

        // ── BE-P8b: reserve the platform-funded customer benefit budget + P7 entitlement BEFORE escrow (§19.7). Idempotent
        //    on the offer contextRef (SR-{sr}-OFFER-{offer}). The discount is already budget-capped, so reserve never fails. ──
        var contextRef = request.IdempotencyKey;
        Domain.Entities.CustomerBenefit.CustomerBenefitReservationEntity? budgetReservation = null;
        Domain.Entities.CommissionBenefit.ProviderCommissionBenefitUsageEntity? entitlementUsage = null;
        if (result.BudgetId is { } budgetId && result.PlatformFundedDiscount > 0m)
            budgetReservation = await _budgetService.ReserveAsync(budgetId, result.PlatformFundedDiscount, contextRef, ct);
        if (result.EntitlementId is { } entId && result.EntitlementBenefitAmount > 0m)
            entitlementUsage = await _entitlementService.ReserveAsync(
                entId, result.EntitlementGmv, result.EntitlementBenefitAmount, contextRef, ct);

        var transaction = new PaymentTransactionEntity[1];
        try
        {
            // ── BE-P9: resolve the auth-mode policy (Capture default / PreAuth per category) for the audit snapshot ──
            var authMode = PaymentAuthModeResolver.Resolve(request.CategoryCode, _authModeOptions.Value);

            // ── Escrow: gross = post-discount CustomerTotal, split = ProviderNet, fed the sub-merchant key (BE-I1) ──
            var gateway = _gatewayResolver.Resolve();
            var context = TransactionContext.ForServiceRequest(request.ServiceRequestId, request.OfferId);
            var init = await gateway.InitiateCheckoutAsync(new CheckoutInitInput
            {
                TransactionId          = 0,
                IdempotencyKey         = request.IdempotencyKey,
                GrossAmount            = core.CustomerTotalAmount,
                CurrencyCode           = request.CurrencyCode,
                PayerProfileId         = request.CustomerProfileId,
                RecipientProfileId     = request.ProviderProfileId,
                ProviderNetAmount      = core.ProviderNetTotal,
                SubMerchantKey         = eligibility.SubMerchantKey,          // BE-I1 verified split-eligible
                ExpectedRetainedAmount = snapshot.PlatformGrossShareSnapshot, // BE-P9 pre-send guard cross-check
                Context                = context,
                EscrowRequired         = true,
                Description            = $"Service payment — {request.IdempotencyKey}",
            }, ct);

            if (!init.IsSuccess)
                throw new AizenBusinessException((int)PaymentErrorCode.GatewayInitiationFailed);

            var transactionCode = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24];
            transaction[0] = PaymentTransactionEntity.Create(
                transactionCode:        transactionCode,
                transactionType:        TransactionType.ServiceRequestEscrow,
                contextType:            TransactionContextType.ServiceRequest,
                contextId:              request.ServiceRequestId,
                contextSubId:           request.OfferId,
                payerProfileId:         request.CustomerProfileId,
                recipientProfileId:     request.ProviderProfileId,
                grossAmount:            core.CustomerTotalAmount,         // customer is charged the post-discount CustomerTotal
                commissionAmount:       core.TransactionCommission,
                commissionRateSnapshot: snapshot.CommissionRateSnapshot,  // reporting-only effective rate
                vatOnCommission:        0m,                               // pre-tax commission (YMM open)
                netPayoutAmount:        core.ProviderNetTotal,            // provider split target
                discountAmount:         core.TotalCustomerDiscount,       // BE-P8b: the applied customer discount
                currencyCode:           request.CurrencyCode,
                gatewayProvider:        gateway.ProviderKey,
                idempotencyKey:         request.IdempotencyKey,
                escrowRequired:         true,
                authMode:               authMode);                          // BE-P9 auth-mode snapshot

            transaction[0].Capture(init.GatewayReference);
            transaction[0].LinkEconomicsSnapshot(snapshot.Id);   // BE-P1 settable-once FK
            await _transactions.AddAsync(transaction[0], ct);

            // ── P2 MarkApplied — the acceptance moment; once per applied rule (self-saves) ──
            foreach (var ruleId in result.AppliedRuleIds)
                await _commissionRules.MarkAppliedAsync(ruleId, ct);
        }
        catch
        {
            // ── Release the reservations on any escrow/persistence failure (§19.7) — never leave a benefit reserved. ──
            if (budgetReservation is not null) await _budgetService.ReleaseAsync(budgetReservation.Id, ct);
            if (entitlementUsage is not null)  await _entitlementService.ReleaseAsync(entitlementUsage.Id, ct);
            throw;
        }

        // ── Consume the reservations on the successful acceptance (§19.7). Idempotent. ──
        if (budgetReservation is not null) await _budgetService.ConsumeAsync(budgetReservation.Id, ct);
        if (entitlementUsage is not null)  await _entitlementService.ConsumeAsync(entitlementUsage.Id, ct);

        // ── BE-P12: post the reporting ledger from the immutable snapshot (additive; derived, never recomputed). ──
        await _ledgerPosting.PostAcceptanceAsync(transaction[0], snapshot, ct);

        _logger.LogInformation(
            "P8 economics approved. SR={SrId} Offer={OfferId} Txn={TxnCode} Snapshot={SnapId} " +
            "CustomerTotal={Total} ProviderNet={Net} Commission={Commission} CustomerDiscount={Discount} Adjusted={Adj}",
            request.ServiceRequestId, request.OfferId, transaction[0].TransactionCode, snapshot.Id,
            core.CustomerTotalAmount, core.ProviderNetTotal, core.TransactionCommission,
            core.TotalCustomerDiscount, core.DiscountAdjusted);

        return new CalculateServiceRequestEconomicsRemoteCallResponse
        {
            Decision               = core.Decision,
            Reason                 = core.Reason,
            TransactionId          = transaction[0].Id,
            EconomicsSnapshotId    = snapshot.Id,
            ResolvedProviderPlanId = result.ResolvedProviderPlanId,
            CustomerTotalAmount    = core.CustomerTotalAmount,
            ProviderNetTotal       = core.ProviderNetTotal,
            PlatformFeeGross       = core.PlatformFeeGross,
            TransactionCommission  = core.TransactionCommission,
        };
    }
}
