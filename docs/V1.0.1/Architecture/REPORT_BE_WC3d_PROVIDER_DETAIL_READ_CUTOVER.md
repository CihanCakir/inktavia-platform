# REPORT — BE_WC3d provider-web SR-detail chat read → Messaging (last `sr.Messages` chat reader) · WC4 gate

> Implements `BE_WC3d_PROVIDER_DETAIL_READ_CUTOVER.md`. Repoints the provider portal's **Request-detail** chat read (the
> one live reader WC3c had to keep) from the legacy SR `GetMessages` route to the unified **Messaging thread**, then
> deletes the now-dead `GetMessages` chain. **`sr.Messages` now has ZERO chat readers**, which unblocks WC4. Builds 0
> errors. **Not committed.**

## FE repoint (provider-web) — WC3d-FE
`inktavia-marine-provider-web/src/features/service-requests/detail/api/messagesApi.ts`:
- `messagesApi.get()` now reads `endpoints.serviceRequests.messagingThread(id)` (the unified Messaging thread) instead
  of `endpoints.serviceRequests.messages(id)` (SR `GetServiceRequestMessages`) — mirroring the already-migrated Messages
  section (`features/messages/api/messagesApi.ts` `getThread`).
- Response mapped `{ items, channelOpen }` → the panel's `ConversationHistory` (`totalCount = items.length`). The
  `ConversationMessage` type gained `messageType` 5/6 + optional `attachmentFileId`/`location*` to match the thread shape.
- **Send/POST unchanged** (`messagesApi` had no send; the send lives in the Messages section and still POSTs to the SR
  `messages` route — the BFF branches on the WC2 flag; the SR write path stays for flag-OFF reversibility until WC4).
- The detail page (`RequestDetailPage`) and hook (`useRequestMessages`) are **unchanged** — they consume `items` +
  `channelOpen`, both preserved.

**Render equivalence (verified):**
- provider-web **tsc `--noEmit` = 0 errors**, **eslint = 0 errors** (repointed files).
- The BFF `messagingThread` route (`ProviderMessagingController.GetThread` → `GetProviderMessagingThreadQueryHandler`)
  returns `ProviderMessagesResponse { Items:[ServiceRequestMessageDto{ senderType, messageType, content, isRead,
  createdAt, attachmentFileId, location* }], ChannelOpen }` — the **exact same DTO the already-live Messages section
  renders**, a strict superset of the fields the detail panel reads. So the repointed read is byte-compatible by
  contract, and the panel renders identically (text/system/offer, sender side, timestamps, read state, channelOpen).
- **Note:** the live in-browser click-through was not run — the Claude browser extension was disconnected this session
  (the stack itself is up: provider-web dev server :3002, MarineProvider BFF :17002). The render is verified by tsc/lint
  + the identical-contract/identical-route equivalence with the already-rendering Messages section; a visual confirm on
  the Request-detail page is the one recommended manual check.

## BE delete — the dead `GetMessages` chain — WC3d-BE
Grep-confirmed **zero readers** first (BFFs + all three FE repos), then removed the full vertical slice:
- **SR module:** `ServiceRequestMessageController.GetMessages` action (GET `.../{srId}/messages`); deleted
  `Query/Message/GetServiceRequestMessages/` (query + handler); deleted `Response/Message/GetServiceRequestMessagesResponse.cs`;
  removed the now-orphaned `IServiceRequestMessageRepository.GetByServiceRequestIdAsync` (interface + impl) — its only
  reader was that query.
- **MarineProvider BFF:** removed `IServiceRequestRemoteCall.GetMessages`; deleted `ServiceRequests/Query/GetProviderMessages/`
  (query + handler + validator); removed the `ServiceRequestsController` GET `.../{srId}/messages` endpoint.
- **Kept:** the POST **send** (SR write path, WC4); the shared **`ProviderMessagesResponse`** — it was co-defined inside
  the deleted `GetProviderMessagesQuery.cs` but is still produced by the live `GetProviderMessagingThread`, so it was
  **relocated** to `ServiceRequests/Dto/ProviderMessagesResponse.cs` (standalone). The shared
  `GetProviderConversationsResponse` / `ProviderConversationDto` (WC3c) also stay.

## Build / QA proof
- **0 errors:** SR host, MarineProvider BFF host, full `Aizen.sln` (Release, exit 0, zero error lines); provider-web
  tsc + eslint 0 errors.
- **Grep proof:** the SR + BFF `.../{srId}/messages` routes are now **POST-only** (GET gone); **zero** `.GetMessages(`
  invocations in any BFF; **zero** `.get(endpoints.serviceRequests.messages)` readers in provider-web / admin-web /
  mobile; the provider-web POST send remains.
- No dangling references after the deletion.

## Live smoke (needs the running stack — not executed here)
1. Provider Request-detail page: chat panel renders from Messaging (network hits `/provider/messaging/service-requests/{id}/thread`, not the removed SR `messages` GET) — same content/sender/timestamps/channelOpen as before.
2. Provider send still posts + the message appears (SR write path + BFF WC2 flag).
3. `curl` the removed GET `.../{srId}/messages` → 404.

## Deferred / next — WC4 closes Phase-4
**`sr.Messages` now has zero chat readers.** Remaining: **WC4** — retire the SR→Messaging **sync consumer**, freeze the
`sr.Messages` **write path** (turn off the SR chat write once flag-OFF reversibility is no longer needed), and remove the
per-SR write **semaphore** (the WC0 partial-unique index on `(ConversationId, SourceKey)` is the sole idempotency guard).
That unifies chat fully onto the canonical Messaging store and closes Phase-4.
