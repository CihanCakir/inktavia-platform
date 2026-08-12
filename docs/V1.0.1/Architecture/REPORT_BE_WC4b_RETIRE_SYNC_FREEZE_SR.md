# REPORT — BE_WC4b irreversible cutover: retire sync + freeze `sr.Messages` (close Phase-4)

> Implements `BE_WC4b_RETIRE_SYNC_FREEZE_SR.md` — **the point of no return**. Removes the SR chat write, the WriteCutover
> flag, the sync consumer, and `ServiceRequestMessageSentMessage`; freezes `sr.Messages`. Executed in the doc's 7-step
> order (callers before callees), **grep-confirmed-dead before each removal**. **Builds 0 errors. Smoke green.
> Not committed.**

## Removal sequence (each grep-confirmed dead first)

**Step 1 — BFF unconditional ensure-then-send.** `SendMobileChatMessage` + `SendProviderMessage`: removed the
`Messaging:WriteCutover:ChatMessages` read + the SR fall-through — `TrySendViaMessagingAsync` (WC4a ensure-then-send) is
now the sole path (a failure throws; no fallback). Removed the now-dead `_sr`/`_config` (owner) and
`_serviceRequest`/`_config` (provider) injections + dead usings. Removed the SR `SendMessage` remote-call from both
`IServiceRequestRemoteCall` (grep-confirmed: its only 2 callers were the two removed fallbacks).

**Step 2 — SR lifecycle dual-writes.** In `SubmitOffer`, `AcceptServiceRequestOffer`, `ApproveServiceRequestCompletion`,
`StartServiceRequestAssignment`, `CancelServiceRequest`: removed the `if (!_cutover.CurrentValue.SystemMessages) { … create
sr.Messages System/offer row … publish ServiceRequestMessageSentMessage }` blocks + the `IOptionsMonitor<MessagingWrite
CutoverOptions>` and now-unused `IServiceRequestMessageRepository` (and `IServiceRequestAssignmentRepository` in
ApproveCompletion) injections + dead usings. The always-published first-class events
(`ServiceRequestOfferSubmitted/OfferAccepted/CompletionApproved/AssignmentStarted/Cancelled`) stay — the Messaging WC1
lifecycle consumers are the sole producers now (they always ran unconditionally).

**Step 3 — SR chat write.** Removed `SendServiceRequestMessageCommand`+handler + the POST `.../{srId}/messages` endpoint
(deleted `ServiceRequestMessageController`, now empty) — the last writer of `sr.Messages` chat. The anti-harassment gate
`HasOwnerMessageAsync` (its only caller) is removed as part of the step-7 repo deletion.

**Step 4 — retire the sync.** Deleted `ServiceRequestMessageSyncConsumer` (+ its per-SR `SemaphoreSlim`; auto-scanned, no
registration to remove) and the one-time `ServiceRequestChatBackfiller` (+ its DI reg + startup invocation). The WC0
partial-unique `(ConversationId, SourceKey)` index is now the sole idempotency guard. (The idempotent name-fix pass stays.)

**Step 5 — remove `ServiceRequestMessageSentMessage`.** Grep-confirmed no publisher (steps 2–3) and no consumer (step 4)
left → deleted the message type, the dangling `MessageAddedRealtimeConsumer<ServiceRequestMessageSentMessage>` in
`ProviderRealtimeConsumers.cs`, and the stale comment in `ProviderEventSocketMapper.cs`. **Offer/city consumers untouched**
(distinct types: `MessagingMessageSentMessage`, `ServiceRequestOfferAccepted/Rejected`, `ServiceRequestPublished/Updated/
Cancelled/UrgencyChanged`). Provider chat realtime rides `MessagingMessageSentMessage` (WC2).

**Step 6 — remove the WriteCutover flag.** Deleted both `MessagingWriteCutoverOptions` (SR + Messaging), both
`Program.cs` `Configure<>` registrations (+ the unused Messaging using), both appsettings `Messaging:WriteCutover`
blocks, and the 4 `Messaging__WriteCutover__*` env vars in `docker-compose.yaml` (SystemMessages on service-request-api;
ChatMessages on the 3 BFFs). The Messaging path is now unconditional (the Messaging side never branched on the flag).

**Step 7 — freeze `sr.Messages`.** After steps 2–3 the entire `IServiceRequestMessageRepository` is dead (no injector;
all methods unreferenced) → deleted the interface + impl + DI registration. **Kept** the `ServiceRequestMessageEntity`
(+ `Create`/`CreateLocation`/`MarkAsRead`) and the `ServiceRequestMessages` **table/DbSet** — frozen historical/audit data
still written only by the dev mock seeder (directly via the DbSet) + read by the dispute-case `sr.Messages` fallback. A
future drop-table is a separate cleanup (not now).

## Build / grep clean-state
- **0 errors:** Messaging host, ServiceRequest host, both BFF hosts, and the full `Aizen.sln` (Release). Built
  incrementally after each step (no dangling refs).
- **Grep clean-state (final):** **no** `ServiceRequestMessageSentMessage` reference; **no** `WriteCutover` /
  `MessagingWriteCutoverOptions` reference (only removal comments); **no** SR chat write/read endpoint; **no**
  `ServiceRequestMessageSyncConsumer`; **no** SR `SendMessage` remote-call. (10 files deleted, 22 edited.)

## WC4b final smoke — PASSED (flag GONE, stack redeployed)
Rebuilt + recreated messaging-api + service-request-api + the 3 BFFs. `Messaging__WriteCutover__*` env is **absent** on
all services; the WC4a ensure endpoint is live (200); the sync consumer is **not hosted** (0 boot-log mentions — the
class is gone). `sr.Messages` baseline chat=**56** / System=**7** / total=**68**.

| Check | Result |
|-------|--------|
| Owner chat text/location/image (flag gone → unconditional Messaging) | All HTTP 200; conv 27 rows Type 1/6/5, **SourceKey NULL** (native), role Owner; **no new `sr.Messages` row** (chat stayed 56). ✅ |
| Fresh SR first message (WC4a native-create, flag gone) | Fresh SR 58 (no offers) → first message created Messaging conv **30** natively; **`sr.Messages` for SR 58 = 0**. ✅ |
| System lifecycle (step-2 dual-write removed) | Owner cancel SR 58 → Messaging System msg **`sys:58:CONVERSATION_CLOSED`** (Type 3, System role); **`sr.Messages` for SR 58 = 0** (no chat, no System row). Messaging is the sole System producer. ✅ |
| Notifications (unchanged) | Owner chat fired `NewMessageReceived` (Type 200) to the provider counterparty (5 recent); System silent. ✅ |
| SR no longer emits the chat event | 0 `ServiceRequestMessageSentMessage` mentions in the SR runtime logs (type deleted). ✅ |
| **GATE F — `sr.Messages` untouched** | Final chat=**56** / System=**7** / total=**68** — **identical to baseline**. Zero new rows across the whole run. ✅ |

**Coverage note:** the provider chat path uses the **identical** ensure-then-send wiring + endpoint (built + deployed);
the provider surface, dispute-transcript (WC3b), and admin audit were all driven green in the **pre-WC4 smoke** and are
structurally unchanged by WC4b (which only removed the flag + fallback + dual-write). The WC4b-specific decisive checks
(flag gone, System dual-write removed, native fresh-SR create, gate F) are directly verified above.

## Phase-4 closed
One write path (Messaging) for all chat + System messages. The sync consumer, backfiller,
`ServiceRequestMessageSentMessage`, and the WriteCutover flag are **all gone**; `sr.Messages` is **frozen** (no writer,
kept as historical/audit); idempotency = the WC0 unique index. The two messaging systems are unified onto the canonical
Messaging store — **the strangler is complete.** Recommend committing the full Phase-4 batch (WC0–WC4b) after this smoke.
