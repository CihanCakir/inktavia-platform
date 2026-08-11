# BE_WC3c — retire the legacy SR chat READ-endpoints (confirm-dead-then-remove)

> **Repo:** `addesso-project` (ServiceRequest + BFFs). Phase-4 **WC3c** per `WC3_PLAN_READER_MIGRATION.md`: remove the
> legacy SR-module chat **read** endpoints + their dead BFF remote-call declarations, now that all chat reads are served
> by Messaging (provider Phase-3, owner MO10a, admin via `IMessagingRemoteCall`). **Confirm each is dead before
> removing.** **Scope = READ endpoints only** — the SR message write path stays until WC4. Requires WC0–WC3b. **Do not
> commit.**

## Targets (investigated — still declared, presumed dead)
**SR-module read endpoints:**
- `ServiceRequestMessageController` — `GET /api/v1/service-requests/{srId}/messages` (`GetServiceRequestMessages`) +
  the mark-read `PATCH` (`MarkServiceRequestMessagesRead`).
- `ServiceRequestConversationController` — `GET /api/v1/messages/conversations` (`GetConversationList`) +
  `GET /api/v1/messages/conversations/{id}` (`GetConversationDetail`).
- `ProviderJobsController` — `GET /api/v1/service-requests/provider/conversations` (`GetProviderConversations`).

**Dead BFF remote-call declarations** (read cutover left them unused):
- MarineProvider `IServiceRequestRemoteCall` — `GetMessages` (`…/{srId}/messages`), `GetProviderConversations`
  (`…/provider/conversations`).
- AdminPanel `IServiceRequestRemoteCall` — `GetAdminConversations` (`/api/v1/messages/conversations`) + the by-id detail.

## Method — per endpoint: confirm dead → remove (or repoint)
For **each** target endpoint + its BFF remote-call method:
1. **Grep for a live caller** — the BFF handlers/controllers that invoke the remote-call method, and the provider/admin
   FE calls to the BFF route. Confirm **zero** live callers (chat reads now go through `IMessagingRemoteCall` /
   `/api/v1/provider/messaging/*` / `/api/v1/mobile/conversations*`).
2. **If dead** → remove the SR endpoint + its handler + query (+ the now-unused repo read method if nothing else uses
   it) **and** the dead BFF remote-call declaration.
3. **If still called** (watch especially **mark-read** — owner/provider may still mark chat read via SR) → **repoint to
   the Messaging store** (a participant-scoped Messaging mark-read, or the existing Messaging read) rather than deleting;
   document why. Do **not** silently break a live read.

**Special case — mark-read:** confirm whether owner/provider chat mark-read still hits `MarkServiceRequestMessagesRead`.
The Messaging module's mark-read is currently `[Authorize(Roles="Admin")]` — if owner/provider need chat mark-read,
this needs a **participant-scoped Messaging mark-read** (small add) before the SR one is retired. If nothing calls the
SR mark-read, retire it.

## Do NOT touch (out of scope — WC4 / unchanged)
- The SR **message entity + repository + write path** (`SendServiceRequestMessage`) — still active for **flag-OFF
  reversibility** until WC4.
- The **SR attachment access-check** (`GetAttachmentAccessCheck` / `GetOwnerAttachmentAccessCheck`) — kept as the WC3a
  two-store fallback (request/evidence + the transition window).
- The **sync consumer** — retired in WC4.
- Any **non-chat** SR endpoint.

## Don't-break / QA
- Pure removal of dead read endpoints + dead remote-call decls (or a documented repoint for a still-live one). No write
  path, no access-check, no Messaging read change. Chat reads (owner/provider/admin) unaffected — they were already on
  Messaging.
- Tests: (1) SR + BFFs + solution build 0 errors after removal (no dangling references); (2) provider chat list +
  thread, owner chat list + thread, admin conversation audit all still load (from Messaging) — a removed SR endpoint has
  no caller; (3) if mark-read was repointed, owner/provider chat mark-read works on the Messaging store; (4) a grep
  proves the removed routes have no remaining caller. Live smoke (all three chat surfaces still read + mark-read) needs
  the stack — document it.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC3c_RETIRE_LEGACY_SR_CHAT.md`: the per-endpoint dead-confirmation (grep evidence),
what was removed (SR endpoints/handlers/queries + BFF remote-call decls), any repoint (esp. mark-read → Messaging), and
the build/QA proof that no chat read regressed. **This leaves `sr.Messages` with no chat READER.** Then **WC4** — retire
the sync consumer, freeze `sr.Messages` (write path off), and remove the per-SR semaphore (the WC0 unique index is the
guard) — closing Phase-4 and unifying chat onto the canonical Messaging store.
