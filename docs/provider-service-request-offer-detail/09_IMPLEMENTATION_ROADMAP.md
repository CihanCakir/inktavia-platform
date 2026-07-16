# 09 — Implementation Roadmap

Ordered by dependency. The offer cannot be honest before its calculation model exists.

## Phase 0 — Domain & contract validation
Confirm decisions: server-authoritative totals; **quantity → decimal**; tax per line; discount as explicit
type+value pre-tax; EmergencyFee taxable; aggregate-save (not fine-grained item commands) for MVP; submitted =
immutable, withdraw+resubmit for changes (formal versioning post-MVP); manual items only (no catalog); no budget;
deposit as structured + free-text terms. **Open product decisions to close:** show owner's `EstimatedUnitPrice`
to the provider? formal versioning in MVP or not? Exit: docs 05/06/08 accepted.

## Phase 1 — Request detail read model (M)
Extend `GetProviderServiceRequestDetail` to project **work scope items**, attachment metadata, timeline, and the
**caller's offer with items**. No prices leaked; snapped coords; access check unchanged. Acceptance: one aggregate
returns everything the read side of the mock needs; no owner PII beyond published; paste the DTO.

## Phase 2 — Offer draft & item domain (L)
Migrations: `Quantity int→decimal(12,3)`; add offer-item `UnitCode`, `TaxRate`, `TaxAmount`, `LineSubtotal`,
`LineTotal`, `DiscountType`, `DiscountValue`, `DiscountAmount`; add offer `Subtotal`, `TaxTotal`, category totals
(or compute-and-store), `DepositType/Value`, `PaymentTermsNote`, `WarrantyNote`, `SubmittedAt`, `ViewedAt`,
`RevisionRequestedAt`, a **concurrency token**. Domain methods for aggregate replace-items. Acceptance: a draft
with mixed item types persists and round-trips.

## Phase 3 — Calculation, validation, lifecycle (L)
The single calculation service (doc 06 algorithm), server-authoritative. FluentValidation (quantity/unit/currency/
tax range/max items/empty-submit). Idempotent submit; optimistic concurrency; withdraw+resubmit. Acceptance:
**server totals match a hand-computed fixture** incl. a percent discount spanning lines and KDV; a client-supplied
total is ignored; a stale save is rejected; double-submit yields one offer. Paste the fixtures.

## Phase 4 — Attachments, messages, activity (M)
Signed read-URL endpoint (per click). Timeline projection (durable events). Message thread read (+ post if the
command exists; else defer). Acceptance: a photo opens via a short-lived URL; the timeline shows real events;
no object keys reach the client.

## Phase 5 — Provider BFF orchestration (M)
Detail aggregate endpoint (+ vessel bulk enrich, one call), draft get/save/preview/submit/withdraw, read-url,
timeline. Assertion auth reused; no domain logic; totals passed through; missing identity ⇒ reject. Acceptance:
auth tests (assertion propagation, empty/wrong secret, unauthorized client, client `providerProfileId` ignored);
one vessel call per detail; totals never computed in the BFF.

## Phase 6 — Frontend detail foundation (M)
Rebuild `ServiceRequestDetailPage`: header + status banner, sticky quick-summary, work-scope cards, vessel
summary, approximate map (reuse MapLibre), photo gallery (signed-on-click), timeline + message thread, states
(skeleton/empty/error/disconnected). i18n every enum; locale formatting; **no budget**. Acceptance: read side
matches `screen.png`; no raw enum; distance only with location.

## Phase 7 — Itemized offer builder (L)
The editable table (add/edit/delete/reorder rows, item-type select, decimal quantity + unit, unit price, tax
rate), provisional totals while typing, **server-total reconciliation on save**, commercial-terms form, auto-save
(debounce + cancel), unsaved-changes guard, preview (customer-facing render), submit confirmation + duplicate
guard + idempotency key, read-only submitted state. Acceptance: full E2E offer; server totals shown; can't
double-submit; submitted is read-only.

## Phase 8 — Realtime & revision (M)  [formal versioning: post-MVP]
Producers/consumers for viewed / revision-requested / rejected / message. Status banner reacts. Revision path:
MVP = withdraw+resubmit; **formal version history is post-MVP** (a `Version` chain + read-only prior versions).
Acceptance: a "viewed" event flips the banner; two-replica realtime re-verified; dedupe holds.

## Phase 9 — Tests, security, performance (M)
Backend: calculation fixtures, validation, authz, identity-override rejection, submit idempotency, concurrency,
privacy, assertion tests. Frontend: adapter, form state, provisional vs server totals, auto-save debounce,
duplicate-submit, revision, read-only, realtime reconnect, i18n, a11y, Playwright E2E (the 12-step flow),
visual vs `screen.png`. Perf: one detail aggregate, one vessel call, per-click signed URLs, auto-save debounced,
no float money.

## Phase 10 — Release & observability
Feature-flag the new detail; keep the old detail until parity. Metrics: detail p95, save/submit latency,
auto-save rate, submit idempotency hits, realtime drops. Rollback = flag off; migrations additive.

## MVP cut
**In:** full read detail (scope, vessel, map, photos, timeline read), itemized builder with server totals,
tax, explicit discount, decimal quantity, commercial terms (deposit + notes), draft auto-save, preview, idempotent
submit, withdraw, viewed/revision-requested banners, realtime.
**Out (recorded):** formal offer versioning, catalog/templates ("Şablondan Başla / Katalogdan Seç"), provider
posting messages if no command exists, payment-schedule engine (Payment module), owner price disclosure unless
decided, signed BFF-assertion JWT.
