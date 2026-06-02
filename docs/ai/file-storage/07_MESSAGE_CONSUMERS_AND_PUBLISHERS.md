# 07 — Message Consumers and Publishers

Implement FileStorage MessageBus integration.

## Message location

All messages and message results must be under:

```text
Aizen.Modules.FileStorage.Abstraction/Message
```

## Consumer location

All consumers must be under:

```text
Aizen.Modules.FileStorage.Api/Consumers
```

Use folder grouping similar to controllers:

```text
Consumers/Upload
Consumers/Processing
Consumers/Owner
Consumers/Cleanup
```

## Request/response consumers

Use `AizenBaseMessageConsumer<TMessage, TResult>`.

Create consumers:

```text
CreateUploadSessionProcessMessageConsumer
CompleteUploadSessionProcessMessageConsumer
ValidateFileOwnershipProcessMessageConsumer
CreateFileReadUrlProcessMessageConsumer
LinkFileToOwnerProcessMessageConsumer
DeleteFileProcessMessageConsumer
```

Each request/response consumer must:
1. Inject `IAizenCQRSProcessor` from `IServiceProvider`.
2. Implement `ExecutePrepareMessage`.
3. Implement `ExecuteCommitMessage`.
4. Implement `ExecuteRollbackMessage`.
5. Map message to Application command/query.
6. Map command/query response to message result.

Example pattern:

```csharp
public sealed class ValidateFileOwnershipProcessMessageConsumer
    : AizenBaseMessageConsumer<ValidateFileOwnershipProcessMessage, ValidateFileOwnershipProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public ValidateFileOwnershipProcessMessageConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override async Task<ValidateFileOwnershipProcessMessageResult> ExecuteCommitMessage(
        ValidateFileOwnershipProcessMessage message,
        CancellationToken cancellationToken)
    {
        var response = await _cqrsProcessor.ProcessAsync(
            new ValidateFileOwnershipQuery(
                fileId: message.FileId,
                ownerModule: message.OwnerModule,
                ownerEntityType: message.OwnerEntityType,
                ownerEntityId: message.OwnerEntityId),
            cancellationToken);

        return new ValidateFileOwnershipProcessMessageResult
        {
            FileId = response.FileId,
            IsValid = response.IsValid,
            Reason = response.Reason
        };
    }

    public override Task<bool> ExecutePrepareMessage(ValidateFileOwnershipProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteRollbackMessage(ValidateFileOwnershipProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

## Fire-and-forget consumers

Use `AizenBaseMessageConsumer<TMessage>`.

Create consumers:

```text
FileUploadedMessageConsumer
FileProcessingRequestedMessageConsumer
FileVirusScanRequestedMessageConsumer
FileThumbnailRequestedMessageConsumer
FileMetadataExtractionRequestedMessageConsumer
FileProcessingCompletedMessageConsumer
FileLinkedToOwnerMessageConsumer
OrphanFileCleanupRequestedMessageConsumer
FileDeletedMessageConsumer
```

Use fire-and-forget consumers for background jobs.

## Application publishing

In Application command handlers, inject:

```csharp
private readonly IAizenMessagePublisher _publisher;
```

Publish from:

```text
CompleteUploadSessionCommandHandler -> FileUploadedMessage
CompleteUploadSessionCommandHandler -> FileProcessingRequestedMessage if processing required
LinkFileToOwnerCommandHandler -> FileLinkedToOwnerMessage
DeleteFileCommandHandler -> FileDeletedMessage
StartFileProcessingCommandHandler -> FileProcessingRequestedMessage
```

Every message, result and consumer must include DocumentationInfo.
