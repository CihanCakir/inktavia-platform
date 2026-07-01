using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.HoldPayout;

[DocumentationInfo("HoldPayoutCommandHandler",
    "Admin places a Pending or Processing payout record on hold for compliance review. " +
    "Transitions payout status to OnHold, records HoldReason + HeldAt timestamp, " +
    "and optionally attaches an AdminNote. Rejects if payout is already OnHold, Completed, or Cancelled.")]
public sealed class HoldPayoutCommandHandler
    : AizenCommandHandler<HoldPayoutCommand, HoldPayoutResult>
{
    private readonly IPayoutRecordRepository               _payouts;
    private readonly ILogger<HoldPayoutCommandHandler>     _logger;

    public HoldPayoutCommandHandler(
        IPayoutRecordRepository            payouts,
        ILogger<HoldPayoutCommandHandler>  logger)
    {
        _payouts = payouts;
        _logger  = logger;
    }

    public override async Task<HoldPayoutResult?> Handle(
        HoldPayoutCommand request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(request.PayoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        if (payout.Status is not (PayoutStatus.Pending or PayoutStatus.Processing))
            throw new AizenBusinessException((int)PaymentErrorCode.PayoutInvalidStateForHold);

        payout.Hold(request.HoldReason, request.AdminNote);
        _payouts.Update(payout);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Payout placed on hold. PayoutId={Id} Reason={Reason}",
            payout.Id, request.HoldReason);

        return new HoldPayoutResult(
            payout.Id,
            request.HoldReason,
            payout.HeldAt!.Value);
    }
}
