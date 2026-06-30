using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Application.Queries.ValidateFileOwnership;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Validate file ownership consumer", "Request/response consumer that validates file ownership for an external module via CQRS.")]
public sealed class ValidateFileOwnershipConsumer
    : AizenBaseMessageConsumer<ValidateFileOwnershipProcessMessage, ValidateFileOwnershipProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;
    private readonly IFileRepository _fileRepository;

    public ValidateFileOwnershipConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
        _fileRepository = serviceProvider.GetRequiredService<IFileRepository>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ValidateFileOwnershipProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<ValidateFileOwnershipProcessMessageResult> ExecuteCommitMessage(
        ValidateFileOwnershipProcessMessage message, CancellationToken cancellationToken)
    {
        var fileEntity = await _fileRepository.GetByGuidAsync(message.FileId, cancellationToken);
        if (fileEntity is null)
        {
            return new ValidateFileOwnershipProcessMessageResult
            {
                FileId = message.FileId,
                IsValid = false,
                Reason = "File not found."
            };
        }

        var result = await _cqrsProcessor.ProcessAsync<FileValidationResultDto>(
            new ValidateFileOwnershipQuery(fileEntity.Id, new ValidateFileOwnershipRequest
            {
                OwnerModule = message.OwnerModule,
                OwnerEntityType = message.OwnerEntityType,
                OwnerEntityId = message.OwnerEntityId
            }), cancellationToken);

        return new ValidateFileOwnershipProcessMessageResult
        {
            FileId = message.FileId,
            IsValid = result?.IsValid ?? false,
            Reason = result?.Reason
        };
    }

    public override Task ExecuteRollbackMessage(
        ValidateFileOwnershipProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
