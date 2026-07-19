# CI-3 — Provider renewal calendar + urgency donut (plan)

Replaces the placeholder on the provider **Yenilemeler** page with a renewal timeline (kits grouped by urgency
bucket) and an urgency donut. Read-only for the provider — preparing/invoicing a renewal stays admin-side.

Backend is mature: `GetCargoDryRenewalCandidatesQuery` returns Active kits expiring within N days with no open
preparation, enriched with product pricing. CI-3 adds a provider filter + surface, then the FE.

## Phases
- **CI-3a — backend** (`CI3A_BACKEND_PROVIDER_RENEWALS.md`): add `ProviderProfileId` to the candidates query
  (handler passes it to the already-scoped `GetExpiringAsync`); provider controller `GET /cargodry/provider/renewals`
  (assertion-forced) + BFF `/provider/cargodry/renewals`. Reuses the existing handler.
- **CI-3b — frontend** (implemented directly in `inktavia-marine-provider-web`, like CI-2b): `CargoDryRenewalsPage`
  gets a renewal timeline (buckets: Gecikmiş / ≤7g / ≤30g / ≤90g), an urgency donut, and a summary; API client +
  hook + i18n + `tsc` gate.

Data reality: provider2 has 2 candidates at withinDays=90 (`CDK-PRV2-0006` ~5d, `CDK-PRV2-0005` ~20d);
`CDK-PRV2-0003/0004` (120d) fall outside 90d. Enough to populate the timeline + donut.

Order: CI-3a → CI-3b → verify on screen. Then CI-4.
