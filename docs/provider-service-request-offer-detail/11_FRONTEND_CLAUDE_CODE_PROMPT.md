# 11 — Frontend Implementation Prompt (Claude Code)

Self-contained. Build the **Service Request Detail + Itemized Offer Builder** at `/app/service-requests/:id`,
replacing today's thin "price + note" detail page. Contracts: `08_API_AND_EVENT_CONTRACTS.md`. Auth: the canonical
Provider flow (`docs/provider-service-request-discovery/06`). Do not touch backend code.

## Design & reuse

`screen.png`, `DESIGN.md`, `code.html` are references, not code. `DESIGN.md` is the Nautical Heritage system the
SPA already implements — **reuse `shared/ui/*` and the discovery scaffolding; no new tokens.** `code.html` is a
Tailwind-CDN mock with sample data and browser-side arithmetic — port intent, never markup or its client totals.

Installed and reusable: React 19 · Vite · TS · `@tanstack/react-query@5` · `react-router-dom@7` (the `:id` route
exists) · `axios` (authenticated `httpClient` + `authInterceptors`; `publicHttpClient` public-only) · `i18next`
(enum maps in `serviceRequestEnums.ts`) · `react-hook-form` + `zod` · `@microsoft/signalr@10`
(`useProviderRealtime` + `realtimeContext`, dedupe+coalesce) · **MapLibre GL + OpenFreeMap** (reuse for the map) ·
formatters (`formatCurrency`, `formatDate`, `formatDistanceKm`, `formatRelativeTime`) · onboarding signed-URL
uploader pattern (for attachment read-on-click).

## Build — `features/service-requests/detail/` + `features/service-requests/offer/`

### Read side (Phase 6)
Header + status banner; sticky quick-summary; work-scope cards ("İş Kapsamı"); vessel summary ("Tekne Bilgileri",
BFF-enriched, render only non-null fields); approximate-location map (MapLibre, snapped coords, distance only when
present); photo/document gallery (mint a **signed read URL on click** — never embed URLs, never show object keys);
activity timeline + message thread (read); states skeleton/empty/error/disconnected.

### Offer builder (Phase 7) — the crux
Editable **itemized table**: columns Type / Name / Quantity / Unit / Unit price / Tax % / Line total; add / edit /
delete / **reorder** rows; item-type select (Service, Product, Labor, Installation, Inspection, Delivery,
EmergencyFee, Discount); **decimal** quantity + unit; money inputs (decimal, locale). **Manual items only** — no
catalog ("Şablondan Başla / Katalogdan Seç" are hidden/disabled with a "coming soon", not wired to a fake API).
Commercial-terms form: currency, earliest start, validity, deposit (type+value), payment-terms note, warranty
note, provider notes.

**Totals: provisional then authoritative.** Compute provisional totals in the browser for instant feedback while
typing, but **on every save/preview/submit response, replace them with the server's values** (per-category,
discount, subtotal, tax, grand). The mock's numbers are illustrative — never treat the client total as final.

**Auto-save**: debounce the aggregate `PUT …/offer/draft` (~1.5 s) with `AbortController`; carry the
**concurrency token** and, on `SR_OFFER_STALE`, show a "reopened elsewhere — reload" state rather than
overwriting. **Unsaved-changes guard** on navigation. **Preview** renders the customer-facing offer from the
server-preview response. **Submit**: confirmation dialog → single call with an **idempotency key**; disable the
button while in flight (no double-submit); on success the offer becomes **read-only** (submitted state), and a
withdraw action appears. Revision (MVP) = withdraw + new draft.

## Rules (hard)

- Provider **BFF only**; no direct module calls. **Never send `providerProfileId`.** `?access_token=` only for the
  hub. Tokens not persisted in app `localStorage`. Rejection can be HTTP 200 + `success:false` — check the body.
- **Never render a raw enum** (`Service`, `Submitted`, `AÇIK`) — i18n owns the words. Locale-aware money / date /
  number / tax / unit. **No budget anywhere.**
- **No client-authoritative totals shown as final.** No hardcoded sample data — grep the bundle for the mock's
  numbers (`13.020`, `Jotun`, `Beneteau`) before finishing.
- Realtime is a hint: a `viewed` / `revision` / `rejected` / message event invalidates and refetches; dedupe by
  id; socket down ⇒ the page still works over REST with a "disconnected" affordance.

## Tests
Adapter · offer form state (add/update/delete/reorder) · **provisional vs server total reconciliation** ·
validation mapping · auto-save debounce + cancel · unsaved-changes guard · preview · submit confirmation +
**duplicate-submit prevention** · read-only submitted state · revision · realtime + reconnect · i18n enum mapping ·
loading/empty/error/disconnected · responsive (tablet/mobile) · a11y (keyboard table nav, focus, labels) ·
**a test asserting no `providerProfileId` is sent** · Playwright E2E (the 12-step flow) · visual vs `screen.png`.
Validate: `npm run typecheck`, `npm run lint`, `npm run build`, E2E.

## Report
`docs/provider-service-request-offer-detail/REPORT_FRONTEND.md`: what was built, screenshots vs `screen.png`,
what the contract could not supply and was therefore not rendered, and anything undone. Unfinished is **not done**.
