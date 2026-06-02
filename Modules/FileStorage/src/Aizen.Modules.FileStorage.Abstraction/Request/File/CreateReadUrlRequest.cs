using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.File;

[DocumentationInfo("Create read URL request", "Requests a pre-signed S3 read URL for a file.")]
public sealed class CreateReadUrlRequest
{
    public TimeSpan ExpiresIn { get; set; } = TimeSpan.FromMinutes(15);
}
