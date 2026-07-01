using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ApproveManualPayout;

[DocumentationInfo("ApproveManualPayoutCommandHandler",
    "Admin manually approves an OnHold payout. Transitions status to Completed, " +
    "records GatewayPayoutId + ProcessedAt timestamp, and publishes PayoutCompletedMessage. " +
    "Rejects if payout is not in OnHold state.")]
public sealed class ApproveManualPayoutCommandHandler
    : AizenCommandHandler<ApproveManualPayoutCommand, ApproveManualPayoutResult>
{
    private readonly IPayoutRecordRepository                       _payouts;
    private readonly IAizenMessagePublisher                        _publisher;
    private readonly ILogger<ApproveManualPayoutCommandHandler>    _logger;

    public ApproveManualPayoutCommandHandler(
        IPayoutRecordRepository                     payouts,
        IAizenMessagePublisher                      publisher,
        ILogger<ApproveManualPayoutCommandHandler>  logger)
    {
        _payouts   = payouts;
        _publisher = publisher;
        _logger    = logger;
    }

    public override async Task<ApproveManualPayoutResult?> Handle(
        ApproveManualPayoutCommand request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(request.PayoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        if (payout.Status != PayoutStatus.OnHold)
            throw new AizenBusinessException((int)PaymentErrorCode.PayoutInvalidStateForApproval);

        payout.ApproveManualPayout(request.GatewayPayoutId, request.AdminNote);
        _payouts.Update(payout);
        // SaveChanges handled by AizenCommandHandlerDecorator.

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
                    "Failed to publish PayoutCompletedMessage after manual approval. PayoutId={Id}", payout.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Payout manually approved. PayoutId={Id} GatewayRef={Ref}",
            payout.Id, request.GatewayPayoutId);

        return new ApproveManualPayoutResult(
            payout.Id,
            request.GatewayPayoutId,
            payout.ProcessedAt!.Value);
    }
}
