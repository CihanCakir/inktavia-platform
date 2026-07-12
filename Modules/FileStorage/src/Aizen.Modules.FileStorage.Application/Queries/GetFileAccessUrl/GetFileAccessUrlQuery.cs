using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;

[DocumentationInfo("Get file access URL query", "Returns a pre-signed S3 read URL for a given file ID.")]
public sealed class GetFileAccessUrlQuery : AizenQuery<FileAccessUrlDto>
{
    public Guid FileId { get; }
    public TimeSpan ExpiresIn { get; }

    public GetFileAccessUrlQuery(Guid fileId, TimeSpan? expiresIn = null)
    {
        FileId = fileId;
        ExpiresIn = expiresIn ?? TimeSpan.FromMinutes(15);
    }
}
