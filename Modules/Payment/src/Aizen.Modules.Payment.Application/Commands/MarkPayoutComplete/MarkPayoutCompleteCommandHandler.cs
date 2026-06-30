using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;

public sealed class MarkPayoutCompleteCommandHandler
    : AizenCommandHandler<MarkPayoutCompleteCommand, bool>
{
    private readonly IPayoutRecordRepository _payouts;
    private readonly ILogger<MarkPayoutCompleteCommandHandler> _logger;

    public MarkPayoutCompleteCommandHandler(
        IPayoutRecordRepository payouts,
        ILogger<MarkPayoutCompleteCommandHandler> logger)
    {
        _payouts = payouts;
        _logger  = logger;
    }

    public override async Task<bool> Handle(MarkPayoutCompleteCommand request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(request.PayoutRecordId, ct)
            ?? throw new InvalidOperationException($"PayoutRecord {request.PayoutRecordId} not found.");

        payout.MarkCompleted(request.GatewayPayoutId);
        _payouts.Update(payout);
        await _payouts.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payout marked complete. PayoutId={Id} GatewayRef={Ref}",
            payout.Id, request.GatewayPayoutId);

        return true;
    }
}
