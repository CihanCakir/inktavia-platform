using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

[DocumentationInfo("Resolve dispute request", "Admin resolves an open dispute.")]
public sealed class ResolveServiceRequestDisputeRequest
{
    public string? ResolutionNotes { get; set; }
}
