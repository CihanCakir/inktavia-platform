# Phase 6 — ServiceRequests controller → gold standard (move controller business logic to Application)

`ServiceRequestsController` (9 endpoints) already routes most calls through CQRS, but it still holds **business logic
at the controller level**, one **service-locator remote-call leak**, an **inline DTO**, and `IActionResult` returns.
Push all of it down to the Application layer. **Apply the Phase-2-fix conventions** (one class per file, validators,
DTOs from module Abstraction).

## Controller-level logic that must move into handlers
1. **`GetDiscovery` — offerState mapping.** The controller maps the enum NAME → int code inline
   (`"notoffered" => 1, "offered" => 2, _ => null`). Move this into `GetProviderDiscoveryBffQuery`/its handler: the
   query carries the raw `OfferState` (string name, nullable), and the **handler** does the name→code translation
   (Any=0/NotOffered=1/Offered=2, unknown→Any). No switch in the controller.
2. **`GetConversations` — service-locator leak.** It uses
   `HttpContext.RequestServices.GetRequiredService<IProviderProfileResolver|IProviderIdentityHolder|IServiceRequestRemoteCall>()`
   then `sr.GetProviderConversations()`. Create `ServiceRequests/Query/GetProviderConversationsBff/` with a proper
   constructor-injected handler (resolve identity → remote-call → return `.Body`), and have the controller call
   `_cqrs.ProcessAsync(new GetProviderConversationsBffQuery(), ct)`. Remove the `GetRequiredService` usage entirely.

## The 9 endpoints
| Endpoint | Message | Kind | Notes |
|---|---|---|---|
| `GET open` | `GetOpenServiceRequestsBffQuery` | Query | typed + PRT |
| `GET {srId}` | `GetServiceRequestDetailBffQuery` | Query | typed + PRT |
| `GET discovery` | `GetProviderDiscoveryBffQuery` | Query | **move offerState mapping into handler** |
| `GET discovery/markers` | `GetDiscoveryMarkersBffQuery` | Query | typed + PRT |
| `GET discovery/summary` | `GetDiscoverySummaryBffQuery` | Query | typed + PRT |
| `GET {srId}/attachments/{fileId}/read-url` | `GetAttachmentReadUrlBffQuery` | Query | already correct — typed + PRT |
| `GET {srId}/messages` | `GetProviderMessagesQuery` | Query | typed + PRT |
| `POST {srId}/messages` | `SendProviderMessageCommand` | Command | **move inline DTO** |
| `GET conversations` | `GetProviderConversationsBffQuery` **(new)** | Query | **remove service-locator leak** |

## Target shape
- Controller injects **only** `IAizenCQRSProcessor`; no `GetRequiredService`, no `RemoteCall`. Each endpoint:
  `[ProducesResponseType(typeof(T), 200)]`, returns `Task<AizenApiResponse<T>>`, `return SetResponse(result);`
  (drop the `Ok(...)` wrappers). `T` = each message's response type.
- **Move the inline DTO:** `SendProviderMessageRequest` (declared in the controller file) → the ServiceRequest module
  Abstraction (`Aizen.Modules.ServiceRequest.Abstraction.Request…`, messaging request namespace); `[FromBody]` binds
  it, the command maps from it. No DTO in the controller or BFF Application.
- **Reorg + one-class-per-file.** Ensure every ServiceRequests operation lives under
  `ServiceRequests/{Query|Command}/{Op}/` with the message, handler, and validator each in their own file
  (`GetAttachmentReadUrlBff` is already correct; bring the rest — GetOpen, GetDetail, GetProviderDiscovery (+its
  Response), GetDiscoveryMarkers, GetDiscoverySummary, GetProviderMessages, SendProviderMessage — to the same shape,
  and add the new GetProviderConversationsBff). Namespace `…Application.ServiceRequests`.
- Validators: `ServiceRequestId > 0` where present; paged queries validate `PageIndex ≥ 0` / `PageSize`/`Skip`/`Take`
  ranges; `SendProviderMessage`: `Content` not empty (unless an attachment/location-only message is allowed — mirror
  current behaviour, don't tighten).

## Keep intact
All routes/verbs/query params byte-identical (the discovery query surface is large — preserve every parameter and its
name). The offerState wire contract stays the enum name from the client; only the *location* of the mapping changes
(controller → handler). Access-check / anti-harassment behaviour is the module's — unchanged.

## Acceptance
- Controller injects only `IAizenCQRSProcessor`; `grep -E "GetRequiredService|RemoteCall|IActionResult"` in the
  controller returns nothing. Every endpoint typed + `[ProducesResponseType]`.
- `GetProviderConversationsBff` query+handler exists; the offerState switch is gone from the controller (now in the
  discovery handler). `SendProviderMessageRequest` lives in the module Abstraction, not the controller.
- All 9 operations under `ServiceRequests/{Query|Command}/{Op}/`, one class per file + validators.
- Builds. After rebuild: open list, discovery (map + list + markers + summary, with offerState filter working),
  request detail, attachment read-url, messages list/send, and conversations inbox all behave exactly as before.

## Report
Append to `REPORT_BACKEND.md` ("Phase 6"): ServiceRequests fully CQRS — moved the offerState mapping into the
discovery handler, replaced the GetConversations service-locator with a `GetProviderConversationsBff` query+handler,
moved `SendProviderMessageRequest` to the module Abstraction, typed + PRT, reorg + validators. (Location/Template are
covered by Phase 3.)
