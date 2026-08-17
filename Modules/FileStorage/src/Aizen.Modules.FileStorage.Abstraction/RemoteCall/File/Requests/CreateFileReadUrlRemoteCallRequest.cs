
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Create file read URL remote call request", "Request model for the FileStorage CreateReadUrl remote call.")]
public sealed class CreateFileReadUrlRemoteCallRequest
{
    public TimeSpan ExpiresIn { get; set; } = TimeSpan.FromMinutes(15);
}
