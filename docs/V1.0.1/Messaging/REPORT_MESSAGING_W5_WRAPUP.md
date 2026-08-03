# REPORT — Messaging Wave 5 wrap-up (live demo + loose ends)

Consolidated final pass on the messaging epic. Repos: `addesso-project` (Messaging module + AdminPanel BFF) and
`inktavia-marine-admin-web` (FE). Parts done in order.

---

## PART 0 — provider→admin live (the acceptance test): diagnosed + edge proven

**Result: the Messaging-module realtime edge is proven live; literal provider→admin is NOT wired (architecture),
scoped as a separate epic per your decision.**

### What was proven (admin realtime edge, live)
Two admin sessions on the same conversation (#990101 "W1 Live Socket Verify"). From tab A I sent a real
Messaging-module message ("W5-PART0 realtime edge proof AB77"). Tab B — **passive, untouched** — updated **live via
the socket**: the message rendered in the thread, the conversation bumped to the top of the list with an updated
preview and a **"1 unread"** badge, dot **Live**. This exercises the exact path the epic built:
`SendMessageCommandHandler → MessagingMessageSentMessage (bus) → AdminMessagingRealtimeConsumer → thin
"messagingEvent" frame → FE refetch`. The admin edge is healthy.

### Why literal provider→admin does not surface (evidence)
- The provider portal's chat calls `:17002/api/v1/provider/**service-requests**/{id}/messages` — the **ServiceRequest
  module**. Its conversations (RT2-11, RT2-12, SR-9011) do not exist in the messaging DB.
- The messaging DB holds only **4 seeded conversations** (SR#9001/#9004/#9010 + the #990101 DM); the SR ones were
  seeded at deploy time, not live-synced.
- There are **no Messaging-module bus consumers** and **no ServiceRequest→Messaging bridge**. A provider send goes to
  the ServiceRequest module and never produces a `MessagingMessageSentMessage`, so the admin Messaging panel can't see
  it.

### Decision (yours)
Accept the proven Messaging-module realtime edge as the epic's technical acceptance. **The admin audit observes the
Messaging module while real provider↔owner chat currently lives in the ServiceRequest module — they are not yet
connected.** Unifying them (ServiceRequest chat → Messaging module) is its own dedicated epic — **do not build the
bridge in this wrap-up**. It is the **top follow-up**, alongside the timestamptz sweep.

---

## PART 1 — commits

Both feature branches are local-only (no upstream); history kept clean, no force-push.

**admin-web** (`feature/messaging-w3-observation-panels`):
- `64fc7c4` — Messaging W3: moderation queue + reports observation panels
- `297bd2b` — Messaging W4: full i18n (tr + en) *(reworded from a prior "fix" commit that held the W4 work)*
- `1fd35ce` — Messaging W5: wire language switcher into Preferences *(Part 2)*
- `1de05d1` — Messaging W5: live moderation queue (react to ModerationEvent frame) *(Part 3 FE)*

**addesso-project** (`feature/messaging-registration`):
- `7fc6b26` — Messaging W3: type report BFF remote calls + fix module reporting 500s
- `6c75f3b` — Messaging W5: live moderation events (thin bus event, layer-2) *(Part 3 backend)*
- Working tree otherwise had **zero uncommitted code** (provider-migration + messaging backend all committed); only
  untracked docs remained (committed with this report).

---

## PART 2 — language switcher (verified on-screen)

`LanguageSwitcher.tsx` already existed and was functional (`i18n.changeLanguage`), rendered on the Settings landing —
but **`PreferencesPage.tsx` was a "coming soon" stub**. Fixes:
- Rendered `LanguageSwitcher` in a "Language" card on Preferences (un-stubbed).
- Restyled the switcher `<select>` for the dark admin theme (it was light-themed; affects both Settings + Preferences).

**On-screen (from the UI, no localStorage hacks):** on `/app/settings/preferences` the select showed "Türkçe";
selecting **English flipped the whole app live, no reload** (sidebar CORE/Dashboard/Vessels/…/Messages/Settings all
English); flipping back to **Türkçe** worked; a full **page reload preserved the language** (localStorage detector
cache `caches: ['localStorage']`). Both directions + persistence confirmed.

---

## PART 3 — live moderation events (thin bus event, layer-2) — implemented + deployed

Flag/moderate/status changes previously reached admins only via the 30s moderation-queue poll (they used
`MessagingRealtimePublisher` → the retired module hub, not the RabbitMQ bus the admin edge consumes). Now they publish
a thin bus event onto the same proven edge.

**Module** (`addesso-project`):
- New contract `MessagingModerationEventMessage : AizenBaseMessage { ConversationId, MessageId?, Kind, NewStatus,
  OccurredAt }` — thin, no content/PII.
- `ModerateMessageCommandHandler` publishes it via `IAizenMessagePublisher` for **every** verdict (allow/flag/block/
  pending), `Kind="Moderated"`, `NewStatus=<status>` (mirrors how `SendMessageCommandHandler` publishes
  `MessagingMessageSentMessage`; retained the existing Blocked-only module-hub call).
- `FlagConversationCommandHandler` publishes it on flag, `Kind="Flagged"`, `NewStatus="Flagged"`.

**AdminPanel BFF**:
- New closing-subclass consumer `AdminMessagingModerationRealtimeConsumer : RealtimeEventConsumer<…>` (auto-hosted).
- Broadened the single `AdminMessagingEventSocketMapper` to map both events to a `"messagingEvent"` frame,
  discriminated by inner `payload.type` (`"MessageAdded"` vs `"ModerationEvent"`), targeting the admin group. No
  second mapper, no hand-rolled SignalR (ADR layer-2).

**FE** (`admin-web`):
- `useMessagingHub` now branches on the inner frame type; on `"ModerationEvent"` it **also invalidates the moderation
  queue** (in addition to the list/detail it already refetched).
- Mounted `useMessagingHub` on `MessagesModerationPage` (conversationId null) so that surface receives frames while
  open → the queue updates live instead of after the 30s poll.

**Build/deploy:** messaging module + AdminPanel BFF build clean (0 errors); FE typecheck + lint clean. Both
`messaging-api` and `bff-adminpanel` rebuilt and redeployed (single replica each — the 2 replicas are marineprovider,
untouched; no split-brain). Runtime wiring confirmed in the redeployed BFF log:
`Configured endpoint AdminMessagingModerationRealtime, Consumer: …AdminMessagingModerationRealtimeConsumer`.

**On-screen two-admin-session UI proof (VERIFIED live):** Two admin sessions on `/app/messages/moderation`. Tab B
(passive viewer, freshly loaded so its 30s poll timer had just reset — baseline **KUYRUKTA 15 / İŞARETLİ 14**). From
Tab A I clicked **Block** ("Engelle") on the top flagged message. Within ~1–2s — far faster than the 30s poll — Tab B,
**untouched**, refetched live: **KUYRUKTA 15→14, İŞARETLİ 14→13**, and the blocked row dropped off the queue. This
exercises the full new path end-to-end: `ModerateMessageCommandHandler → MessagingModerationEventMessage (bus) →
AdminMessagingModerationRealtimeConsumer → "ModerationEvent" frame → useMessagingHub → modQueue refetch`. (The queue
query is `WHERE ModerationStatus IN (3,2)` = Flagged/PendingReview, so a Blocked verdict correctly leaves the queue.)
Runtime consumer hosting was also confirmed in the redeployed BFF log
(`Configured endpoint AdminMessagingModerationRealtime, Consumer: …AdminMessagingModerationRealtimeConsumer`).

Note: this proof blocked one already-flagged test message in the #990101 test-vehicle conversation (which is
deliberately kept as W1 test scaffolding and is out of Part 4's reset scope).

---

## PART 4 — test-data cleanup (statements surfaced for approval — NOT executed)

These are shared-DB mutations on seeded rows, and hard-deleting/altering messages is not something I run autonomously —
per the spec, the exact statements are surfaced for you to run/approve. Findings vs the original seed
(`MessagingMockDataSeeder`):

- **#9010 — no residue.** The Messaging conversation (Id 3) is **pristine seed** (exactly the 4 seeded messages,
  moderation states unchanged); the ServiceRequest module has 0 messages for SR 9010. Nothing to reset.
- **#9004 — reset to seed** (Messaging schema; enums: `ConversationStatus.Active=1/Flagged=3`,
  `MessageModerationStatus.Allowed=1/Blocked=4`). Seed created it Active with msg2_2 Allowed/no-reason; the DB now has
  the conversation Flagged and that message Blocked with a test reason:
  ```sql
  UPDATE messaging.conversations
     SET "Status" = 1                        -- Flagged(3) -> Active(1)
   WHERE "Id" = 2 AND "ContextId" = 9004;

  UPDATE messaging.conversation_messages
     SET "ModerationStatus" = 1,             -- Blocked(4) -> Allowed(1)
         "ModerationReason" = NULL
   WHERE "Id" = 6 AND "ConversationId" = 2;  -- "Can we use OEM parts only? No aftermarket."
  ```
- **W5 Part-0 proof message** I added to #990101 (soft-delete, consistent with the module's IsDeleted):
  ```sql
  UPDATE messaging.conversation_messages
     SET "IsDeleted" = true
   WHERE "Id" = 35 AND "Content" = 'W5-PART0 realtime edge proof AB77';
  ```
  (The rest of #990101's messages are the pre-existing W1/W2/W3 test scaffolding — not in Part 4's list, left as-is.)
- **SR 9011 / offer 90001 (ServiceRequest module)** — this is the provider-side test SR
  ("Acil: Dümen sistemi arızası — Çeşme"): `servicerequest.service_request_messages` holds **9 rows** for
  `ServiceRequestId = 9011`, plus offer `90001` residue in the Payment/ServiceRequest domain. I did not generate delete
  statements here because I can't reliably distinguish your injected test rows from the base test SR without the
  injection record. Please confirm the scope (e.g. "delete all SR 9011 messages + offer 90001"), and I'll surface the
  precise `DELETE`/soft-delete statements for approval.

---

## Out of scope — flagged as the top follow-ups (do NOT bundle)

1. **ServiceRequest → Messaging unification** (real provider↔owner chat into the Messaging module the admin observes) —
   the true enabler of literal provider→admin live. Its own epic.
2. **timestamptz / date-aggregation sweep** — the recurring Postgres `timestamp with time zone` bug class (P12 ledger +
   both messaging reporting endpoints) likely affects other date-bucketing endpoints. A systemic, investigation-heavy
   audit+fix — its own scoped pass.

---

## QA summary
- Backend builds clean (0 errors); FE typecheck + lint clean; tr+en parity preserved (W4 unchanged).
- No regression to W1–W4 or provider realtime (Part 3 is additive — a new event type through the existing, proven
  edge; existing message live-feed unchanged).
- Redeploy: same image, single replica each for messaging + admin-panel; marineprovider replicas untouched
  (no split-brain).
