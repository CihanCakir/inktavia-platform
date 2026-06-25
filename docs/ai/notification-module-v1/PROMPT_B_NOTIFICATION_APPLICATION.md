# PROMPT B — Notification Module: Application Layer

## Scope

Implement the `Aizen.Modules.Notification.Application` project covering:

1. **CQRS** — Commands, Queries, Handlers, Validators (MediatR / Aizen CQRS pattern)
2. **MassTransit Consumers** — One consumer per cross-module integration event
3. **Dispatcher Services** — InApp, Push, Composite channel dispatchers
4. **Template Interpolator** — `{{variable}}` resolution
5. **Integration Event Extension** — Add `MessagingMessageSentMessage` to `Aizen.Modules.Messaging.Abstraction`
6. **DI Registration** — `DependencyInjection.cs` extension

---

## Architecture Rules

- Follow the `Aizen.Modules.ServiceRequest.Application` and `Aizen.Modules.Messaging.Application` patterns exactly.
- Commands inherit `AizenCommand<TResponse>`, handlers inherit `AizenCommandHandler<TCommand, TResponse>`.
- Queries inherit `AizenQuery<TResponse>`, handlers inherit `AizenQueryHandler<TQuery, TResponse>`.
- Consumers inherit `AizenBaseMessageConsumer<TMessage>` (3 abstract methods: `ExecutePrepareMessage`, `ExecuteCommitMessage`, `ExecuteRollbackMessage`).
- For fire-and-forget notification consumers: `ExecutePrepareMessage` returns `Task.FromResult(true)` immediately. All logic goes in `ExecuteCommitMessage`. `ExecuteRollbackMessage` logs and no-ops.
- Use `IAizenInfoAccessor` for caller identity where needed.
- Use `ILogger<T>` for all logging.
- All handlers and consumers receive dependencies through constructor injection via `IServiceProvider`.

---

## STEP 0 — Add Missing Integration Message to Messaging Abstraction

The Messaging module currently publishes only via SignalR. We need to add an integration event so the Notification module can consume it via MassTransit.

**File to CREATE:** `Aizen.Modules.Messaging.Abstraction/Message/MessagingMessageSentMessage.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Message;

/// <summary>
/// Published by the Messaging module after a message is persisted and moderated.
/// Consumed by the Notification module to trigger new-message notifications
/// for conversation participants who are not the sender.
/// </summary>
public sealed class MessagingMessageSentMessage : AizenBaseMessage
{
    public long                    ConversationId    { get; set; }
    public string                  ConversationTitle { get; set; } = default!;
    public long                    SenderUserId      { get; set; }
    public string                  SenderName        { get; set; } = default!;
    public MessagingContextType    ContextType       { get; set; }
    public long                    ContextId         { get; set; }
    /// <summary>All participant UserIds EXCEPT the sender.</summary>
    public List<long>              RecipientUserIds  { get; set; } = [];
    public bool                    IsInternalNote    { get; set; }
    public DateTimeOffset          SentAt            { get; set; }
}
```

**File to EDIT:** `Aizen.Modules.Messaging.Application/Command/SendMessage/SendMessageCommandHandler.cs`

After the line where the message is persisted and realtime is published, add:
```csharp
// Publish integration event for Notification module
var recipientIds = conversation.Participants
    .Where(p => p.UserId != currentUserId && !message.IsInternalNote)
    .Select(p => p.UserId)
    .ToList();

if (recipientIds.Count > 0)
{
    var publisher = _serviceProvider.GetRequiredService<IAizenMessagePublisher>();
    await publisher.PublishAsync(new MessagingMessageSentMessage
    {
        ConversationId    = conversation.Id,
        ConversationTitle = conversation.Title,
        SenderUserId      = currentUserId,
        SenderName        = participant.DisplayName,
        ContextType       = conversation.ContextType,
        ContextId         = conversation.ContextId,
        RecipientUserIds  = recipientIds,
        IsInternalNote    = request.IsInternalNote,
        SentAt            = message.SentAt,
    }, cancellationToken);
}
```

---

## STEP 1 — CQRS Commands

### 1.1 SendNotificationCommand

**File:** `Command/SendNotification/SendNotificationCommand.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.SendNotification;

public sealed class SendNotificationCommand : AizenCommand<SendNotificationCommandResponse>
{
    public long                RecipientUserId { get; set; }
    public NotificationType    Type            { get; set; }
    public NotificationChannel Channel         { get; set; }
    /// <summary>
    /// Template variable dictionary. Keys match {{variable}} placeholders in the template.
    /// E.g.: { "requestCode": "SR-2025-001", "providerName": "Marine Experts Ltd" }
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
    /// <summary>Optional JSON metadata stored on the notification for deep-link routing.</summary>
    public string? MetadataJson { get; set; }
}

public sealed class SendNotificationCommandResponse
{
    public long   NotificationId { get; init; }
    public bool   Dispatched     { get; init; }
}
```

**File:** `Command/SendNotification/SendNotificationCommandHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.SendNotification;

public sealed class SendNotificationCommandHandler
    : AizenCommandHandler<SendNotificationCommand, SendNotificationCommandResponse>
{
    private readonly INotificationRepository         _notificationRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationDispatcher         _dispatcher;
    private readonly ITemplateInterpolator           _interpolator;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        INotificationDispatcher dispatcher,
        ITemplateInterpolator interpolator,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _templateRepository     = templateRepository;
        _dispatcher             = dispatcher;
        _interpolator           = interpolator;
        _logger                 = logger;
    }

    public override async Task<SendNotificationCommandResponse?> Handle(
        SendNotificationCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve template
        var template = await _templateRepository
            .GetActiveByTypeAndChannelAsync(request.Type, request.Channel, cancellationToken);

        if (template is null)
        {
            _logger.LogWarning(
                "No active notification template found for Type={Type} Channel={Channel}. Skipping.",
                request.Type, request.Channel);
            return new SendNotificationCommandResponse { NotificationId = 0, Dispatched = false };
        }

        // 2. Interpolate
        var title = _interpolator.Interpolate(template.TitleTemplate, request.Variables);
        var body  = _interpolator.Interpolate(template.BodyTemplate,  request.Variables);

        // 3. Persist
        var entity = NotificationEntity.Create(
            request.RecipientUserId, request.Type, request.Channel,
            template.TemplateCode, title, body, request.MetadataJson);

        await _notificationRepository.AddAsync(entity, cancellationToken);

        // 4. Dispatch
        try
        {
            await _dispatcher.DispatchAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Dispatch failed for NotificationId={Id} Channel={Channel}",
                entity.Id, request.Channel);
            entity.MarkAsFailed();
            // Persist updated status — use direct DB update, not AddAsync
            await _notificationRepository.AddAsync(entity, cancellationToken);
        }

        return new SendNotificationCommandResponse
        {
            NotificationId = entity.Id,
            Dispatched     = entity.Status == Abstraction.Enum.NotificationStatus.Sent,
        };
    }
}
```

### 1.2 MarkNotificationAsReadCommand

**File:** `Command/MarkNotificationAsRead/MarkNotificationAsReadCommand.cs`
```csharp
namespace Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommand : AizenCommand<bool>
{
    public long NotificationId { get; set; }
    public long RequestingUserId { get; set; }
}
```

**File:** `Command/MarkNotificationAsRead/MarkNotificationAsReadCommandHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler
    : AizenCommandHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly INotificationRepository _repository;

    public MarkNotificationAsReadCommandHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (entity is null || entity.RecipientUserId != request.RequestingUserId)
            return false;

        entity.MarkAsRead();
        // Persist via direct EF update (SaveChanges on tracked entity)
        return true;
    }
}
```

### 1.3 BulkMarkAsReadCommand

**File:** `Command/BulkMarkAsRead/BulkMarkAsReadCommand.cs`
```csharp
namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommand : AizenCommand<bool>
{
    public long UserId { get; set; }
}
```

**File:** `Command/BulkMarkAsRead/BulkMarkAsReadCommandHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommandHandler
    : AizenCommandHandler<BulkMarkAsReadCommand, bool>
{
    private readonly INotificationRepository _repository;

    public BulkMarkAsReadCommandHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        BulkMarkAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repository.BulkMarkAsReadAsync(request.UserId, cancellationToken);
        return true;
    }
}
```

### 1.4 RegisterDeviceTokenCommand

**File:** `Command/RegisterDeviceToken/RegisterDeviceTokenCommand.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommand : AizenCommand<bool>
{
    public long         UserId      { get; set; }
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
```

**File:** `Command/RegisterDeviceToken/RegisterDeviceTokenCommandHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : AizenCommandHandler<RegisterDeviceTokenCommand, bool>
{
    private readonly IUserDeviceTokenRepository _repository;

    public RegisterDeviceTokenCommandHandler(IUserDeviceTokenRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        await _repository.UpsertAsync(request.UserId, request.DeviceToken, request.Platform, cancellationToken);
        return true;
    }
}
```

---

## STEP 2 — CQRS Queries

### 2.1 GetUserNotificationsQuery

**File:** `Query/GetUserNotifications/GetUserNotificationsQuery.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQuery : AizenQuery<GetUserNotificationsResponse>
{
    public long UserId { get; set; }
    public int  Skip   { get; set; } = 0;
    public int  Take   { get; set; } = 20;
}

public sealed class GetUserNotificationsResponse
{
    public List<NotificationDto> Items       { get; init; } = [];
    public int                   Total       { get; init; }
    public int                   UnreadCount { get; init; }
}
```

**File:** `Query/GetUserNotifications/GetUserNotificationsQueryHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQueryHandler
    : AizenQueryHandler<GetUserNotificationsQuery, GetUserNotificationsResponse>
{
    private readonly INotificationRepository _repository;

    public GetUserNotificationsQueryHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<GetUserNotificationsResponse?> Handle(
        GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var items       = await _repository.GetByRecipientAsync(request.UserId, request.Skip, request.Take, cancellationToken);
        var unreadCount = await _repository.GetUnreadCountAsync(request.UserId, cancellationToken);

        return new GetUserNotificationsResponse
        {
            Items = items.Select(n => new NotificationDto
            {
                Id           = n.Id,
                Type         = n.Type,
                Channel      = n.Channel,
                Status       = n.Status,
                Title        = n.Title,
                Body         = n.Body,
                MetadataJson = n.MetadataJson,
                IsRead       = n.IsRead,
                CreatedAt    = n.CreatedAt,
                ReadAt       = n.ReadAt,
            }).ToList(),
            Total       = items.Count,
            UnreadCount = unreadCount,
        };
    }
}
```

### 2.2 GetNotificationTemplatesQuery (Admin)

**File:** `Query/GetNotificationTemplates/GetNotificationTemplatesQuery.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;

public sealed class GetNotificationTemplatesQuery : AizenQuery<List<NotificationTemplateDto>> { }
```

**File:** `Query/GetNotificationTemplates/GetNotificationTemplatesQueryHandler.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;

public sealed class GetNotificationTemplatesQueryHandler
    : AizenQueryHandler<GetNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _repository;

    public GetNotificationTemplatesQueryHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<List<NotificationTemplateDto>?> Handle(
        GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);
        return templates.Select(t => new NotificationTemplateDto
        {
            Id            = t.Id,
            TemplateCode  = t.TemplateCode,
            Name          = t.Name,
            Type          = t.Type,
            Channel       = t.Channel,
            TitleTemplate = t.TitleTemplate,
            BodyTemplate  = t.BodyTemplate,
            IsActive      = t.IsActive,
        }).ToList();
    }
}
```

---

## STEP 3 — MassTransit Consumers

Each consumer follows the `AizenBaseMessageConsumer<TMessage>` pattern:
- `ExecutePrepareMessage` → validate + return `true` to proceed
- `ExecuteCommitMessage` → do the actual work (send notifications)
- `ExecuteRollbackMessage` → log and no-op

Internal helper: `INotificationSender` (private interface within the Application layer) that calls `SendNotificationCommand` via `ISender`/mediator.

### 3.1 ServiceRequest Consumers

**File:** `Consumers/ServiceRequest/ServiceRequestCreatedConsumer.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Consumers.ServiceRequest;

public sealed class ServiceRequestCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestCreatedConsumer> _logger;

    public ServiceRequestCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ServiceRequestCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        ServiceRequestCreatedMessage message, CancellationToken ct)
    {
        // Notify the owner that their request was created
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.ServiceRequestCreated,
            Channel         = NotificationChannel.InApp,
            Variables       = new Dictionary<string, string>
            {
                { "requestCode",   message.RequestCode },
                { "serviceName",   message.ServiceCategoryCode },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for ServiceRequestCreated: {RequestCode}", message.RequestCode);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: ServiceRequestCreatedConsumer for {RequestCode}: {Error}",
            message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
```

**File:** `Consumers/ServiceRequest/ServiceRequestStatusChangedConsumer.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Consumers.ServiceRequest;

public sealed class ServiceRequestStatusChangedConsumer
    : AizenBaseMessageConsumer<ServiceRequestStatusChangedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestStatusChangedConsumer> _logger;

    public ServiceRequestStatusChangedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestStatusChangedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ServiceRequestStatusChangedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        ServiceRequestStatusChangedMessage message, CancellationToken ct)
    {
        // Notify the owner (ActorType != Owner → owner is recipient, else provider)
        var recipientUserId = message.ActorUserId ?? 0;
        if (recipientUserId == 0)
        {
            _logger.LogWarning("StatusChanged notification skipped — no ActorUserId for {Code}", message.RequestCode);
            return;
        }

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = recipientUserId,
            Type            = NotificationType.ServiceRequestStatusChanged,
            Channel         = NotificationChannel.InApp,
            Variables       = new Dictionary<string, string>
            {
                { "requestCode", message.RequestCode },
                { "fromStatus",  message.FromStatus.ToString() },
                { "toStatus",    message.ToStatus.ToString() },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestStatusChangedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: ServiceRequestStatusChangedConsumer {Code}: {Error}",
            message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
```

**File:** `Consumers/ServiceRequest/ServiceRequestOfferCreatedConsumer.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Consumers.ServiceRequest;

public sealed class ServiceRequestOfferCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestOfferCreatedConsumer> _logger;

    public ServiceRequestOfferCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestOfferCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ServiceRequestOfferCreatedMessage message, CancellationToken ct) => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        ServiceRequestOfferCreatedMessage message, CancellationToken ct)
    {
        // Notify the owner that a provider submitted an offer
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.OfferCreated,
            Channel         = NotificationChannel.InApp,
            Variables       = new Dictionary<string, string>
            {
                { "requestCode",  message.RequestCode },
                { "providerName", message.ProviderName ?? "A provider" },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"offerId\":{message.OfferId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestOfferCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: OfferCreated for {Code}: {Error}", message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
```

**File:** `Consumers/ServiceRequest/ServiceRequestOfferAcceptedConsumer.cs`
```csharp
// Pattern identical to OfferCreatedConsumer.
// Notify the PROVIDER that their offer was accepted.
// RecipientUserId = message.ProviderUserId
// NotificationType.OfferAccepted
// Variables: { "requestCode", "ownerName" }
// MetadataJson: {"serviceRequestId":X,"offerId":Y}
```
> Implement following the same pattern as `ServiceRequestOfferCreatedConsumer`.

**File:** `Consumers/ServiceRequest/ServiceRequestAssignmentCreatedConsumer.cs`
```csharp
// Notify the PROVIDER they have been assigned.
// RecipientUserId = message.ProviderUserId
// NotificationType.AssignmentCreated
// Variables: { "requestCode" }
// MetadataJson: {"serviceRequestId":X,"assignmentId":Y}
```
> Implement following the same pattern.

**File:** `Consumers/ServiceRequest/ServiceRequestCompletionSubmittedConsumer.cs`
```csharp
// Notify the OWNER that the provider submitted completion.
// RecipientUserId = message.OwnerUserId
// NotificationType.CompletionSubmitted
// Variables: { "requestCode", "providerName" }
```
> Implement following the same pattern.

**File:** `Consumers/ServiceRequest/ServiceRequestDisputeOpenedConsumer.cs`
```csharp
// Notify BOTH PARTIES (owner + provider) plus admin.
// Send two SendNotificationCommand invocations.
// NotificationType.DisputeOpened
// Variables: { "requestCode" }
```
> Implement following the same pattern. Loop over recipient IDs.

---

### 3.2 Messaging Consumer

**File:** `Consumers/Messaging/MessagingMessageSentConsumer.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Consumers.Messaging;

public sealed class MessagingMessageSentConsumer
    : AizenBaseMessageConsumer<MessagingMessageSentMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<MessagingMessageSentConsumer> _logger;

    public MessagingMessageSentConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<MessagingMessageSentConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        MessagingMessageSentMessage message, CancellationToken ct)
        => Task.FromResult(!message.IsInternalNote); // Skip internal notes

    public override async Task ExecuteCommitMessage(
        MessagingMessageSentMessage message, CancellationToken ct)
    {
        // Notify each recipient (participants except sender)
        var tasks = message.RecipientUserIds.Select(userId => _sender.Send(
            new SendNotificationCommand
            {
                RecipientUserId = userId,
                Type            = NotificationType.NewMessageReceived,
                Channel         = NotificationChannel.InApp,
                Variables       = new Dictionary<string, string>
                {
                    { "senderName",        message.SenderName },
                    { "conversationTitle", message.ConversationTitle },
                },
                MetadataJson = $"{{\"conversationId\":{message.ConversationId}}}",
            }, ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "New message notifications sent for ConversationId={Id} to {Count} recipients",
            message.ConversationId, message.RecipientUserIds.Count);
    }

    public override Task ExecuteRollbackMessage(
        MessagingMessageSentMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: MessagingMessageSentConsumer ConversationId={Id}: {Error}",
            message.ConversationId, ex.Message);
        return Task.CompletedTask;
    }
}
```

---

### 3.3 CargoDry Consumer

**File:** `Consumers/CargoDry/CargoDryKitExpiringConsumer.cs`
```csharp
// Consumes: CargoDryKitExpiringMessage (must be defined in Aizen.Modules.CargoDry.Abstraction)
// Notify the OWNER: NotificationType.CargoDryKitExpiringReminder
// Variables: { "kitCode", "daysLeft", "expiresAt" }
// Pattern: same as above consumers.
// NOTE: If CargoDryKitExpiringMessage does not yet exist in the CargoDry abstraction,
//       add it following the same pattern as ServiceRequestCreatedMessage.
```

---

## STEP 4 — Dispatcher Services

### 4.1 Template Interpolator

**File:** `Services/TemplateInterpolator.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using System.Text.RegularExpressions;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class TemplateInterpolator : ITemplateInterpolator
{
    private static readonly Regex _placeholder =
        new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Interpolate(string template, IReadOnlyDictionary<string, string> variables)
    {
        return _placeholder.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
```

### 4.2 InApp Dispatcher

**File:** `Services/InAppNotificationDispatcher.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Dispatches in-app notifications via SignalR to the recipient's user group.
/// Group name pattern: user:{userId}
/// </summary>
public sealed class InAppNotificationDispatcher : INotificationDispatcher
{
    // IHubContext is injected here — the Hub itself lives in the API layer but
    // the dispatcher lives in Application layer using IHubContext<THub, TClient>.
    // To avoid circular dependency, we use a weak abstraction:
    private readonly IInAppNotificationPusher _pusher;
    private readonly ILogger<InAppNotificationDispatcher> _logger;

    public InAppNotificationDispatcher(
        IInAppNotificationPusher pusher,
        ILogger<InAppNotificationDispatcher> logger)
    {
        _pusher = pusher;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        try
        {
            await _pusher.PushToUserAsync(notification.RecipientUserId, new InAppNotificationPayload
            {
                NotificationId = notification.Id,
                Type           = notification.Type.ToString(),
                Title          = notification.Title,
                Body           = notification.Body,
                MetadataJson   = notification.MetadataJson,
                CreatedAt      = notification.CreatedAt,
            }, ct);

            notification.MarkAsSent();
            _logger.LogInformation(
                "InApp notification dispatched: Id={Id} UserId={UserId}",
                notification.Id, notification.RecipientUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InApp dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed();
        }
    }
}

public interface IInAppNotificationPusher
{
    Task PushToUserAsync(long userId, InAppNotificationPayload payload, CancellationToken ct);
}

public sealed class InAppNotificationPayload
{
    public long          NotificationId { get; init; }
    public string        Type           { get; init; } = default!;
    public string        Title          { get; init; } = default!;
    public string        Body           { get; init; } = default!;
    public string?       MetadataJson   { get; init; }
    public DateTimeOffset CreatedAt     { get; init; }
}
```

### 4.3 Push Notification Dispatcher (FCM)

**File:** `Services/PushNotificationDispatcher.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Dispatches push notifications via FCM/APNS.
/// Requires FirebaseAdmin SDK (Firebase.Admin NuGet) or direct FCM HTTP v1 API.
/// MVP: HTTP v1 API via HttpClient. Full: Firebase.Admin SDK.
/// </summary>
public sealed class PushNotificationDispatcher : INotificationDispatcher
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly IFcmSender _fcmSender;
    private readonly ILogger<PushNotificationDispatcher> _logger;

    public PushNotificationDispatcher(
        IUserDeviceTokenRepository tokenRepository,
        IFcmSender fcmSender,
        ILogger<PushNotificationDispatcher> logger)
    {
        _tokenRepository = tokenRepository;
        _fcmSender       = fcmSender;
        _logger          = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        var tokens = await _tokenRepository.GetActiveByUserAsync(notification.RecipientUserId, ct);

        if (tokens.Count == 0)
        {
            _logger.LogInformation(
                "No active device tokens for UserId={UserId}. Push skipped.",
                notification.RecipientUserId);
            return;
        }

        foreach (var token in tokens)
        {
            try
            {
                var messageId = await _fcmSender.SendAsync(
                    token.DeviceToken, notification.Title, notification.Body,
                    notification.MetadataJson, ct);

                notification.MarkAsSent(messageId);
                _logger.LogInformation(
                    "Push sent to token {Token}: MessageId={MessageId}",
                    token.DeviceToken[..10] + "...", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Push failed for token {Token}", token.DeviceToken[..10] + "...");
                // Deactivate invalid tokens (FCM returns 404 for expired tokens)
                if (ex.Message.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase))
                {
                    await _tokenRepository.DeactivateAsync(token.DeviceToken, ct);
                }
            }
        }
    }
}

/// <summary>
/// Abstraction for FCM HTTP v1 sending. Register a real implementation using HttpClient + FCM credentials.
/// </summary>
public interface IFcmSender
{
    /// <returns>FCM Message ID (for delivery tracking)</returns>
    Task<string> SendAsync(
        string deviceToken, string title, string body,
        string? dataJson, CancellationToken ct);
}

/// <summary>
/// Stub implementation used in development/test environments.
/// Replace with real FCM HTTP v1 implementation for production.
/// </summary>
public sealed class FcmSenderStub : IFcmSender
{
    private readonly ILogger<FcmSenderStub> _logger;

    public FcmSenderStub(ILogger<FcmSenderStub> logger) => _logger = logger;

    public Task<string> SendAsync(
        string deviceToken, string title, string body,
        string? dataJson, CancellationToken ct)
    {
        _logger.LogInformation(
            "[FCM STUB] Push to {Token}: Title={Title}",
            deviceToken[..10] + "...", title);
        return Task.FromResult($"stub-{Guid.NewGuid()}");
    }
}
```

### 4.4 Composite Dispatcher

**File:** `Services/CompositeNotificationDispatcher.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Routes each notification to the correct channel dispatcher based on NotificationChannel.
/// Registered as the primary INotificationDispatcher in DI.
/// </summary>
public sealed class CompositeNotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationDispatcher> _dispatchers;
    private readonly ILogger<CompositeNotificationDispatcher> _logger;

    // Named registrations via keyed DI (.NET 8+)
    public CompositeNotificationDispatcher(
        [FromKeyedServices(NotificationChannel.InApp)]  INotificationDispatcher inApp,
        [FromKeyedServices(NotificationChannel.Push)]   INotificationDispatcher push,
        ILogger<CompositeNotificationDispatcher> logger)
    {
        _dispatchers = [inApp, push];
        _logger      = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        var dispatcher = notification.Channel switch
        {
            NotificationChannel.InApp => _dispatchers.First(),
            NotificationChannel.Push  => _dispatchers.Skip(1).First(),
            _ => null,
        };

        if (dispatcher is null)
        {
            _logger.LogWarning(
                "No dispatcher registered for channel {Channel}", notification.Channel);
            return;
        }

        await dispatcher.DispatchAsync(notification, ct);
    }
}
```

---

## STEP 5 — DI Registration

**File:** `DependencyInjection.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Service;
using Aizen.Modules.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplicationServices(
        this IServiceCollection services)
    {
        // Template interpolation
        services.AddScoped<ITemplateInterpolator, TemplateInterpolator>();

        // FCM sender — swap FcmSenderStub with real implementation in production
        services.AddScoped<IFcmSender, FcmSenderStub>();

        // Channel dispatchers (keyed by channel enum)
        services.AddKeyedScoped<INotificationDispatcher, InAppNotificationDispatcher>(
            NotificationChannel.InApp);
        services.AddKeyedScoped<INotificationDispatcher, PushNotificationDispatcher>(
            NotificationChannel.Push);

        // Composite dispatcher — the primary INotificationDispatcher
        services.AddScoped<INotificationDispatcher, CompositeNotificationDispatcher>();

        return services;
    }
}
```

---

## STEP 6 — csproj References

### `Aizen.Modules.Notification.Application.csproj`
```xml
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="MassTransit" Version="8.*" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Core" Version="9.*" />
<ProjectReference Include="..\Aizen.Modules.Notification.Domain\Aizen.Modules.Notification.Domain.csproj" />
<ProjectReference Include="..\Aizen.Modules.Notification.Abstraction\Aizen.Modules.Notification.Abstraction.csproj" />
<!-- Cross-module message contracts -->
<ProjectReference Include="..\..\ServiceRequest\src\Aizen.Modules.ServiceRequest.Abstraction\Aizen.Modules.ServiceRequest.Abstraction.csproj" />
<ProjectReference Include="..\..\Messaging\src\Aizen.Modules.Messaging.Abstraction\Aizen.Modules.Messaging.Abstraction.csproj" />
<!-- Add CargoDry.Abstraction when CargoDry module is ready -->
```

---

## Verification Checklist

- [ ] `SendNotificationCommandHandler` resolves template, interpolates, persists, dispatches
- [ ] `MessagingMessageSentConsumer.ExecutePrepareMessage` returns `false` for `IsInternalNote = true`
- [ ] `CompositeNotificationDispatcher` routes `InApp` → `InAppNotificationDispatcher`, `Push` → `PushNotificationDispatcher`
- [ ] `FcmSenderStub` logs correctly, returns a stub message ID
- [ ] All consumers compile with correct `AizenBaseMessageConsumer<T>` abstract method signatures
- [ ] `TemplateInterpolator` correctly replaces `{{variable}}` and leaves unknown placeholders intact
- [ ] `MessagingMessageSentMessage` added to `Aizen.Modules.Messaging.Abstraction`
- [ ] `SendMessageCommandHandler` publishes `MessagingMessageSentMessage` after persistence
