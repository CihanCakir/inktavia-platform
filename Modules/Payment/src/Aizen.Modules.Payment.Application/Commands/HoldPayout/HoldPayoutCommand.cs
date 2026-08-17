using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.HoldPayout;

/// <summary>Admin places a payout record on hold for review.</summary>
public sealed class HoldPayoutCommand : AizenCommand<HoldPayoutResult>
{
    public required long    PayoutRecordId { get; init; }
    public required string  HoldReason     { get; init; }
    public string?          AdminNote      { get; init; }
}
