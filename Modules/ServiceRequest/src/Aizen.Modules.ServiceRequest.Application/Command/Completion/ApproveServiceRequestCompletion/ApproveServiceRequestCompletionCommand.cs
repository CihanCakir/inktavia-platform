using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Approve completion command", "Owner approves a provider's completion submission (or the N3-C auto-approval job with a system actor).")]
public sealed class ApproveServiceRequestCompletionCommand : AizenCommand<ApproveServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public ApproveServiceRequestCompletionRequest Request { get; }

    /// <summary>
    /// N3-C — when set (the auto-approval job), the acting user id is this instead of the HTTP-context user, and the
    /// status-history actor is <see cref="ActorTypeOverride"/>. Null on the normal owner path (reads the JWT context).
    /// This keeps auto-approval on the exact same approval path (same escrow-release consequences) — no parallel flow.
    /// </summary>
    public long? ActingUserIdOverride { get; }
    public ServiceRequestActorType? ActorTypeOverride { get; }

    public ApproveServiceRequestCompletionCommand(
        long serviceRequestId, ApproveServiceRequestCompletionRequest request,
        long? actingUserIdOverride = null, ServiceRequestActorType? actorTypeOverride = null)
    {
        ServiceRequestId = serviceRequestId; Request = request;
        ActingUserIdOverride = actingUserIdOverride; ActorTypeOverride = actorTypeOverride;
    }
}
