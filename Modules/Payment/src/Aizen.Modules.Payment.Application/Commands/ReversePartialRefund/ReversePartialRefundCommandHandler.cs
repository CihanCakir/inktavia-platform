using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;

public sealed class ReversePartialRefundCommandHandler
    : AizenCommandHandler<ReversePartialRefundCommand, bool>
{
    private readonly IPaymentTransactionRepository             _transactions;
    private readonly IAizenMessagePublisher                    _publisher;
    private readonly ILogger<ReversePartialRefundCommandHandler> _logger;

    public ReversePartialRefundCommandHandler(
        IPaymentTransactionRepository transactions,
        IAizenMessagePublisher publisher,
        ILogger<ReversePartialRefundCommandHandler> logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<bool> Handle(ReversePartialRefundCommand request, CancellationToken ct)
    {
        // Fetch the refund record first to get the parent transaction ID
        var refundRecord = await _transactions.GetRefundRecordByIdAsync(request.RefundRecordId, ct)
            ?? throw new InvalidOperationException(
                $"RefundRecord {request.RefundRecordId} not found.");

        // Load the parent transaction with full refund records for domain method guards
        var tx = await _transactions.GetByIdWithRefundsAsync(refundRecord.PaymentTransactionId, ct)
            ?? throw new InvalidOperationException(
                $"Transaction {refundRecord.PaymentTransactionId} not found.");

        var reversedAmount = refundRecord.Amount;

        // Domain method: marks record as Reversed, subtracts from TotalRefundedAmount, recalculates Status
        // Throws if record.Status != Processed
        tx.ReverseRefund(refundRecord, request.ReversalReason, request.AdminNote);

        _transactions.Update(tx);
        await _transactions.SaveChangesAsync(ct);

        _ = _publisher.PublishAsync(new PartialRefundReversedMessage
        {
            TransactionId   = tx.Id,
            TransactionCode = tx.TransactionCode,
            RefundRecordId  = refundRecord.Id,
            RefundCode      = refundRecord.RefundCode,
            ContextType     = tx.ContextType,
            ContextId       = tx.ContextId,
            PayerProfileId  = tx.PayerProfileId,
            ReversedAmount  = reversedAmount,
            CurrencyCode    = tx.CurrencyCode,
            ReversalReason  = request.ReversalReason,
            ReversedAtUtc   = refundRecord.ReversedAt!.Value,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PartialRefundReversedMessage for RefundRecord {Id}", refundRecord.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Refund reversed. RefundRecordId={RId} Amount={Amount} TransactionId={TxId} NewTxStatus={Status}",
            refundRecord.Id, reversedAmount, tx.Id, tx.Status);

        return true;
    }
}
