using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;

/// <summary>Admin confirms physical bank transfer completed.</summary>
public sealed class MarkPayoutCompleteCommand : AizenCommand<MarkPayoutCompleteResult>
{
    public required long   PayoutRecordId   { get; init; }
    public required string GatewayPayoutId  { get; init; }
    public string?         AdminNote        { get; init; }
}
