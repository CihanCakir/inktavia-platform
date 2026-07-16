# 06 — Offer Domain & Calculation Rules

The heart of this screen. The offer is **not "price + note"** — the mock is a real itemized quote with tax,
per-category totals, and commercial terms. This doc defines the domain, and every number the **server** owns.

## Server is authoritative for every total. Full stop.

The browser may compute provisional totals for instant feedback while typing. But **save/submit responses carry
server-computed values, and the UI must replace its provisional numbers with them.** The server must **never**
trust a client-supplied `lineSubtotal`, `taxAmount`, `lineTotal`, `discountAmount`, or `grandTotal`. It receives
only the inputs (`itemType`, `quantity`, `unitPrice`, `taxRate`, discount inputs, `sortOrder`) and recomputes the
rest. A marketplace where the seller submits their own arithmetic is a marketplace that gets defrauded.

## Money & quantity precision

- **Money: `decimal`, never float.** Store `UnitPrice`, all computed amounts, and totals as `decimal`. Postgres
  `numeric(18,2)` for amounts (currency-minor precision), `numeric(9,4)` for tax rate if stored as a fraction.
- **Quantity: change from `int` to `decimal(12,3)`.** Marine work is priced in hours (2.5 h), litres, metres
  (13.85 m). Integer quantity cannot express the mock's own domain. This is a migration on both
  `ServiceRequestOfferItemEntity` and (optionally) `ServiceRequestItemEntity`.
- **Rounding: round each line to 2 decimals, then sum.** Sum-then-round and round-then-sum differ by cents;
  pick round-per-line (matches how the customer reads the printed quote) and document it. Use
  `Math.Round(value, 2, MidpointRounding.AwayFromZero)`.

## Item types — what each may do

| Type | Quantity? | Sign | Taxable? | Notes |
|---|---|---|---|---|
| Service, Product, Labor, Installation, Inspection, Delivery | yes | positive | yes | ordinary priced lines |
| **EmergencyFee** | no (qty = 1) | positive | **yes** (recommended — it is revenue) | a surcharge line |
| **Discount** | no (qty = 1) | reduces total | **applied pre-tax** (recommended) | see below |
| Other | yes | positive | yes | escape hatch |

## Discount — explicit fields, not a mystery negative line

The entity today has a bare `IsDiscount` bool. Decide and implement **explicit discount modelling**:

- A `Discount` line carries `DiscountType ∈ { Amount, Percent }` and `DiscountValue`.
  - `Amount`: subtract `DiscountValue` (a positive number) from the pre-tax subtotal.
  - `Percent`: subtract `DiscountValue`% of the pre-tax subtotal of **taxable non-discount lines**.
- The server computes `DiscountAmount` (never trust the client's).
- **Discount applies pre-tax** (reduces the taxable base). This is the common, defensible choice; state it so tax
  is computed on the discounted base, not the gross.
- A discount cannot make the grand total negative — clamp at zero and return a validation error if it would.

Rationale for explicit fields over a raw negative line: a negative-amount line is ambiguous (is it pre- or
post-tax? what's the base of a %?), and it lets the client smuggle in an arbitrary "discount total". Explicit
type+value with server computation removes the ambiguity and the attack surface.

## Tax — per line, offer-level totals

- Each taxable line has a `TaxRate` (e.g. 0.20 for KDV %20; the mock shows a per-line KDV %).
- `LineSubtotal = round(Quantity × UnitPrice, 2)`.
- Discount is applied to the taxable base **before** tax (see above).
- `LineTax = round(taxableBaseForLine × TaxRate, 2)`; `LineTotal = LineSubtotal + LineTax` (for display).
- Offer-level: **compute the tax total on the discounted base**, don't just sum naive per-line taxes if a
  percent-discount spans lines. Define the algorithm precisely (below) and keep it in one place.

### The one algorithm (server, single source of truth)

```
subtotalByCategory = Σ round(qty×unitPrice,2)  grouped by item type   // Service total, Product total, …
subtotal           = Σ all non-discount line subtotals
discountAmount     = resolve(Discount lines) against the taxable base  // Amount or Percent, server-computed
taxableBase        = subtotal − discountAmount        (never < 0)
taxTotal           = Σ over taxable lines of round(lineTaxableShare × lineTaxRate, 2)
grandTotal         = taxableBase + taxTotal
```

Return **all** of: per-category totals (Service/Product/Labor/Installation/Inspection/Delivery/EmergencyFee),
`discountTotal`, `subtotal`, `taxTotal`, `grandTotal`. The mock's totals panel needs them; the UI must not
compute them for the final display.

## Validation

- `quantity > 0` for priced lines; `unitPrice >= 0`; `taxRate ∈ [0, 1]`; currency identical across all lines and
  the offer (reject mixed currency — the offer has one `CurrencyCode`).
- Unit code (when present) validated against ReferenceData units; unknown ⇒ reject (fail-closed, as elsewhere).
- Max item count (e.g. 100) to bound payloads.
- Duplicate items are allowed (two "Labor" lines are legitimate); no dedupe.
- Empty offer (no priced line) cannot be **submitted** (a draft may be empty).

## Lifecycle — recommendation

Current: `Draft → Submitted → UnderReview → Accepted | Rejected | Withdrawn | Expired`. `Update()` mutates in
place; no version, no immutability, no "viewed"/"revision requested".

**MVP recommendation (documented default, not yet built):**

- **One active offer per (provider, request).** A draft is editable freely. `Submit()` freezes the commercial
  content.
- **Submitted offers are immutable.** To change a submitted offer, the provider **withdraws** and submits a new
  one, **or** — if the customer requested a revision — a revision creates a **new version** and the previous
  version stays readable.
- **Versioning is the honest model but is an M-sized addition** (a `Version` int + keeping prior rows / a history
  table). Classify: **full version history = post-MVP**. **MVP simplification:** submitted = immutable; the
  provider may **withdraw and re-submit** (a new offer row), and we keep withdrawn rows for history. This gives
  auditability without a formal version chain. `ViewedAt` / `RevisionRequestedAt` are small additions worth doing
  in MVP so the status banner is honest.
- **Idempotent submit** (an idempotency key) and an **optimistic concurrency token** are MVP — an auto-saving,
  double-click-prone builder will otherwise double-submit or lose updates.

If the product wants formal versioning in MVP, that is a deliberate upsize (doc 09 Phase 8) — flagged, not
smuggled.

## Deposit / payment terms / warranty

The mock shows "%50 Peşin" and a payment plan. These are **commercial terms** on the offer: `DepositType`
(Amount/Percent) + `DepositValue`, `PaymentTermsNote`, `WarrantyNote`. **MVP: store them as structured-where-cheap
(deposit) + free text (payment/warranty notes).** Do not build a payment schedule engine — that is Payment-module
territory and Payment is out of scope here.
