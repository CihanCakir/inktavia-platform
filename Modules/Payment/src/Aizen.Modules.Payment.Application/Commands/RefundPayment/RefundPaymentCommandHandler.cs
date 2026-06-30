using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.RefundPayment;

[DocumentationInfo("Refund payment command handler",
    "Calls the gateway to refund a captured transaction, persists a TransactionRefundRecord, and publishes PaymentRefundedMessage.")]
public sealed class RefundPaymentCommandHandler
    : AizenCommandHandler<RefundPaymentCommand, RefundPaymentResult>
{
    private readonly IPaymentTransactionRepository        _transactions;
    private readonly PaymentGatewayResolver               _gatewayResolver;
    private readonly IAizenMessagePublisher               _publisher;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>       unitOfWork,
        IPaymentTransactionRepository            transactions,
        PaymentGatewayResolver                   gatewayResolver,
        IAizenMessagePublisher                   publisher,
        ILogger<RefundPaymentCommandHandler>     logger)
    {
        _transactions    = transactions;
        _gatewayResolver = gatewayResolver;
        _publisher       = publisher;
        _logger          = logger;
    }

    public override async Task<RefundPaymentResult?> Handle(
        RefundPaymentCommand request, CancellationToken ct)
    {
        // Load transaction with existing refund records to allow ApplyRefund guard checks
        var tx = await _transactions.GetByIdWithRefundsAsync(request.TransactionId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        // ── Gateway call ──────────────────────────────────────────────────────
        var gateway = _gatewayResolver.Resolve();
        var gatewayResult = await gateway.RefundAsync(new RefundInput
        {
            TransactionId    = tx.Id,
            GatewayReference = tx.GatewayReference ?? string.Empty,
            RefundAmount     = request.RefundAmount,
            Currency         = tx.CurrencyCode,
            Reason           = request.Reason,
            AdminNote        = request.AdminNote,
        }, ct);

        if (!gatewayResult.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        // ── Create and persist refund record ──────────────────────────────────
        var refundCode = GenerateRefundCode();
        var record = TransactionRefundRecord.Create(
            paymentTransactionId: tx.Id,
            refundCode:           refundCode,
            amount:               request.RefundAmount,
            currencyCode:         tx.CurrencyCode,
            refundType:           request.RefundType,
            reason:               request.Reason,
            adminNote:            request.AdminNote);

        record.MarkProcessed(
            gatewayRefundReference: gatewayResult.GatewayRefundReference,
            adminNote:              request.AdminNote);

        await _transactions.AddRefundRecordAsync(record, ct);

        // ── Apply to parent transaction (updates TotalRefundedAmount + Status) ─
        tx.ApplyRefund(record);
        _transactions.Update(tx);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        // ── Publish event ─────────────────────────────────────────────────────
        _ = _publisher.PublishAsync(new PaymentRefundedMessage
        {
            TransactionId    = tx.Id,
            TransactionCode  = tx.TransactionCode,
            ContextType      = tx.ContextType,
            ContextId        = tx.ContextId,
            ContextSubId     = tx.ContextSubId,
            PayerProfileId   = tx.PayerProfileId,
            RefundedAmount   = request.RefundAmount,
            OriginalAmount   = tx.GrossAmount,
            CurrencyCode     = tx.CurrencyCode,
            IsPartial        = tx.TotalRefundedAmount < tx.GrossAmount,
            Reason           = request.Reason.ToString(),
            RefundedAtUtc    = DateTime.UtcNow,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PaymentRefundedMessage for transaction {Id}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Refund applied. TransactionId={Id} RefundRecordId={RId} Amount={Amount} Status={Status}",
            tx.Id, record.Id, request.RefundAmount, tx.Status);

        return new RefundPaymentResult(
            record.Id,
            record.RefundCode,
            gatewayResult.GatewayRefundReference ?? string.Empty,
            request.RefundAmount);
    }

    private static string GenerateRefundCode()
    {
        var datePart   = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"REF-{datePart}-{uniquePart}";
    }
}
