# REPORT — BE_MO2b: BFF-side provider-name resolver (enrich owner offers)

> Executes `BE_MO2b_PROVIDER_NAME_RESOLVER.md`. **Marine.Participant.Mobile BFF only** — module + Identity unchanged.
> The reusable resolver + MO2 enrichment are implemented, build clean, and are cost-free / no-N+1. **Two environmental
> blockers (verified live) stop the *visible* name from resolving today — both outside "BFF-only / Identity unchanged".**
> Additive; graceful (no regression). **Not committed.**

---

## What was built (BFF-only, additive)

1. **Remote call** — added `GetUserProfilesByProfileIds(long[] profileIds)` to the Mobile BFF `IIdentityRemoteCall`
   (`[Refit.Query(CollectionFormat.Multi)]` → repeated `profileIds=` query keys), pointing at the existing Identity
   endpoint `GET /api/v1/identity/admin/users/profiles/bulk-by-profile-ids`. Typed, envelope-correct. Mirrors the same
   method the AdminPanel BFF already uses. **No new Identity endpoint.**
2. **Reusable resolver** — `IProviderNameResolver` / `ProviderNameResolver` in `Common/Services` (registered
   `AddScoped`): given a set of provider profile ids → **one** batch Identity call → an `id → displayName` map. **No
   N+1** (collect all ids, one call). Short `IMemoryCache` TTL (10 min; 2 min negative) so a provider seen across
   several offers/screens is fetched once. A missing/unknown id → `null`; **any failure is caught → `null`** (the FE
   keeps its localized fallback — never throws for a display nicety).
3. **MO2 enrichment** — the offers **list** handler collects every offer's `providerProfileId` and resolves them in one
   batch; the **detail** + **reject** handlers resolve the single id. `MapOffer(offer, providerName)` sets
   **`ProviderName`** on the cost-free `MobileServiceRequestOfferDto`. **The provider profile id is used only as the
   lookup key and is never placed on the DTO.**

**Reusability:** the resolver is a shared `Common` service — provider disputes / jobs / conversations can reuse the same
id→name resolution. Only MO2 owner-offers is wired in this pass.

## Verification (code)

- **BFF builds 0 errors.**
- **Cost-free / no id leak:** `MobileServiceRequestOfferDto` exposes `ProviderName` and has **no** `providerProfileId` /
  `providerUserId` / commission / funding / provider-net / part-cost. `providerProfileId` appears **only** as the
  resolver's lookup argument in the handlers (grep-confirmed).
- **No N+1:** the resolver makes exactly one `GetUserProfilesByProfileIds` call per uncached set; the list handler passes
  all ids in a single `ResolveAsync`.
- **Graceful fallback:** on a missing id or any error, `ProviderName` is `null` → the FE renders "Servis Sağlayıcı" —
  identical to pre-MO2b behaviour, so **no regression**.

## ⚠ Two blockers to the *visible* result (verified live) — outside "BFF-only / Identity unchanged"

**The spec's two premises about the existing Identity endpoint don't hold in this codebase:**

1. **The batch endpoint is Admin-gated; the mobile-bff SA can't call it (verified 403).**
   `bulk-by-profile-ids` is `[Authorize(Roles = "Admin,identity.admin")]`. A `client_credentials` token for
   `marine-mobile-bff` carries realm roles **`identity_read`, `identity_write`** — **not** `Admin`/`identity.admin`.
   Calling the endpoint with that token returns **HTTP 403** (tested directly against `identity-api:7101` with the SA's
   own token). So the resolver's call 403s → graceful fallback → names stay "Servis Sağlayıcı".
   - **Fix (pick one, outside this task's scope):** grant the `marine-mobile-bff` service account the `identity.admin`
     realm role in Keycloak (ops/config — note: broadens a participant BFF's identity privileges, a security call), **or**
     relax that endpoint's `[Authorize]` to also accept `identity_read` (Identity change), **or** add a non-admin
     name-only batch endpoint (Identity change).

2. **`UserProfileListItemDto` doesn't carry `CompanyName` — so the name is the person, not the company.**
   The batch returns `UserProfileListItemDto` (Id/UserId/**FirstName/LastName**/…) — **no `CompanyName`** (it lives on
   `UserProfileEntity`, not this projection). So `displayName = "{First} {Last}".Trim()`. For the seed providers this is
   already company-like ("Teknik Servis", "CargoDry Ekip", "Marina Operasyonlar"), but for **profile 100011** it would
   show **"Cihan Çakır"**, not the company **"PROVIDER 2 AS"** (which *is* in the data, just not in this DTO).
   - **Fix (outside scope):** project `CompanyName` into `UserProfileListItemDto` (Identity Abstraction change), then the
     resolver's `CompanyName ?? person` logic yields the company name. The resolver already prefers company-first in
     intent — it just has no company field to read today.

**Net:** the BFF code is correct, additive, cost-free, no-N+1, and non-regressing, but it **cannot surface a real name in
the current environment** without one of the above (a Keycloak role grant and, for the company name, a one-field Identity
DTO change). Both are explicitly outside "BFF-only / module + Identity unchanged", so they are **flagged for a decision**
rather than done here.

## Verify (on screen — once a participant token + the above are in place)
- [ ] With the SA granted `identity.admin`: offers show the resolved name (person name today; company name once
      `CompanyName` is projected) instead of "Servis Sağlayıcı"; graceful fallback otherwise.
- [x] Payload grep: `ProviderName` present, **no** `providerProfileId`, no cost/commission. ✓ (verified statically)
- [x] One Identity call per offers list (no N+1). ✓ (code)

## Recommendation / next
Decide the resolution for blocker (1): I recommend granting `identity.admin` to the `marine-mobile-bff` SA **only if** the
security review is comfortable with a participant BFF reading admin user-profile data; otherwise add a scoped non-admin
name-batch endpoint. For blocker (2), a one-line `CompanyName` addition to `UserProfileListItemDto` unlocks the company
name. Both are quick once approved. Then **MO3** (accept → checkout, iyzico-gated).

**Not committed.**
