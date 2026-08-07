# BE_MO2c — scoped, least-privilege display-name endpoint + retarget the resolver

> **Repos:** `addesso-project` — **Identity module** (small additive) + **Marine.Participant.Mobile BFF** (retarget the
> resolver). Unblocks MO2b: the resolver is built but hit two blockers — the batch profile endpoint is **Admin-gated (403
> for the mobile-bff SA)** and its DTO **has no CompanyName**. Fix **without** granting the mobile BFF admin or relaxing the
> admin endpoint: add a **purpose-built, `IdentityRead`-gated, minimal display-name endpoint** and point the resolver at it.
> Least-privilege + minimal data. Additive. **Do not commit.**

## Why this design (security-preserving)
- **`IdentityRead` policy already exists** — `AuthorizationPolicyExtensions`: `RequireRole("identity_read", "admin_user",
  "identity.read", "identity.admin")`. The **marine-mobile-bff SA carries `identity_read`**, and `UserLinkController`
  already uses `[Authorize(Policy = "IdentityRead")]` — the established **non-admin service-token read** pattern. So an
  endpoint gated this way is callable by the mobile BFF SA **without** any admin privilege.
- **Minimal data:** the new endpoint returns **only** `{ profileId, displayName }` — not full profiles. This is a far
  narrower surface than the existing Admin "`bulk-by-profile-ids` → full `UserProfileListItemDto`" endpoint, so exposing it
  at `IdentityRead` is safe. Do **not** grant `identity.admin` to the SA and do **not** relax the existing admin endpoint.

## Identity module (additive)
1. **DTO** (`Identity.Abstraction`): `ProfileDisplayNameDto { long ProfileId; string? DisplayName; }`.
2. **Query** `GetProfileDisplayNamesByProfileIds(long[] profileIds)` → `List<ProfileDisplayNameDto>`: batch-load the
   `UserProfile` rows by id (reuse the existing repo/UoW), projecting **`DisplayName = CompanyName ?? $"{FirstName}
   {LastName}".Trim()`** (null when both empty). One query, no N+1. Dedupe/limit the id list defensively.
3. **Endpoint on a NEW general service-token lookup controller** — do **not** modify the Admin-heavy `QueryController`.
   Follow the **established convention** (`UserLinkController` / `ParticipantLinkController` / `ProviderLinkController`:
   `[Route("api/v1/identity")]`, **per-action `[Authorize(Policy="IdentityRead")]`**, "callable by any trusted BFF service
   account"). Create a **general, reusable lookup controller** (e.g. `IdentityLookupController` — a home for cross-BFF
   Identity **read/lookup** endpoints) with this first action `GET /api/v1/identity/profiles/display-names?ids=1&ids=2…`
   `[Authorize(Policy = "IdentityRead")]` (NOT Admin), returning the minimal projection. **Discipline:** each action carries
   its **own explicit `[Authorize(Policy=…)]`** — no blanket controller-wide gate that future actions silently inherit
   (least-privilege per endpoint). Keep this the **service-token read** axis (BFF SA), distinct from participant-USER-token
   endpoints (`ProfileController` / `participant/profile/me`). Fully additive → `QueryController` + every existing endpoint
   stay **byte-for-byte unchanged**. This controller is the reusable home for future BFF-facing Identity reads
   (display-names now; provider-name enrichment for disputes/jobs/conversations, and other lookups, later —
   [[bff_providername_enrichment]]).

## Mobile BFF (retarget the already-built resolver)
- Point `IProviderNameResolver`'s Refit call at the **new** endpoint: replace `IIdentityRemoteCall.GetUserProfilesByProfileIds`
  (admin, 403) with **`GetProfileDisplayNamesByProfileIds(long[])`** (`CollectionFormat.Multi`, `IdentityRead`-callable).
  The resolver now reads `DisplayName` directly (CompanyName-first is computed Identity-side → **B2 solved server-side**);
  keep the batch/cache/graceful-fallback + cost-free (name only, no id leaked). No other resolver/enrichment change — MO2b
  already wired the offers.

## Don't-break / QA
- Additive: one Identity DTO + query + `IdentityRead` endpoint + a resolver retarget. **No admin-role grant, no admin
  endpoint change**, no module-boundary change (SR still returns only the id). Existing admin bulk endpoints untouched.
  Identity + BFF build clean.
- Tests: (1) the new endpoint returns `{profileId, displayName}` with **CompanyName preferred** over person name; (2) it is
  callable with an `identity_read` service token (**200, not 403**) and rejects a token without it; (3) the resolver batch =
  one call, no N+1; (4) the offer payload exposes `ProviderName` but **no providerProfileId** / cost / commission;
  (5) unknown id → null → FE fallback, no error.

## Verify (on screen — MO2 offers, participant token)
- [ ] Offers show the **company name** ("PROVIDER 2 AS"), not the person name or "Servis Sağlayıcı"; the mobile-bff SA call
      returns 200 (no 403).
- [ ] Missing name → graceful fallback; payload cost-free (ProviderName only).

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO2c_DISPLAYNAME.md`: the scoped `IdentityRead` display-name endpoint (CompanyName-first,
minimal projection), the resolver retarget, the security rationale (no admin grant / no admin-endpoint relaxation), and the
verification. This closes the MO2b provider-name gap; the endpoint is the reusable primitive for disputes/jobs/conversations.
Then **MO3** (accept → checkout, iyzico-gated).
