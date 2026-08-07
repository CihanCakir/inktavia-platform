# REPORT — BE_MO2c: scoped IdentityRead display-name endpoint + resolver retarget

> Executes `BE_MO2c_SCOPED_DISPLAYNAME_ENDPOINT.md`. Identity module (additive, new files only) + Marine.Participant.Mobile
> BFF (retarget the MO2b resolver). **Closes both MO2b blockers — verified live.** No admin grant, no admin-endpoint
> change, `QueryController` untouched, SR module unchanged. Least-privilege + minimal data + zero regression. **Not committed.**

---

## What was blocking (MO2b) and how MO2c fixes it

MO2b's resolver was correct but hit two blockers: the batch profile endpoint is **Admin-gated (403 for the mobile-bff SA)**
and its DTO **has no CompanyName**. MO2c fixes both by adding a **purpose-built, `IdentityRead`-gated, minimal** endpoint
and pointing the resolver at it — instead of granting admin or relaxing the admin endpoint.

## Identity module (additive — new files only)

1. **DTO** `ProfileDisplayNameDto { long ProfileId; string? DisplayName; }` (`Identity.Abstraction/Dto/Common`) — the
   minimal, cost-free projection (id + name, nothing else).
2. **Query + handler** `GetProfileDisplayNamesByProfileIds(long[])` → `IList<ProfileDisplayNameDto>`
   (`Identity.Application/Common/Query/…`). Batch-loads the profiles in **one** query (`IAizenUnitOfWork` + repo
   `GetAllAsync`, reading only the three name columns), ids **deduped + capped (500)**, and computes
   **`DisplayName = CompanyName ?? "{FirstName} {LastName}".Trim()`** (null when both empty) **in memory** (the
   interpolation isn't SQL-translatable). No N+1.
3. **New controller** `IdentityLookupController` (`Controller/V1/Identity`) — follows the `UserLinkController`
   convention: `[Route("api/v1/identity")]`, **no blanket controller policy**, and the single action
   `GET /api/v1/identity/profiles/display-names?ids=1&ids=2…` carries its **own** `[Authorize(Policy = "IdentityRead")]`
   (the service-token READ axis, callable by any trusted BFF SA — not Admin). `QueryController` and every existing admin
   endpoint are **byte-for-byte unchanged**. This is the reusable primitive for all provider/user name enrichment.

**Security rationale:** `IdentityRead` already exists (`RequireRole "identity_read","admin_user","identity.read","identity.admin"`)
and the marine-mobile-bff SA carries `identity_read`. Because the endpoint exposes only `{ profileId, displayName }` — a far
narrower surface than the admin "full-profile" bulk endpoint — serving it at `IdentityRead` is least-privilege and safe. No
`identity.admin` grant; no admin-endpoint relaxation.

## Mobile BFF (retarget the already-built resolver)

- `IIdentityRemoteCall`: **replaced** the MO2b admin call `GetUserProfilesByProfileIds` (admin, 403) with
  `GetProfileDisplayNamesByProfileIds([Refit.Query(CollectionFormat.Multi)] long[] ids)` on the new endpoint
  (`AizenApiResponse<List<ProfileDisplayNameDto>>`). The admin method is fully removed (0 references).
- `ProviderNameResolver`: now reads `p.DisplayName` **directly** (CompanyName-first is computed Identity-side → **B2
  solved server-side**). The batch (one call) / cache / graceful-fallback and cost-free behaviour are unchanged; the MO2
  offer enrichment (list + detail + reject) is untouched.

## Verification (live)

Rebuilt + restarted `identity-api` + `bff-marine-mobile`. Using a real `client_credentials` token for
`marine-mobile-bff` (realm roles `identity_read`, `identity_write` — **no** admin):

- ✅ **New endpoint 200 + CompanyName-first.** `GET …/profiles/display-names?ids=100011&ids=11012&ids=11013&ids=999999999`
  → **HTTP 200**:
  `[{11012,"Teknik Servis"},{11013,"CargoDry Ekip"},{100011,"PROVIDER 2 AS"}]`. **Profile 100011 resolves to the company
  "PROVIDER 2 AS"** (not the person "Cihan Çakır") — B2 solved. The others have no CompanyName so they fall back to the
  person name, as designed.
- ✅ **Callable at IdentityRead (no 403).** The same SA token that **403s** on the admin `bulk-by-profile-ids` (verified,
  unchanged) gets **200** here. No token → **401**.
- ✅ **Unknown id → graceful.** `999999999` is simply **absent** from the result → resolver `GetValueOrDefault` → null →
  the FE keeps its localized fallback. No error.
- ✅ **No N+1.** The resolver makes exactly one `GetProfileDisplayNamesByProfileIds` call per uncached set.
- ✅ **Cost-free / no id leak.** `MobileServiceRequestOfferDto` exposes `ProviderName` only — **no** `providerProfileId` /
  cost / commission (the profile id is used solely as the resolver's lookup key). The mobile offers route is still
  registered (401 without a token).
- ✅ **Zero regression.** `QueryController` and the admin endpoints are unmodified (git shows no change); the admin
  endpoint still 403s. **Identity + BFF build clean** (0 errors; only pre-existing codebase-wide CS8609 nullability
  warnings).

### Acceptance tests
1. Endpoint returns `{profileId, displayName}` CompanyName-preferred — ✓ (100011 → "PROVIDER 2 AS").
2. Callable with an `identity_read` token (200, not 403); rejects no token (401); admin endpoint still 403 — ✓ (live).
3. Resolver batch = one call, no N+1 — ✓.
4. Offer payload exposes `ProviderName`, no `providerProfileId` / cost / commission — ✓.
5. Unknown id → null → FE fallback, no error — ✓ (live).

### On-screen (participant token — env-dependent)
The SA → endpoint → resolver chain is proven end-to-end; once an owner opens the offers screen (mobile OTP/Keycloak login),
each offer's `ProviderName` carries the resolved name — e.g. SR 9011's accepted offer (provider profile 100011) shows
**"PROVIDER 2 AS"** instead of "Servis Sağlayıcı". The offers route itself is auth-gated (401 verified); the full
authenticated round-trip needs a participant owner token.

## Reuse & next
`IdentityLookupController.GetProfileDisplayNames` is the shared, least-privilege primitive for BFF-side name enrichment —
provider disputes / jobs / conversations can call the same `IProviderNameResolver` (which now points here). This closes the
MO2b provider-name gap. Next: **MO3** (accept → checkout, iyzico-gated).

**Not committed.**
