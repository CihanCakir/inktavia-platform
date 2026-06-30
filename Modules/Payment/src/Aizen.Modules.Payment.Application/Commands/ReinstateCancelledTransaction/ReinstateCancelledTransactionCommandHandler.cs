using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;

public sealed class ReinstateCancelledTransactionCommandHandler
    : AizenCommandHandler<ReinstateCancelledTransactionCommand, bool>
{
    private readonly IPaymentTransactionRepository                      _transactions;
    private readonly IAizenMessagePublisher                             _publisher;
    private readonly ILogger<ReinstateCancelledTransactionCommandHandler> _logger;

    public ReinstateCancelledTransactionCommandHandler(
        IPaymentTransactionRepository transactions,
        IAizenMessagePublisher publisher,
        ILogger<ReinstateCancelledTransactionCommandHandler> logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<bool> Handle(
        ReinstateCancelledTransactionCommand request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {request.TransactionId} not found.");

        // Domain method throws if Status != Cancelled
        tx.ReinstateCancellation(request.AdminNote);
        _transactions.Update(tx);
        await _transactions.SaveChangesAsync(ct);

        _ = _publisher.PublishAsync(new PaymentCancellationReinstatedMessage
        {
            TransactionId   = tx.Id,
            TransactionCode = tx.TransactionCode,
            ContextType     = tx.ContextType,
            ContextId       = tx.ContextId,
            ContextSubId    = tx.ContextSubId,
            PayerProfileId  = tx.PayerProfileId,
            GrossAmount     = tx.GrossAmount,
            CurrencyCode    = tx.CurrencyCode,
            AdminNote       = request.AdminNote,
            ReinstatedAtUtc = tx.ReinstatedAt!.Value,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PaymentCancellationReinstatedMessage for transaction {Id}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Transaction cancellation reinstated. Id={Id} Note={Note}",
            tx.Id, request.AdminNote);

        return true;
    }
}
