# REPORT — BE_WC3c retire legacy SR chat READ endpoints (confirm-dead-then-remove)

> Implements `BE_WC3c_RETIRE_LEGACY_SR_CHAT.md`. Removes the legacy ServiceRequest-module chat **read** endpoints + their
> dead BFF chains, now that chat reads are served by Messaging. **Each target was grep-confirmed dead before removal.**
> Scope = READ paths only (the SR write path stays until WC4). **Builds 0 errors. Not committed.**

## Headline deviation — one endpoint was NOT dead
The doc listed the MarineProvider `GetMessages` remote-call (SR `GET /api/v1/service-requests/{srId}/messages`) among the
dead paths. **It is LIVE** — the provider portal's **Request-detail page** still reads the chat thread through it
(`inktavia-marine-provider-web/src/features/service-requests/detail/api/messagesApi.ts:35` → `useRequestMessages.ts:13`
→ `RequestDetailPage.tsx:94,159-160,456,526`). Per the doc's own rule ("confirm each is dead before removing… do NOT
silently break a live read"), **`GetMessages` and its whole chain were KEPT** (SR endpoint + `GetServiceRequestMessages`
query/handler + `GetServiceRequestMessagesResponse` + the message-repo `GetByServiceRequestIdAsync` + the MarineProvider
`GetMessages` remote-call/handler/endpoint). See **Follow-up** for how to retire it later.

## Per-endpoint dead-confirmation + action

| Target | Evidence | Action |
|--------|----------|--------|
| SR `MarkServiceRequestMessagesRead` (PATCH `.../messages/mark-read`) | No BFF remote-call routes to it (all 3 SR remote-call interfaces expose only GET/POST for messages); provider portal + owner mobile do **not** mark chat read at all; the only live chat mark-read (admin) is Messaging-backed (`IMessagingRemoteCall.MarkReadAsync`). Referenced only inside the SR module. | **REMOVED** |
| SR `GetProviderConversations` (GET `/service-requests/provider/conversations`) | Provider FE uses `/messaging/conversations`; legacy `endpoints.serviceRequests.conversations` has zero FE references. BFF `GetConversations` endpoint had no FE caller. | **REMOVED** (full chain) |
| SR `GetConversationList` (GET `/messages/conversations`) | Admin FE uses `/messaging/conversations`; zero refs to the legacy route in admin-web. | **REMOVED** (full chain) |
| SR `GetConversationDetail` (GET `/messages/conversations/{id}`) | Admin FE uses `/messaging/conversations/{id}`; zero refs to the legacy route. | **REMOVED** (full chain) |
| SR `GetMessages` (GET `.../{srId}/messages`) | **LIVE** — provider Request-detail page reads through it. | **KEPT** (deviation) |

Each dead remote-call had exactly one C# invoker (its BFF handler) dispatched by one controller endpoint, so removing the
Refit declaration alone would not compile — each was removed as a **full vertical slice** (remote-call decl + BFF query +
handler + BFF controller endpoint) together with the SR module endpoint/query/handler.

## What was removed

**ServiceRequest module**
- `ServiceRequestMessageController`: removed the `MarkRead` action (kept `Send` + `GetMessages`).
- `ServiceRequestConversationController`: **deleted** (both its actions were targets).
- `ProviderJobsController`: removed the `GetConversations` action (kept the other 18 actions).
- Deleted query/command folders: `Query/Message/GetServiceRequestMessages`… **kept** (live); deleted
  `Command/Message/MarkServiceRequestMessagesRead/`, `Query/Conversation/` (list + detail), `Query/Provider/GetProviderConversations/`.
- Deleted DTOs: `Request/Message/MarkServiceRequestMessagesReadRequest.cs`, `Response/Conversation/GetConversationListResponse.cs`,
  `Response/Conversation/GetConversationDetailResponse.cs` (the `Response/Conversation` folder is now empty and removed).
- Repository: **deleted** `IServiceRequestConversationRepository` + `ServiceRequestConversationRepository` +
  its DI registration (entirely orphaned — only the two removed conversation handlers used it). Removed the orphaned
  `IServiceRequestMessageRepository.Update(...)` method (its only caller was the removed mark-read handler).

**MarineProvider BFF** — removed `IServiceRequestRemoteCall.GetProviderConversations` + `GetProviderConversationsBff/`
query+handler + `ServiceRequestsController.GetConversations` endpoint.

**AdminPanel BFF** — removed `IServiceRequestRemoteCall.GetAdminConversations` + `GetAdminConversationDetail` (+ the now-dangling
`using …Response.Conversation`) + `GetConversationsBff/` + `GetConversationDetailBff/` query+handlers +
`ServiceRequestsController` conversation endpoints + the `AdminConversationsResponse` / `AdminConversationDetailResponse` DTOs.

## Deliberately KEPT (not dead / out of scope)
- **SR `GetMessages` chain** — live (see headline).
- **`GetProviderConversationsResponse` + `ProviderConversationDto`** (SR Abstraction) — **shared** with the live
  Messaging-backed provider inbox (`GetProviderMessagingConversations` → `ProviderMessagingController`), so kept.
- **SR message write path** (`SendServiceRequestMessage`), the **attachment access-check**, the **sync consumer**, the
  `ServiceRequestMessageEntity` (`IsRead`/`MarkAsRead` still used by `GetUnreadCountAsync` + the mock seeder) — all
  untouched (WC4 / out of scope).

## Build / QA proof
- **0 errors:** SR host, MarineProvider BFF host, AdminPanel BFF host, and the **full solution** (`Aizen.sln`, Release,
  exit 0, zero error lines) — no dangling references after removal.
- **Grep proof:** the removed routes (`/messages/conversations`, `/provider/conversations`, `/messages/mark-read`) have
  **no remaining route declaration** (only `BE_WC3c` removal comments remain); the kept `GetMessages` endpoint +
  `GetServiceRequestMessagesQuery` + the shared conversation DTOs remain referenced by their live callers.
- Chat reads (owner/provider/admin conversation list + thread) are unaffected — they were already on Messaging.

## Live smoke (needs the running stack — not executed here)
1. Provider portal: **Messages** section conversation list + thread load (Messaging); **Request-detail** page chat still
   loads (kept SR `GetMessages`).
2. Owner mobile: conversation list + thread load (Messaging).
3. Admin panel: conversation audit list + detail load (Messaging) — the removed SR-backed admin routes have no caller.
4. A `curl` of each removed route now returns 404 (route gone).

## Deferred / next
- **Follow-up to fully retire `GetMessages`:** migrate `provider-web .../detail/api/messagesApi.ts` to the Messaging
  thread route (`messagingThread(id)`, as the Messages section already does), then remove the SR `GetMessages` chain.
  Deferred here because it is a sibling-FE refactor + a provider read change that needs its own smoke — retiring it blind
  would violate "do not silently break a live read".
- **`sr.Messages` still has ONE chat reader** (the kept `GetMessages`), so the doc's "no chat reader" end-state is reached
  only after the follow-up. **WC4** (retire the sync consumer, freeze `sr.Messages` write path, drop the per-SR semaphore)
  is gated on that follow-up landing first.
