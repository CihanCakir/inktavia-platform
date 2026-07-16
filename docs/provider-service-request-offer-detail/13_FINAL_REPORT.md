# 13 — Final Report

**Date:** 2026-07-15 · Analysis only; no production code modified.

## Projects inspected
`Modules/ServiceRequest` (offer + offer-item entities, item-type/status enums, offer commands, work-scope item,
provider detail query, realtime), `Modules/Vessel` (bulk summary from 09b.1), `FileStorage` (signed-URL pattern),
`ReferenceData` (units/categories); `Bff/src/MarineProvider`; `inktavia-marine-provider-web`.
Design: `screen.png`, `DESIGN.md`, `code.html`. Prior contracts: `docs/provider-service-request-discovery/`.

## Documents created
`docs/provider-service-request-offer-detail/01…13`.

## The headline finding
**The offer domain is far behind the mock.** The screen is a real itemized quote with per-line tax, per-category
totals, discount, and commercial terms. The entities are not:

- **No tax anywhere.** `RecalculateTotal = Σ(Quantity × UnitPrice)` — no `TaxRate`, `TaxAmount`, line totals, or
  offer-level subtotal/tax. The mock's "KDV %20 / Ara Toplam / Genel Toplam" have no backing.
- **`Quantity` is `int`.** Labour hours (2.5 h) and lengths (13.85 m) — the mock's own domain — can't be expressed.
- **Discount is a bare `IsDiscount` bool.** No type/value/amount; ambiguous and client-forgeable.
- **No versioning, no `ViewedAt`, no `RevisionRequestedAt`, no deposit/payment/warranty.** `Update()` mutates a
  submitted offer in place — a submitted commercial document is silently editable.
- **No fine-grained item commands, no idempotent submit, no concurrency token** — an auto-saving, double-click
  builder would double-submit and lose updates.

What *is* solid: the item-type enum is complete; **work scope (`ServiceRequestItem`) is real** (İş Kapsamı has
data); the request detail query + access check exist; money fields are `decimal` (no float); the realtime bridge
and the canonical auth flow are proven and reusable; Vessel bulk enrich + FileStorage signed URLs already exist.

## Critical request-detail gaps
Detail query must project **work scope**, **my offer with items**, attachment metadata, timeline. Vessel specs
must be **BFF-enriched** (snapshot removed 09b.1) — one bulk call. No budget (decision stands).

## Critical offer-domain gaps
Tax (per line + totals), decimal quantity, explicit discount, per-category totals, deposit/terms/warranty,
`SubmittedAt`/`ViewedAt`/`RevisionRequestedAt`, concurrency token, idempotent submit. All server-authoritative.

## Critical BFF gaps
Detail aggregate (+ vessel enrich), draft get/save/preview/submit/withdraw, signed read-URL, timeline. No domain
logic; totals passed through; identity from assertion.

## Critical frontend gaps
Everything past the current thin form: itemized editable table, decimal/money/tax inputs, provisional-vs-server
total reconciliation, auto-save + concurrency, preview, idempotent submit + duplicate guard, read-only submitted
state, timeline + message thread, photo gallery (signed-on-click), approximate map.

## Offer calculation strategy
**Server-authoritative, single algorithm** (doc 06): round per line; discount explicit + pre-tax; tax on the
discounted base; per-category + subtotal + tax + grand totals returned. Client totals are UX-only and replaced by
server values on save/submit. Money `decimal`, quantity `decimal(12,3)`.

## Offer lifecycle recommendation
One active offer per (provider, request); **submitted = immutable**; change = **withdraw + resubmit** (rows kept
for history). `ViewedAt`/`RevisionRequestedAt` in MVP for an honest banner. **Formal version history = post-MVP.**
Idempotent submit + optimistic concurrency = MVP.

## MVP blockers
1. Tax + per-line/offer totals (calculation model) — the screen is a quote; without tax it is wrong, not partial.
2. Decimal quantity (migration).
3. Server-authoritative totals + validation (anti-fraud).
4. Detail projection of work scope + my offer with items.
5. Vessel BFF enrichment (snapshot is gone).
6. Idempotent submit + concurrency token (auto-save/double-click safety).

## Open product decisions (recorded; recommended defaults chosen)
- Show the owner's `EstimatedUnitPrice` to the provider? **Default: hide.**
- Formal offer versioning in MVP? **Default: no — withdraw+resubmit; versioning post-MVP.**
- Catalog/templates ("Şablondan Başla / Katalogdan Seç")? **No catalog module active — manual items MVP; hide the
  buttons.** Do not build a fake catalog API.
- Deposit modelling depth? **Default: structured deposit (type+value) + free-text payment/warranty notes; no
  schedule engine (Payment module out of scope).**

## Prompts
Backend: `docs/provider-service-request-offer-detail/10_BACKEND_CLAUDE_CODE_PROMPT.md`
Frontend: `docs/provider-service-request-offer-detail/11_FRONTEND_CLAUDE_CODE_PROMPT.md`

## Recommended order
P0 (decisions) → P1 (detail read) → P2 (offer migrations/domain) → P3 (calc/validation/lifecycle) → P4
(attachments/messages/timeline) → P5 (BFF) → P6 (FE detail) → P7 (FE offer builder) → P8 (realtime) → P9 (tests/
security/perf) → P10 (release). Run the backend prompt phase-by-phase, verifying between each — do not run it as
one job.
