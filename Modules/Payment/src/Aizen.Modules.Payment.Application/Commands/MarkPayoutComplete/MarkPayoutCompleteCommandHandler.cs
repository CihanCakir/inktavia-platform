using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;

[DocumentationInfo("Mark payout complete command handler",
    "Admin confirms physical bank transfer completed for a pending payout record. Publishes PayoutCompletedMessage.")]
public sealed class MarkPayoutCompleteCommandHandler
    : AizenCommandHandler<MarkPayoutCompleteCommand, MarkPayoutCompleteResult>
{
    private readonly IPayoutRecordRepository                    _payouts;
    private readonly ProviderNegativeBalanceService             _negativeBalance;
    private readonly IAizenMessagePublisher                     _publisher;
    private readonly ILogger<MarkPayoutCompleteCommandHandler>  _logger;

    public MarkPayoutCompleteCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>              unitOfWork,
        IPayoutRecordRepository                         payouts,
        ProviderNegativeBalanceService                 negativeBalance,
        IAizenMessagePublisher                          publisher,
        ILogger<MarkPayoutCompleteCommandHandler>       logger)
    {
        _payouts         = payouts;
        _negativeBalance = negativeBalance;
        _publisher       = publisher;
        _logger          = logger;
    }

    public override async Task<MarkPayoutCompleteResult?> Handle(
        MarkPayoutCompleteCommand request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(request.PayoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        // ── BE-P10 §7.3/§7.4: offset the provider's negative balance from this payout first; over-limit blocks it.
        await _negativeBalance.OffsetBeforePayoutAsync(
            payout.ProviderProfileId, payout.CurrencyCode, payout.Amount, allowOverLimit: false, ct);

        payout.MarkCompleted(request.GatewayPayoutId, request.AdminNote);
        _payouts.Update(payout);
        // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

        _ = _publisher.PublishAsync(new PayoutCompletedMessage
        {
            PayoutRecordId    = payout.Id,
            GatewayPayoutId   = request.GatewayPayoutId,
            ProviderProfileId = payout.ProviderProfileId,
            Amount            = payout.Amount,
            CurrencyCode      = payout.CurrencyCode,
            ProcessedAtUtc    = payout.ProcessedAt!.Value,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PayoutCompletedMessage for PayoutRecord {Id}", payout.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Payout marked complete. PayoutId={Id} GatewayRef={Ref}",
            payout.Id, request.GatewayPayoutId);

        return new MarkPayoutCompleteResult(
            payout.Id,
            request.GatewayPayoutId,
            payout.ProcessedAt!.Value);
    }
}
