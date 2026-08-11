# BE_WC3d — provider-web SR-detail chat read → Messaging (the last `sr.Messages` chat reader) · WC4 gate

> **Repos:** `inktavia-marine-provider-web` (FE) + `addesso-project` (SR + MarineProvider BFF cleanup). Phase-4
> **WC3d** — the small follow-up WC3c surfaced: the provider portal's **Request-detail** page is the **one remaining
> live reader** of the SR `GetMessages` chat endpoint. Repoint it to the unified **Messaging thread** route, then delete
> the now-dead `GetMessages` chain — leaving `sr.Messages` with **no chat reader**, which unblocks WC4. Requires
> WC3c. **Do not commit.**

## Baseline (investigated)
- **Live SR reader:** `provider-web/src/features/service-requests/detail/api/messagesApi.ts:35` reads the chat thread
  via `endpoints.serviceRequests.messages(serviceRequestId)` → the SR `GetServiceRequestMessages` endpoint (the one
  WC3c had to KEEP because it's live).
- **Already migrated sibling:** `provider-web/src/features/messages/api/messagesApi.ts:67` reads via
  `endpoints.serviceRequests.messagingThread(serviceRequestId)` — "Phase 3 — thread read from the unified MESSAGING
  module", the canonical provider chat read. The Request-detail page simply never got moved.
- **Write is unaffected:** the send still `POST`s to `endpoints.serviceRequests.messages(...)` (the BFF branches
  internally per the WC2 flag; the SR write path stays for flag-OFF reversibility until WC4). WC3d is **read-only** on
  the FE.

## FE — repoint the SR-detail chat read (provider-web)
1. In `features/service-requests/detail/api/messagesApi.ts`, change the **read** from
   `endpoints.serviceRequests.messages(id)` → `endpoints.serviceRequests.messagingThread(id)` — mirroring the Messages
   section.
2. **Adapt to the Messaging thread response shape** by reusing the Messages section's mapper/types (both now read the
   same unified thread), so the Request-detail chat panel renders identically (text/image/location/system, sender
   side, timestamps, read state) from Messaging.
3. Keep the send/POST path unchanged. tsc + lint clean; provider read still works for deep-linked + in-page threads.

## BE — delete the now-dead `GetMessages` chain (after the FE repoint is verified)
Once the Request-detail page reads from Messaging and **no caller** hits SR `GetMessages` (grep the BFFs + all three FE
repos again to confirm), remove the full vertical slice:
- SR `GetServiceRequestMessages` endpoint (`ServiceRequestMessageController` GET `.../{srId}/messages`) + query +
  handler (+ the SR message read repo method if now orphaned).
- MarineProvider BFF `IServiceRequestRemoteCall.GetMessages` (+ its BFF handler/route if any).
- **Keep** the shared `GetProviderConversationsResponse` / `ProviderConversationDto` (live — used by the Messaging-backed
  provider inbox, per WC3c).
Do the delete **after** the FE change so the build never has a live-caller gap; if you prefer, split: WC3d-FE (repoint
+ smoke) then WC3d-BE (delete) — but both land before WC4.

## Do NOT touch (WC4 / unchanged)
The SR message **write path** (`SendServiceRequestMessage`) + entity + repo write (flag-OFF reversibility), the SR
attachment access-check (WC3a two-store fallback), and the sync consumer — all WC4.

## Don't-break / QA
- **The provider Request-detail chat panel must render the same** post-repoint (from Messaging) — text/image/location/
  system, both parties, ordering, unread. The already-migrated Messages section proves the shape works.
- After the `GetMessages` delete: SR + MarineProvider BFF + solution build 0 errors (no dangling refs); a grep proves
  `serviceRequests.messages` (GET) has **no remaining reader** in any FE/BFF; the POST (send) route is untouched.
- Tests: (1) provider-web tsc + lint 0 errors; (2) Request-detail chat loads from Messaging (same render as before);
  (3) after BE delete, builds clean + grep-proof no `GetMessages` reader; (4) send still works (SR write path, BFF
  flag). Live smoke: provider Request-detail chat reads from Messaging; send still posts + appears — needs the stack.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC3d_PROVIDER_DETAIL_READ_CUTOVER.md`: the FE repoint (SR-detail → messagingThread,
shared mapper), the `GetMessages` chain deletion (grep-proof dead first), and the confirmation that **`sr.Messages` now
has zero chat readers**. Then **WC4** — retire the sync consumer, freeze the `sr.Messages` write path, remove the per-SR
semaphore (WC0 unique index is the guard) — closing Phase-4.
