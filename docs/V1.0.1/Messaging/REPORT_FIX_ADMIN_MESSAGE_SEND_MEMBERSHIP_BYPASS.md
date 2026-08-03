# REPORT — admin message-send membership bypass (Messaging module)

> Closes the W2 Part E gap: an admin can now post (especially **internal notes**) into **any** conversation
> without being a pre-existing participant. **Messaging module only** — no controller/BFF/FE change.
> Builds clean; deployed to the running `messaging-api`.

## Files changed

| File | Change |
|------|--------|
| `Application/Command/SendMessage/SendMessageCommandHandler.cs` | admin-role bypass + synthesized admin sender |
| `Repository/Seed/MessagingMockDataSeeder.cs` | dropped the moot placeholder `Admin` participant (`UserId=1`) from conv #9001 + #9004 (optional cleanup) |

## The handler change

Before, the sender was resolved strictly from membership and threw otherwise:
```csharp
var participant = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId)
    ?? throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");
// used participant.DisplayName / participant.Role
```

After:
```csharp
var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
var isAdmin       = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;

var participant   = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId);
if (participant is null && !isAdmin)
    throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");

var senderDisplayName = participant?.DisplayName ?? "Admin";
var senderRole        = participant?.Role ?? MessagingParticipantRole.Admin;
```
`senderDisplayName` / `senderRole` / `currentUserId` are then used for **both** the created message
(`ConversationMessageEntity.Create`) and the `MessagingMessageSentMessage` bus publish.

### Decisions

- **Role source — the important correction.** The fix reads `HttpContext.User.IsInRole("Admin")` (via an injected
  `IHttpContextAccessor`), **not** `UserInfo.Roles`. The admin BFF calls the module with a Keycloak **service
  token + `X-Aizen-User-Id` assertion**, and `AizenUserInfoMiddleware.TryAcceptBffAssertion` hard-codes
  `Roles = Array.Empty<string>()` on that path — so `UserInfo.Roles` is **always empty** for BFF-forwarded admin
  calls (the spec's `UserInfo.Roles.Contains("Admin")` silently never matched — send kept throwing). The
  authenticated **service principal** still carries the `Admin` role, which is exactly what `[Authorize(Roles=
  "Admin")]` checks — so `User.IsInRole("Admin")` is "the same source" the spec intended, and mirrors the
  `ConversationsController` / `ServiceRequestMessageController` pattern. `IHttpContextAccessor` is injected into
  the handler (already DI-registered; `Aizen.Modules.Messaging.Application` resolves the type transitively, as
  `Aizen.Modules.Identity.Application` already does) — no controller/BFF/FE change.
- **Synthesized sender is NOT persisted as a participant.** Messages store `SenderUserId` / `SenderName` /
  `SenderRole` independently of participant rows, so the admin posts with `SenderRole = Admin` and name `"Admin"`
  without joining the conversation — participant lists and unread counts stay correct. (`AizenUserInfo` exposes
  only `UserId` / `PhoneNumber` / `Roles` — no display name — so the fallback name is the literal `"Admin"`.)
- **Non-admin non-participant is still rejected** — the throw now guards on `participant is null && !isAdmin`,
  so a non-admin non-member hits exactly the same `UnauthorizedAccessException` as before.
- **Internal-note gating unchanged.** The handler still skips LLM analysis and the participant integration-event
  when `IsInternalNote` (both gated by `!request.IsInternalNote`), and the realtime `PublishMessageSentAsync` is
  gated by `!IsInternalNote` (added in the prior W2 pass). So an admin **internal** note is created + visible to
  admins but produces **no** participant-facing side-effect; an admin **non-internal** message posts visibly as
  `Admin` (legitimate intervention) and broadcasts/notifies participants normally.

### Seed cleanup

The placeholder `Admin` participant (`UserId=1`) on conv #9001 and #9004 was removed — it never matched a real
Keycloak admin subject and is now unnecessary. The seeded admin **message** rows (e.g. the internal note in
#9004) are unchanged (messages are independent of participant rows). Cleanup applies to fresh seeds only; it does
not alter already-seeded DBs.

## Build / scope

- `dotnet build` of `Aizen.Modules.Messaging.Application` + `Aizen.Modules.Messaging` (web, pulls in the seeder):
  **0 errors**.
- Changes: `SendMessageCommandHandler` (bypass + injected `IHttpContextAccessor`) + the seeder. No controller /
  BFF / FE / migration / csproj change. Participant sends (real owner/provider) hit the unchanged
  `participant != null` path and behave exactly as before. Flag/moderate/list/detail untouched.
- Deployed: `messaging-api` image rebuilt and container recreated (running the new handler).

## Verification — on-screen, CONFIRMED

Fresh admin session on `/app/messages`, conversation **#9010** ("Navigation Electronics Calibration", Elara Kane
⇄ Daria Solano — **no admin participant**):

1. **Admin internal note (non-participant) → posts.** Toggled INTERNAL NOTE, sent → **no "Failed to send"**; the
   note renders as an **"INTERNAL ADMIN NOTE"** bubble attributed to **Admin**. DB (`conversation_messages`,
   conv id 3): new row `SenderRole=Admin`, `SenderName="Admin"`, `IsInternalNote=true`. **Admin was NOT added as
   a participant** — `conversation_participants` for conv 3 still lists only Elara Kane (Owner) + Daria Solano
   (Provider). ✅
2. **Internal note NOT delivered to participants (SAFETY).** The participant read path
   (`GetConversationByContextQuery`, filter `!IsInternalNote`) for conv 3 returns msgs 9/10/11/12/34 — the
   internal note (33) is **absent**; the non-internal admin message (34) **is** present. ✅
3. **Non-internal admin message → visible intervention.** Toggled INTERNAL NOTE off, sent → posts as a normal
   bubble attributed to **Admin**, and the conversation jumped to the top of the list with an **Unread** badge
   (it went through the normal participant delivery + integration-event path). DB: `SenderRole=Admin`,
   `IsInternalNote=false`. ✅
4. **Non-admin non-participant still rejected** — the `participant == null && !isAdmin` throw is unchanged for
   non-admins (verified in code; not drivable from the admin UI without a non-admin token). ✅
5. **Regression** — participant path (`participant != null`) unchanged; flag/moderate unaffected. ✅

## Notes

- **First attempt failed** with `UserInfo.Roles.Contains("Admin")` (spec's snippet): the BFF assertion path
  zeroes `UserInfo.Roles`, so `isAdmin` was always false and send still threw. Switched to
  `HttpContext.User.IsInRole("Admin")` (injected `IHttpContextAccessor`) — the source `[Authorize(Roles=
  "Admin")]` actually reads — and both admin sends then succeeded. See the role-source note above.
- The synthesized admin messages persist with `SenderUserId = 0` (the BFF assertion did not carry a non-zero
  admin user id in this flow); `SenderName`/`SenderRole` are correctly `Admin`/`Admin`, which is what renders.
  Cosmetic; call it out if a real admin user id on the message is later required.
- Admin display name is the literal `"Admin"` (no name field on `AizenUserInfo`); a richer label would need a
  name claim on the user-info pipeline.
