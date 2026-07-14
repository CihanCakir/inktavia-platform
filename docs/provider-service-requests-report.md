# Provider Service Requests, Offers & Job Lifecycle — Report

**Date:** 2026-07-13
**Branch:** `feature/messaging-registration`

---

## Part A — ServiceRequest Module: New Queries & Security Fix

### A1. Open service requests a provider may bid on

**Endpoint:** `GET /api/v1/service-requests/provider/open`

**Query params:** `pageIndex`, `pageSize`, `serviceCategoryCode`, `locationCityCode`, `locationCountryCode`, `searchTerm`

**Implementation:**
- Repository: `GetOpenForProviderAsync` / `CountOpenForProviderAsync` in `ServiceRequestRepository`
- Biddable statuses: `Open`, `WaitingForOffer`, `OfferReceived`
- Excludes: SRs the provider already has an offer on (via `!x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)`)
- Excludes: SRs already assigned (`x.Assignment == null`)
- Filters: category (case-insensitive), city, country, min priority, search (title/code)
- Sorted newest first, paged
- City/region is plain equality — geo/radius belongs to GeoDiscovery module

**Response DTO:** `GetOpenServiceRequestsResponse` with `OpenServiceRequestItemDto` containing: Id, RequestCode, Title, ServiceCategoryCode, LocationCityCode, LocationCountryCode, LocationMarinaName, Priority, RequestedStartDate/EndDate, AttachmentCount, OfferCount, CreatedAt

### A2. The provider's own offers

**Endpoint:** `GET /api/v1/service-requests/provider/my-offers`

**Query params:** `pageIndex`, `pageSize`, `status` (ServiceRequestOfferStatus enum value)

**Implementation:**
- Repository: `GetByProviderProfileIdWithSrAsync` in `ServiceRequestOfferRepository` — filters by status, paged
- Handler: looks up each SR by ID to populate the snapshot (title, code)
- Provider identity from `IAizenInfoAccessor.KeycloakTokenInfoAccessor.KeycloakTokenInfo.ProviderProfileId`

**Response DTO:** `GetMyOffersResponse` with `MyOfferItemDto` containing: OfferId, ServiceRequestId, ServiceRequestTitle, ServiceRequestCode, OfferStatus, TotalAmount, CurrencyCode, EstimatedStartDate/EndDate, CreatedAt, AcceptedAt, RejectedAt, WithdrawnAt

### A3. IDOR Fix — Offer Update and Withdraw

**This was a live IDOR vulnerability.**

`UpdateServiceRequestOfferCommandHandler` and `WithdrawServiceRequestOfferCommandHandler` extracted `currentUserId` from `IAizenInfoAccessor` but **never compared it to `offer.ProviderUserId`**. Any authenticated user could update or withdraw any other provider's offer by knowing the offer ID.

**Fix:** Both handlers now check `offer.ProviderUserId != currentUserId` before proceeding and throw `UnauthorizedAccessException` on mismatch.

| Handler | File | Line added |
|---------|------|------------|
| `UpdateServiceRequestOfferCommandHandler` | `.../Command/Offer/UpdateServiceRequestOffer/` | `if (offer.ProviderUserId != currentUserId) throw new UnauthorizedAccessException(...)` |
| `WithdrawServiceRequestOfferCommandHandler` | `.../Command/Offer/WithdrawServiceRequestOffer/` | Same check |

### Controller

All three new endpoints are on the existing `ProviderJobsController` (route `api/v1/service-requests/provider`):
- `GET /open` — open service requests
- `GET /my-offers` — provider's offers  
- `GET /jobs` — existing, unchanged

---

## Part B — MarineProvider BFF

### Remote Call Extensions

`IProviderServiceRequestRemoteCall` extended with:
- `GetOpenServiceRequests(...)` → `GET /provider/open`
- `GetMyOffers(...)` → `GET /provider/my-offers`
- `GetServiceRequestDetail(...)` → `GET /{serviceRequestId}`
- `CreateOffer(...)` → `POST /{srId}/offers`
- `UpdateOffer(...)` → `PUT /{srId}/offers/{offerId}`
- `WithdrawOffer(...)` → `PATCH /{srId}/offers/{offerId}/withdraw`

### BFF Controllers

| Controller | Route | Endpoints |
|------------|-------|-----------|
| `ProviderServiceRequestsController` | `/api/v1/provider/service-requests` | `GET /open` |
| `ProviderOffersController` | `/api/v1/provider` | `GET /offers`, `POST /service-requests/{id}/offers`, `PUT /service-requests/{id}/offers/{offerId}`, `POST /offers/{offerId}/withdraw` |

### BFF Handlers

All handlers:
1. Call `IProviderProfileResolver.ResolveAsync()` first
2. Fail closed when `ProfileId` is null or 0
3. Use typed properties (no `JsonElement`)
4. Surface module error messages from `result.Header.ErrorMessage`

| Handler | Purpose |
|---------|---------|
| `GetOpenServiceRequestsBffQueryHandler` | Proxies to module open query |
| `GetMyOffersBffQueryHandler` | Proxies to module my-offers query |
| `CreateOfferBffCommandHandler` | Sets ProviderProfileId from resolver, calls module |
| `UpdateOfferBffCommandHandler` | Proxies update, relies on module IDOR check |
| `WithdrawOfferBffCommandHandler` | Proxies withdraw with reason |

### Authorization

- `ProviderServiceRequestsController` and `ProviderOffersController` use `ProviderActive` policy (Approved + Active)
- Provider identity is NEVER taken from request body or query string
- `CreateOfferBffCommandHandler` explicitly sets `ProviderProfileId` from the resolver, overriding any client-supplied value

---

## Part C — Frontend

**Not implemented.** The frontend is in a separate repository (`inktavia-marine-provider-web`). The prompt specifies:
- C1: Service request discovery page (`/app/service-requests`)
- C2: Request detail + offer form (`/app/service-requests/:id`)
- C3: My offers list (`/app/offers`)
- C4: Job detail + lifecycle actions (`/app/jobs/:id`)
- C5: Dashboard real counts

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Open Items

| Item | Status |
|------|--------|
| Service request detail endpoint in BFF (access control: only open + own-offer + assigned) | Remote call wired, BFF handler not yet implemented |
| Job lifecycle BFF endpoints (accept, start, complete, work logs) | Not implemented |
| Frontend pages (C1-C5) | Separate repo |
| Browser verification of cross-provider isolation | Not done |
| Offer create handler: verify SR is in a biddable status before forwarding | Relies on module-side validation |
| SR detail access control: provider should not read arbitrary SRs | TODO — needs BFF-side or module-side guard |
