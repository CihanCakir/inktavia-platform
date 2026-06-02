# 01 — Analyze Existing Architecture

Do not write code in this step.

## Inspect

Search and inspect:

```text
Aizen.Modules.Identity
Aizen.Modules.ReferenceData
Aizen.Modules.Vessel
Aizen.Modules.AuthStore
Aizen.Modules.*.Consumers
AizenBaseMessageConsumer
AizenBaseMessage
AizenMessageResult
AizenMessageError
IAizenMessagePublisher
IAizenCQRSProcessor
ReferenceDataDbContext
AizenDbContext
AizenDocumentBase
IAizenDistributedCache
IAizenQueryHandlerCacheable
DocumentationInfo
DependencyInjection.cs
Program.cs
Starter
Operation
```

## MessageBus pattern to follow

Find request/response examples like:

```csharp
public class CreateDataProcessMessageConsumer : AizenBaseMessageConsumer<CreateDataProcessMessage, CreateDataProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public CreateDataProcessMessageConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override async Task<CreateDataProcessMessageResult> ExecuteCommitMessage(CreateDataProcessMessage message, CancellationToken cancellationToken)
    {
        var response = await _cqrsProcessor.ProcessAsync(new CreateDataCommand(...), cancellationToken);
        return response.MaptoCreateResponse();
    }

    public override Task<bool> ExecutePrepareMessage(CreateDataProcessMessage message, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public override Task ExecuteRollbackMessage(CreateDataProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

Also find fire-and-forget examples:

```csharp
public class SendPushNotificationConsumer : AizenBaseMessageConsumer<SendPushNotificationMessage>
{
}
```

## Report

Produce:

```text
1. Existing module folder conventions
2. Existing MessageBus consumer conventions
3. Existing message/message result namespace conventions
4. Existing IAizenMessagePublisher usage
5. Existing IAizenCQRSProcessor usage inside consumers
6. Existing DbContext base class
7. Existing Repository DI pattern
8. Existing DocumentationInfo standard
9. Existing cache/session pattern
10. Required FileStorage implementation plan
```
