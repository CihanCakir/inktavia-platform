using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;

[DocumentationInfo("Get file access URL query", "Returns a pre-signed S3 read URL for a given file ID.")]
public sealed class GetFileAccessUrlQuery : AizenQuery<FileAccessUrlDto>
{
    public long FileId { get; }
    public long? UserId { get; }
    public TimeSpan ExpiresIn { get; }

    public GetFileAccessUrlQuery(long fileId, long? userId = null, TimeSpan? expiresIn = null)
    {
        FileId = fileId;
        UserId = userId;
        ExpiresIn = expiresIn ?? TimeSpan.FromMinutes(15);
    }
}
