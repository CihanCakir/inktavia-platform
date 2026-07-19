# Phase 5 — Offers + Notifications → typed + PRT + DTOs to module Abstraction

Both controllers already use CQRS (`_cqrs.ProcessAsync`), so this phase is lighter: convert `IActionResult` →
typed `AizenApiResponse<T>` + `[ProducesResponseType]`, move the **inline controller DTOs** into the relevant module
Abstraction, and bring the handlers to the Phase-2-fix shape (one class per file + validators).

## Common target
- Each endpoint: `[ProducesResponseType(typeof(T), StatusCodes.Status200OK)]`, returns `Task<AizenApiResponse<T>>`,
  body `var result = await _cqrs.ProcessAsync(new …{Command|Query}{…}, ct); return SetResponse(result);`
  (drop the `Ok(SetResponse(result))` / `Ok(result)` wrappers). `T` = the existing message's `AizenCommand<T>` /
  `AizenQuery<T>` response type.
- Handlers: one class per file (`*Command.cs`/`*Query.cs`, `*Handler.cs`, `*Validator.cs`); add validators where
  missing. Namespaces stay `…Application.Offers` / `…Application.Notifications`.
- **No request/response DTO declared inside the controller or the BFF Application** — move them to the module
  Abstraction and reference.

## Offers — `OffersController` (`api/v1/provider`, policy `ProviderActive`) — 8 endpoints
All already route through existing `Offers/{Command|Query}` messages. Keep routes/verbs/params exactly, including the
**two** withdraw routes (`POST offers/{offerId}/withdraw` and `POST service-requests/{srId}/offer/{offerId}/withdraw`,
both → `WithdrawOfferBffCommand`).

| Endpoint | Message | Kind |
|---|---|---|
| `GET offers` | `GetMyOffersBffQuery` | Query |
| `POST service-requests/{srId}/offers` | `CreateOfferBffCommand` | Command |
| `PUT service-requests/{srId}/offers/{offerId}` | `UpdateOfferBffCommand` | Command |
| `POST offers/{offerId}/withdraw` | `WithdrawOfferBffCommand` | Command |
| `PUT service-requests/{srId}/offer/draft` | `SaveOfferDraftBffCommand` | Command |
| `POST service-requests/{srId}/offer/preview` | `PreviewOfferBffCommand` | Command |
| `POST service-requests/{srId}/offer/{offerId}/submit` | `SubmitOfferBffCommand` | Command |
| `POST service-requests/{srId}/offer/{offerId}/withdraw` | `WithdrawOfferBffCommand` | Command |

**Move the inline DTO:** `WithdrawOfferBffRequest` (declared at the bottom of the controller file — `ServiceRequestId`,
`Reason`) → `Aizen.Modules.ServiceRequest.Abstraction.Request.Offer` (e.g. `WithdrawServiceRequestOfferRequest`);
update the two withdraw endpoints' `[FromBody]` + the command mapping to reference it. All other bodies already use
module Abstraction requests (`CreateServiceRequestOfferRequest`, `UpdateServiceRequestOfferRequest`,
`SaveOfferDraftRequest`, `SubmitOfferRequest`) — keep.

Validators (in each op folder): `ServiceRequestId > 0` and/or `OfferId > 0` as applicable + body not null for
create/update/draft/preview/submit/withdraw; `GetMyOffersBff`: `PageIndex ≥ 0`, `PageSize` 1..100.

## Notifications — `NotificationsController` (`api/v1/provider/notifications`, policy `ProviderAuthenticated`) — 2 endpoints
| Endpoint | Message | Kind |
|---|---|---|
| `POST push-subscriptions` | `SubscribePushCommand` | Command |
| `DELETE push-subscriptions` | `UnsubscribePushCommand` | Command |

**Move the inline DTOs:** `PushSubscriptionRequest`, `PushSubscriptionKeys`, `PushUnsubscribeRequest` (declared in the
controller file) → the Notification module Abstraction (`Aizen.Modules.Notification.Abstraction.Request` or the
existing request namespace there); `[FromBody]` binds the Abstraction types, and the commands map from them. Validators:
`Endpoint` not empty; subscribe — `Keys.P256dh` + `Keys.Auth` not empty.

## Acceptance
- Both controllers: every endpoint typed `AizenApiResponse<T>` + `[ProducesResponseType]`; no `Ok(...)` wrapper.
- No DTO declared in either controller file or under `…Application/Offers|Notifications/**` — `WithdrawOfferBffRequest`
  and the three Push* types now live in the module Abstractions and are referenced.
- Each op folder: one class per file + validator. Builds.
- After rebuild: offer builder (draft/preview/submit), my offers list, create/update/withdraw, and push
  subscribe/unsubscribe all behave exactly as before.

## Report
Append to `REPORT_BACKEND.md` ("Phase 5"): Offers + Notifications converted to typed `AizenApiResponse<T>` + PRT;
inline controller DTOs (`WithdrawOfferBffRequest`, Push* types) moved to module Abstractions; handlers split
one-class-per-file with validators.
