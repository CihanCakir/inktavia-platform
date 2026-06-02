using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Create file read URL consumer", "Request/response consumer that generates a pre-signed S3 read URL via CQRS.")]
public sealed class CreateFileReadUrlConsumer
    : AizenBaseMessageConsumer<CreateFileReadUrlProcessMessage, CreateFileReadUrlProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;
    private readonly IFileRepository _fileRepository;

    public CreateFileReadUrlConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
        _fileRepository = serviceProvider.GetRequiredService<IFileRepository>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CreateFileReadUrlProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<CreateFileReadUrlProcessMessageResult> ExecuteCommitMessage(
        CreateFileReadUrlProcessMessage message, CancellationToken cancellationToken)
    {
        var fileEntity = await _fileRepository.GetByGuidAsync(message.FileId, cancellationToken);
        if (fileEntity is null)
        {
            return new CreateFileReadUrlProcessMessageResult
            {
                FileId = message.FileId,
                ReadUrl = string.Empty,
                ExpiresAt = DateTime.UtcNow
            };
        }

        var result = await _cqrsProcessor.ProcessAsync<FileAccessUrlDto>(
            new GetFileAccessUrlQuery(fileEntity.Id, expiresIn: message.ExpiresIn),
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
