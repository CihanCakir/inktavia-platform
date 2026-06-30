using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CapturePayment;

[DocumentationInfo("Capture payment command handler",
    "Transitions a PendingIntent transaction to Captured state. Idempotent — returns existing result if already captured.")]
public sealed class CapturePaymentCommandHandler
    : AizenCommandHandler<CapturePaymentCommand, CapturePaymentResult>
{
    private readonly IPaymentTransactionRepository          _transactions;
    private readonly ILogger<CapturePaymentCommandHandler> _logger;

    public CapturePaymentCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>     unitOfWork,
        IPaymentTransactionRepository          transactions,
        ILogger<CapturePaymentCommandHandler>  logger)
    {
        _transactions = transactions;
        _logger       = logger;
    }

    public override async Task<CapturePaymentResult?> Handle(
        CapturePaymentCommand request, CancellationToken ct)
    {
        var tx = request.TransactionId.HasValue
            ? await _transactions.GetByIdAsync(request.TransactionId.Value, ct)
            : await _transactions.GetByGatewayReferenceAsync(request.GatewayReference, ct);

        if (tx is null)
        {
            _logger.LogWarning(
                "CapturePayment: transaction not found. TransactionId={Id} GatewayRef={Ref}",
                request.TransactionId, request.GatewayReference);
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);
        }

        // Idempotency — duplicate webhook: return existing state without re-modifying
        if (tx.CapturedAt.HasValue)
        {
            _logger.LogInformation(
                "CapturePayment: transaction {Id} already captured at {At}. Idempotent return.",
                tx.Id, tx.CapturedAt);
            return new CapturePaymentResult(
                tx.Id,
                tx.TransactionCode,
                tx.GatewayReference ?? request.GatewayReference,
                WasAlreadyCaptured: true);
        }

        tx.Capture(request.GatewayReference);
        _transactions.Update(tx);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        _logger.LogInformation(
            "Payment captured. TransactionId={Id} GatewayRef={Ref} Amount={Amount} {Currency}",
            tx.Id, request.GatewayReference, request.PaidAmount, request.CurrencyCode);

        return new CapturePaymentResult(
            tx.Id,
            tx.TransactionCode,
            request.GatewayReference,
            WasAlreadyCaptured: false);
    }
}
