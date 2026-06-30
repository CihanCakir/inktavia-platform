using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;

[DocumentationInfo("Create payment escrow command handler",
    "Initiates a gateway checkout and persists a PendingIntent transaction. Idempotent via IdempotencyKey.")]
public sealed class CreatePaymentEscrowCommandHandler
    : AizenCommandHandler<CreatePaymentEscrowCommand, CreatePaymentEscrowResult>
{
    private readonly IPaymentTransactionRepository             _transactions;
    private readonly CommissionCalculationService              _commission;
    private readonly PaymentGatewayResolver                    _gatewayResolver;
    private readonly ILogger<CreatePaymentEscrowCommandHandler> _logger;

    public CreatePaymentEscrowCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>              unitOfWork,
        IPaymentTransactionRepository                   transactions,
        CommissionCalculationService                    commission,
        PaymentGatewayResolver                         gatewayResolver,
        ILogger<CreatePaymentEscrowCommandHandler>      logger)
    {
        _transactions    = transactions;
        _commission      = commission;
        _gatewayResolver = gatewayResolver;
        _logger          = logger;
    }

    public override async Task<CreatePaymentEscrowResult?> Handle(
        CreatePaymentEscrowCommand request, CancellationToken ct)
    {
        // Idempotency guard — return existing result if already processed
        var existing = await _transactions.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Duplicate escrow creation blocked. IdempotencyKey={Key}", request.IdempotencyKey);
            return new CreatePaymentEscrowResult(
                existing.Id, existing.TransactionCode, existing.GatewayReference ?? string.Empty,
                existing.CommissionRateSnapshot, existing.CommissionAmount, existing.NetPayoutAmount);
        }

        // Calculate commission (data-driven, no hardcoded rates)
        // Precedence: ProviderOverride → ProviderPlan → Category → GlobalDefault
        // VatRate comes from COMMISSION_VAT_RATE system parameter (not hardcoded)
        var breakdown = await _commission.CalculateAsync(
            grossAmount:       request.GrossAmount,
            discountAmount:    request.DiscountAmount,
            providerProfileId: request.RecipientProfileId,
            providerPlanId:    request.ProviderPlanId,
            categoryCode:      request.CategoryCode,
            ct:                ct);

        // Generate transaction code before gateway call (used as idempotency reference)
        var transactionCode = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24];

        // Initiate with gateway
        var gateway = _gatewayResolver.Resolve();
        var initResult = await gateway.InitiateCheckoutAsync(new CheckoutInitInput
        {
            TransactionId      = 0, // placeholder — real ID set after Save; gateway uses IdempotencyKey
            IdempotencyKey     = request.IdempotencyKey,
            GrossAmount        = request.GrossAmount,
            CurrencyCode       = request.CurrencyCode,
            PayerProfileId     = request.PayerProfileId,
            RecipientProfileId = request.RecipientProfileId,
            ProviderNetAmount  = breakdown.NetPayoutAmount,
            Context            = request.Context,
            EscrowRequired     = request.EscrowRequired,
            Description        = $"Service payment — {request.IdempotencyKey}",
        }, ct);

        if (!initResult.IsSuccess)
            throw new AizenBusinessException((int)PaymentErrorCode.GatewayInitiationFailed);

        var transaction = PaymentTransactionEntity.Create(
            transactionCode:        transactionCode,
            transactionType:        request.TransactionType,
            contextType:            request.Context.Type,
            contextId:              request.Context.GeneralId,
            contextSubId:           request.Context.SubId,
            payerProfileId:         request.PayerProfileId,
            recipientProfileId:     request.RecipientProfileId,
            grossAmount:            breakdown.GrossAmount,
            commissionAmount:       breakdown.CommissionAmount,
            commissionRateSnapshot: breakdown.CommissionRate,
            vatOnCommission:        breakdown.VatOnCommission,
            netPayoutAmount:        breakdown.NetPayoutAmount,
            discountAmount:         breakdown.DiscountAmount,
            currencyCode:           request.CurrencyCode,
            gatewayProvider:        gateway.ProviderKey,
            idempotencyKey:         request.IdempotencyKey,
            escrowRequired:         request.EscrowRequired);

        // Capture gateway reference (for Iyzico: checkoutToken; for manual: MANUAL-{key})
        transaction.Capture(initResult.GatewayReference);

        await _transactions.AddAsync(transaction, ct);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        _logger.LogInformation(
            "Escrow created. TransactionCode={Code} GrossAmount={Amount} Commission={Commission} Net={Net}",
            transaction.TransactionCode, transaction.GrossAmount,
            transaction.CommissionAmount, transaction.NetPayoutAmount);

        return new CreatePaymentEscrowResult(
            transaction.Id,
            transaction.TransactionCode,
            initResult.GatewayReference,
            breakdown.CommissionRate,
            breakdown.CommissionAmount,
            breakdown.NetPayoutAmount);
    }
}
