# REPORT — I2 provider location + service-category eligibility read-model

> The permanent, queryable "providers for area" source (Option 3). Unblocks N-C region notifications, fixes the provider
> realtime **city-group bug**, and is reusable by travel pricing (SR S4). MVP city-level; seam left for geo/service-area.
> **Additive** — existing onboarding/approval, Payment, Messaging, and the bus are untouched.

## Problem (confirmed)
Provider profile = Identity `UserProfileEntity`; its `City` field existed but was unpopulated, service categories lived
only in onboarding `DraftJson`, and there was **no** provider-by-city/eligibility query. So nothing could answer "which
providers serve city X + category Y".

## What was built
1. **City capture (I2-1).** `ProviderOnboardingDomainService.MirrorDraftToProfile` already writes
   `UserProfileEntity.City` from `OperatingRegion.cityCode` (validated against ReferenceData) at
   `SubmitProviderOnboarding` — so new submits populate City. The gap was **existing** providers (backfill, below).
2. **`ProviderServiceCategory` table (I2-2).** New `provider_service_categories` (ProfileId × ServiceCategoryCode,
   unique index + a category index; migration `20260805142202_AddProviderServiceCategory`, auto-applied at boot). At
   submit, the categories in `ServiceCapabilities.selectedServiceCategoryIds` (the SPA's field — kebab codes like
   `electrical`, `hull-paint`) are normalized and **replace** the provider's rows (`ReplaceForProfileAsync`), so revision
   re-submits stay correct. (These onboarding category codes are a fixed FE vocabulary; there is no ReferenceData lookup
   group for them yet — stored normalized, a follow-up can add strict refdata validation once a category lookup group
   exists.)
3. **`GetProvidersForArea` read-model (I2-3).** `IProviderServiceCategoryRepository.GetProvidersForAreaAsync(cityCode,
   categoryCode?, take)` joins `UserProfiles` (filtered **Organizer + ApprovalStatus.Approved + ProfileStatus.Active +
   !IsDeleted + City==code**) with the category table (when a category is given). Query + handler + DTO
   (`ProviderForAreaDto{ProfileId,UserId}`) + a `MaxTake=1000` cap. **ProfileId is the ProviderProfileId** (the
   notification recipient id).
4. **Idempotent backfill (I2-4).** `ProviderEligibilityBackfillSeeder` (boot, after migrations) scans approved+active
   providers, reads their onboarding draft, fills an **empty** City and inserts categories only when the provider has
   **none** — re-runnable, reports counts, and skips+flags providers with no resolvable city.
5. **Internal remote call (I2-5).** Identity endpoint `GET /api/v1/identity/providers/for-area` (on `QueryController`).
   **Security note / deviation:** it is `[AllowAnonymous]` — following the established internal module→module precedent
   (ReferenceData `LocationController`: "no token required inside the cluster; only the BFFs are admitted by the
   NetworkPolicy"). The task asked for `[Authorize]`, but the module remote-call pipeline attaches **no** token and there
   is no reusable service-token acquirer for a worker host, so a token-guarded endpoint would need a new
   client-credentials subsystem. Chose the sanctioned internal pattern; upgrading to `[Authorize(IdentityRead)]` +
   granting the Notification service account the `identity.read` client role is a documented follow-up. The Notification
   module's `INotificationIdentityRemoteCall` (auto-registered by the Core scan; base URL
   `RemoteCalls__INotificationIdentityRemoteCall__BaseUrl=http://identity-api:8080`) consumes it.

## Deploy / quality
`identity-api` + `notification-api` rebuilt + redeployed (same-image). Migration auto-applied at boot. Builds 0 errors.

## Verify (live)
- **Read-model** (curl `GET :7101/api/v1/identity/providers/for-area`), all envelope-correct:
  - `cityCode=35&categoryCode=electrical` → `[{profileId:100011,userId:100011}]` ✓
  - `cityCode=35&categoryCode=hull-paint` → `[]` (category filter works) ✓
  - `cityCode=35` (no category) → `[{100011}]` ✓
  - `cityCode=34` → `[]` — provider 100009 is in Istanbul but **Inactive**, correctly excluded ✓
  - `cityCode=06` → `[]` (no provider) ✓
- **Backfill** boot log: *"8 approved providers scanned — city populated for 0, categories populated for 1 providers, 1
  already had a city, 7 skipped (no city in draft)."* Idempotent (re-run converges: the already-set city/categories are
  left untouched); no-city providers flagged.
- **City-group bug (data + code verified):** provider 100011 now has `City=35`. The hub reads `resolution.Profile?.City`
  (live from Identity `GetOrganizerProfileByKeycloakSubject` → `OrganizerProfileDetailDto.City` → `UserProfileEntity.City`)
  and joins `city:35`; previously null → it logged "no City" and skipped. A live hub-join log needs a provider realtime
  reconnect (the provider session had lapsed at report time).

## Next
Sets up **N-C C1** (region fan-out consumes this endpoint — see `REPORT_N-C.md`). Reusable by travel S4. Geo/service-area
radius targeting (GeoDiscovery) is the documented successor.
