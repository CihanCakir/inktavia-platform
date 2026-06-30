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

namespace Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;

[DocumentationInfo("Reinstate cancelled transaction command handler",
    "Restores a Cancelled transaction back to PendingIntent. Domain method throws if Status != Cancelled. Publishes PaymentCancellationReinstatedMessage.")]
public sealed class ReinstateCancelledTransactionCommandHandler
    : AizenCommandHandler<ReinstateCancelledTransactionCommand, ReinstateCancelledTransactionResult>
{
    private readonly IPaymentTransactionRepository                       _transactions;
    private readonly IAizenMessagePublisher                              _publisher;
    private readonly ILogger<ReinstateCancelledTransactionCommandHandler> _logger;

    public ReinstateCancelledTransactionCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>                    unitOfWork,
        IPaymentTransactionRepository                         transactions,
        IAizenMessagePublisher                                publisher,
        ILogger<ReinstateCancelledTransactionCommandHandler>  logger)
    {
        _transactions = transactions;
        _publisher    = publisher;
        _logger       = logger;
    }

    public override async Task<ReinstateCancelledTransactionResult?> Handle(
        ReinstateCancelledTransactionCommand request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        // Domain method throws AizenBusinessException if Status != Cancelled
        tx.ReinstateCancellation(request.AdminNote);
        _transactions.Update(tx);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

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

        return new ReinstateCancelledTransactionResult(
            tx.Id,
            tx.TransactionCode,
            tx.ReinstatedAt!.Value);
    }
}
