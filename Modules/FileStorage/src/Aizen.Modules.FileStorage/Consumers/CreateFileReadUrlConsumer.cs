using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Commands.CreateReadUrl;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Create file read URL consumer", "Request/response consumer that generates a pre-signed S3 read URL via CreateReadUrlCommand.")]
public sealed class CreateFileReadUrlConsumer
    : AizenBaseMessageConsumer<CreateFileReadUrlProcessMessage, CreateFileReadUrlProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public CreateFileReadUrlConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CreateFileReadUrlProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<CreateFileReadUrlProcessMessageResult> ExecuteCommitMessage(
        CreateFileReadUrlProcessMessage message, CancellationToken cancellationToken)
    {
        var result = await _cqrsProcessor.ProcessAsync<FileAccessUrlDto>(
            new CreateReadUrlCommand
            {
                FileId = message.FileId,
                Request = new CreateReadUrlRequest { ExpiresIn = message.ExpiresIn }
            },
            cancellationToken);

        return new CreateFileReadUrlProcessMessageResult
        {
            FileId = message.FileId,
            ReadUrl = result?.ReadUrl ?? string.Empty,
            ExpiresAt = result?.ExpiresAt ?? DateTime.UtcNow
        };
    }

    public override Task ExecuteRollbackMessage(
        CreateFileReadUrlProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
