using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.RecordChargeback;

[DocumentationInfo("Record chargeback command handler (BE-P10 §21.2)",
    "Records a gateway chargeback (≤ 13 months): marks the transaction disputed, runs the §7.3 release-after recovery " +
    "against the provider (clawback → negative-balance ledger), and books a distinct ChargebackExpense. Idempotent on " +
    "the gateway chargeback reference — a duplicate returns the existing record without re-clawing back.")]
public sealed class RecordChargebackCommandHandler
    : AizenCommandHandler<RecordChargebackCommand, RecordChargebackResult>
{
    private readonly IPaymentTransactionRepository        _transactions;
    private readonly IChargebackRecordRepository          _chargebacks;
    private readonly IPaymentEconomicsSnapshotRepository  _snapshots;
    private readonly IRefundAllocationPolicyRepository    _policies;
    private readonly IProviderBalanceRepository           _balances;
    private readonly FinancialLedgerPostingService        _ledgerPosting;
    private readonly IAizenMessagePublisher               _publisher;
    private readonly ILogger<RecordChargebackCommandHandler> _logger;

    public RecordChargebackCommandHandler(
        IPaymentTransactionRepository        transactions,
        IChargebackRecordRepository          chargebacks,
        IPaymentEconomicsSnapshotRepository  snapshots,
        IRefundAllocationPolicyRepository    policies,
        IProviderBalanceRepository           balances,
        FinancialLedgerPostingService        ledgerPosting,
        IAizenMessagePublisher               publisher,
        ILogger<RecordChargebackCommandHandler> logger)
    {
        _transactions = transactions;
        _chargebacks  = chargebacks;
        _snapshots    = snapshots;
        _policies     = policies;
        _balances     = balances;
        _ledgerPosting = ledgerPosting;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<RecordChargebackResult?> Handle(
        RecordChargebackCommand request, CancellationToken ct)
    {
        // ── Idempotency (§21.2): a chargeback already recorded for this gateway reference → return it, no re-clawback. ──
        var existing = await _chargebacks.GetByGatewayReferenceAsync(request.GatewayChargebackReference, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Duplicate chargeback blocked. GatewayRef={Ref}", request.GatewayChargebackReference);
            return new RecordChargebackResult(
                existing.Id, existing.PaymentTransactionId, existing.GatewayChargebackReference,
                existing.Amount, existing.ProviderRecoveredAmount, existing.RemainingNegativeBalance,
                existing.ChargebackExpenseAmount, AlreadyProcessed: true);
        }

        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        var now              = DateTime.UtcNow;
        var chargebackAmount = request.ChargebackAmount > 0m ? MoneyMath.Round(request.ChargebackAmount) : tx.GrossAmount;
        var expense          = MoneyMath.Round(request.ChargebackExpenseAmount);

        // ── §7.3 release-after recovery from the immutable snapshot (never recompute rates). Legacy transactions with no
        //    linked snapshot record the chargeback without a provider clawback. ──
        decimal providerRecovered = 0m, remainingNegative = 0m;
        if (tx.EconomicsSnapshotId is { } snapId && tx.RecipientProfileId is { } providerId)
        {
            var snapshot = await _snapshots.GetByIdAsync(snapId, ct);
            if (snapshot is not null)
            {
                var serviceAmount = MoneyMath.Round(snapshot.ServiceAmountSnapshot);
                var customerTotal = MoneyMath.Round(snapshot.CustomerTotalAmountSnapshot);
                var serviceRefund = customerTotal <= 0m || chargebackAmount >= customerTotal
                    ? serviceAmount
                    : MoneyMath.Round(serviceAmount * (chargebackAmount / customerTotal));

                var policy = await _policies.ResolveAsync(tx.CurrencyCode, now, ct);
                var (mode, fixedFee) = policy?.RuleFor(RefundCause.DisputeCustomerFavoured) ?? (PlatformFeeRefundMode.Full, (decimal?)null);

                // A chargeback recovers already-settled funds → always the release-after path.
                var allocation = RefundAllocationCalculator.Resolve(
                    snapshot, serviceRefund, RefundCause.DisputeCustomerFavoured,
                    ReleaseState.AfterProviderRelease, mode, fixedFee);

                if (allocation.ProviderNetReversalAmount > 0m)
                {
                    var balance = await _balances.GetByProviderAsync(providerId, tx.CurrencyCode, ct);
                    var isNewBalance = balance is null;
                    if (balance is null)
                    {
                        balance = ProviderBalanceEntity.Create(providerId, tx.CurrencyCode, policy?.NegativeBalanceLimit ?? 0m);
                        await _balances.AddAsync(balance, ct);
                    }
                    balance.Clawback(allocation.ProviderNetReversalAmount, ProviderBalanceMovementType.ChargebackClawback,
                        refundRecordId: null, chargebackRecordId: null, note: $"Chargeback {request.GatewayChargebackReference}", now);
                    if (!isNewBalance) _balances.Update(balance);   // a freshly-added balance is already tracked as Added

                    providerRecovered = allocation.ProviderNetReversalAmount;
                    remainingNegative = balance.NegativeAmount;
                }
            }
        }

        // ── Mark disputed (Captured/Released only — sets DisputedAt); a chargeback on an already-refunded tx just records. ──
        if (tx.Status is PaymentTransactionStatus.Captured or PaymentTransactionStatus.Released)
        {
            tx.Dispute();
            _transactions.Update(tx);
        }

        var record = ChargebackRecordEntity.Create(
            paymentTransactionId:       tx.Id,
            gatewayChargebackReference: request.GatewayChargebackReference,
            amount:                     chargebackAmount,
            currencyCode:               tx.CurrencyCode,
            chargebackExpenseAmount:    expense,
            receivedAtUtc:              now,
            notes:                      request.Notes);
        record.SetRecovery(providerRecovered, remainingNegative);
        await _chargebacks.AddAsync(record, ct);
        await _chargebacks.SaveChangesAsync(ct);   // materialise record.Id

        // ── BE-P12: post the chargeback reporting ledger (ChargebackExpense + recovery) from the record. ──
        await _ledgerPosting.PostChargebackAsync(record, tx.RecipientProfileId, ct);
        // SaveChanges for tx + balance is handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Chargeback recorded. Tx={TxId} GatewayRef={Ref} Amount={Amt} Recovered={Rec} RemainingNeg={Neg} Expense={Exp}",
            tx.Id, request.GatewayChargebackReference, chargebackAmount, providerRecovered, remainingNegative, expense);

        // ── N3-B: announce the (fresh) chargeback so Notification can alert the provider + admins. Fire-and-forget;
        //    only reached on the new-record path (the idempotent duplicate returned earlier), so no double-publish. ──
        _ = _publisher.PublishAsync(new PaymentChargebackRecordedMessage
        {
            TransactionId              = tx.Id,
            TransactionCode            = tx.TransactionCode,
            ContextType                = tx.ContextType,
            ContextId                  = tx.ContextId,
            ContextSubId               = tx.ContextSubId,
            ProviderProfileId          = tx.RecipientProfileId ?? 0,
            PayerProfileId             = tx.PayerProfileId,
            Amount                     = chargebackAmount,
            CurrencyCode               = tx.CurrencyCode,
            GatewayChargebackReference = request.GatewayChargebackReference,
            ReceivedAtUtc              = now,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to publish PaymentChargebackRecordedMessage for Tx {TxId}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        return new RecordChargebackResult(
            record.Id, tx.Id, request.GatewayChargebackReference, chargebackAmount,
            providerRecovered, remainingNegative, expense, AlreadyProcessed: false);
    }
}
