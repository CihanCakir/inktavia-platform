# 13a — Backend: two projected fields for the request card (Claude Code)

Self-contained. Implements Phase C1 of `13_REQUEST_CARD_STATES_ROADMAP.md`. Two small additive fields on the
discovery projection so the card can show the "offer submitted" and "updated" states. **No migration, no new
endpoint, no new remote call** — these are pass-through projections. Do not touch the frontend.

## Context verified in source (2026-07-15)

- `Modules/ServiceRequest/.../Repository/Repositories/ServiceRequestRepository.cs` — `GetDiscoveryAsync`
  projects the caller's own offer at **lines ~279–287**:
  ```csharp
  HasProviderOffer = x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted),
  ProviderOfferId = x.Offers
      .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
      .Select(o => (long?)o.Id).FirstOrDefault(),
  ProviderOfferStatus = x.Offers
      .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
      .Select(o => o.Status.ToString()).FirstOrDefault(),
  ```
  There is a **second** discovery projection for markers/summary (`BuildDiscoveryBaseQuery` region, ~line 328) —
  the amount is only needed on the **list** projection (markers carry no offer amount), so add it to
  `GetDiscoveryAsync` only.
- `ServiceRequestOfferEntity` has `public decimal TotalAmount`.
- `ServiceRequestEntity : AizenEntityWithAudit`. The audit "last modified" field is **`ModifyDate` (`DateTime?`)**,
  and `CreateDate` (`DateTime?`). There is **no `UpdateDate`** — use `ModifyDate`. `PublishedAt` was added in 09a
  and backfilled from `CreateDate`.
- DTOs: module `ProviderDiscoveryItemDto` (`…Abstraction/Response/Provider/ProviderDiscoveryResponse.cs`) and BFF
  `ProviderDiscoveryBffItemDto` (`…Bff.MarineProvider.Application/ServiceRequests/GetProviderDiscoveryBffResponse.cs`).

## Work

### 1. My offer amount — `ProviderOfferTotalAmount` (decimal?)

- Add `public decimal? ProviderOfferTotalAmount { get; init; }` to **`ProviderDiscoveryItemDto`**.
- Project it in `GetDiscoveryAsync`, right after `ProviderOfferStatus`, using the **same filtered subquery** so
  it stays the caller's own offer and nothing else:
  ```csharp
  ProviderOfferTotalAmount = x.Offers
      .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
      .Select(o => (decimal?)o.TotalAmount).FirstOrDefault(),
  ```
- **Privacy: only the caller's own amount.** Never project another provider's offer amount, and do not add it to
  the markers DTO. This is the same rule that already governs `ProviderOfferId`.

### 2. "Updated after publish" — `IsUpdated` (bool)

- Add `public bool IsUpdated { get; init; }` to `ProviderDiscoveryItemDto`.
- Project it in `GetDiscoveryAsync`:
  ```csharp
  IsUpdated = x.PublishedAt != null && x.ModifyDate != null && x.ModifyDate > x.PublishedAt,
  ```
- **Document that this is approximate**: `ModifyDate` moves on any audit write after publication, not only an
  owner content edit, so `IsUpdated` may be true for a request that was only touched internally. The precise
  version (a dedicated `ContentUpdatedAt` set solely by the owner-edit command) is post-MVP — leave a code
  comment saying so, do not build it now.
- Add it to the **list** projection only (the card is a list concern). Not markers, not summary.

### 3. Carry both through the BFF

- Add `ProviderOfferTotalAmount` (`decimal?`) and `IsUpdated` (`bool`) to `ProviderDiscoveryBffItemDto`.
- In `GetProviderDiscoveryBffQueryHandler`, map them straight through from the module item to the BFF item
  (they sit alongside the existing `HasProviderOffer` / `ProviderOfferId` / `ProviderOfferStatus` mapping). No new
  remote-call parameter — these come back in the module response body.

## Constraints

- No migration. No new endpoint. No new remote call. No `object` / `JsonElement` on the wire.
- No domain logic in the BFF — pure pass-through.
- Provider identity from the assertion, as before; `providerProfileId` in the projection is the resolved one, not
  anything from the client.
- Do not expose any other provider's offer id, status, or amount. `OfferCount` stays the only cross-provider
  aggregate.
- Do not add these to the markers or summary responses.

## Acceptance — observed, not asserted

- A request the calling provider has bid on returns a non-null `providerOfferTotalAmount` equal to their offer's
  `TotalAmount`. Paste the row.
- A request that was edited after publication returns `isUpdated = true`; a freshly published, untouched one
  returns `false`. Paste both.
- A request another provider bid on (but the caller did not) returns `hasProviderOffer = false`,
  `providerOfferTotalAmount = null` — **no leakage**. Paste it.
- The generated SQL still shows the offer subqueries server-side (no client evaluation).
- Build succeeds; the existing discovery browser flow still returns rows.

## Report

Append to `docs/provider-service-request-discovery/REPORT_BACKEND_09c.md` (section "13a — card fields"): the two
projected rows, the no-leakage row, and the note that `IsUpdated` is approximate. Unfinished is **not done**.
