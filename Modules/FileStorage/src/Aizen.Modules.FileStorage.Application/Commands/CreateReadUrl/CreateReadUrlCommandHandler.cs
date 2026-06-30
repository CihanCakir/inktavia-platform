using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateReadUrl;

[DocumentationInfo("Create read URL command handler", "Delegates to IFileAccessService to generate a pre-signed S3 read URL.")]
public sealed class CreateReadUrlCommandHandler : AizenCommandHandler<CreateReadUrlCommand, FileAccessUrlDto>
{
    private readonly IFileAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public CreateReadUrlCommandHandler(IFileAccessService accessService, IAizenInfoAccessor info)
    {
        _accessService = accessService;
        _info = info;
    }

    public override async Task<FileAccessUrlDto?> Handle(CreateReadUrlCommand command, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var expiresIn = command.Request?.ExpiresIn ?? TimeSpan.FromMinutes(15);
        return await _accessService.CreateReadUrlAsync(command.FileId, expiresIn, userId, cancellationToken);
    }
}
