
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Link file to owner remote call response", "Response model for the FileStorage LinkFileToOwner remote call.")]
public sealed class LinkFileToOwnerRemoteCallResponse
{
    public Guid FileId { get; set; }
    public bool IsLinked { get; set; }
}
