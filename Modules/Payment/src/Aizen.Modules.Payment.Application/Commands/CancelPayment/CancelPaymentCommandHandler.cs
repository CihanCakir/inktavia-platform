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

namespace Aizen.Modules.Payment.Application.Commands.CancelPayment;

[DocumentationInfo("Cancel payment command handler",
    "Voids a PendingIntent transaction before any gateway capture. Publishes PaymentCancelledMessage so the SR module can react.")]
public sealed class CancelPaymentCommandHandler
    : AizenCommandHandler<CancelPaymentCommand, CancelPaymentResult>
{
    private readonly IPaymentTransactionRepository         _transactions;
    private readonly IAizenMessagePublisher                _publisher;
    private readonly ILogger<CancelPaymentCommandHandler> _logger;

    public CancelPaymentCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>    unitOfWork,
        IPaymentTransactionRepository         transactions,
        IAizenMessagePublisher                publisher,
        ILogger<CancelPaymentCommandHandler>  logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<CancelPaymentResult?> Handle(
        CancelPaymentCommand request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        // Idempotency — already cancelled: return without re-modifying
        if (tx.CancelledAt.HasValue)
        {
            _logger.LogInformation(
                "CancelPayment: transaction {Id} already cancelled at {At}. Idempotent return.",
                tx.Id, tx.CancelledAt);
            return new CancelPaymentResult(
                tx.Id,
                tx.TransactionCode,
                tx.CancelledAt!.Value,
                WasAlreadyCancelled: true);
        }

        tx.Cancel(request.CancellationReason, request.AdminNote);
        _transactions.Update(tx);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        // Fire-and-forget: SR module reacts (AwaitingPayment or Cancelled based on reason)
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
            "Transaction cancelled. Id={Id} Reason={Reason}", tx.Id, request.CancellationReason);

        return new CancelPaymentResult(
            tx.Id,
            tx.TransactionCode,
            tx.CancelledAt!.Value,
            WasAlreadyCancelled: false);
    }
}
