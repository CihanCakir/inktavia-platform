# PROMPT D — Messaging Module: API Layer
# `Aizen.Modules.Messaging` (Web API project)

## Context & Architecture Rules

You are implementing the **API layer** of `Aizen.Modules.Messaging`. This layer contains the SignalR hub, domain registrar, controllers, and the `Program.cs` wiring.

**Confirmed patterns from ServiceRequest module — follow exactly:**

```csharp
// Program.cs pattern (copy from ServiceRequest, adapt for Messaging):
builder.Services.AddAizenRealtime(builder.Configuration);
builder.Services.AddDomainHub<MessagingHub>("messaging");
builder.Services.AddSingleton<IRealtimeDomainRegistrar, MessagingRealtimeDomainRegistrar>();
// ...
app.UseAizenRealtime();
app.MapHub<MessagingHub>("/hubs/messaging");
```

**Hub pattern:**
- Inherits `DomainHubBase` (from `Aizen.Core.Realtime.Hubs`)
- `DomainName` property must match the domain key passed to `AddDomainHub<>()`
- Constructor: `public MessagingHub(IRealtimePublisher publisher) : base(publisher) { }`
- `OnConnectedAsync`: add user to `user:{userId}` group
- Group naming convention: `messaging:conv:{conversationId}` for conversation rooms

**Controller pattern:**
- Inherits `AizenWebApiController` (from `Aizen.Core.Infrastructure.Api`)
- Returns `AizenApiResponse<T?>` using `SetResponse(result)` helper
- `IAizenCQRSProcessor` injected, sends commands via `_cqrs.ProcessAsync<TResponse>(command, ct)`
- Route prefix: `api/v1/`
- `[Tags("...")]` for Swagger grouping
- `[DocumentationInfo("...", "...")]` on controller class

---

## Step 1 — SignalR Hub (`Hubs/MessagingHub.cs`)

```csharp
using Aizen.Core.Realtime.Hubs;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.Messaging.Hubs;

[DocumentationInfo("Messaging Hub",
    "SignalR hub for real-time messaging events across all conversation contexts.")]
[Authorize]
public sealed class MessagingHub : DomainHubBase
{
    public override string DomainName => "messaging";

    public MessagingHub(IRealtimePublisher publisher) : base(publisher) { }

    public override async Task OnConnectedAsync()
    {
        // Auto-subscribe each user to their personal notification channel
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client subscribes to a conversation room to receive real-time messages.
    /// Called after history is loaded via REST — then live events flow through here.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"messaging:conv:{conversationId}");
    }

    /// <summary>Client leaves a conversation room (e.g., navigates away).</summary>
    public async Task LeaveConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"messaging:conv:{conversationId}");
    }

    /// <summary>Admin subscribes to the moderation operations channel.</summary>
    [Authorize(Roles = "Admin")]
    public async Task SubscribeToModerationQueue()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin:messaging-moderation");
    }

    /// <summary>Admin unsubscribes from moderation channel.</summary>
    [Authorize(Roles = "Admin")]
    public async Task UnsubscribeFromModerationQueue()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin:messaging-moderation");
    }
}

/// <summary>Hub event name constants used by server → client broadcasts.</summary>
public static class MessagingHubEvents
{
    public const string MessageReceived          = "MessageReceived";
    public const string ConversationCreated      = "ConversationCreated";
    public const string ConversationStatusChanged = "ConversationStatusChanged";
    public const string MessageModerated         = "MessageModerated";
    public const string UnreadCountUpdated       = "UnreadCountUpdated";
    public const string ParticipantJoined        = "ParticipantJoined";
}
```

---

## Step 2 — Realtime Domain Registrar (`Realtime/MessagingRealtimeDomainRegistrar.cs`)

```csharp
using Aizen.Core.Realtime.Abstraction.Domain;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Modules.Messaging.Realtime;

[DocumentationInfo("Messaging realtime domain registrar",
    "Registers all Messaging module realtime event names into the global registry at startup.")]
public sealed class MessagingRealtimeDomainRegistrar : IRealtimeDomainRegistrar
{
    public void Register()
    {
        RealtimeEventRegistry.RegisterDomainEvents("messaging",
            "MessageReceived",
            "ConversationCreated",
            "ConversationStatusChanged",
            "MessageModerated",
            "UnreadCountUpdated",
            "ParticipantJoined",
            "ParticipantLeft"
        );
    }
}
```

---

## Step 3 — Controllers (`Controller/V1/`)

### 3.1 `ConversationsController.cs`
Path: `Controller/V1/Conversations/ConversationsController.cs`

```csharp
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request;
using Aizen.Modules.Messaging.Abstraction.Response;
using Aizen.Modules.Messaging.Application.Command.CreateConversation;
using Aizen.Modules.Messaging.Application.Query.GetConversationDetail;
using Aizen.Modules.Messaging.Application.Query.GetConversationByContext;
using Aizen.Modules.Messaging.Application.Query.GetConversationList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Conversations;

[ApiController]
[Route("api/v1/conversations")]
[Tags("Messaging - Conversations")]
[Authorize]
[DocumentationInfo("Conversations controller",
    "Manages conversation creation, listing, and detail retrieval across all context types.")]
public sealed class ConversationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>
    /// List all conversations with optional filters.
    /// GET /api/v1/conversations?status=Active&contextType=ServiceRequest&skip=0&take=20
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse?>> GetList(
        [FromQuery] ConversationStatus? status,
        [FromQuery] MessagingContextType? contextType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationListResponse>(
            new GetConversationListQuery(status, contextType, skip, take), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Get full conversation detail with messages.
    /// GET /api/v1/conversations/{id}
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetDetail(
        [FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(
            new GetConversationDetailQuery(id), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Get or create conversation by context (used by other modules).
    /// GET /api/v1/conversations/by-context?contextType=ServiceRequest&contextId=9001
    /// </summary>
    [HttpGet("by-context")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetByContext(
        [FromQuery] MessagingContextType contextType,
        [FromQuery] long contextId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(
            new GetConversationByContextQuery(contextType, contextId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Create a new conversation (called by other modules on entity creation).
    /// POST /api/v1/conversations
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateConversationResponse), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<CreateConversationResponse?>> Create(
        [FromBody] CreateConversationRequest request, CancellationToken ct = default)
    {
        var command = new CreateConversationCommand(
            request.ContextType, request.ContextId, request.Title, request.Participants);
        var result = await _cqrs.ProcessAsync<CreateConversationResponse>(command, ct);
        return SetResponse(result);
    }
}
```

---

### 3.2 `ConversationMessagesController.cs`
Path: `Controller/V1/Conversations/ConversationMessagesController.cs`

```csharp
[ApiController]
[Route("api/v1/conversations/{conversationId:long}/messages")]
[Tags("Messaging - Messages")]
[Authorize]
[DocumentationInfo("Conversation messages controller",
    "Handles sending and retrieving messages within a conversation.")]
public sealed class ConversationMessagesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationMessagesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>
    /// Send a message in a conversation.
    /// POST /api/v1/conversations/{conversationId}/messages
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendMessageResponse?>> Send(
        [FromRoute] long conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        var command = new SendMessageCommand(
            conversationId, request.Content, request.Type, request.IsInternalNote,
            request.AttachmentFileStorageId, request.AttachmentFileName, request.AttachmentFileType);
        var result = await _cqrs.ProcessAsync<SendMessageResponse>(command, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Mark all messages in a conversation as read by admin.
    /// PATCH /api/v1/conversations/{conversationId}/messages/mark-read
    /// </summary>
    [HttpPatch("mark-read")]
    [Authorize(Roles = "Admin")]
    public async Task<AizenApiResponse<bool?>> MarkRead(
        [FromRoute] long conversationId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new MarkConversationReadCommand(conversationId), ct);
        return SetResponse(result);
    }
}
```

---

### 3.3 `ConversationModerationController.cs`
Path: `Controller/V1/Moderation/ConversationModerationController.cs`

```csharp
[ApiController]
[Route("api/v1/moderation")]
[Tags("Messaging - Moderation")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Conversation moderation controller",
    "Admin-only endpoints for content moderation: flag conversations, moderate messages, view moderation queue.")]
public sealed class ConversationModerationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationModerationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Get flagged/pending messages for review.</summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(GetModerationQueueResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetModerationQueueResponse?>> GetQueue(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetModerationQueueResponse>(
            new GetModerationQueueQuery(skip, take), ct);
        return SetResponse(result);
    }

    /// <summary>Update moderation verdict on a specific message.</summary>
    [HttpPatch("messages/{messageId:long}")]
    public async Task<AizenApiResponse<bool?>> ModerateMessage(
        [FromRoute] long messageId,
        [FromBody] ModerateMessageRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new ModerateMessageCommand(messageId, request.Status, request.Reason), ct);
        return SetResponse(result);
    }

    /// <summary>Flag an entire conversation for review.</summary>
    [HttpPatch("conversations/{conversationId:long}/flag")]
    public async Task<AizenApiResponse<bool?>> FlagConversation(
        [FromRoute] long conversationId,
        [FromBody] FlagConversationRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new FlagConversationCommand(conversationId, request.Reason), ct);
        return SetResponse(result);
    }
}
```

---

## Step 4 — Program.cs

```csharp
using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Starter;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Hubs;
using Aizen.Modules.Messaging.Realtime;
using Aizen.Modules.Messaging.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Messaging",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

// ── Database ─────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<MessagingDbContext>(builder.Configuration, "Messaging", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.Messaging.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Infrastructure ───────────────────────────────────────────────────────
builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);           // Available for archival if needed
builder.Services.AddAizenInfoAccessor(builder.Configuration);

// ── Module services ──────────────────────────────────────────────────────
builder.Services.AddMessagingRepository();
builder.Services.AddMessagingServices();

// ── Realtime (SignalR) ───────────────────────────────────────────────────
builder.Services.AddScoped<MessagingRealtimePublisher>();
builder.Services.AddSingleton<IRealtimeDomainRegistrar, MessagingRealtimeDomainRegistrar>();
builder.Services.AddAizenRealtime(builder.Configuration);
builder.Services.AddDomainHub<MessagingHub>("messaging");

// ── Build ────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseAizenRealtime();

await app.SeedMessagingAsync();

app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
```

---

## Step 5 — appsettings additions

Add to `configuration/appsettings.json` and `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Messaging": "Host=localhost;Database=aizen;Username=postgres;Password=postgres;Search Path=messaging"
  },
  "Messaging": {
    "ContentPolicy": {
      "OffPlatformKeywords": ["iban", "swift", "whatsapp", "telegram", "banka hesabı"],
      "MaxUrlsPerMessage": 2,
      "SpamWindowMinutes": 5,
      "SpamRepeatThreshold": 3
    }
  }
}
```

**Update `MessageContentPolicyService`** to read `OffPlatformKeywords` from `IConfiguration["Messaging:ContentPolicy:OffPlatformKeywords"]` instead of hardcoded array.

---

## Quality Gates

- [ ] `MessagingHub` inherits `DomainHubBase`, `DomainName == "messaging"`
- [ ] `AddDomainHub<MessagingHub>("messaging")` matches `DomainName`
- [ ] `app.MapHub<MessagingHub>("/hubs/messaging")` — hub accessible at this path
- [ ] `MessagingRealtimeDomainRegistrar` registered as `IRealtimeDomainRegistrar` Singleton
- [ ] All 3 controllers registered (auto-scanned via `AizenApplicationBuilder`)
- [ ] `[Authorize(Roles = "Admin")]` on moderation controller
- [ ] Hub `JoinConversation` group name: `messaging:conv:{id}` (matches publisher's group name)
- [ ] `Program.cs` has no references to ServiceRequest — Messaging is fully standalone
- [ ] Swagger shows 3 tag groups: "Messaging - Conversations", "Messaging - Messages", "Messaging - Moderation"
