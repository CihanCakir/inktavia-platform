# Agent Execution Prompt — Notification Module v1

## Context

You are implementing the `Aizen.Modules.Notification` module for the Inktavia Marine OS platform.
This is a single-process operation-status module (NOT split into scheduler/worker/api — everything runs in one ASP.NET Core host).
All cross-module communication uses MassTransit integration events over RabbitMQ.
In-app real-time delivery uses SignalR. Push notifications use FCM.

## Module Location

```
Modules/Notification/src/
├── Aizen.Modules.Notification              ← ASP.NET Core host (Program.cs, controllers, hub)
├── Aizen.Modules.Notification.Abstraction  ← Enums, DTOs, interfaces, message contracts
├── Aizen.Modules.Notification.Application  ← CQRS handlers, consumers, dispatcher services
├── Aizen.Modules.Notification.Core         ← (reserved for future business rules)
├── Aizen.Modules.Notification.Domain       ← Entities
└── Aizen.Modules.Notification.Repository   ← EF Core DbContext, repos, seed data
```

## Execution Order

Execute the following steps **in order**. Do not skip ahead. Each step builds on the previous.

---

### Phase 1 — Foundation (PROMPT_A_NOTIFICATION_FOUNDATION.md)

**Step 1.1** — Remove `Class1.cs` from all 5 projects.

**Step 1.2** — Implement `Abstraction` project:
- Enums: `NotificationChannel`, `NotificationStatus`, `NotificationType`, `PushPlatform`
- DTOs: `NotificationDto`, `NotificationTemplateDto`
- Integration message: `NotificationSentMessage`
- Repository interfaces: `INotificationRepository`, `INotificationTemplateRepository`, `IUserDeviceTokenRepository`
- Service interfaces: `INotificationDispatcher`, `ITemplateInterpolator`

**Step 1.3** — Implement `Domain` project:
- `NotificationEntity` with `Create`, `MarkAsSent`, `MarkAsFailed`, `MarkAsRead`
- `NotificationTemplateEntity` with `Create`, `Update`, `SetActive`
- `UserDeviceTokenEntity` with `Create`, `Refresh`, `Deactivate`

**Step 1.4** — Implement `Repository` project:
- `NotificationDbContext` (schema: `notification`)
- 3 EF Core configurations (tables: `notifications`, `notification_templates`, `user_device_tokens`)
- 3 repository implementations
- `NotificationTemplateSeed` (idempotent — checks by `TemplateCode` before insert)
- `DependencyInjection.cs` with `AddNotificationRepository()` and `SeedNotificationAsync()`

**Verify:** `dotnet build` on all 3 projects — zero errors.

---

### Phase 2 — Application (PROMPT_B_NOTIFICATION_APPLICATION.md)

**Step 2.0** — Add `MessagingMessageSentMessage` to `Aizen.Modules.Messaging.Abstraction/Message/`.
Then edit `SendMessageCommandHandler.cs` to publish this message via `IAizenMessagePublisher.PublishAsync` after a message is persisted (skip if `IsInternalNote == true`).

**Step 2.1** — CQRS Commands:
- `SendNotificationCommand` + Handler (resolve template → interpolate → persist → dispatch)
- `MarkNotificationAsReadCommand` + Handler
- `BulkMarkAsReadCommand` + Handler
- `RegisterDeviceTokenCommand` + Handler

**Step 2.2** — CQRS Queries:
- `GetUserNotificationsQuery` + Handler
- `GetNotificationTemplatesQuery` + Handler

**Step 2.3** — Services:
- `TemplateInterpolator` — `{{variable}}` regex replacement
- `InAppNotificationDispatcher` — pushes via `IInAppNotificationPusher`
- `PushNotificationDispatcher` — loops device tokens, calls `IFcmSender`
- `FcmSenderStub` — logs only, returns stub message ID
- `CompositeNotificationDispatcher` — routes by `NotificationChannel` using keyed DI
- `InAppNotificationPayload` DTO
- `IInAppNotificationPusher` interface (bridge between Application and SignalR Hub)

**Step 2.4** — MassTransit Consumers (one file each):
| Consumer | Consumes | Notifies | Type |
|---|---|---|---|
| `ServiceRequestCreatedConsumer` | `ServiceRequestCreatedMessage` | Owner | `ServiceRequestCreated` |
| `ServiceRequestStatusChangedConsumer` | `ServiceRequestStatusChangedMessage` | Actor | `ServiceRequestStatusChanged` |
| `ServiceRequestOfferCreatedConsumer` | `ServiceRequestOfferCreatedMessage` | Owner | `OfferCreated` |
| `ServiceRequestOfferAcceptedConsumer` | `ServiceRequestOfferAcceptedMessage` | Provider | `OfferAccepted` |
| `ServiceRequestAssignmentCreatedConsumer` | `ServiceRequestAssignmentCreatedMessage` | Provider | `AssignmentCreated` |
| `ServiceRequestCompletionSubmittedConsumer` | `ServiceRequestCompletionSubmittedMessage` | Owner | `CompletionSubmitted` |
| `ServiceRequestDisputeOpenedConsumer` | `ServiceRequestDisputeOpenedMessage` | Owner + Provider | `DisputeOpened` |
| `MessagingMessageSentConsumer` | `MessagingMessageSentMessage` | All recipients | `NewMessageReceived` |

**Step 2.5** — `DependencyInjection.cs`:
```csharp
services.AddScoped<ITemplateInterpolator, TemplateInterpolator>();
services.AddScoped<IFcmSender, FcmSenderStub>();
services.AddKeyedScoped<INotificationDispatcher, InAppNotificationDispatcher>(NotificationChannel.InApp);
services.AddKeyedScoped<INotificationDispatcher, PushNotificationDispatcher>(NotificationChannel.Push);
services.AddScoped<INotificationDispatcher, CompositeNotificationDispatcher>();
```

**Verify:** `dotnet build Aizen.Modules.Notification.Application` — zero errors.

---

### Phase 3 — API + Hub + Program.cs (PROMPT_C_NOTIFICATION_API.md)

**Step 3.1** — Hub:
- `NotificationHub : Hub` at `/hubs/notification`
  - `JoinUserGroup(userId)` → `Groups.AddToGroupAsync($"user:{userId}")`
  - `LeaveUserGroup(userId)`
- `NotificationHubPusher : IInAppNotificationPusher`
  - Injects `IHubContext<NotificationHub>`
  - `PushToUserAsync` → `SendAsync("NotificationReceived", payload)`

**Step 3.2** — Controllers:
- `NotificationsController` at `/api/v1/notification/notifications`:
  - `GET /` → `GetUserNotificationsQuery`
  - `PATCH /{id}/read` → `MarkNotificationAsReadCommand`
  - `POST /mark-all-read` → `BulkMarkAsReadCommand`
  - `POST /device-token` → `RegisterDeviceTokenCommand`
- `NotificationTemplatesController` at `/api/v1/notification/admin/notification-templates`:
  - `GET /` → `GetNotificationTemplatesQuery`
  - `GET /{code}` → direct repo call
  - `POST /` → `NotificationTemplateEntity.Create` + `AddAsync`
  - `PUT /{code}` → `entity.Update` + `UpdateAsync`
  - `PATCH /{code}/toggle` → `entity.SetActive(!IsActive)` + `UpdateAsync`

**Step 3.3** — Replace `Program.cs` (currently weather forecast template) with:
```csharp
AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Notification",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args)
```
Register: UnitOfWork, Repository, ApplicationServices, MassTransit (all consumers), SignalR, MediatR.
Map: `MapControllers()`, `MapHub<NotificationHub>("/hubs/notification")`.
Seed: `await app.SeedNotificationAsync()`.

**Step 3.4** — BFF integration:
- Add `INotificationAdminBffRemoteCall` (Refit interface) to AdminPanel BFF
- Add `NotificationTemplatesAdminController` at `/api/v1/admin-panel/notification-templates`
- Register Refit client in AdminPanel `Program.cs` pointing to `Services:Notification`

**Verify:**
```bash
dotnet build Aizen.Modules.Notification
curl http://localhost:{port}/api/v1/admin-panel/notification-templates
# Should return HTTP 200 with template list (was 404 before)
```

---

### Phase 4 — Migration + Smoke Test

```bash
# Generate EF Core migration
dotnet ef migrations add InitialCreate \
  --project Aizen.Modules.Notification.Repository \
  --startup-project Aizen.Modules.Notification \
  --context NotificationDbContext \
  --output-dir Persistence/Migrations

# Run the service
dotnet run --project Aizen.Modules.Notification

# Verify:
# 1. Migration runs against `notification` schema
# 2. Seed templates inserted (check notification.notification_templates table)
# 3. GET /api/v1/notification/admin/notification-templates → 200 with 12 templates
# 4. SignalR hub reachable at wss://host/hubs/notification
# 5. Publish a ServiceRequestCreated event → consumer fires → notification created
```

---

## Key Constraints

1. **Single process** — All in `Aizen.Modules.Notification`. No separate Worker or Scheduler projects.
2. **Event-only inbound** — Other modules communicate to Notification ONLY via MassTransit `PublishAsync`. No direct HTTP calls between modules.
3. **Channel routing** — One `NotificationEntity` per recipient × channel. `InApp` is always created; `Push` is added when user has registered device tokens.
4. **Template required** — `SendNotificationCommandHandler` silently skips (logs a warning) if no active template found. Never throws.
5. **Idempotent seed** — `NotificationTemplateSeed` checks by `TemplateCode` uniqueness before inserting.
6. **FCM stub in dev** — `FcmSenderStub` is the default. Swap with real FCM HTTP v1 implementation for production by registering a different `IFcmSender`.
7. **No circular refs** — Application layer references `IInAppNotificationPusher` (interface only). The concrete `NotificationHubPusher` lives in the host project.
