using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;

[DocumentationInfo("Get file access URL query handler", "Delegates to IFileAccessService to generate the pre-signed read URL.")]
public sealed class GetFileAccessUrlQueryHandler : AizenQueryHandler<GetFileAccessUrlQuery, FileAccessUrlDto>
{
    private readonly IFileAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public GetFileAccessUrlQueryHandler(IFileAccessService accessService, IAizenInfoAccessor info)
    {
        _accessService = accessService;
        _info = info;
    }

    public override async Task<FileAccessUrlDto> Handle(GetFileAccessUrlQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        return await _accessService.CreateReadUrlAsync(request.FileId, request.ExpiresIn, userId, cancellationToken);
    }
}
