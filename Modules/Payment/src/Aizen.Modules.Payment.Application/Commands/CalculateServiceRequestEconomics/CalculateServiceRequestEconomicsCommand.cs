using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Application.Commands.CalculateServiceRequestEconomics;

/// <summary>
/// BE-P8 — calculate acceptance economics and, on an approving decision, create the escrow + immutable snapshot in one
/// idempotent operation. Wraps the remote-call request; returns the remote-call response.
/// </summary>
public sealed class CalculateServiceRequestEconomicsCommand
    : AizenCommand<CalculateServiceRequestEconomicsRemoteCallResponse>
{
    public required CalculateServiceRequestEconomicsRemoteCallRequest Request { get; init; }
}
