
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Validate file ownership remote call response", "Response model for the FileStorage ValidateFileOwnership remote call.")]
public sealed class ValidateFileOwnershipRemoteCallResponse
{
    public Guid FileId { get; set; }
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
}
