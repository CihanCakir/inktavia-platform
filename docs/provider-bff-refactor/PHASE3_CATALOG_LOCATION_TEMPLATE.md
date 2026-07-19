# Phase 3 — Catalog + Location + Template controllers → gold standard

Three fully-broken controllers (direct remote-call, `IActionResult`, no CQRS/PRT) brought to the canonical pattern.
**Apply the Phase 2-fix conventions from the start** (see `PHASE2_FIX_FILE_SPLIT_AND_ABSTRACTION.md`):
- One class per file: `*Query.cs`/`*Command.cs`, `*Handler.cs`, `*Validator.cs` — separate, normally formatted.
- Controller injects **only** `IAizenCQRSProcessor`; each endpoint `[ProducesResponseType(typeof(T), 200)]` + returns
  `Task<AizenApiResponse<T>>` + `SetResponse(result)`.
- Handlers own identity resolve (`_resolver.ResolveAsync` then the ProfileId guard) + the remote-call, return `.Body`.
- **All request/response DTOs come from the relevant module's Abstraction** (`Aizen.Modules.ServiceRequest.Abstraction`
  for Catalog/Template, `Aizen.Modules.ReferenceData.Abstraction` for Location) — nothing inline. Use each
  remote-call method's `.Body` type as the Query/Command response type + the `ProducesResponseType` type.
- Remove the hand-rolled `Ok(new { header = new { isSuccess = true } })` on the deletes.

Routes, verbs, `[Tags]`, policies, and query/body params stay **exactly** as they are.

## Catalog — `CatalogController` (`api/v1/provider/offer-catalog`, policy `ProviderActive`, `IServiceRequestRemoteCall`)
Feature folder `Catalog/`. Identity resolve in every handler.
| Endpoint | Op (folder) | Kind | Remote call | Response |
|---|---|---|---|---|
| `GET` | `Catalog/Query/ListOfferCatalogBff/` | Query | `ListCatalogItems()` | `.Body` type |
| `POST` | `Catalog/Command/CreateOfferCatalogItemBff/` | Command | `CreateCatalogItem(body)` | `.Body` type |
| `PUT {id}` | `Catalog/Command/UpdateOfferCatalogItemBff/` | Command | `UpdateCatalogItem(id, body)` | `.Body` type |
| `DELETE {id}` | `Catalog/Command/DeleteOfferCatalogItemBff/` | Command | `DeleteCatalogItem(id)` | `bool` (true) |
Body type is the module `CatalogItemRequest` (`…ServiceRequest.Abstraction.Request.Offer`) — bind `[FromBody]` to it.
Validators: Create/Update — body not null + `Id > 0` (Update); Delete — `Id > 0`.

## Location — `LocationController` (`api/v1/provider/location`, policy `ProviderAuthenticated`, `IReferenceDataRemoteCall`)
Feature folder `Location/`. **No identity resolve** (reference data, not provider-scoped — matches current
behaviour; do not add a resolver).
| Endpoint | Op (folder) | Kind | Remote call | Response |
|---|---|---|---|---|
| `GET cities` | `Location/Query/GetCitiesBff/` | Query (`Country="TR"`) | `GetCitiesByCountry(Country)` | `.Body` type |
Validator: `Country` not empty.

## Template — `TemplateController` (`api/v1/provider/offer-templates`, policy `ProviderActive`, `IServiceRequestRemoteCall`)
Feature folder `Template/`. Identity resolve in every handler (drop the `EnsureIdentityAsync` controller helper — that
logic moves into each handler).
| Endpoint | Op (folder) | Kind | Remote call | Response |
|---|---|---|---|---|
| `GET` | `Template/Query/ListOfferTemplatesBff/` | Query | `ListTemplates()` | `.Body` type |
| `GET {id}` | `Template/Query/GetOfferTemplateBff/` | Query (`Id`) | `GetTemplate(id)` | `.Body` type |
| `POST` | `Template/Command/CreateOfferTemplateBff/` | Command | `CreateTemplate(body)` | `.Body` type |
| `PUT {id}` | `Template/Command/UpdateOfferTemplateBff/` | Command | `UpdateTemplate(id, body)` | `.Body` type |
| `DELETE {id}` | `Template/Command/DeleteOfferTemplateBff/` | Command | `DeleteTemplate(id)` | `bool` (true) |
Body type is the module `OfferTemplateRequest` (`…ServiceRequest.Abstraction.Request.Offer`). Validators: Get/Delete —
`Id > 0`; Create/Update — body not null + `Id > 0` (Update).

## Namespaces
`Aizen.Bff.MarineProvider.Application.Catalog`, `.Location`, `.Template` (feature-level, flat).

## Acceptance
- All three controllers inject only `IAizenCQRSProcessor`; `grep RemoteCall` in each returns nothing.
- Every endpoint typed `AizenApiResponse<T>` + `[ProducesResponseType]`. 10 operations under
  `{Catalog|Location|Template}/{Query|Command}/{Op}/`, one class per file, validators present.
- No response/request DTO declared inside these feature folders — all from the module Abstractions.
- Builds. After rebuild: offer-catalog list/create/update/delete, offer-templates list/get/create/update/delete, and
  `/location/cities?country=TR` all behave exactly as before (smoke on the offer builder screens where these feed).

## Report
Append to `REPORT_BACKEND.md` ("Phase 3"): Catalog/Location/Template converted to CQRS (gold standard + Phase-2-fix
conventions); controllers free of remote-calls; typed + PRT; DTOs from module Abstractions; deletes no longer
hand-roll the envelope.
