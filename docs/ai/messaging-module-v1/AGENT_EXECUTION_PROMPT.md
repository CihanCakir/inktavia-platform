# AGENT EXECUTION PROMPT — Messaging Module v1
# `Aizen.Modules.Messaging` — Full Implementation

## Mission

You are implementing the **`Aizen.Modules.Messaging`** module for Inktavia Marine OS — the platform-wide communication backbone. This module handles all messaging across ServiceRequest, CommerceOrder, CargoDrySupport, VenueInquiry, and DirectMessage contexts.

The module is already scaffolded (projects exist in solution). You are adding all implementation files.

Execute the 5 prompt files below **strictly in order**. Each phase has a gate — do not proceed until it passes.

---

## Project Structure (confirmed)

```
Modules/Messaging/
├── src/
│   ├── Aizen.Modules.Messaging/                  ← API (PROMPT D, PROMPT E)
│   │   ├── Hubs/MessagingHub.cs
│   │   ├── Realtime/MessagingRealtimeDomainRegistrar.cs
│   │   ├── Controller/V1/Conversations/
│   │   ├── Controller/V1/Moderation/
│   │   └── Program.cs
│   ├── Aizen.Modules.Messaging.Abstraction/      ← Enums, Request/Response DTOs (PROMPT A)
│   │   └── Enum/
│   ├── Aizen.Modules.Messaging.Domain/           ← Entities, interfaces (PROMPT A)
│   │   ├── Entities/Conversation/
│   │   └── Interface/Repository/
│   ├── Aizen.Modules.Messaging.Application/      ← Commands, Queries, Services (PROMPT B)
│   │   ├── Command/
│   │   ├── Query/
│   │   ├── Services/
│   │   └── Realtime/
│   └── Aizen.Modules.Messaging.Repository/       ← DbContext, Repos, Seed (PROMPT C, E)
│       ├── Persistence/
│       ├── Repositories/
│       ├── Seed/
│       └── DependencyInjection.cs
```

---

## Architecture Contract

> These rules are non-negotiable. Any deviation will cause compile errors.

| Rule | Value |
|------|-------|
| Entity ID type | `long` — NEVER `Guid` |
| Entity base class | `AizenEntityWithAudit` |
| Command base | `AizenCommand<TResponse>` |
| Query base | `AizenQuery<TResponse>` |
| CommandHandler base | `AizenCommandHandler<TCmd, TResp>` |
| QueryHandler base | `AizenQueryHandler<TQuery, TResp>` |
| Controller base | `AizenWebApiController` |
| Return type | `AizenApiResponse<T?>` via `SetResponse(result)` |
| CQRS processor | `IAizenCQRSProcessor.ProcessAsync<T>(command, ct)` |
| Current user | `_info.UserInfoAccessor.UserInfo.UserId` (returns `long`) |
| Hub base | `DomainHubBase` |
| Domain registrar | `IRealtimeDomainRegistrar` |
| Realtime publisher | `IRealtimePublisher` |
| DB schema | `"messaging"` |
| Table names | lowercase snake_case |
| DateTime | Always UTC, `NormalizeDateTimeProperties()` in DbContext |
| Attribute | `[DocumentationInfo("...", "...")]` on every class |

---

## Execution Phases

### ► PHASE 1 — Domain + Abstraction
**File:** `PROMPT_A_MESSAGING_DOMAIN.md`

What to implement:
- 6 enums in `Aizen.Modules.Messaging.Abstraction/Enum/`
- 4 domain entities in `Aizen.Modules.Messaging.Domain/Entities/Conversation/`
- 2 repository interfaces in `Domain/Interface/Repository/`
- `IMessageContentPolicy` interface + `ContentPolicyResult` record in `Domain/Interface/`

**Gate:** `dotnet build Aizen.Modules.Messaging.Domain` — 0 errors.

---

### ► PHASE 2 — Application Layer
**File:** `PROMPT_B_MESSAGING_APPLICATION.md`

What to implement:
- `MessageContentPolicyService : IMessageContentPolicy` with 5 moderation scenarios
- `MessagingRealtimePublisher` using `IRealtimePublisher`
- `MessagingRealtimeEventDto` record
- 5 Commands + handlers + validators: `CreateConversation`, `SendMessage`, `MarkConversationRead`, `FlagConversation`, `ModerateMessage`
- 4 Queries + handlers: `GetConversationList`, `GetConversationDetail`, `GetConversationByContext`, `GetModerationQueue`
- All Request/Response DTO records in Abstraction project

**Gate:** `dotnet build Aizen.Modules.Messaging.Application` — 0 errors.

---

### ► PHASE 3 — Repository Layer + Migration
**File:** `PROMPT_C_MESSAGING_REPOSITORY.md`

What to implement:
- `MessagingDbContext : AizenDbContext` with `HasDefaultSchema("messaging")`
- 4 EF Core `IEntityTypeConfiguration<T>` classes
- `ConversationRepository` + `ConversationMessageRepository`
- `MessagingDesignTimeFactory`
- `DependencyInjection.cs` with `AddMessagingRepository()`, `AddMessagingServices()`, `SeedMessagingAsync()`

Run migration:
```bash
dotnet ef migrations add InitialMessaging \
  --project Aizen.Modules.Messaging.Repository \
  --startup-project Aizen.Modules.Messaging
```

**Gate:**
```sql
SELECT COUNT(*) FROM information_schema.tables 
WHERE table_schema = 'messaging';
-- Expected: 4 (conversations, conversation_participants, conversation_messages, message_attachments)
```
Also verify UNIQUE index on `(context_type, context_id)` exists.

---

### ► PHASE 4 — API Layer
**File:** `PROMPT_D_MESSAGING_API.md`

What to implement:
- `MessagingHub : DomainHubBase` with group management methods
- `MessagingHubEvents` static constants class
- `MessagingRealtimeDomainRegistrar : IRealtimeDomainRegistrar`
- `ConversationsController` at `api/v1/conversations`
- `ConversationMessagesController` at `api/v1/conversations/{id}/messages`
- `ConversationModerationController` at `api/v1/moderation`
- Complete `Program.cs`

**Gate:**
```bash
dotnet build Aizen.Modules.Messaging
dotnet run --project Aizen.Modules.Messaging
# Navigate to https://localhost:{port}/swagger
# Verify 3 tag groups visible: "Messaging - Conversations", "Messaging - Messages", "Messaging - Moderation"
# Verify hub route: GET /hubs/messaging → 200 or 101 (WebSocket upgrade)
```

---

### ► PHASE 5 — Seed Data
**File:** `PROMPT_E_MESSAGING_SEED.md`

What to implement:
- `MessagingMockDataSeeder` class with 3 SR conversations and 12 messages
- Register in `DependencyInjection.cs`
- Update `SeedMessagingAsync` to call seeder only in Development

**Gate (SQL):**
```sql
SELECT COUNT(*) FROM messaging.conversations;          -- 3
SELECT COUNT(*) FROM messaging.conversation_messages;  -- 12
SELECT COUNT(*) FROM messaging.conversation_messages 
WHERE moderation_status = 2;                           -- 1 (PendingReview)
SELECT COUNT(*) FROM messaging.conversation_messages 
WHERE is_internal_note = true;                         -- 1 (Admin note on SR 9004)
```

---

## Content Moderation Contract

The `MessageContentPolicyService` must implement exactly these 5 checks in order:

| Priority | Check | Result | Policy Code |
|----------|-------|--------|-------------|
| 1 | Message exceeds 4000 chars | Block | `CONTENT_TOO_LONG` |
| 2 | Off-platform keywords (iban, swift, whatsapp, telegram, banka hesabı) | Block | `OFF_PLATFORM_SOLICITATION` |
| 3 | Phone number or email leak via regex | Review | `REVIEW_REQUIRED` |
| 4 | More than 2 URLs in content | Review | `REVIEW_REQUIRED` |
| 5 | Same content sent 3+ times in 5 min (via Redis cache) | Block | `SPAM_DETECTED` |
| — | All checks pass | Allow | — |

Cache key for spam: `"messaging:spam:{userId}:{contentHash}"` → `IncrementAsync` with 5-minute expiry.

---

## Data Contracts

### Hub Group Names
| Purpose | Group |
|---------|-------|
| Conversation room | `messaging:conv:{conversationId}` |
| User personal feed | `user:{userId}` |
| Admin moderation | `admin:messaging-moderation` |

### Hub Events (server → client)
| Event | Payload | Trigger |
|-------|---------|---------|
| `MessageReceived` | `MessagingRealtimeEventDto` | After `SendMessage` succeeds |
| `ConversationCreated` | `MessagingRealtimeEventDto` | After `CreateConversation` |
| `ConversationStatusChanged` | `MessagingRealtimeEventDto` | After status mutation |
| `MessageModerated` | `MessagingRealtimeEventDto` | After `ModerateMessage` |
| `UnreadCountUpdated` | `MessagingRealtimeEventDto` | After admin marks read |

### Hub Route
```
SignalR endpoint: /hubs/messaging
Domain key: "messaging"
Hub class: MessagingHub
```

---

## Known Pitfalls — Read Before Starting

1. **UNIQUE index enforcement** — `(context_type, context_id)` is UNIQUE. `CreateConversationCommand` must call `GetByContextAsync` first and return the existing conversation if found (upsert pattern, not duplicate).

2. **`InternalNote` visibility** — Messages with `IsInternalNote = true` must NEVER be included in responses to non-admin callers. The query handler `GetConversationDetailQuery` must filter these based on caller role.

3. **DateTime UTC** — All `DateTimeOffset.UtcNow` in entities, all EF properties normalized via `NormalizeDateTimeProperties()`. Do not use `DateTime.Now`.

4. **Hub `DomainName` must match** — `MessagingHub.DomainName` property must return `"messaging"` — the same string passed to `AddDomainHub<MessagingHub>("messaging")` in `Program.cs`.

5. **Participant `ConversationId` in seed** — Participants must be added after `SaveChangesAsync()` so `conv.Id` is populated (EF generates the `long` PK on insert). See PROMPT_E for the correct two-step seed pattern.

6. **`IWebHostEnvironment`** — Available in the seeder scope because `AizenApplicationBuilder` registers it. If not available, inject `IConfiguration` and check `ASPNETCORE_ENVIRONMENT`.

7. **Redis for spam detection** — `IAizenCache` must be registered (`AddAizenCache` in `Program.cs`). If Redis is unavailable in dev, catch `RedisConnectionException` and log a warning rather than blocking the message.

---

## Rollback Plan

| Phase fails | Rollback action |
|-------------|----------------|
| Phase 1 (Domain) | Delete entity files, no DB changes |
| Phase 2 (Application) | Delete handler/command files, no DB changes |
| Phase 3 (Repository) | `dotnet ef migrations remove`, drop `messaging` schema: `DROP SCHEMA messaging CASCADE;` |
| Phase 4 (API) | Remove hub/controller files, revert `Program.cs` to stub |
| Phase 5 (Seed) | `DELETE FROM messaging.conversation_messages; DELETE FROM messaging.conversation_participants; DELETE FROM messaging.conversations;` |

---

## Definition of Done

- [ ] `dotnet build` on all 5 projects — 0 errors, 0 warnings
- [ ] Migration applied — 4 tables visible in `messaging` schema
- [ ] UNIQUE index verified on `(context_type, context_id)`
- [ ] Swagger: 3 tag groups, all endpoints documented
- [ ] Hub endpoint `/hubs/messaging` responds to WebSocket upgrade
- [ ] Seed: 3 conversations, 12 messages in DB
- [ ] Moderation queue endpoint returns 1 pending-review message
- [ ] `GetConversationByContext` for `contextType=1, contextId=9001` returns conv1
- [ ] Second call to `CreateConversation` for same context returns existing conversation — not a new one
- [ ] `InternalNote` message NOT visible when caller is not Admin
