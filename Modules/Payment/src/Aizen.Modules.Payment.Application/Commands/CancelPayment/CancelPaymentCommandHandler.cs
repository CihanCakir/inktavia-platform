using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CancelPayment;

public sealed class CancelPaymentCommandHandler
    : AizenCommandHandler<CancelPaymentCommand, bool>
{
    private readonly IPaymentTransactionRepository      _transactions;
    private readonly IAizenMessagePublisher             _publisher;
    private readonly ILogger<CancelPaymentCommandHandler> _logger;

    public CancelPaymentCommandHandler(
        IPaymentTransactionRepository transactions,
        IAizenMessagePublisher publisher,
        ILogger<CancelPaymentCommandHandler> logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<bool> Handle(CancelPaymentCommand request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {request.TransactionId} not found.");

        // Idempotency: already cancelled — no-op
        if (tx.CancelledAt.HasValue)
        {
            _logger.LogInformation(
                "CancelPayment: transaction {Id} already cancelled at {At}. Skipping.",
                tx.Id, tx.CancelledAt);
            return true;
        }

        tx.Cancel(request.CancellationReason, request.AdminNote);
        _transactions.Update(tx);
        await _transactions.SaveChangesAsync(ct);

        // Publish event so the ServiceRequest module can react (e.g., move to AwaitingPayment)
        _ = _publisher.PublishAsync(new PaymentCancelledMessage
        {
            TransactionId      = tx.Id,
            TransactionCode    = tx.TransactionCode,
            ContextType        = tx.ContextType,
            ContextId          = tx.ContextId,
            ContextSubId       = tx.ContextSubId,
            PayerProfileId     = tx.PayerProfileId,
            GrossAmount        = tx.GrossAmount,
            CurrencyCode       = tx.CurrencyCode,
            CancellationReason = request.CancellationReason.ToString(),
            CancelledAtUtc     = tx.CancelledAt!.Value,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PaymentCancelledMessage for transaction {Id}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Transaction cancelled. Id={Id} Reason={Reason}",
            tx.Id, request.CancellationReason);

        return true;
    }
}
