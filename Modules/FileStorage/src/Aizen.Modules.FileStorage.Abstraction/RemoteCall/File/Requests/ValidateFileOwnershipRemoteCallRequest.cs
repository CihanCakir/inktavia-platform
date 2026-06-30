
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Validate file ownership remote call request", "Request model for the FileStorage ValidateFileOwnership remote call.")]
public sealed class ValidateFileOwnershipRemoteCallRequest
{
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}
