using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;

[DocumentationInfo("Create upload session command handler", "Delegates to IFileUploadSessionService to create the upload session.")]
public sealed class CreateUploadSessionCommandHandler : AizenCommandHandler<CreateUploadSessionCommand, FileUploadSessionDto>
{
    private readonly IFileUploadSessionService _uploadSessionService;

    public CreateUploadSessionCommandHandler(IFileUploadSessionService uploadSessionService)
    {
        _uploadSessionService = uploadSessionService;
    }

    public override async Task<FileUploadSessionDto?> Handle(CreateUploadSessionCommand command, CancellationToken cancellationToken)
    {
        return await _uploadSessionService.CreateUploadSessionAsync(
            command.Request, command.UserId, command.ClientId, command.DeviceId, cancellationToken);
    }
}
