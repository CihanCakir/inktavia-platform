# REPORT — BE_WC4a Messaging conversation-create self-sufficiency

> Implements `BE_WC4a_CONVERSATION_SELF_SUFFICIENCY.md`. Lets the Messaging path **create the conversation itself**
> (idempotent ensure-by-context, server-resolved participants), so a chat write no longer depends on the SR bootstrap
> for the first message — the WC4b prerequisite. **Additive, reversible (SR fallback kept), builds 0 errors. Smoke gate
> PASSED. Not committed.**

## The gap (closed)
`SendMobileChatMessage` (owner) + `SendProviderMessage` (provider), even with the flag ON, resolved the conversation via
`GetMyConversationByContext` (a **read**) and **fell through to the SR path when it returned null** — so the SR write
bootstrapped the conversation for the very first message. `EnsureConversationForContextCommand` existed but had no
endpoint and takes participants as input; the real SR write paths (sync consumer / lifecycle writer / backfiller) resolve
owner + accepted-provider **server-side by raw SQL** over the shared DB (Messaging has no SR remote-call). WC4a exposes a
server-side-resolving ensure endpoint and has the BFFs call it before send.

## BE — WC4a
1. **`EnsureServiceRequestConversationCommand` + handler** (Messaging Application) — idempotent get-or-create for
   `(ContextType=ServiceRequest, ContextId=srId)`. Resolves participants **server-side** exactly as the lifecycle writer:
   owner + title from `servicerequest.service_requests`, accepted provider from `service_request_offers WHERE Status=4`
   (raw SQL over the shared `inktavia_store` DB); display names via `MessagingUserNameResolver`. Creates the conversation
   with the **Owner** participant (+ **Provider** iff an accepted offer exists yet). Idempotency is the **WC0 unique index**
   on `(ContextType, ContextId)`: a concurrent create that loses the race is caught as a `23505` and re-read.
   Returns `EnsureConversationByContextResponse { ConversationId, Created }` (Abstraction; `ConversationId=0` on
   unresolved SR → BFF falls back).
2. **Endpoint** `POST /api/v1/messaging/internal/conversations/ensure-by-context?contextType=&contextId=` on the WC3b
   internal `MessagingInternalController` (`[AllowAnonymous]`, cluster-internal, gateway-excluded — module hosts attach no
   outbound token). Server-side participant resolution means **no participants from the client** (and the provider BFF's
   SR-detail view is privacy-filtered and *cannot* supply the owner id — so server-side is mandatory, not just cleaner).
3. **BFF ensure-then-send** — both `SendMobileChatMessage.TrySendViaMessagingAsync` and
   `SendProviderMessage.TrySendViaMessagingAsync`: when the flag is ON, call `_messaging.EnsureConversationByContext(...)`
   (get-or-create) for the numeric conversation id, then `SendMessage(conversationId, body)`. A brand-new conversation is
   now created **natively** on the Messaging side — no SR bootstrap. Added `EnsureConversationByContext` to both BFFs'
   `IMessagingRemoteCall`.
4. **SR fallback kept** (belt-and-suspenders): `ConversationId<=0` or a Refit failure → the existing SR path still runs.
   The provider anti-harassment gate stays module-side inside `SendMessage`. Nothing removed — WC4b removes the fallback.

## Build
- **0 errors:** Messaging host, owner mobile BFF, provider BFF, and the full `Aizen.sln` (Release). Rebuilt + redeployed
  messaging-api + both BFFs (`--no-cache`, BFF images removed first per the stale-image gotcha). Flags ON.

## Smoke gate — PASSED (owner, fresh SR **57**, no offer card)
| Check | Result |
|-------|--------|
| Ensure endpoint idempotent (existing) | `POST …/ensure-by-context?…contextId=55` → `{conversationId: 27, created: false}` — returns the existing conversation. ✅ |
| Fresh SR precondition | SR 57 created (published, **0 offers**); **no** Messaging conversation, **0** `sr.Messages` before the message. ✅ |
| **THE GATE — first message creates conversation natively, no SR bootstrap** | Owner first message → HTTP 200. Messaging conversation **29** created natively (title set, participant `100029:1` = owner; no provider, since no accepted offer). Message = **native Messaging row** (id 167, Type=Text, **SourceKey NULL**, SenderRole=Owner). **`sr.Messages` rows for SR 57 = 0**; global `sr.Messages` total unchanged (68). ✅ |
| Idempotency (2nd message) | Same conversation (conversations for SR 57 = **1**, no dup), 2 native msgs, `sr.Messages` for SR 57 still **0**. ✅ |
| Existing conversation still works | Message on SR 55 (has conv 27) → ensure returned existing conv 27, message landed native; `sr.Messages` chat for SR 55 unchanged (5→5); conversations for SR 55 still **1**. ✅ |
| Concurrent first-messages → one conversation | Guarded by the WC0 unique `(ContextType, ContextId)` index + the handler's `23505`-catch-and-re-read (verified structurally; not a live race). ✅ |

**Decisive result:** the first-ever message on a fresh SR with no offer card creates the conversation **natively via
Messaging** and lands as a **native Messaging row with zero `sr.Messages` bootstrap rows.**

## Notes / coverage
- Owner path driven live end-to-end. The **provider** path uses the **identical** ensure-then-send wiring + the same
  endpoint (built + deployed); a live provider "first message with no conversation" is hard to stage in practice because
  the offer→accept lifecycle already creates the conversation via WC1 system messages — the owner-on-fresh-SR case is the
  canonical one and is proven.
- Additive + reversible: the SR fallback and the flag gate are untouched; existing chat is unaffected (ensure returns the
  existing id).

## Next — WC4b (the irreversible cutover, closes Phase-4)
With the Messaging path self-sufficient, WC4b removes the SR chat write + the `Messaging:WriteCutover:ChatMessages` flag
(+ OFF branches) + the SR→Messaging **sync consumer** + `ServiceRequestMessageSentMessage`, freezes `sr.Messages`, and
drops the per-SR semaphore (the WC0 unique index is the sole guard) — unifying chat on the canonical Messaging store.
