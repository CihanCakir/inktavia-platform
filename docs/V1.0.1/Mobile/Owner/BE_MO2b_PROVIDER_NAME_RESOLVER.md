# BE_MO2b — BFF-side provider-name resolver (enrich owner offers with the real provider/company name)

> **Repos:** `addesso-project` — **Marine.Participant.Mobile BFF** only (module + Identity unchanged). Closes the MO2
> follow-up: owner offers show a "Servis Sağlayıcı" fallback because the provider display name is null. Resolve it **at the
> BFF** (the module returns only `providerProfileId`; the BFF enriches the name) — the clean, reusable pattern that matches
> `bff_providername_enrichment`. Additive, cost-free (name only, no provider id leaked). **Do not commit.**

## What exists (so this is BFF-only, no module/Identity change)
- **Identity already exposes a batch profile lookup:** `GetUserProfilesByProfileIdsQuery(long[] profileIds)` →
  `List<UserProfileListItemDto>`, served by `QueryController.GetUserProfilesByProfileIds` (GET, `/api/v1/identity/...`).
  `UserProfileEntity` carries **`CompanyName`** (+ name fields). So the id→name data is already available — **no new
  Identity endpoint.**
- The Mobile BFF's MO2 owner-offers handler already has each offer's `providerProfileId` internally (it just **drops** it
  from the cost-free DTO). So the BFF can resolve the name server-side and expose **only the name**.

## The resolver (BFF, reusable)
1. **Remote call:** add `GetUserProfilesByProfileIds(long[] profileIds)` to the Mobile BFF `IIdentityRemoteCall` (Refit),
   pointing at the existing Identity batch endpoint; typed, envelope-correct.
2. **Reusable resolver helper** in `Common` (e.g. `IProviderNameResolver` / `ProviderNameResolver`): given a set of
   `providerProfileId`s, **one batch call** returns an `id → displayName` map, where `displayName = CompanyName ?? "{First}
   {Last}".Trim() ?? null`. **No N+1** (collect all ids, one call). Short in-memory cache (per-request or a brief TTL) so a
   list of offers resolves once. Missing/absent id → null (FE keeps its localized fallback).
3. **Enrich MO2 offers:** in `GetServiceRequestOffersForOwnerBff` (list) + the offer-detail handler, collect the offers'
   `providerProfileId`s, resolve in one batch, and set **`ProviderName`** on the cost-free `MobileServiceRequestOfferDto`.
   **Expose the name only — never the providerProfileId** (cost-free/privacy preserved).

## Reusability (write it to be shared, wire MO2 now)
Build the resolver as a shared `Common` service so other BFF surfaces (provider activity, disputes, jobs, conversations)
can reuse the same id→name resolution instead of re-implementing it. Scope the **wiring** to the MO2 owner-offers handlers
in this pass; note the reuse.

## Don't-break / QA
- BFF-only + additive: new remote-call method + a resolver service + name enrichment on the MO2 offer DTOs. The
  ServiceRequest module and Identity are unchanged; the module still returns only the id. Cost-free: the offer payload gains
  `ProviderName` (a display string) and still leaks **no** provider id / cost / commission. Identity call via the existing
  service-token path; batch (no N+1). BFF builds 0 errors.
- Tests: (1) an offers list resolves all provider names in **one** Identity call (assert no N+1); (2) `CompanyName` wins,
  else the person name, else null→FE fallback; (3) the offer payload exposes `ProviderName` but **no `providerProfileId`**;
  (4) a missing/unknown id degrades to the fallback, no error.

## Verify (on screen — MO2 offers, once a participant token is available)
- [ ] The owner offers inbox shows the **real provider/company name** per offer (not "Servis Sağlayıcı") when Identity has
      it; graceful fallback when it doesn't.
- [ ] Comparing offers is now by name + amount/ETA/status.
- [ ] Payload grep: `ProviderName` present, no `providerProfileId`, no cost/commission.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO2b_PROVIDER_NAME.md`: the IIdentityRemoteCall batch method, the reusable resolver
(batch + cache + CompanyName-first), the MO2 offer enrichment, the no-N+1/cost-free verification, and a note that the
resolver is shared for future surfaces. Then **MO3** (accept → checkout, iyzico-gated).
