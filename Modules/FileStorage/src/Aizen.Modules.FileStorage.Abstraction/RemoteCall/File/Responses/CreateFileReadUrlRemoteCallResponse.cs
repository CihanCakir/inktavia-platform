
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Create file read URL remote call response", "Response model for the FileStorage CreateReadUrl remote call.")]
public sealed class CreateFileReadUrlRemoteCallResponse
{
    public Guid FileId { get; set; }
    public string ReadUrl { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
