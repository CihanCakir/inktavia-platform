# BE_WC4a — Messaging conversation-create self-sufficiency (WC4b prerequisite)

> **Repo:** `addesso-project` (Messaging + owner/provider BFFs). Phase-4 **WC4a** per `WC4_PLAN_CLOSE_PHASE4.md`: let
> the Messaging path **create the conversation itself** (idempotent ensure-by-context), so the SR chat write — which
> today also **bootstraps** a brand-new conversation — can be removed in WC4b. **Additive, reversible, smoke-gated**
> (the SR fallback stays until WC4b). Requires WC0–WC3d. **Do not commit.**

## The gap (investigated)
`SendMobileChatMessage` (owner) + `SendProviderMessage` (provider), even with `Messaging:WriteCutover:ChatMessages`
**ON**, resolve the conversation via `GetMyConversationByContext` (a **read**) and **fall through to the SR path when
it returns null** — i.e. the SR write **bootstraps** the conversation (via the sync's EnsureConversation) for the very
first message. `EnsureConversationForContextCommand` **exists** (idempotent get-or-create, unique `(ContextType,
ContextId)` index) but has **no HTTP endpoint** — only the sync/backfiller/lifecycle call it internally. So there is no
way for the BFF to create the conversation on the Messaging side; it depends on the SR bootstrap. WC4a closes that.

## BE — WC4a
1. **Expose `EnsureConversationForContext` as an endpoint** (Messaging): idempotent **get-or-create by SR context**
   `(ContextType=ServiceRequest, ContextId=srId)`, resolving the **owner** (from the SR) + the **accepted provider**
   participants **server-side** (mirror the sync/backfiller/lifecycle participant resolution — do not take participants
   from the client). Returns the conversation id. Repeated calls return the existing one (the unique index guarantees
   one). Auth: the same path the send uses (participant assertion for the owner/provider send; or an internal S2S
   variant if cleaner — match the existing conversation endpoints' auth).
2. **BFF: ensure-then-send.** In `SendMobileChatMessage` + `SendProviderMessage`, when the flag is ON: **call ensure**
   (get-or-create) to obtain the conversation id, then Messaging `SendMessage`. A brand-new conversation is now created
   **natively** on the Messaging side — no SR bootstrap.
3. **Keep the SR fallback** for this phase (belt-and-suspenders) — if ensure/send fails, the existing SR path still
   runs. WC4a only needs to **prove** the native create works end-to-end; WC4b removes the fallback.
4. Reuse the WC0 unique `(ContextType, ContextId)` conversation index (already the guard) — concurrent ensure calls
   converge on one conversation.

## Don't-break / QA
- **Additive + reversible:** a new ensure endpoint + the BFF calling it before send; the SR path stays as fallback; the
  flag still gates. Nothing removed. Existing chat (conversation already exists) is unaffected — ensure returns the
  existing id.
- **Idempotent:** two concurrent first-messages on the same SR create exactly **one** conversation (unique index); no
  duplicate participants.
- Tests: (1) Messaging + BFFs + solution build 0 errors; (2) ensure-by-context creates a conversation with owner +
  accepted-provider participants, and a **second** call returns the same id (no dup); (3) BFF ensure-then-send: a chat
  on an SR **with no prior conversation** (no offer card yet) creates the conversation **natively** and the message
  lands as a **native Messaging row — no `sr.Messages` row**; (4) a chat on an SR that **already** has a conversation
  still works (ensure returns existing); (5) concurrent first-messages → one conversation. **Smoke gate:** send the
  first-ever message on a fresh SR (owner or provider) with no offer card → conversation created via Messaging, message
  native, visible on all surfaces, **no SR bootstrap row**.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC4a_CONVERSATION_SELF_SUFFICIENCY.md`: the `EnsureConversationForContext` endpoint
(idempotent, server-resolved participants), the BFF ensure-then-send, and the smoke proof that a first-ever message on
a fresh SR creates the conversation **natively** with no SR bootstrap. On green → **WC4b** (the irreversible cutover:
remove the SR chat write + flag + sync consumer + `ServiceRequestMessageSentMessage`, freeze `sr.Messages`) — closing
Phase-4.
