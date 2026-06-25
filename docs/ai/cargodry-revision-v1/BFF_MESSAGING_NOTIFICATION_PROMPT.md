# BFF: Messaging & Notification Templates — Implementation Prompt

## Context

You are implementing BFF-layer wiring for two modules in the Aizen Marine OS admin panel:

1. **Messaging** — The `Aizen.Modules.Messaging` API is fully built and running at port 7109. The frontend makes calls to `/api/v1/admin-panel/messaging/*` but the BFF has NO controller or remote call interface for messaging. All such requests return 404.

2. **Notification Templates** — The `Aizen.Modules.Notification` API is fully built and running at port 7110. The BFF application layer already has CQRS handlers (`GetAdminNotificationTemplatesQueryHandler`, `CreateNotificationTemplateCommandHandler`, etc.) and a `INotificationAdminBffRemoteCall` interface. However: the remote call is NOT registered in DI, and there is NO controller. Routes currently return 501 from `AdminInactiveModulesController`.

### Project conventions (MUST follow)
- BFF controllers live in: `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/`
- BFF remote call interfaces live in: `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/`
- BFF DI registration file: `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/DependencyInjection.cs`
- Inactive modules controller: `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminInactiveModulesController.cs`
- Reference controller: `AdminCargoDryController.cs` — injects `IAdminCargoDryBffRemoteCall` + `IAizenCQRSProcessor`, calls remote directly, wraps with `SetResponse(result)`
- Refit registration pattern (from DI): `RestService.For<TInterface>(factory.CreateClient(nameof(TInterface)), AizenRefitSettings)` for simple clients; `new HttpClient(forwardingHandler)` + `RestService.For<>` for clients that need auth header forwarding
- `AuthorizationForwardingHandler` forwards the caller's bearer token downstream — use this for both messaging and notification admin remote calls (same as CargoDry)
- Response wrapping: `return SetResponse(result)` — the remote call result (`AizenApiResponse<T>`) is wrapped by `AizenWebApiController.SetResponse()` into the outer envelope

---

## Task 1: Create `IAdminMessagingBffRemoteCall`

**File to create:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IAdminMessagingBffRemoteCall.cs`

This is a **Refit** interface (like `IAdminCargoDryBffRemoteCall`). Auth forwarding is handled by `AuthorizationForwardingHandler` — do NOT pass auth headers as parameters.

```csharp
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin messaging BFF remote call", "Refit interface for BFF-to-Messaging module communication. Auth forwarded via AuthorizationForwardingHandler.")]
public interface IAdminMessagingBffRemoteCall
{
    // ─── Conversations ────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/conversations</summary>
    [Get("/api/v1/conversations")]
    Task<GetConversationListResponse> GetConversationsAsync(
        [Query] string? status      = null,
        [Query] string? contextType = null,
        [Query] int     skip        = 0,
        [Query] int     take        = 20,
        CancellationToken ct        = default);

    /// <summary>GET /api/v1/conversations/{id}</summary>
    [Get("/api/v1/conversations/{id}")]
    Task<GetConversationDetailResponse> GetConversationAsync(
        long id,
        CancellationToken ct = default);

    // ─── Messages ─────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/conversations/{conversationId}/messages</summary>
    [Post("/api/v1/conversations/{conversationId}/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        long conversationId,
        [Body] SendMessageRequest body,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/conversations/{conversationId}/messages/mark-read</summary>
    [Patch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task MarkReadAsync(
        long conversationId,
        CancellationToken ct = default);

    /// <summary>POST /api/v1/conversations/{conversationId}/messages/attachment-upload-url</summary>
    [Post("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<object> GetAttachmentUploadUrlAsync(
        long conversationId,
        [Body] AttachmentUploadUrlRequest body,
        CancellationToken ct = default);

    // ─── Moderation ───────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/moderation/queue</summary>
    [Get("/api/v1/moderation/queue")]
    Task<GetModerationQueueResponse> GetModerationQueueAsync(
        [Query] int skip = 0,
        [Query] int take = 20,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/moderation/messages/{messageId}</summary>
    [Patch("/api/v1/moderation/messages/{messageId}")]
    Task ModerateMessageAsync(
        long messageId,
        [Body] ModerateMessageRequest body,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/moderation/conversations/{conversationId}/flag</summary>
    [Patch("/api/v1/moderation/conversations/{conversationId}/flag")]
    Task FlagConversationAsync(
        long conversationId,
        [Body] FlagConversationRequest body,
        CancellationToken ct = default);

    // ─── Reporting ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/reporting/messaging/provider-response-time</summary>
    [Get("/api/v1/reporting/messaging/provider-response-time")]
    Task<object> GetProviderResponseTimeAsync(
        [Query] string? from = null,
        [Query] string? to   = null,
        CancellationToken ct = default);

    /// <summary>GET /api/v1/reporting/messaging/channel-usage</summary>
    [Get("/api/v1/reporting/messaging/channel-usage")]
    Task<object> GetChannelUsageAsync(
        [Query] string? from = null,
        [Query] string? to   = null,
        CancellationToken ct = default);
}
```

**Important notes on response types:**
- `GetConversationListResponse`, `GetConversationDetailResponse`, `GetModerationQueueResponse`, `SendMessageResponse` are defined in `Aizen.Modules.Messaging.Abstraction.Response.Messaging` — use them directly.
- `AttachmentUploadUrlRequest`, `SendMessageRequest`, `ModerateMessageRequest`, `FlagConversationRequest` are in `Aizen.Modules.Messaging.Abstraction.Request.Messaging`.
- Reporting and attachment-upload-url use `object` as return type — the BFF passes through the raw response.

---

## Task 2: Create `AdminMessagingController`

**File to create:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminMessagingController.cs`

**Frontend endpoint mapping (from `src/shared/api/endpoints.ts`):**

| Frontend call | BFF route | Module route |
|---|---|---|
| `GET /admin-panel/messaging/conversations` | `GET api/v1/admin-panel/messaging/conversations` | `GET /api/v1/conversations` |
| `GET /admin-panel/messaging/conversations/{id}` | `GET api/v1/admin-panel/messaging/conversations/{id}` | `GET /api/v1/conversations/{id}` |
| `POST /admin-panel/messaging/conversations/{id}/messages` | `POST api/v1/admin-panel/messaging/conversations/{id}/messages` | `POST /api/v1/conversations/{id}/messages` |
| `PATCH /admin-panel/messaging/conversations/{id}/mark-read` | `PATCH api/v1/admin-panel/messaging/conversations/{id}/mark-read` | `PATCH /api/v1/conversations/{id}/messages/mark-read` |
| `POST /admin-panel/messaging/conversations/{id}/attachment-upload-url` | `POST api/v1/admin-panel/messaging/conversations/{id}/attachment-upload-url` | `POST /api/v1/conversations/{id}/messages/attachment-upload-url` |
| `GET /admin-panel/messaging/moderation/queue` | `GET api/v1/admin-panel/messaging/moderation/queue` | `GET /api/v1/moderation/queue` |
| `PATCH /admin-panel/messaging/moderation/messages/{id}` | `PATCH api/v1/admin-panel/messaging/moderation/messages/{id}` | `PATCH /api/v1/moderation/messages/{id}` |
| `PATCH /admin-panel/messaging/moderation/conversations/{id}/flag` | `PATCH api/v1/admin-panel/messaging/moderation/conversations/{id}/flag` | `PATCH /api/v1/moderation/conversations/{id}/flag` |
| `GET /admin-panel/messaging/reports` | `GET api/v1/admin-panel/messaging/reports` | `GET /api/v1/reporting/messaging/channel-usage` + `provider-response-time` |

**Implementation:**

```csharp
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/messaging")]
[Tags("Admin Panel - Messaging")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminMessagingController : AizenWebApiController
{
    private readonly IAdminMessagingBffRemoteCall _messaging;

    public AdminMessagingController(
        IHttpContextAccessor httpContextAccessor,
        IAdminMessagingBffRemoteCall messaging)
        : base(httpContextAccessor)
    {
        _messaging = messaging;
    }

    // ─── Conversations ────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/messaging/conversations</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse>> GetConversations(
        [FromQuery] string? status      = null,
        [FromQuery] string? contextType = null,
        [FromQuery] int     skip        = 0,
        [FromQuery] int     take        = 20,
        CancellationToken ct            = default)
    {
        var result = await _messaging.GetConversationsAsync(status, contextType, skip, take, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/messaging/conversations/{id}</summary>
    [HttpGet("conversations/{id:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse>> GetConversation(
        long id,
        CancellationToken ct = default)
    {
        var result = await _messaging.GetConversationAsync(id, ct);
        return SetResponse(result);
    }

    // ─── Messages ─────────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/messaging/conversations/{id}/messages</summary>
    [HttpPost("conversations/{id:long}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendMessageResponse>> SendMessage(
        long id,
        [FromBody] SendMessageRequest body,
        CancellationToken ct = default)
    {
        var result = await _messaging.SendMessageAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/conversations/{id}/mark-read</summary>
    [HttpPatch("conversations/{id:long}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct = default)
    {
        await _messaging.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST api/v1/admin-panel/messaging/conversations/{id}/attachment-upload-url</summary>
    [HttpPost("conversations/{id:long}/attachment-upload-url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> GetAttachmentUploadUrl(
        long id,
        [FromBody] AttachmentUploadUrlRequest body,
        CancellationToken ct = default)
    {
        var result = await _messaging.GetAttachmentUploadUrlAsync(id, body, ct);
        return SetResponse(result);
    }

    // ─── Moderation ───────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/messaging/moderation/queue</summary>
    [HttpGet("moderation/queue")]
    [ProducesResponseType(typeof(GetModerationQueueResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetModerationQueueResponse>> GetModerationQueue(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _messaging.GetModerationQueueAsync(skip, take, ct);
        return SetResponse(result);
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/moderation/messages/{id}</summary>
    [HttpPatch("moderation/messages/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ModerateMessage(
        long id,
        [FromBody] ModerateMessageRequest body,
        CancellationToken ct = default)
    {
        await _messaging.ModerateMessageAsync(id, body, ct);
        return NoContent();
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/moderation/conversations/{id}/flag</summary>
    [HttpPatch("moderation/conversations/{id:long}/flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FlagConversation(
        long id,
        [FromBody] FlagConversationRequest body,
        CancellationToken ct = default)
    {
        await _messaging.FlagConversationAsync(id, body, ct);
        return NoContent();
    }

    // ─── Reporting ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/v1/admin-panel/messaging/reports
    /// Merges provider-response-time and channel-usage into one response object.
    /// The frontend (useMessagingReportsQuery) expects a single MessagingReportResponse shape:
    /// { providerResponseTime: [...], channelUsage: [...], generatedAt: string }
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> GetReports(
        [FromQuery] string? from = null,
        [FromQuery] string? to   = null,
        CancellationToken ct     = default)
    {
        var (providerTask, channelTask) = (
            _messaging.GetProviderResponseTimeAsync(from, to, ct),
            _messaging.GetChannelUsageAsync(from, to, ct));

        await Task.WhenAll(providerTask, channelTask);

        var merged = new
        {
            providerResponseTime = await providerTask,
            channelUsage         = await channelTask,
            generatedAt          = DateTimeOffset.UtcNow.ToString("O")
        };

        return SetResponse(merged);
    }
}
```

**Note on `SetResponse` overloads:** If `SetResponse(T value)` does not accept a plain object directly (only `AizenApiResponse<T>` from remote calls), wrap manually:

```csharp
return Ok(new AizenApiResponse<object> { Body = merged, Header = new ... });
```

Check how `AizenWebApiController.SetResponse` is defined and use the appropriate overload. If a typed overload like `SetResponse<T>(T value)` exists, prefer it.

---

## Task 3: Register `IAdminMessagingBffRemoteCall` in DI

**File to modify:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/DependencyInjection.cs`

Add the following block **after** the `IAdminCargoDryBffRemoteCall` registration (follow the exact same pattern):

```csharp
services.AddTransient<IAdminMessagingBffRemoteCall>(provider =>
{
    var remoteCallConfigs = provider.GetRequiredService<IOptions<RemoteCallConfigurations>>().Value;
    remoteCallConfigs.TryGetValue(nameof(IAdminMessagingBffRemoteCall), out var messagingConfig);

    var forwardingHandler = provider.GetRequiredService<AuthorizationForwardingHandler>();
    forwardingHandler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(forwardingHandler);
    if (messagingConfig?.BaseUrl is not null)
        httpClient.BaseAddress = new Uri(messagingConfig.BaseUrl);

    return RestService.For<IAdminMessagingBffRemoteCall>(
        httpClient,
        RemoteCallBuilderExtensions.AizenRefitSettings);
});
```

The `BaseUrl` is injected from docker-compose via:
```yaml
RemoteCalls__IAdminMessagingBffRemoteCall__BaseUrl: http://messaging-api:8080
```
(Already added to docker-compose in a previous step.)

---

## Task 4: Register `INotificationAdminBffRemoteCall` in DI

**File to modify:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/DependencyInjection.cs`

The interface `INotificationAdminBffRemoteCall` already exists in the Application layer but is NOT registered. It uses `IAizenRemoteCall` pattern (not Refit). Check how the existing `IAizenRemoteCall` implementations are registered in this project (e.g., look for any existing `AizenRemoteCallFactory` or similar in the codebase).

If no existing `IAizenRemoteCall` factory helper is found, register it manually similarly to how the notification inbox remote call (`INotificationBffRemoteCall`) is registered. `INotificationBffRemoteCall` IS already registered in DI — find it and add `INotificationAdminBffRemoteCall` using the same pattern:

```csharp
services.AddTransient<INotificationAdminBffRemoteCall>(provider =>
{
    var remoteCallConfigs = provider.GetRequiredService<IOptions<RemoteCallConfigurations>>().Value;
    remoteCallConfigs.TryGetValue(nameof(INotificationAdminBffRemoteCall), out var notifAdminConfig);

    var forwardingHandler = provider.GetRequiredService<AuthorizationForwardingHandler>();
    forwardingHandler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(forwardingHandler);
    if (notifAdminConfig?.BaseUrl is not null)
        httpClient.BaseAddress = new Uri(notifAdminConfig.BaseUrl);

    return RestService.For<INotificationAdminBffRemoteCall>(
        httpClient,
        RemoteCallBuilderExtensions.AizenRefitSettings);
});
```

**IMPORTANT:** `INotificationAdminBffRemoteCall` uses `AizenRemoteCallGet/Post/Put/Patch` custom attributes (NOT standard Refit `[Get]`/`[Post]`). Before using `RestService.For<>`, confirm whether `AizenRefitSettings` is compatible with these custom attributes, or whether `IAizenRemoteCall`-based interfaces require a different factory. If they require a different factory (e.g., `AizenRemoteCallFactory.Create<T>()`), use that instead.

The handlers (`GetAdminNotificationTemplatesQueryHandler`, `CreateNotificationTemplateCommandHandler`, etc.) call the remote with explicit auth headers:
```csharp
var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
var result = await _notification.GetNotificationTemplates($"Bearer {serviceToken}", request.UserToken);
```

This means the service token is fetched separately from `IAdminPanelBffKeycloakServiceTokenProvider` — the `AuthorizationForwardingHandler` may NOT be needed here if auth is passed as explicit headers. Verify and adjust accordingly: if the interface passes auth headers as method parameters, do NOT add `AuthorizationForwardingHandler` to this client.

The `BaseUrl` comes from:
```yaml
RemoteCalls__IAdminNotificationBffRemoteCall__BaseUrl: http://notification-api:8080
```
(Already added to docker-compose.)

Note: The env var key uses `IAdminNotificationBffRemoteCall` — check if this matches `nameof(INotificationAdminBffRemoteCall)`. If there is a mismatch, use the exact key from docker-compose: `IAdminNotificationBffRemoteCall`.

---

## Task 5: Create `AdminNotificationTemplatesController`

**File to create:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminNotificationTemplatesController.cs`

The CQRS handlers already exist in `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminNotificationTemplates/`:
- `GetAdminNotificationTemplatesQuery` / `GetAdminNotificationTemplatesQueryHandler`
- `GetAdminNotificationTemplateByCodeQuery` / `GetAdminNotificationTemplateByCodeQueryHandler`
- `CreateNotificationTemplateCommand` / `CreateNotificationTemplateCommandHandler`
- `UpdateNotificationTemplateCommand` / `UpdateNotificationTemplateCommandHandler`
- `ToggleNotificationTemplateCommand` / `ToggleNotificationTemplateCommandHandler`

The `UserToken` passed in CQRS messages is the raw bearer token from the incoming request. Extract it from `HttpContext.Request.Headers["Authorization"]` (strip the `Bearer ` prefix to get the raw token — or check how other controllers provide this value, e.g., `GetUserToken()` or similar method on `AizenWebApiController`).

The frontend calls:
- `GET /admin-panel/notification-templates` → list all
- `POST /admin-panel/notification-templates` → create
- (Assumed) `GET /admin-panel/notification-templates/{code}` → get by code
- (Assumed) `PUT /admin-panel/notification-templates/{code}` → update
- (Assumed) `PATCH /admin-panel/notification-templates/{code}/toggle` → toggle active

```csharp
using Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;
using Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/notification-templates")]
[Tags("Admin Panel - Notification Templates")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminNotificationTemplatesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminNotificationTemplatesController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private string GetUserToken()
    {
        var raw = HttpContext.Request.Headers.Authorization.ToString();
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..]
            : raw;
    }

    /// <summary>GET api/v1/admin-panel/notification-templates</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<NotificationTemplateDto>>> GetAll(CancellationToken ct)
    {
        var result = await _cqrs.SendAsync(
            new GetAdminNotificationTemplatesQuery(GetUserToken()), ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notification-templates/{code}</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateDto>> GetByCode(
        string code, CancellationToken ct)
    {
        var result = await _cqrs.SendAsync(
            new GetAdminNotificationTemplateByCodeQuery(code, GetUserToken()), ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/notification-templates</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest body, CancellationToken ct)
    {
        await _cqrs.SendAsync(new CreateNotificationTemplateCommand
        {
            TemplateCode  = body.TemplateCode,
            Name          = body.Name,
            Type          = body.Type,
            Channel       = body.Channel,
            TitleTemplate = body.TitleTemplate,
            BodyTemplate  = body.BodyTemplate,
            UserToken     = GetUserToken()
        }, ct);
        return Ok();
    }

    /// <summary>PUT api/v1/admin-panel/notification-templates/{code}</summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        await _cqrs.SendAsync(new UpdateNotificationTemplateCommand
        {
            Code          = code,
            Name          = body.Name,
            TitleTemplate = body.TitleTemplate,
            BodyTemplate  = body.BodyTemplate,
            UserToken     = GetUserToken()
        }, ct);
        return NoContent();
    }

    /// <summary>PATCH api/v1/admin-panel/notification-templates/{code}/toggle</summary>
    [HttpPatch("{code}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        await _cqrs.SendAsync(new ToggleNotificationTemplateCommand
        {
            Code      = code,
            UserToken = GetUserToken()
        }, ct);
        return NoContent();
    }
}

// ─── Request models (BFF-side, mirror Notification module requests) ───────────

public sealed class CreateNotificationTemplateRequest
{
    public string              TemplateCode  { get; set; } = default!;
    public string              Name          { get; set; } = default!;
    public NotificationType    Type          { get; set; }
    public NotificationChannel Channel       { get; set; }
    public string              TitleTemplate { get; set; } = default!;
    public string              BodyTemplate  { get; set; } = default!;
}

public sealed class UpdateNotificationTemplateRequest
{
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
```

**Notes:**
- `ToggleNotificationTemplateCommand` must have a `Code` property. Check the existing command class — if it only has a `UserToken`, add `Code` to it (or check if it already exists).
- `_cqrs.SendAsync(query, ct)` — verify the exact method signature on `IAizenCQRSProcessor`. It might be `SendAsync<TResponse>(IQuery<TResponse> query, ct)` or similar. Follow the pattern used in other BFF controllers.

---

## Task 6: Clean up `AdminInactiveModulesController`

**File to modify:**
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminInactiveModulesController.cs`

Remove the two notification-template action methods — they are now handled by the real controller:

```csharp
// DELETE these two methods:

[HttpGet("notification-templates")]
public IActionResult GetNotificationTemplates() => NotImplementedEnvelope("Notification");

[HttpPost("notification-templates")]
public IActionResult CreateNotificationTemplate() => NotImplementedEnvelope("Notification");
```

Leave all other methods (payments, reports, analytics, files) as-is — those modules are still inactive.

---

## Verification Checklist

After applying all tasks, verify:

1. **Build** — `dotnet build` on the BFF project (`Aizen.Bff.AdminPanel`) produces zero errors.

2. **Route check** — With the BFF running locally, hit these endpoints and confirm they return data (or forward correctly to the module):
   - `GET /api/v1/admin-panel/messaging/conversations`
   - `GET /api/v1/admin-panel/notification-templates`
   - `GET /api/v1/admin-panel/notification-templates` should no longer return 501

3. **DI resolution** — Confirm no `InvalidOperationException` on startup about unresolved `IAdminMessagingBffRemoteCall` or `INotificationAdminBffRemoteCall`.

4. **No duplicate routes** — Ensure `AdminInactiveModulesController` no longer declares `GET /admin-panel/notification-templates` (removed in Task 6), otherwise ASP.NET route conflict will throw at startup.

5. **`ToggleNotificationTemplateCommand.Code`** — Verify the property exists on the command. If missing, add it.

---

## File Summary

| Action | File |
|---|---|
| CREATE | `Bff/.../Common/RemoteClients/IAdminMessagingBffRemoteCall.cs` |
| CREATE | `Bff/.../Controllers/V1/AdminMessagingController.cs` |
| CREATE | `Bff/.../Controllers/V1/AdminNotificationTemplatesController.cs` |
| MODIFY | `Bff/.../Application/DependencyInjection.cs` (add 2 registrations) |
| MODIFY | `Bff/.../Controllers/V1/AdminInactiveModulesController.cs` (remove 2 methods) |
