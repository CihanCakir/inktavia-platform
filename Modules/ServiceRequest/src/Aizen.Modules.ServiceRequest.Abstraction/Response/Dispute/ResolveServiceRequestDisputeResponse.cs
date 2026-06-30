
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

[DocumentationInfo("Resolve dispute response", "Response after admin resolves a dispute.")]
public sealed class ResolveServiceRequestDisputeResponse(long disputeId)
{
    public long DisputeId { get; } = disputeId;
    public bool Resolved { get; } = true;
}
