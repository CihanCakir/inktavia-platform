using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;

public sealed class ReleasePaymentEscrowCommandHandler
    : AizenCommandHandler<ReleasePaymentEscrowCommand, ReleasePaymentEscrowResult>
{
    private readonly IPaymentTransactionRepository _transactions;
    private readonly IPayoutRecordRepository       _payouts;
    private readonly PaymentGatewayResolver        _gatewayResolver;
    private readonly ILogger<ReleasePaymentEscrowCommandHandler> _logger;

    public ReleasePaymentEscrowCommandHandler(
        IPaymentTransactionRepository transactions,
        IPayoutRecordRepository payouts,
        PaymentGatewayResolver gatewayResolver,
        ILogger<ReleasePaymentEscrowCommandHandler> logger)
    {
        _transactions    = transactions;
        _payouts         = payouts;
        _gatewayResolver = gatewayResolver;
        _logger          = logger;
    }

    public override async Task<ReleasePaymentEscrowResult?> Handle(
        ReleasePaymentEscrowCommand request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {request.TransactionId} not found.");

        // Commission was frozen at creation — use snapshot, never recalculate.
        var gateway = _gatewayResolver.Resolve();
        var payoutResult = await gateway.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId     = tx.Id,
            GatewayReference  = tx.GatewayReference ?? string.Empty,
            ProviderNetAmount = tx.NetPayoutAmount,
            AdminNote         = request.AdminNote,
        }, ct);

        if (!payoutResult.Processed)
            throw new InvalidOperationException(
                $"Gateway failed to release escrow for transaction {tx.Id}: {payoutResult.Note}");

        tx.Release();
        _transactions.Update(tx);

        var payout = PayoutRecordEntity.Create(
            providerProfileId:    tx.RecipientProfileId ?? 0,
            paymentTransactionId: tx.Id,
            amount:               tx.NetPayoutAmount,
            currencyCode:         tx.CurrencyCode,
            gatewayProvider:      gateway.ProviderKey
        );
        payout.MarkCompleted(payoutResult.GatewayPayoutId, request.AdminNote);
        await _payouts.AddAsync(payout, ct);
        await _transactions.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Escrow released. TransactionId={TxId} PayoutId={PayoutId} Net={Net}",
            tx.Id, payout.Id, tx.NetPayoutAmount);

        return new ReleasePaymentEscrowResult(
            payout.Id,
            payoutResult.GatewayPayoutId ?? string.Empty,
            tx.NetPayoutAmount);
    }
}
