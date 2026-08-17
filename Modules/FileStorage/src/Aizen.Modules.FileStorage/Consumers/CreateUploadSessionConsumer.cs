using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Create upload session consumer", "Request/response consumer that creates an S3 pre-signed upload session via CQRS.")]
public sealed class CreateUploadSessionConsumer
    : AizenBaseMessageConsumer<CreateUploadSessionProcessMessage, CreateUploadSessionProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public CreateUploadSessionConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CreateUploadSessionProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override async Task<CreateUploadSessionProcessMessageResult> ExecuteCommitMessage(
        CreateUploadSessionProcessMessage message, CancellationToken cancellationToken)
    {
        var result = await _cqrsProcessor.ProcessAsync<Abstraction.Dto.UploadSession.FileUploadSessionDto>(
            new CreateUploadSessionCommand
            {
                Request = new CreateUploadSessionRequest
                {
                    OriginalFileName = message.OriginalFileName,
                    ContentType = message.ContentType,
                    SizeInBytes = message.SizeInBytes,
                    Category = message.Category,
                    Visibility = message.Visibility,
                    OwnerModule = message.OwnerModule,
                    OwnerEntityType = message.OwnerEntityType,
                    OwnerEntityId = message.OwnerEntityId
                }
            }, cancellationToken);

        return new CreateUploadSessionProcessMessageResult
        {
            FileId = result?.FileId ?? Guid.Empty,
            UploadSessionCode = result?.UploadSessionCode ?? string.Empty,
            UploadUrl = result?.UploadUrl ?? string.Empty,
            ObjectKey = result?.ObjectKey ?? string.Empty,
            ExpiresAt = result?.ExpiresAt ?? DateTime.UtcNow
        };
    }

    public override Task ExecuteRollbackMessage(
        CreateUploadSessionProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
