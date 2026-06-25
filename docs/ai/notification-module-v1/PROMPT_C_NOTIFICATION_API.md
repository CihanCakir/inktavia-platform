# PROMPT C — Notification Module: API + SignalR Hub + Program.cs + BFF

## Scope

Implement the `Aizen.Modules.Notification` host project covering:

1. **Program.cs** — Full `AizenApplicationBuilder` bootstrap matching the Identity/ServiceRequest pattern
2. **NotificationHub** — SignalR hub + `IInAppNotificationPusher` implementation
3. **NotificationsController** — User-facing REST endpoints
4. **NotificationTemplatesController** — Admin CRUD for templates
5. **BFF Remote Call** — `INotificationAdminBffRemoteCall` interface + Refit extension for the AdminPanel BFF
6. **BFF Controller** — AdminPanel BFF notification-templates endpoints

---

## Architecture Rules

- Follow `Aizen.Modules.Identity/Program.cs` and `Aizen.Modules.ServiceRequest` exactly.
- Use `AizenApplicationBuilder.CreateBuilder` with `AppType.Operation` + `TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }`.
- Register all MassTransit consumers inside `AddMassTransit(...)`.
- Register `AddAizenUnitOfWork<NotificationDbContext>` with `UseMigration = true`.
- Controllers use standard ASP.NET Core attribute routing under `/api/v1/notification`.
- SignalR Hub lives at `/hubs/notification`.
- `IInAppNotificationPusher` is the bridge between Application layer dispatchers and the Hub's `IHubContext`.

---

## STEP 1 — NotificationHub (SignalR)

**File:** `Hubs/NotificationHub.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.Notification.Hubs;

/// <summary>
/// Client connects → calls JoinUserGroup(userId) → receives real-time notifications.
/// Group pattern: user:{userId}
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
        => _logger = logger;

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation(
            "Connection {ConnId} joined user group user:{UserId}", Context.ConnectionId, userId);
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("NotificationHub connected: {ConnId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug(
            "NotificationHub disconnected: {ConnId}, Error: {Error}",
            Context.ConnectionId, exception?.Message);
        return base.OnDisconnectedAsync(exception);
    }
}
```

**File:** `Hubs/NotificationHubPusher.cs`

This implements `IInAppNotificationPusher` (defined in Application layer) using `IHubContext<NotificationHub>`.

```csharp
using Aizen.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.Notification.Hubs;

/// <summary>
/// Implements IInAppNotificationPusher by pushing to SignalR groups.
/// Registered as the concrete implementation so Application layer stays decoupled from ASP.NET Core.
/// </summary>
public sealed class NotificationHubPusher : IInAppNotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubPusher(IHubContext<NotificationHub> hubContext)
        => _hubContext = hubContext;

    public Task PushToUserAsync(long userId, InAppNotificationPayload payload, CancellationToken ct)
        => _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("NotificationReceived", payload, ct);
}
```

---

## STEP 2 — NotificationsController (User-facing)

**File:** `Controllers/NotificationsController.cs`
```csharp
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;
using Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;
using Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;
using Aizen.Modules.Notification.Application.Query.GetUserNotifications;
using Aizen.Modules.Notification.Abstraction.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender             _sender;
    private readonly IAizenInfoAccessor  _info;

    public NotificationsController(ISender sender, IAizenInfoAccessor info)
    {
        _sender = sender;
        _info   = info;
    }

    /// <summary>
    /// GET /api/v1/notification/notifications
    /// Returns paginated notifications for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(
            new GetUserNotificationsQuery { UserId = userId, Skip = skip, Take = take }, ct);
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/v1/notification/notifications/{id}/read
    /// Marks a single notification as read.
    /// </summary>
    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var ok = await _sender.Send(
            new MarkNotificationAsReadCommand { NotificationId = id, RequestingUserId = userId }, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// POST /api/v1/notification/notifications/mark-all-read
    /// Marks all notifications as read for the authenticated user.
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        await _sender.Send(new BulkMarkAsReadCommand { UserId = userId }, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/notification/notifications/device-token
    /// Registers or refreshes an FCM/APNS device token for push notifications.
    /// </summary>
    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenRequest body,
        CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        await _sender.Send(new RegisterDeviceTokenCommand
        {
            UserId      = userId,
            DeviceToken = body.DeviceToken,
            Platform    = body.Platform,
        }, ct);
        return NoContent();
    }
}

public sealed class RegisterDeviceTokenRequest
{
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
```

---

## STEP 3 — NotificationTemplatesController (Admin)

**File:** `Controllers/NotificationTemplatesController.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;
using Aizen.Modules.Notification.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/admin/notification-templates")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class NotificationTemplatesController : ControllerBase
{
    private readonly ISender                         _sender;
    private readonly INotificationTemplateRepository _repository;

    public NotificationTemplatesController(
        ISender sender,
        INotificationTemplateRepository repository)
    {
        _sender     = sender;
        _repository = repository;
    }

    /// <summary>
    /// GET /api/v1/notification/admin/notification-templates
    /// Lists all notification templates (active and inactive).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetNotificationTemplatesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/notification/admin/notification-templates/{code}
    /// Gets a single template by TemplateCode.
    /// </summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var template = await _repository.GetByCodeAsync(code, ct);
        return template is null ? NotFound() : Ok(template);
    }

    /// <summary>
    /// POST /api/v1/notification/admin/notification-templates
    /// Creates a new notification template.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        var existing = await _repository.GetByCodeAsync(body.TemplateCode, ct);
        if (existing is not null)
            return Conflict($"Template '{body.TemplateCode}' already exists.");

        var entity = NotificationTemplateEntity.Create(
            body.TemplateCode, body.Name, body.Type, body.Channel,
            body.TitleTemplate, body.BodyTemplate);

        await _repository.AddAsync(entity, ct);
        return CreatedAtAction(nameof(GetByCode), new { code = entity.TemplateCode }, entity);
    }

    /// <summary>
    /// PUT /api/v1/notification/admin/notification-templates/{code}
    /// Updates name, title/body templates of an existing template.
    /// </summary>
    [HttpPut("{code}")]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        var entity = await _repository.GetByCodeAsync(code, ct);
        if (entity is null) return NotFound();

        entity.Update(body.Name, body.TitleTemplate, body.BodyTemplate);
        await _repository.UpdateAsync(entity, ct);
        return NoContent();
    }

    /// <summary>
    /// PATCH /api/v1/notification/admin/notification-templates/{code}/toggle
    /// Activates or deactivates a template.
    /// </summary>
    [HttpPatch("{code}/toggle")]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        var entity = await _repository.GetByCodeAsync(code, ct);
        if (entity is null) return NotFound();

        entity.SetActive(!entity.IsActive);
        await _repository.UpdateAsync(entity, ct);
        return NoContent();
    }
}

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

---

## STEP 4 — Program.cs

**File:** `Program.cs`
```csharp
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Modules.Notification.Application;
using Aizen.Modules.Notification.Application.Consumers.Messaging;
using Aizen.Modules.Notification.Application.Consumers.ServiceRequest;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Hubs;
using Aizen.Modules.Notification.Repository;
using MassTransit;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Notification",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<NotificationDbContext>(builder.Configuration, "Notification", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.Notification.Repository";
});

// ── Domain / Application / Repository ─────────────────────────────────────────
builder.Services.AddNotificationRepository();
builder.Services.AddNotificationApplicationServices();

// ── MassTransit Consumers ──────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // ServiceRequest consumers
    x.AddConsumer<ServiceRequestCreatedConsumer>();
    x.AddConsumer<ServiceRequestStatusChangedConsumer>();
    x.AddConsumer<ServiceRequestOfferCreatedConsumer>();
    x.AddConsumer<ServiceRequestOfferAcceptedConsumer>();
    x.AddConsumer<ServiceRequestAssignmentCreatedConsumer>();
    x.AddConsumer<ServiceRequestCompletionSubmittedConsumer>();
    x.AddConsumer<ServiceRequestDisputeOpenedConsumer>();

    // Messaging consumers
    x.AddConsumer<MessagingMessageSentConsumer>();

    // CargoDry consumers (add when CargoDry module abstraction is ready)
    // x.AddConsumer<CargoDryKitExpiringConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── SignalR ────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
// Wire IInAppNotificationPusher → NotificationHubPusher
builder.Services.AddScoped<IInAppNotificationPusher, NotificationHubPusher>();

// ── MediatR (Handlers) ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Aizen.Modules.Notification.Application.DependencyInjection).Assembly));

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

// ── Seed ───────────────────────────────────────────────────────────────────────
await app.SeedNotificationAsync();

app.Run();
```

---

## STEP 5 — BFF Integration (AdminPanel)

Add the Notification module's template management to the existing AdminPanel BFF.

### 5.1 Remote Call Interface

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/RemoteCalls/Notification/INotificationAdminBffRemoteCall.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Dto;
using Refit;

namespace Aizen.Bff.AdminPanel.RemoteCalls.Notification;

public interface INotificationAdminBffRemoteCall
{
    [Get("/api/v1/notification/admin/notification-templates")]
    Task<List<NotificationTemplateDto>> GetNotificationTemplatesAsync(
        CancellationToken ct = default);

    [Get("/api/v1/notification/admin/notification-templates/{code}")]
    Task<NotificationTemplateDto> GetNotificationTemplateByCodeAsync(
        string code, CancellationToken ct = default);

    [Post("/api/v1/notification/admin/notification-templates")]
    Task CreateNotificationTemplateAsync(
        [Body] CreateNotificationTemplateRemoteRequest body,
        CancellationToken ct = default);

    [Put("/api/v1/notification/admin/notification-templates/{code}")]
    Task UpdateNotificationTemplateAsync(
        string code,
        [Body] UpdateNotificationTemplateRemoteRequest body,
        CancellationToken ct = default);

    [Patch("/api/v1/notification/admin/notification-templates/{code}/toggle")]
    Task ToggleNotificationTemplateAsync(
        string code, CancellationToken ct = default);
}

public sealed class CreateNotificationTemplateRemoteRequest
{
    public string                                      TemplateCode  { get; set; } = default!;
    public string                                      Name          { get; set; } = default!;
    public Aizen.Modules.Notification.Abstraction.Enum.NotificationType    Type          { get; set; }
    public Aizen.Modules.Notification.Abstraction.Enum.NotificationChannel Channel       { get; set; }
    public string                                      TitleTemplate { get; set; } = default!;
    public string                                      BodyTemplate  { get; set; } = default!;
}

public sealed class UpdateNotificationTemplateRemoteRequest
{
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
```

### 5.2 BFF Controller

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/NotificationTemplatesAdminController.cs`
```csharp
using Aizen.Bff.AdminPanel.RemoteCalls.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers;

[ApiController]
[Route("api/v1/admin-panel/notification-templates")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class NotificationTemplatesAdminController : ControllerBase
{
    private readonly INotificationAdminBffRemoteCall _remote;

    public NotificationTemplatesAdminController(INotificationAdminBffRemoteCall remote)
        => _remote = remote;

    /// <summary>
    /// GET /api/v1/admin-panel/notification-templates
    /// Used by the Admin Panel frontend to list and manage templates.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _remote.GetNotificationTemplatesAsync(ct);
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await _remote.GetNotificationTemplateByCodeAsync(code, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRemoteRequest body,
        CancellationToken ct)
    {
        await _remote.CreateNotificationTemplateAsync(body, ct);
        return Created();
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateNotificationTemplateRemoteRequest body,
        CancellationToken ct)
    {
        await _remote.UpdateNotificationTemplateAsync(code, body, ct);
        return NoContent();
    }

    [HttpPatch("{code}/toggle")]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        await _remote.ToggleNotificationTemplateAsync(code, ct);
        return NoContent();
    }
}
```

### 5.3 Register BFF Remote Call in AdminPanel Program.cs

In `Aizen.Bff.AdminPanel/Program.cs`, add:
```csharp
// Add alongside other Refit remote calls
builder.Services
    .AddRefitClient<INotificationAdminBffRemoteCall>()
    .ConfigureHttpClient(c =>
        c.BaseAddress = new Uri(builder.Configuration["Services:Notification"]!));
```

In `appsettings.json`:
```json
{
  "Services": {
    "Notification": "http://aizen-notification:8080"
  }
}
```

---

## STEP 6 — csproj References for Host Project

### `Aizen.Modules.Notification.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit.RabbitMQ"  Version="8.*" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="9.*" />
    <PackageReference Include="MediatR" Version="12.*" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.*" />
    <PackageReference Include="Aizen.Core.Starter" Version="*" />
    <PackageReference Include="Aizen.Core.Infrastructure.UnitOfWork" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Aizen.Modules.Notification.Application\Aizen.Modules.Notification.Application.csproj" />
    <ProjectReference Include="..\Aizen.Modules.Notification.Repository\Aizen.Modules.Notification.Repository.csproj" />
  </ItemGroup>
</Project>
```

---

## STEP 7 — appsettings.json Template

**File:** `appsettings.json`
```json
{
  "ConnectionStrings": {
    "Notification": "Host=postgres;Database=inktavia;Username=inktavia;Password=secret;Search Path=notification"
  },
  "RabbitMq": {
    "Host":     "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Authority": "http://keycloak:8080/realms/inktavia"
  },
  "Fcm": {
    "ProjectId":       "inktavia-marine",
    "ServiceAccountKeyPath": "/run/secrets/fcm-service-account.json"
  }
}
```

---

## STEP 8 — SignalR Client Usage (Frontend Reference)

Frontend notification bell integration pattern:
```typescript
// useNotificationHub.ts
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notification', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build()

await connection.start()
await connection.invoke('JoinUserGroup', String(userId))

connection.on('NotificationReceived', (payload: InAppNotificationPayload) => {
  // payload: { notificationId, type, title, body, metadataJson, createdAt }
  // Add to notification bell badge count
  incrementUnreadCount()
  // Show toast
  showToast(payload.title, payload.body)
  // Deep-link routing from metadataJson
  // e.g. {"serviceRequestId": 123} → navigate to /service-requests/123
})
```

---

## Verification Checklist

- [ ] `Program.cs` compiles with `AizenApplicationBuilder.CreateBuilder` — no vanilla weather forecast template remaining
- [ ] `NotificationHub` accessible at `wss://{host}/hubs/notification`
- [ ] `NotificationHubPusher` injects `IHubContext<NotificationHub>` correctly
- [ ] `NotificationsController` GET `/notifications` returns paginated list for authenticated user
- [ ] `NotificationTemplatesController` all 5 endpoints compile and respond
- [ ] BFF `GET /api/v1/admin-panel/notification-templates` returns HTTP 200 (was 404 before this prompt)
- [ ] MassTransit consumers registered and bound to correct queues
- [ ] `SeedNotificationAsync()` runs migrations then seeds templates without duplicates
- [ ] `appsettings.json` includes `Services:Notification` URL for BFF
- [ ] Docker-compose or Helm chart updated to include the Notification service
