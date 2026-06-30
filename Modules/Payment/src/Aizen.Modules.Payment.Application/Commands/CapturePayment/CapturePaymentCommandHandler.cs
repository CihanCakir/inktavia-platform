using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CapturePayment;

public sealed class CapturePaymentCommandHandler
    : AizenCommandHandler<CapturePaymentCommand, bool>
{
    private readonly IPaymentTransactionRepository _transactions;
    private readonly ILogger<CapturePaymentCommandHandler> _logger;

    public CapturePaymentCommandHandler(
        IPaymentTransactionRepository transactions,
        ILogger<CapturePaymentCommandHandler> logger)
    {
        _transactions = transactions;
        _logger       = logger;
    }

    public override async Task<bool> Handle(CapturePaymentCommand request, CancellationToken ct)
    {
        // Resolve by explicit ID when available; fall back to gateway reference (webhook flow).
        var tx = request.TransactionId.HasValue
            ? await _transactions.GetByIdAsync(request.TransactionId.Value, ct)
            : await _transactions.GetByGatewayReferenceAsync(request.GatewayReference, ct);

        if (tx is null)
        {
            _logger.LogWarning(
                "CapturePayment: transaction not found. TransactionId={Id} GatewayRef={Ref}",
                request.TransactionId, request.GatewayReference);
            return false;
        }

        // Idempotency guard — ignore duplicate webhook deliveries
        if (tx.CapturedAt.HasValue)
        {
            _logger.LogInformation(
                "CapturePayment: transaction {Id} already captured at {At}. Skipping.",
                tx.Id, tx.CapturedAt);
            return true;
        }

        tx.Capture(request.GatewayReference);
        _transactions.Update(tx);
        await _transactions.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment captured. TransactionId={Id} GatewayRef={Ref} Amount={Amount} {Currency}",
            tx.Id, request.GatewayReference, request.PaidAmount, request.CurrencyCode);

        return true;
    }
}
