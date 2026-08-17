using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ApproveManualPayout;

/// <summary>Admin manually approves an OnHold payout and records the external gateway payout ID.</summary>
public sealed class ApproveManualPayoutCommand : AizenCommand<ApproveManualPayoutResult>
{
    public required long   PayoutRecordId  { get; init; }
    public required string GatewayPayoutId { get; init; }
    public string?         AdminNote       { get; init; }
}
