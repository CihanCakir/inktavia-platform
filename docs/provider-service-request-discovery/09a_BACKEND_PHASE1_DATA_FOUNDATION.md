# 09a — Backend Phase 1: Discovery data foundation (no geo yet)

Run first. Scope: make the discovery query **correct**. Geo comes in 09b; the BFF in 09c. Do not touch the
frontend.

## Facts verified in the source (accept them)

- `Modules/ServiceRequest/src/…/Domain/Entities/ServiceRequest/ServiceRequestEntity.cs` **has**
  `LocationLatitude`/`LocationLongitude` (`decimal?`), `LocationCityCode` (canonical **plate code**: `34`, `35`,
  `48`), `Priority` (Low=1 … **Urgent=4, Emergency=5**), `ExpiresAt`, `VesselId`, `VesselName`.
- It **does not have** `PublishedAt`, budget fields, or vessel type/manufacturer/model/length.
- `ServiceRequestRepository.BuildProviderOpenQuery`:
  - **excludes** requests the calling provider already bid on (`!x.Offers.Any(o => o.ProviderProfileId == me)`),
  - `.Include(x => x.Offers).Include(x => x.Attachments)` and counts them **in memory**.
  Both are wrong for this screen. The exclusion must become a projection; the `Include`s must become subqueries.
- There is **no Vessel remote call and no Vessel batch endpoint** anywhere in the solution.

## Auth (unchanged, non-negotiable)

Provider identity comes from the BFF assertion:
`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`. **Never** from a DTO, query string, route
value, or a header read inside a handler. The filter DTO must have **no `ProviderProfileId` field**.

Missing or `<= 0` ⇒ **reject**. Do **not** copy `GetOpenServiceRequestsQueryHandler`, which returns an empty list
in that case: a provider seeing "no work" because their identity failed to resolve is a silent failure, and there
is no way to tell it apart from a quiet market.

## Budget — DECIDED: **NO** (2026-07-14)

**Do not add `BudgetMin` / `BudgetMax` / `BudgetCurrencyCode`. Do not add them to any DTO. Do not add a
placeholder.** The mock shows a budget range; the product decision is that we do not ask the owner for one.

Reasons, so nobody re-adds them in six weeks:

- A published budget **anchors every offer to the ceiling**. Providers price to the stated maximum, not to the
  work. With supply still thin, that is a transfer from the buyer — our primary customer — to the seller.
- Owners **cannot** state a budget for marine work. Antifouling on a 45 ft hull varies threefold with hull
  condition and paint. Asking produces either a made-up number or an empty field.
- It is not the information a provider needs anyway. Vessel type/length, category, urgency, marina and distance
  decide whether a job is worth the drive. A budget replaces none of them.

The market-range idea (show a range **derived from historical offers**, rather than asking the owner) is
recorded as post-MVP in `08_IMPLEMENTATION_ROADMAP.md`. It informs the owner without whispering a ceiling to the
supply side.

**If a future decision reverses this**, the whole chain ships together — columns + validation + an owner-side
producer. Columns without a producer are dead columns and a permanently grey card section.

## Work

### 1. `PublishedAt` (blocker)
- Nullable UTC column, private setter + domain method.
- Set in the **publication command** at the Draft → Open/Published transition. **Idempotent**: a republish must
  not overwrite an existing value.
- **Backfill**: rows already in a published status ⇒ `PublishedAt = CreateDate`. State in the report that these
  are **approximate** — `CreateDate` is draft creation, not publication.
- `CreateDate` must not drive "added today" or relative time. It does not give a missing value there; it gives a
  **wrong** one, which is worse because it looks right.

### 2. Vessel snapshot (decision already taken: snapshot, not a batch endpoint)
- Columns on `ServiceRequestEntity`, written **at publication**: `VesselTypeCode`, `VesselManufacturer`,
  `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode` (`VesselName` exists).
- **Immutable after publication** — the snapshot describes the boat *as the job was advertised*; it must not
  change under providers who already bid.
- **One-off backfill** from Vessel where the vessel still exists; `null` otherwise. **Do not guess.**
- **No per-card Vessel call, ever.** There is no remote call and no batch endpoint; one call per card would be
  N+1 across the network on every keystroke and every map pan.

### 3. Discovery query
- `ProviderServiceRequestDiscoveryFilter` (Abstraction) per doc 07 — **minus** the geo fields, which arrive in
  09b. Typed. **No `object`. No `System.Text.Json.JsonElement` on any wire contract** (MVC binds with Newtonsoft;
  `JsonElement` silently loses data — it has already cost this project days).
- `GetProviderServiceRequestDiscoveryQuery` + handler + **FluentValidation** validator (`PageSize` ≤ 50; unknown
  `LocationCityCode` ⇒ **reject**, same rule as onboarding).
- Repository: a **`Select` projection**. No `Include`.
  - `OfferCount`, `AttachmentCount`, `PhotoCount` → `.Count()` subqueries.
  - `HasProviderOffer` → `.Any(o => o.ProviderProfileId == me && !o.IsDeleted)`.
  - `ProviderOfferId`, `ProviderOfferStatus` → projected from the **caller's own** offer.
  - **Other providers' offer rows must never be materialised.** Today's query loads every offer — including
    their amounts — to produce two integers. That is a privacy smell as well as a performance defect.
- **Delete the hard exclusion.** `OfferState { Any, NotOffered, Offered }` becomes an **optional predicate**.
- **Cursor pagination**: `PublishedAtDesc` ⇒ `(PublishedAt, Id)`, `PriorityDesc` ⇒ `(Priority, Id)`. Always
  tiebreak on `Id`. Encode a **filter fingerprint**; a cursor minted under different filters is **rejected**, not
  silently mis-paged. **No `TotalCount`** on the list.
- Indexes: `(Status, LocationCityCode)`, `(Status, PublishedAt)`, and a search index for `Title`.

## Acceptance (observations, not claims)

- A provider who has already bid on a request **sees it**, badged with their offer state.
- The generated SQL for the discovery query is pasted in the report: **no child collection is materialised**;
  counts are subqueries.
- Cursor paging is stable while new requests are published (no duplicates, no skipped rows). A cursor from
  different filters is rejected.
- Sending `providerProfileId` from the client changes nothing.
- Missing identity ⇒ rejection, not an empty list.

## Tests

Repository filters · offer-state projection · count projections · cursor stability under concurrent insert ·
privacy (no `OwnerUserId`, no `RequestedByEmail`, no foreign offer rows in the projection) · `PublishedAt`
idempotency on republish · unknown city code rejected · client-supplied `providerProfileId` cannot change the
authorization context.

## Report

`docs/provider-service-request-discovery/REPORT_BACKEND_09a.md`: the generated SQL; what was backfilled and how
approximate it is; the budget decision you were given (or that you were blocked on); what you could **not** do.

**Unfinished is *not done*** — not "done with open items". Do not put work the feature requires into an
"open items" section; if it is not working, the feature is not working.
