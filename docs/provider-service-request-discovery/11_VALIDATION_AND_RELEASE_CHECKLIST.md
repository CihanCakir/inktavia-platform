# 11 — Validation & Release Checklist

Each line is an **observation**, not an assertion. If it was not observed, it is **not done**.

## Authentication & identity

- [ ] The end-user Keycloak token is **never** forwarded to a module. (Inspect the outgoing headers on a real
      call, do not read the code and assume.)
- [ ] `X-Aizen-User-Token` is not sent by the Provider BFF.
- [ ] Every internal call carries: service-account `Authorization`, `X-Aizen-Bff-Assertion`, `X-Aizen-User-Id`,
      `X-Aizen-Provider-Profile-Id`.
- [ ] `ProviderProfileId` is server-resolved. **Test:** send `providerProfileId` in the query and body; the
      result is identical to sending none, and no other provider's data is ever returned.
- [ ] Handlers read identity only from the middleware context — never from DTOs, query, route, or raw headers.
- [ ] Missing/invalid identity ⇒ **reject**, not an empty list. (`GetOpenServiceRequests` returns empty today;
      new handlers must not copy it — a provider seeing "no work" because identity failed is a silent failure.)
- [ ] `?access_token=` is accepted **only** on `/hubs`; REST rejects it.
- [ ] Module rejects: empty secret (feature disabled) · wrong secret (constant-time compare) · unauthorized
      client id.

## Production security (blocking)

- [ ] Internal modules have **no public ingress**; a `NetworkPolicy` admits only authorized BFFs/service clients.
      The browser can reach only the Provider BFF.
- [ ] **`AllowedClientIds` is explicitly configured.** With it empty, any valid service token plus the secret is
      accepted — that is not a theoretical risk, it is the whole control surface.
- [ ] No secrets in logs: assertion secret · user token · service token · whole auth headers · exact coordinates.
- [ ] `/hubs` query-string logging disabled at the ingress (the SignalR token travels there).
- [ ] The static-secret → signed-assertion-JWT migration is recorded as a tracked follow-up.

## Correctness

- [ ] A provider who already bid on a request **sees it in discovery**, badged with their offer state.
- [ ] `PublishedAt` drives "added today" and relative time; `CreateDate` is used for neither. Backfilled values
      are documented as approximate. Republishing does not overwrite it.
- [ ] **No budget anywhere**: no columns, no DTO fields, no card section, no placeholder (decided 2026-07-14).
      Grep the bundle for `₺18.000` and friends — the mock's numbers must not have survived.
- [ ] Vessel snapshot fields render only when non-null; the snapshot does not change after publication.
- [ ] Distance appears only when an origin was supplied.
- [ ] Cursor paging is stable while requests are published; a cursor from different filters is **rejected**.

## Geospatial

- [ ] The generated SQL for discovery and markers is in the report; the bbox predicate uses the index.
- [ ] Distance is computed **in the database**. No `ToList()` before a distance sort anywhere.
- [ ] `RadiusKm` and `PageSize` capped; out-of-range coordinates rejected.
- [ ] Browser coordinates are used **only** to narrow the caller's own view — never for authorization or
      entitlement.
- [ ] Location denied ⇒ city fallback works, distances hidden, radius disabled, state shown.
- [ ] Markers require bounds; over the cap ⇒ `Truncated`, and the UI says "zoom in".
- [ ] Every geo file carries the **GEO ON LOAN** header with its migration triggers.

## Privacy

- [ ] Discovery DTOs carry no owner identity and no owner personal data.
- [ ] Snapped coordinates only; exact coordinates reachable solely by the assigned provider.
- [ ] No foreign offer rows are loaded or returned; only the aggregate count.
- [ ] Draft / Cancelled / Closed / Expired / assigned requests never appear.

## Realtime

- [ ] Published / updated / cancelled / urgency-changed all reach the list.
- [ ] A duplicate frame does not duplicate a card.
- [ ] **Two-replica backplane test re-run**: the event arrives when the socket-less instance consumes it.
- [ ] Socket down ⇒ the screen still works over REST and says so.

## Frontend

- [ ] No backend enum in the DOM; KPI labels come from `LocationMode` codes; the phrase "service area" appears
      nowhere.
- [ ] Money, dates, relative time and distance are locale-formatted.
- [ ] No `providerProfileId` is sent anywhere.
- [ ] Tokens are not persisted in app-defined `localStorage`; `publicHttpClient` is used only for public routes.
- [ ] Zero sample data in the bundle.
- [ ] `typecheck`, `lint`, `build`, unit, component and E2E pass; visual + a11y pass.

## Performance

- [ ] No N+1 (vessel, offers, attachments) — verified with query logs.
- [ ] Marker payload minimal; page size capped; summary cached with invalidation.
- [ ] Discovery p95 recorded before release.

## Release

- [ ] Feature-flagged; old list retained until parity.
- [ ] Metrics: discovery p95 · markers/viewport · realtime drops · geolocation denial rate.
- [ ] Rollback = flag off; migrations additive.
