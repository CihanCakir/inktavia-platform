# Provider QA — Service Requests / Discovery (P-QA5)

## Static findings
Route `/app/service-requests` → `discovery/pages/DiscoveryPage.tsx`, **0 query hooks** (renders EmptyState). Nav marks it
`planned`. The provider discovers open requests here (city-level MVP; GeoDiscovery deferred). 4 pages in the feature +
api/hooks exist, so some flow is wired — but the main discovery page has no live query.

## Questions to resolve
- Is discovery meant to be live now (city-level list of open requests + offer entry) or intentionally EmptyState until
  GeoDiscovery? Per the deferred-geo decision, **city-level matching is the MVP** → discovery should at least list open
  requests for the provider's city and lead into the offer builder.

## Live walkthrough checklist (localhost:3002/app/service-requests)
- [ ] Open requests list for the provider's city loads (loading/empty/error) — not a bare EmptyState if data exists.
- [ ] A request opens to detail → "make offer" leads to the offer builder.
- [ ] Discovery map (if shown) has no hardcoded credential/keys; degrades gracefully.
- [ ] Nav flag reconciled (implemented vs planned) once wired.

## Fix candidates
Wire the discovery list to the open-requests endpoint (city-level) + offer entry → `FIX_*` doc here.
