# REPORT — Phase 3: provider READ cutover (reads → unified Messaging; writes stay on ServiceRequest)

> **Repos:** `addesso-project` (Messaging module participant-scoped read + MarineProvider BFF Messaging read client) and
> `inktavia-marine-provider-web` (FE repoint + optimistic send + realtime tolerance). Owner-approved: the provider
> portal now **reads** its conversations + threads from the unified Messaging store (the live mirror of SR from the
> Phase-2 live-sync — the same store the admin audits). **Writes still go to the ServiceRequest module**, unchanged and
> live-synced. Full write cutover is Phase 4+. **Not committed** (per instruction).
>
> Interim consistency (writes→SR, reads→Messaging, async sync ~1–2s) is designed in, not hand-waved: optimistic send +
> tolerant reconcile on the provider's own message, refetch tolerance on the counterpart's realtime event. See PART C/D.

---

## PART A — Messaging module: participant-scoped read (additive; admin path untouched)

`Modules/Messaging/…` — the module previously had only the **unscoped admin** `GetListAsync`. Added a participant-scoped
read; the caller is resolved **server-side** from the authenticated principal (`IAizenInfoAccessor.UserInfo.UserId`,
the BFF-asserted `X-Aizen-User-Id`) — never a query/route parameter, so cross-participant leakage is structurally impossible.

- **Repository** (`IConversationRepository` / `ConversationRepository`):
  - `GetListForParticipantAsync(userId, contextType?, skip, take)` — mirrors `GetListAsync` (AsNoTracking, Include
    Participants, `!IsDeleted`, order by `LastMessageAt` desc) **plus** `Where(x => x.Participants.Any(p => p.UserId == userId))`.
  - `CountForParticipantAsync(userId, contextType?)` — same predicate, for pagination.
  - `GetByContextWithMessagesAsync(contextType, contextId)` — by-context lookup that **also loads Messages (+attachments)**.
    (Note: the pre-existing `GetByContextAsync` loads Participants only, so the old `by-context` endpoint returned empty
    messages — this new method is what makes the participant thread read actually carry the conversation.)
- **CQRS** (returns the existing summary/detail DTOs — no new response contracts):
  - `GetMyConversationsQuery` → `GetMyConversationsQueryHandler`: resolves userId from `IAizenInfoAccessor`, returns
    `GetConversationListResponse` (summary DTOs).
  - `GetMyConversationByContextQuery` → `GetMyConversationByContextQueryHandler`: resolves userId, loads by context,
    **authorizes the caller as a participant** (throws `UnauthorizedAccessException` otherwise), returns the non-admin
    view of `GetConversationDetailResponse` (internal notes + blocked messages excluded, oldest-first).
- **Controller** (`ConversationsController`, additive endpoints; admin `GET /conversations` unchanged):
  - `GET /api/v1/conversations/mine?contextType=&skip=&take=`
  - `GET /api/v1/conversations/by-context/mine?contextType=&contextId=`

## PART B — MarineProvider BFF: Messaging read client, scoped to the resolved provider

`Bff/src/MarineProvider/…` — new remote calls to the Messaging module, mapped onto the shape the provider FE already
consumes so FE churn is a URL swap.

- **Remote call** `IMessagingRemoteCall : IAizenRemoteCall` — `GET /api/v1/conversations/mine` and
  `/by-context/mine`, both `Task<AizenApiResponse<T>>` (envelope-correct, per the recurring lesson). **No user id on
  the wire** — the `MarineProviderBffAuthDelegatingHandler` auto-attaches the trusted-BFF `X-Aizen-User-Id` assertion
  from `IProviderIdentityHolder.UserId` (resolved from the Keycloak token, never client-supplied). Registered in
  `DependencyInjection.cs`; base URL `RemoteCalls__IMessagingRemoteCall__BaseUrl: http://messaging-api:8080` added to
  both `bff-marineprovider` compose blocks. Added the `Messaging.Abstraction` project ref.
- **Handlers** (resolve identity → guard on `UserId` → call Messaging → map):
  - `GetProviderMessagingConversationsQueryHandler` → maps Messaging `ConversationSummaryDto` → `ProviderConversationDto`
    (the FE inbox shape), filtered to `MessagingContextType.ServiceRequest`.
  - `GetProviderMessagingThreadQueryHandler` → keyed by `serviceRequestId` (via `by-context`), maps Messaging
    `ChatMessageDto` → `ServiceRequestMessageDto`, returns the existing `ProviderMessagesResponse`. `ChannelOpen` mirrors
    the SR-backed handler exactly (`items.Any(SenderType == Owner)`). Sanitizes a module 403/404 to "not found" (no
    id-space probing signal).
- **Controller** `ProviderMessagingController` (`api/v1/provider/messaging`, `ProviderActive` policy):
  - `GET /messaging/conversations`
  - `GET /messaging/service-requests/{serviceRequestId:long}/thread`

**Interim mapping tradeoffs (documented — degrade gracefully, restored at write-cutover):** the Messaging read model
doesn't carry a few SR-only display fields, so the BFF defaults them: `RequestCode=""`, `LifecycleStatus=null`,
`LastMessageType=Text`, and **`UnreadCount=0`** (Messaging tracks *admin* unread only — per-provider unread is Phase-4
read-receipts; we deliberately do **not** surface the admin count). Message `IsRead=true` (rendered thread) since the
read model has no per-participant read state. Title + preview + timestamp + messages + participants are exact.

## PART C — Provider FE: repoint reads, optimistic send, realtime tolerance (`features/messages`)

`inktavia-marine-provider-web` (typecheck clean; lint neutral — see QA):
- **Repoint reads** (`messagesApi.ts` + `endpoints.ts`): `listConversations` → `/messaging/conversations`;
  `getThread` → `/messaging/service-requests/{id}/thread`. Return shapes unchanged (`ConversationSummary[]` /
  `ApiResult<Thread>`), so `MessagesPage` needs no change. **`send` stays on `POST /service-requests/{id}/messages`.**
- **Optimistic send** (`useMessages.ts` `useSendMessage`): `onMutate` appends a temp `ThreadMessage`
  (`id=-Date.now()`, `__optimistic`) to the thread cache so the provider sees their message **instantly**, before the
  SR→Messaging sync row exists. `onError` and the logical-failure branch of `onSuccess` (a non-ok result like
  `SR_MSG_CHANNEL_LOCKED`) **roll it back**. On success, `reconcileThread` refetches and **merges** — server rows win,
  but any optimistic bubble whose synced row hasn't appeared yet is **kept (deduped by sender+content)** and retried
  once after 1.5 s. Net: the provider's own message never flickers out during the sync window, and no duplicate survives.
- **Realtime tolerance** (`useProviderRealtime.tsx`, `MessageAdded`): now (a) invalidates the thread in **both string-
  and number-keyed** forms — `useThread` keys by string, so the previous bare-number invalidate **silently missed the
  open thread** (fixed), and (b) re-invalidates once after 1.5 s to tolerate sync lag, so a counterpart message that the
  first refetch raced ahead of still lands. Eventual consistency, not polling.

## PART D — guards + the two config prerequisites this surfaced

**Scope guard — no cross-provider leak (proven live, deterministically).** The scope is the asserted principal, matched
against `conversation_participants.UserId`. Verified end-to-end through the real endpoint with a real
`provider-portal-bff` service token + BFF assertion:

| Test | Assertion | Result |
|---|---|---|
| Scoped list | user **100011** | `SRs=[9011]`, total 1 ✅ (only its own) |
| **Leak test** | user **10012** | `SRs=[9003,9006,30002]` — **no 9011** ✅ disjoint |
| Thread by-context (SR 9011) | participant **100011** | title + **11 messages** + participants[Owner 10008, Provider 100011, System] ✅ |
| **Auth test** | non-participant **10012** on SR 9011 | **denied** — "User 10012 is not a participant of conversation 7" ✅ (BFF sanitizes to "not found") |
| Admin unscoped `GET /conversations` | no assertion | **all 15** conversations ✅ (admin observation unaffected) |

DB-level cross-check confirmed disjoint provider→conversation sets (10011→{9001,30004,30011}, 10012→{9003,9006,30002},
10013→{30006,30012}, 100011→{9011}, …) — every provider is isolated to its own participations.

**Live-sync health (feeds the read model).** messaging-api boot backfill converged: `conversations created 0, reused
13, messages inserted 0, skipped 50, errors 0`. The Phase-2 sync consumer is hosted and unchanged.

**⚠️ Two real config prerequisites this phase surfaced and fixed (would have made the provider inbox silently empty):**
1. **messaging-api had no `BffAssertion` config in `docker-compose.yaml`.** Unlike service-request/notification/cargodry
   APIs (secret len 64), messaging-api's `BffAssertion__SharedSecret` was empty → it would reject the provider BFF's
   `X-Aizen-User-Id` assertion → `UserId=0` → empty inbox. **Fixed:** added `BffAssertion__SharedSecret`
   (`${AIZEN_BFF_ASSERTION_SECRET}`) + `AllowedClientIds` (provider-portal-bff, admin-panel-bff) to the messaging-api
   block; recreated the container (secret now len 64).
2. **The `provider-portal-bff` Keycloak client's service token lacked `messaging-api` in its `aud`.** messaging-api
   validates `aud == messaging-api`; the provider BFF token carried identity/service-request/cargodry/file-storage/
   payment/notification/reference-data but **not messaging-api** (nor vessel-api) → 401 before the assertion middleware.
   The provisioning source `infrastructure/keycloak/provider-realm/setup-provider-realm.sh` (lines 113–114) **already
   includes messaging-api** in its audience-mapper loop — the running Keycloak was simply provisioned before that line
   was added (also missing vessel-api). **Fixed at runtime** by adding the `aud-messaging-api` oidc-audience-mapper to
   the provider-portal-bff client via kcadm; the token now carries `messaging-api`. **No source change needed** — re-run
   `setup-provider-realm.sh` on a fresh Keycloak and it's correct. (This same audience gap will apply to any other
   env/cluster provisioned from the older script — re-run it there too.)

**⚠️ A third runtime gotcha (found during on-screen verify):** the BFF **caches its Keycloak service token**, so after
adding the audience mapper the two `bff-marineprovider` replicas had to be **restarted** to mint a fresh token carrying
`messaging-api` — until then the inbox call 401'd (BFF log: `Messaging GetMyConversations failed for provider user
100011 … 401`). A restart fixed it. (In a real rollout the mapper exists before deploy, so this is a local-sequencing artifact.)

**🐞 Two correctness bugs surfaced by the on-screen send test and fixed (both in the Messaging read/sync path):**
1. **Duplicate synced message (pre-existing Phase-2 live-sync race).** A single provider send created **one** SR row
   but **two** identical Messaging rows (same `SentAt` to the microsecond) → two identical bubbles in the thread. Root
   cause: the two-phase bus delivers the SR-message commit more than once **concurrently** (each consumer of the SR
   event republishes a commit onto the shared exchange), and the sync consumer's dedup checks the **in-memory**
   `conv.Messages` loaded per-invocation → a TOCTOU race where both invocations miss and both insert. **Fix:** a per-
   service-request `SemaphoreSlim` gate around load→dedup→save in `ServiceRequestMessageSyncConsumer` serializes
   concurrent commits so the second re-loads after the first saved and the dedup catches it. Correct on the single-
   replica messaging-api; a DB unique index on the message key is the durable multi-replica follow-up. **Verified:** a
   fresh send now produces **exactly one** row (was two). This is adjacent to the spec's deferred "duplicate-notification
   fan-out" but a duplicate persistent *message* corrupts the read model the provider now consumes, so it was fixed here.
2. **Soft-deleted messages leaked into the provider thread.** There is no global `IsDeleted` query filter on messages,
   and the read handler didn't exclude them → a soft-deleted message (`IsDeleted=true`) still rendered. **Fix:** added
   `.Where(m => !m.IsDeleted)` to the participant thread handler's message filter. **Verified:** the soft-deleted message
   disappeared from the thread on-screen. (The admin detail handler has the same gap; left as-is — admins may audit
   deleted content — but noted.)

## Deferred (not this phase) — unchanged from spec
Write cutover (Phase 4+, removes the sync gap); per-message read receipts (would restore per-provider unread); the
duplicate-notification bus fan-out; the timestamptz sweep. No change to the SR write path or provider realtime framework.

## QA / build / deploy
- **Backend builds clean** (0 errors): Messaging host project + MarineProvider BFF (only pre-existing nullable warnings).
- **FE**: `tsc --noEmit` clean. `eslint .` = **36 problems (22 errors, 14 warnings)** — **identical to the pre-change
  baseline** (verified by stashing my edits); all are pre-existing repo strictness (React-Compiler/react-hooks rules in
  files I didn't touch). **My changes introduced zero new lint problems.**
- **Deploy (same-image):** messaging-api + **both** `bff-marineprovider` replicas rebuilt & redeployed (no split-brain).
  New routes confirmed in Swagger on both services. messaging-api was recreated for the env-only assertion fix, and
  rebuilt+redeployed twice more for the two correctness fixes (dedup gate + `IsDeleted` filter); the BFF replicas were
  restarted once to refresh the cached Keycloak token (see the runtime gotcha above).
- The sync-consumer change (`ServiceRequestMessageSyncConsumer`, +42 lines) touches the Phase-2 write/sync path — it is
  a **race fix only** (a per-SR gate + no behavior change to the mapping/dedup semantics), not part of the read cutover;
  called out separately so it's reviewed as such.
- No regression: admin unscoped read verified unchanged (all 15, live); SR write path unchanged (send still 200 via the
  SR route); provider realtime framework untouched; the new module query + BFF client are additive.

## Verification status
- ✅ **Backend, end-to-end through the real BFF-assertion path** (identical to what the FE consumes): scoped list, no
  cross-provider leak, participant-authorized thread read (with messages), non-participant denial, admin path unchanged.
- ✅ Builds/typecheck/lint/deploy as above; live-sync healthy; the config prerequisites found & fixed.
- ✅ **On-screen (visual) — COMPLETED, logged in as the active provider PROVIDER 2 AS (user 100011) + admin.** Network
  confirmed the FE now calls `/api/v1/provider/messaging/conversations` and `…/messaging/service-requests/9011/thread`
  (both **200**); the old SR message routes are no longer used for reads.
  1. **Provider inbox from Messaging, scoped, no leak** — the inbox showed **exactly one** conversation ("Acil: Dümen
     sistemi arızası — Çeşme", SR #9011, status AKTİF); no other provider's conversations appeared.
  2. **Thread from Messaging, keyed by SR id** (`/app/messages/9011`) — customer (left) and provider "Siz" (right)
     messages rendered with correct sender sides.
  3. **Optimistic send → reconciles to a single bubble** — a sent message appeared and settled to **exactly one** bubble
     (no duplicate, no flicker-out); the write went to the SR route (`POST /service-requests/9011/messages` → 200) while
     the thread refetched from `/messaging/*` (two refetches — the reconcile + the 1.5 s tolerance retry).
  4. **Admin observation unaffected** — the admin Communication Audit (dot **Canlı**/Live) listed **all 15**
     conversations (unscoped) and #9011's preview updated live to the provider's just-sent message → the store is unified.
  5. **Soft-deleted message hidden** — after the `IsDeleted` fix, a soft-deleted message no longer renders in the thread.
- ◻️ **Counterpart→provider realtime arrival not directly demonstrated** — it needs an *owner/customer* send via the SR
  module, and no owner app exists in this environment yet. The mechanisms it relies on are proven: the Messaging-backed
  thread read (✅) and the `MessageAdded` refetch-with-tolerance (✅ code + the reverse provider→admin live path shown).

## Phase-4 (write cutover) readiness
De-risked: the participant-scoped read model + BFF mapping + server-side identity scoping are proven live, and the
provider already reads the unified store. Phase-4 prerequisites: (1) write path → `EnsureConversationForContext` in
Messaging (retire SR chat); (2) per-participant read-receipt model (restores real unread + read state, replacing the
interim `UnreadCount=0`/`IsRead=true`); (3) dual-realtime de-dup during transition; (4) carry request-code/lifecycle
onto the Messaging read model (or resolve from SR) to restore the inbox chips; (5) enumerate every reader of
`ServiceRequestMessageEntity` before retiring SR chat.
