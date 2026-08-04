# PHASE 3 — provider read cutover: provider reads conversations/threads from the unified Messaging module

> **Repo:** `addesso-project` (Messaging module scoped query + provider BFF) + `inktavia-marine-provider-web` (FE
> repoint + optimistic send). Owner-approved: the provider portal **reads** its conversations + threads from the unified
> Messaging store (already a live mirror of SR via Phase-2 live-sync), same model the admin audits. **Writes stay on the
> ServiceRequest module** (unchanged, live-synced into Messaging). Full write cutover is Phase 4+.
>
> **Interim-consistency reality (design this in, don't hand-wave):** writes → SR, reads → Messaging, sync is **async**
> (~1–2s). So (i) the provider's **own** just-sent message would lag, and (ii) a **counterpart** message's realtime
> refetch may hit Messaging before the sync appended it. Both are handled below (optimistic send + refetch tolerance).
> Value = unification progress + a unified read model; **not** new user-facing capability (the provider already sees
> their chats today).

## PART A — Messaging module: participant-scoped read
Add a **participant-scoped** conversation query (the module only has the unscoped admin `GetListAsync` today):
- `GetConversationsForParticipant(long userId, MessagingContextType? contextType, int skip, int take)` — conversations
  where `userId` is a participant, ordered by last-message time; return the existing summary DTO. (Provider passes
  `contextType=ServiceRequest`.)
- Thread read: reuse `GetByIdWithMessagesAsync` **or** `GetByContextAsync(ServiceRequest, srId)` (so the FE can keep
  keying by `serviceRequestId`), with a **participant-authorization check** (the caller must be a participant). Return
  the existing detail DTO.

## PART B — provider BFF: Messaging read client (scoped to the resolved provider)
- Add remote calls to the Messaging module: get-my-conversations + get-thread, **scoped server-side to the resolved
  provider's user id** (from `IProviderIdentityHolder` — never a client-supplied id; mirror the provider hub's
  server-side identity discipline). Envelope-correct return types (`Task<AizenApiResponse<T>>` — the module wraps; per
  the recurring envelope lesson).
- **Map Messaging DTOs → the shape the provider FE already consumes** (conversation list item + thread message) so FE
  churn is minimal. Key the thread by `serviceRequestId` (via `GetByContext(ServiceRequest, srId)`) so existing deep
  links / `messages.thread(id)` keys don't break.

## PART C — provider FE: repoint reads + optimistic send (`features/messages`)
- Repoint `useConversations` + `useThread` (`useMessages.ts` / `messagesApi.ts`) from the ServiceRequest endpoints to the
  new Messaging-backed BFF endpoints. **Keep `useSendMessage` writing to the ServiceRequest module unchanged.**
- **Optimistic send (required):** on send, immediately `setQueryData` to append the sent message to the active thread
  (temp id / pending state), so the provider sees their own message instantly despite sync lag; reconcile on the next
  refetch (replace temp with the synced row — de-dupe by content+timestamp or temp-id swap). Handle send-failure
  rollback.
- **Realtime refetch tolerance:** the `MessageAdded` handler refetches the (now Messaging-backed) thread; if the newest
  message isn't present yet (sync lag), refetch once more after a short delay (or re-refetch on the next event). Keep it
  minimal — eventual consistency, not polling.

## PART D — consistency + scope guards
- The thread is **eventually consistent** during this interim (write-SR → async-sync → read-Messaging). Optimistic send
  + refetch tolerance cover the visible gaps; document the window.
- Provider sees **only their own** conversations (Part A scoping + Part B server-side identity). Verify no cross-provider
  leakage.
- Live-sync must be healthy (it feeds the read model). Admin observation unaffected (same Messaging store).

## Deferred (not this phase)
- **Write cutover** (provider/owner write directly to Messaging, SR chat retired) — Phase 4+, removes the sync gap
  entirely; needs owner-app maturity + split-write handling.
- Per-message read receipts; the duplicate-notification bus fan-out; timestamptz sweep — all separate.

## Don't-break / QA
- Writes still go to SR (unchanged); provider realtime (framework) unchanged; admin W1–W4 + live-sync unregressed. New
  scoped query is additive; no change to the admin unscoped path. Envelope-correct. Backend builds clean; FE typecheck/
  lint clean; same-image redeploy.

## Verification (on-screen)
1. Provider portal conversation list + a thread now render from the **Messaging** store (unified with what admin sees),
   scoped to that provider only (no other provider's conversations).
2. Provider **sends** → their message shows **instantly** (optimistic), then reconciles when the sync row arrives (no
   duplicate, no flicker-out).
3. A **counterpart** message appears in the provider thread (within the sync window; the refetch tolerance handles the
   race — no permanently-missing message).
4. No cross-provider leakage; admin still sees everything live; SR write path + provider realtime unregressed.

## Report
`docs/V1.0.1/Architecture/REPORT_PHASE3_PROVIDER_READ_CUTOVER.md`: the scoped query, the BFF read client + DTO mapping,
the FE repoint + optimistic send + refetch tolerance, the interim-consistency handling proven on-screen (own message
instant, counterpart message arrives, no leak), and Phase-4 (write cutover) readiness notes.
