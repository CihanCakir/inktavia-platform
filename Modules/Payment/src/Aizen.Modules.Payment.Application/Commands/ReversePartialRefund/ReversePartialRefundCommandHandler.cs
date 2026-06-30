using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;

[DocumentationInfo("Reverse partial refund command handler",
    "Marks a Processed refund record as Reversed, reduces TotalRefundedAmount on the parent transaction, and publishes PartialRefundReversedMessage.")]
public sealed class ReversePartialRefundCommandHandler
    : AizenCommandHandler<ReversePartialRefundCommand, ReversePartialRefundResult>
{
    private readonly IPaymentTransactionRepository              _transactions;
    private readonly IAizenMessagePublisher                     _publisher;
    private readonly ILogger<ReversePartialRefundCommandHandler> _logger;

    public ReversePartialRefundCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>             unitOfWork,
        IPaymentTransactionRepository                  transactions,
        IAizenMessagePublisher                         publisher,
        ILogger<ReversePartialRefundCommandHandler>    logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<ReversePartialRefundResult?> Handle(
        ReversePartialRefundCommand request, CancellationToken ct)
    {
        // Fetch the refund record first to get the parent transaction ID
        var refundRecord = await _transactions.GetRefundRecordByIdAsync(request.RefundRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.RefundRecordNotFound);

        // Load the parent transaction with full refund records for domain method guards
        var tx = await _transactions.GetByIdWithRefundsAsync(refundRecord.PaymentTransactionId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        var reversedAmount = refundRecord.Amount;

        // Domain method: marks record as Reversed, subtracts from TotalRefundedAmount, recalculates Status
        // Throws AizenBusinessException if record.Status != Processed
        tx.ReverseRefund(refundRecord, request.ReversalReason, request.AdminNote);

        _transactions.Update(tx);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

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

        return new ReversePartialRefundResult(
            refundRecord.Id,
            refundRecord.RefundCode,
            reversedAmount,
            refundRecord.ReversedAt!.Value);
    }
}
