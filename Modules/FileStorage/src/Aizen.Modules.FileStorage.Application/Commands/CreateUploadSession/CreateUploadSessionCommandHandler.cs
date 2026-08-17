using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;

[DocumentationInfo("Create upload session command handler", "Delegates to IFileUploadSessionService to create the upload session.")]
public sealed class CreateUploadSessionCommandHandler : AizenCommandHandler<CreateUploadSessionCommand, FileUploadSessionDto>
{
    private readonly IFileUploadSessionService _uploadSessionService;
    private readonly IAizenInfoAccessor _info;

    public CreateUploadSessionCommandHandler(IFileUploadSessionService uploadSessionService, IAizenInfoAccessor info)
    {
        _uploadSessionService = uploadSessionService;
        _info = info;
    }

    public override async Task<FileUploadSessionDto?> Handle(CreateUploadSessionCommand command, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var clientId = _info.ClientInfoAccessor.ClientInfo.AppName;
        var deviceId = _info.DeviceInfoAccessor.DeviceInfo.DeviceId;

        return await _uploadSessionService.CreateUploadSessionAsync(
            command.Request, userId, clientId, deviceId, cancellationToken);
    }
}
