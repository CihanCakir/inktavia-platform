using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;

[DocumentationInfo("Get file access URL query handler", "Delegates to IFileAccessService to generate the pre-signed read URL.")]
public sealed class GetFileAccessUrlQueryHandler : AizenQueryHandler<GetFileAccessUrlQuery, FileAccessUrlDto>
{
    private readonly IFileAccessService _accessService;

    public GetFileAccessUrlQueryHandler(IFileAccessService accessService)
    {
        _accessService = accessService;
    }

    public override async Task<FileAccessUrlDto> Handle(GetFileAccessUrlQuery request, CancellationToken cancellationToken)
    {
        return await _accessService.CreateReadUrlAsync(request.FileId, request.ExpiresIn, request.UserId, cancellationToken);
    }
}
