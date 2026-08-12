# WC4 — close Phase-4 (retire sync + freeze `sr.Messages`) — PLAN (a/b)

> Phase-4 **WC4** per `PHASE4_WRITE_CUTOVER_PLAN.md` — the **point of no return**: cut the SR chat write path, retire
> the SR→Messaging sync consumer, and freeze `sr.Messages`. Because this removes reversibility, WC4 is split into
> **WC4a (additive, smoke-gated)** and **WC4b (irreversible)**. Requires WC0–WC3d + the pre-WC4 Messaging-only smoke
> green. **Do not commit.**

## Critical finding (why the split)
The BFF chat write, **even with the flag ON**, still **falls through to the SR path when the conversation doesn't
exist yet** — the SR write **bootstraps** the conversation (via the sync consumer's EnsureConversation). Confirmed:
`SendMobileChatMessage` (owner) + `SendProviderMessage` (provider) resolve the conversation via
`GetMyConversationByContext` (a **read**) and fall back to SR on a miss. `EnsureConversationForContextCommand` **exists
but has no HTTP endpoint** (only the sync/backfiller/lifecycle call it internally; `POST /conversations` is a generic
create, not idempotent ensure-by-context). **So the SR write path is also the conversation-bootstrap** — it cannot be
removed until Messaging can create the conversation itself. That is WC4a.

## WC4a — Messaging conversation-create self-sufficiency (ADDITIVE, reversible, smoke-gated)
- **Expose `EnsureConversationForContext`** as an endpoint: idempotent **get-or-create by SR context**
  `(ServiceRequest, srId)` with the **owner + accepted-provider** participants resolved server-side (same semantics as
  the sync/backfiller). Participant/service-scoped; safe to call repeatedly (the unique `(ContextType, ContextId)`
  index guarantees one).
- **BFF: ensure-then-send.** `SendMobileChatMessage` + `SendProviderMessage` call **ensure** (get-or-create) → then
  Messaging `SendMessage`, so a brand-new conversation is created **natively** — no SR bootstrap needed.
- **Keep the SR fallback in place** for this phase (safety) — WC4a only proves the native create works.
- **Smoke gate:** a chat on an SR with **no prior conversation** (no offer card yet) creates the conversation via
  Messaging and the message lands natively — **no SR row**, on all surfaces. Once green, WC4b can remove the SR path.
- Kickoff `BE_WC4a_CONVERSATION_SELF_SUFFICIENCY.md`.

## WC4b — the irreversible cutover (after WC4a smoke green)
1. **BFF:** remove the SR path + the flag read in `SendMobileChatMessage` + `SendProviderMessage` → **always** Messaging
   (ensure-then-send). Drop the fall-through.
2. **SR commands:** remove the flag-gated `sr.Messages` System/offer writes in `SubmitOffer`, `AcceptServiceRequestOffer`,
   `ApproveServiceRequestCompletion`, `StartServiceRequestAssignment`, `CancelServiceRequest` (the WC1 Messaging
   lifecycle consumers are the sole producers). Remove `SendServiceRequestMessage` (chat write) + its **POST** endpoint
   (dead once the BFFs never call it — grep-confirm, like WC3d did for GET).
3. **Retire the sync:** delete `ServiceRequestMessageSyncConsumer` + the per-SR `SemaphoreSlim` (the WC0 partial-unique
   index is the sole idempotency guard). Retire the one-time `ServiceRequestChatBackfiller` (historical; keep the code
   in git history).
4. **`ServiceRequestMessageSentMessage`:** now has **no publisher** → remove the message type + any residual reference
   (the provider realtime SR-chat arm was already removed in WC2).
5. **Remove the `Messaging:WriteCutover:*` flag** (both `ChatMessages` + `SystemMessages`) + `MessagingWriteCutoverOptions`
   (SR + Messaging) + the ~13 flag branches → the Messaging-only path is unconditional.
6. **Freeze `sr.Messages`:** no readers (WC3) + no writers (this) → a **historical/frozen** table (kept for audit;
   note a future drop-table as a separate cleanup). The SR message entity/repo write surface is removed.
- Kickoff `BE_WC4b_RETIRE_SYNC_FREEZE_SR.md` (written after WC4a is verified).

## Sequencing & gate
Pre-WC4 Messaging-only smoke green → **WC4a** (additive) → **WC4a smoke** (native conversation-create, no SR bootstrap)
→ **WC4b** (irreversible removals) → **WC4b smoke** (full chat/System/dispute/notification on Messaging-only, sync
gone, `sr.Messages` frozen). **Do not start WC4b until WC4a proves native conversation-create** — otherwise the first
message of a new conversation would have nowhere to land.

## Definition of done (Phase-4 closed)
One write path (Messaging) for all chat + System messages; the sync consumer + backfiller + `ServiceRequestMessageSentMessage`
+ the WriteCutover flag all gone; `sr.Messages` frozen (no reader, no writer); idempotency = the WC0 unique index. The
two messaging systems are unified onto the canonical Messaging store — the strangler is complete.
