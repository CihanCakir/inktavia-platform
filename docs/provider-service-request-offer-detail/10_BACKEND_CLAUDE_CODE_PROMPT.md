# 10 — Backend + Provider BFF Implementation Prompt (Claude Code)

Self-contained. Implements the request-detail read model and the **itemized offer domain** with
server-authoritative totals. Do not touch the frontend. **Run the phases in order; verify between them — do not
run this as one job.** (This project's reports have repeatedly claimed "complete" over code that never ran.)

## Facts verified in source (2026-07-15) — accept them

- `ServiceRequestOfferEntity`: `Status`, `TotalAmount` (decimal), `CurrencyCode`, `Description`, `ProviderNotes`,
  `EstimatedStart/End`, `EstimatedDurationMinutes`, `ExpiresAt`, Accepted/Rejected/Withdrawn At+reasons, `Items`.
  `RecalculateTotal() => Items.Sum(Quantity * UnitPrice)`. **No tax, no version, no ViewedAt, no deposit.**
- `ServiceRequestOfferItemEntity`: `ItemType`, `Title`, `Description`, **`Quantity` (int)**, `UnitPrice` (decimal),
  `CurrencyCode`, `SortOrder`, `IsDiscount` (bool). **No UnitCode, no tax, no line totals, no discount fields.**
- Item-type enum complete (Service…Discount, Other). Offer status: Draft/Submitted/UnderReview/Accepted/Rejected/
  Withdrawn/Expired. **No RevisionRequested/Viewed.**
- Offer commands: Create/Update/Withdraw/Reject/Accept. **No fine-grained item commands.**
- `ServiceRequestItemEntity` (work scope) is real (ItemType/Title/Description/Quantity(int)/UnitCode/
  EstimatedUnitPrice/SortOrder). `GetProviderServiceRequestDetail` exists with the access check.
- Vessel snapshot columns were **removed** (09b.1); enrich from `IProviderVesselRemoteCall` bulk summary.
- Realtime bridge proven; auth is the canonical assertion flow.

## Decisions already taken (do not relitigate) — see doc 06

Server owns every total. Money = decimal, never float. **Quantity → decimal(12,3).** Tax per line. Discount =
explicit `DiscountType{Amount,Percent}` + `DiscountValue`, applied **pre-tax**, server-computed. EmergencyFee
taxable. **Aggregate save** (no fine-grained item commands for MVP). Submitted = **immutable**; change = withdraw
+ resubmit; **formal versioning is post-MVP**. Manual items only (no catalog). No budget. Deposit = structured +
free-text notes. Idempotent submit + optimistic concurrency are MVP.

## Work (phases from doc 09)

**P1 — detail read model.** Extend `GetProviderServiceRequestDetail` to project work-scope items, attachment
metadata (ids/kind/contentType/size — **no signed URL, no object key**), timeline (durable events), and the
caller's offer with items + computed totals. Access check unchanged; snapped coords; **no owner PII beyond what
the owner published; do not project other providers' offers.**

**P2 — migrations + domain.** Quantity `int→decimal(12,3)` (offer items; consider request items). Add offer-item:
`UnitCode`, `TaxRate` (numeric(9,4)), `TaxAmount`, `LineSubtotal`, `LineTotal`, `DiscountType`, `DiscountValue`,
`DiscountAmount` (all numeric(18,2) for money). Add offer: `Subtotal`, `TaxTotal`, per-category totals (store or
compute), `DepositType`, `DepositValue`, `PaymentTermsNote`, `WarrantyNote`, `SubmittedAt`, `ViewedAt`,
`RevisionRequestedAt`, a **row-version/concurrency token**. Domain methods to replace the item set atomically.

**P3 — calculation + lifecycle.** One calculation service implementing the doc 06 algorithm exactly (round per
line, discount pre-tax, tax on discounted base, per-category totals, grand total, clamp ≥ 0). FluentValidation
(quantity>0 for priced lines, unitPrice≥0, taxRate∈[0,1], single currency, unit validated vs ReferenceData,
max items, empty-submit rejected). Aggregate `SaveOfferDraft` command (recomputes, ignores any client-sent
totals). `Submit` command: **idempotency key**, Draft→Submitted, freeze, set `SubmittedAt`. Optimistic
concurrency on save (stale ⇒ reject). Withdraw keeps the row.

**P4 — attachments/messages/timeline.** Signed **read** URL endpoint (per file, short-lived, FileStorage
pattern). Timeline projection (durable module events). Message thread read; provider post only if a command
already exists — else defer and say so.

**P5 — Provider BFF.** Endpoints per doc 08 (`/{id}/detail`, `/attachments/{fileId}/read-url`, `/timeline`,
`/offer/draft` GET+PUT, `/offer/preview`, `/offer/submit`, `/offer/withdraw`). Vessel bulk-enrich **one call** per
detail. **No domain logic; totals passed through untouched.** Identity from the assertion; a client
`providerProfileId` is ignored. Missing identity ⇒ reject (not empty).

**P8 (realtime).** Producers + BFF consumers for `OfferViewedByCustomer`, `OfferRevisionRequested`,
`OfferRejected`, message-added → `provider:{profileId}`. Idempotent.

## Explicitly forbidden

- Raw end-user token forwarding · `X-Aizen-User-Token` · synthetic Identity JWT · client-controlled
  `ProviderProfileId`/`UserId`.
- Domain logic in the BFF · **client-authoritative totals** (line/tax/discount/grand) · **floating-point money**.
- N+1 (per-field vessel calls, per-item queries) · fake catalog dependencies · parallel realtime infra ·
  untyped (`object`) responses · `JsonElement` on the wire.

## Acceptance — observed, not asserted
- Server totals match a hand-computed fixture including a percent discount across lines + KDV %20; a client-sent
  `grandTotal` is ignored. Paste the fixture and the response.
- Decimal quantity persists (2.5 h). Mixed currency rejected. Empty offer cannot submit. Stale save rejected.
  Double-submit (same idempotency key) yields one offer.
- One vessel call per detail (assert the count). No object keys or signed URLs in the detail aggregate.
- Auth tests: assertion propagation; empty/wrong secret; unauthorized client; **client `providerProfileId`
  cannot change the offer's owner**.

## Report
`docs/provider-service-request-offer-detail/REPORT_BACKEND.md`: the calculation fixture(s), the generated SQL for
the detail projection, the vessel call-count, the auth-test results, and anything unfinished. **Unfinished is not
done** — say it in the summary line, not mid-paragraph.
