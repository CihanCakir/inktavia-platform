using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>BE-S11b — provider proposes a post-acceptance change order. Nothing financial happens (status Proposed).</summary>
[DocumentationInfo("Propose change order command", "Provider proposes extra/removed work on an accepted SR (Proposed; no economics yet).")]
public sealed class ProposeServiceChangeOrderCommand : AizenCommand<ServiceChangeOrderDto>
{
    public long ServiceRequestId { get; }
    public ProposeServiceChangeOrderRequest Request { get; }
    public ProposeServiceChangeOrderCommand(long serviceRequestId, ProposeServiceChangeOrderRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
