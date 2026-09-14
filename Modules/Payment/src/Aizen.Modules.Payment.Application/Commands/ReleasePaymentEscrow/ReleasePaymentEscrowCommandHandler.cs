using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;

[DocumentationInfo("Release payment escrow command handler",
    "Calls the gateway to release escrowed funds to the provider and creates a PayoutRecord. Commission is frozen at creation — never recalculated.")]
public sealed class ReleasePaymentEscrowCommandHandler
    : AizenCommandHandler<ReleasePaymentEscrowCommand, ReleasePaymentEscrowResult>
{
    private readonly IPaymentTransactionRepository             _transactions;
    private readonly IPayoutRecordRepository                   _payouts;
    private readonly PaymentGatewayResolver                    _gatewayResolver;
    private readonly ILogger<ReleasePaymentEscrowCommandHandler> _logger;

    public ReleasePaymentEscrowCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>               unitOfWork,
        IPaymentTransactionRepository                    transactions,
        IPayoutRecordRepository                          payouts,
        PaymentGatewayResolver                           gatewayResolver,
        ILogger<ReleasePaymentEscrowCommandHandler>      logger)
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
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        // ── PrincipalSale / platform-collected (CargoDry supply): no provider split. The whole gross was already
        //    captured to the platform at accept (RecipientProfileId=null, NetPayout=0). There is no Iyzico sub-merchant
        //    to approve and NO PayoutRecord to create — "release" is purely the escrow-hold → released state transition
        //    (finalises the hold so a post-activation refund is no longer expected). The provider is paid ONLY via the
        //    CargoDry sell-through settlement. Routing this through the marketplace release below would throw
        //    (missing GatewayItemTransactionId), so branch first. ──
        if (tx.RecipientProfileId is null || tx.NetPayoutAmount == 0m)
        {
            tx.Release();
            _transactions.Update(tx);
            _logger.LogInformation(
                "Platform-collected escrow finalised (no provider payout). TransactionId={TxId} Type={Type} Gross={Gross}",
                tx.Id, tx.TransactionType, tx.GrossAmount);
            return new ReleasePaymentEscrowResult(0, string.Empty, 0m);
        }

        // Commission was frozen at creation — use snapshot, never recalculate.
        var gateway = _gatewayResolver.Resolve();
        var payoutResult = await gateway.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId            = tx.Id,
            GatewayReference         = tx.GatewayReference ?? string.Empty,
            GatewayItemTransactionId = tx.GatewayItemTransactionId,   // BE-P9-fix §4: item approve target
            ProviderNetAmount        = tx.NetPayoutAmount,
            AdminNote                = request.AdminNote,
        }, ct);

        if (!payoutResult.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.EscrowReleaseInvalidState);

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
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        _logger.LogInformation(
            "Escrow released. TransactionId={TxId} PayoutId={PayoutId} Net={Net}",
            tx.Id, payout.Id, tx.NetPayoutAmount);

        return new ReleasePaymentEscrowResult(
            payout.Id,
            payoutResult.GatewayPayoutId ?? string.Empty,
            tx.NetPayoutAmount);
    }
}
