# BE_WC4b — irreversible cutover: retire sync + freeze `sr.Messages` (close Phase-4)

> **Repo:** `addesso-project` (Messaging + SR + BFFs). Phase-4 **WC4b** per `WC4_PLAN_CLOSE_PHASE4.md` — **the point of
> no return**: remove the SR chat write, the WriteCutover flag, the sync consumer, and `ServiceRequestMessageSentMessage`;
> freeze `sr.Messages`. **Only after WC4a smoke is green** (Messaging creates conversations natively). **Grep-confirm-
> dead before each removal** (the WC3c/WC3d discipline that caught live callers). Requires WC4a. **Do not commit.**

## Preconditions
Pre-WC4 Messaging-only smoke ✅ (gate F: zero new `sr.Messages` chat/System rows) **and** WC4a smoke ✅ (native
conversation-create, no SR bootstrap). WC4b is irreversible — do not start without both.

## Removal sequence (callers before callees — no build gap)
### 1. BFF — always Messaging (drop the flag + SR fallback)
`SendMobileChatMessage` (owner) + `SendProviderMessage` (provider): remove the `Messaging:WriteCutover:ChatMessages`
read, the SR fall-through, and the `GetMyConversationByContext`-then-fallback — replace with **ensure-then-send**
(WC4a's `EnsureServiceRequestConversation` → Messaging `SendMessage`) **unconditionally**. Remove the now-unused SR
`SendMessage` from the mobile `IServiceRequestRemoteCall` (+ provider equivalent) after confirming no other caller.

### 2. SR lifecycle commands — stop writing `sr.Messages`
In `SubmitOffer`, `AcceptServiceRequestOffer`, `ApproveServiceRequestCompletion`, `StartServiceRequestAssignment`,
`CancelServiceRequest`: remove the `if (!_cutover.CurrentValue.SystemMessages) { … create sr.Messages System/offer row
… publish ServiceRequestMessageSentMessage }` blocks + the `IOptionsMonitor<MessagingWriteCutoverOptions>` injection.
The **WC1 Messaging lifecycle consumers are the sole producers** of System/offer messages now (proven in the smokes).

### 3. SR chat write — remove `SendServiceRequestMessage` + its endpoint
Grep-confirm **no BFF caller** after step 1 → remove the `SendServiceRequestMessage` command + handler + the **POST**
`.../{srId}/messages` endpoint (`ServiceRequestMessageController`). Remove the now-dead SR anti-harassment gate
`HasOwnerMessageAsync` (WC2 moved the gate to the Messaging store). This is the **last writer of `sr.Messages` chat**.

### 4. Retire the sync
Delete `ServiceRequestMessageSyncConsumer` + its per-SR `SemaphoreSlim` (the WC0 partial-unique index is now the sole
idempotency guard). Retire the one-time `ServiceRequestChatBackfiller` (its job is done; git history preserves it).

### 5. Remove `ServiceRequestMessageSentMessage` (no publisher left)
After steps 2–4 there is **no publisher**. Remove: the message type (`…Abstraction/Message/ServiceRequestMessageSentMessage.cs`),
the **provider realtime dangling** `MessageAddedRealtimeConsumer<ServiceRequestMessageSentMessage>` in
`ProviderRealtimeConsumers.cs` (already a no-op — WC2 removed the mapper arm), and the stale comment in
`ProviderEventSocketMapper.cs`. **Leave the offer/city consumers untouched** — they subscribe to **other** message
types (`ServiceRequestPublished/Updated/Cancelled/UrgencyChanged/OfferAccepted/OfferRejected`), not this one. Provider
chat realtime rides `MessagingMessageSentMessage` (WC2).

### 6. Remove the WriteCutover flag
Delete `Messaging:WriteCutover:*` (both `ChatMessages` + `SystemMessages`), `MessagingWriteCutoverOptions` (SR +
Messaging), and the ~13 flag branches → the Messaging-only path is **unconditional**. Remove the flag env from
`docker-compose.yaml` (and note k8s).

### 7. Freeze `sr.Messages`
No readers (WC3) + no writers (steps 2–3) remain → remove the SR message **write** surface (the entity's write methods
/ repo add — keep only what other code still needs, if any). Keep the **table** as **historical/frozen** (backfilled +
pre-cutover audit data); note a future **drop-table** as a separate cleanup (not now).

## Don't-break / QA
- **Grep-confirm-dead before each removal** (steps 1→7 order avoids dangling refs). No offer/city realtime, no
  Messaging read, no dispute transcript, no notification path changes — those are all on Messaging already.
- Builds 0 errors across Messaging + SR + all BFFs + solution after each step; a final grep proves: no
  `ServiceRequestMessageSentMessage` reference, no `WriteCutover` reference, no SR chat write/read endpoint, no sync
  consumer.
- **WC4b smoke (final):** with the flag GONE (unconditional Messaging), repeat the pre-WC4 checks — owner↔provider
  text/image/location + System lifecycle + dispute transcript + notifications all green on owner/provider/admin;
  `sr.Messages` receives **zero** new rows; the sync consumer is not hosted (bus scan); a brand-new SR's first message
  still creates the conversation natively (WC4a path). Needs the stack.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC4b_RETIRE_SYNC_FREEZE_SR.md`: each removal with its grep-dead proof, the final
build/grep clean-state, and the WC4b smoke. **This closes Phase-4** — one write path (Messaging) for all chat + System
messages; sync + backfiller + `ServiceRequestMessageSentMessage` + the flag all gone; `sr.Messages` frozen; idempotency
= the WC0 unique index. The two messaging systems are unified onto the canonical Messaging store — the strangler is
complete. **Recommend committing the full Phase-4 batch (WC0–WC4b) after this smoke.**
