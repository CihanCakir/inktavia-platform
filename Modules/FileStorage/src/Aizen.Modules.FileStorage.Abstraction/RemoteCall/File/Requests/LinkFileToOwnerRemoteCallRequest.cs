using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Link file to owner remote call request", "Request model for the FileStorage LinkFileToOwner remote call.")]
public sealed class LinkFileToOwnerRemoteCallRequest
{
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}
