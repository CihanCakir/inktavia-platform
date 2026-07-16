# 10c — Backend: calculation, validation & offer lifecycle (Claude Code)

Run **after 10b is merged and verified in the DB.** This is the heart: the **server-authoritative** calculation,
validation, and the draft→submit lifecycle. Do not touch the frontend. 10d adds the BFF endpoints after this.

## The rule this phase exists to enforce

**The server computes every total; the client's numbers are never trusted.** The request carries only inputs
(`itemType`, `quantity`, `unitPrice`, `taxRate`, `discountType`, `discountValue`, `sortOrder`, commercial terms).
The server recomputes `lineSubtotal`, `taxAmount`, `lineTotal`, `discountAmount`, per-category totals, subtotal,
tax total, discount total, grand total — and **ignores any of those if the client sends them.** A marketplace that
accepts the seller's own arithmetic gets defrauded.

## The one calculation algorithm (doc 06 — single source of truth)

Implement once, in a module service (e.g. `OfferCalculationService`), called by save/preview/submit:

```
round(x) = Math.Round(x, 2, MidpointRounding.AwayFromZero)

for each non-discount line:
    lineSubtotal = round(quantity * unitPrice)
subtotal        = Σ lineSubtotal
categoryTotals  = Σ lineSubtotal grouped by ItemType
discountAmount  = resolve(Discount lines) against the taxable base:
                    Amount  → min(DiscountValue, subtotal)
                    Percent → round(subtotal * DiscountValue)      // DiscountValue in [0,1] or /100 — pick, document
taxableBase     = max(subtotal - discountAmount, 0)
for each taxable line: lineShareOfBase = lineSubtotal - (its pro-rata share of discountAmount)
    lineTax = round(lineShareOfBase * taxRate)
taxTotal        = Σ lineTax
grandTotal      = taxableBase + taxTotal
```

State the discount %-unit ([0,1] vs 0–100) and the pro-rata rule explicitly. Discount applies **pre-tax**.
`grandTotal` can never be < 0. Store the computed snapshots on the items and the totals on the offer (10b columns).

## Validation (FluentValidation, fail-closed)

- `quantity > 0` for priced lines; `unitPrice >= 0`; `taxRate ∈ [0, 1]`.
- **Single currency** across all lines and the offer — reject mixed.
- `unitCode` (when present) validated against ReferenceData units; unknown ⇒ reject.
- Max item count (e.g. 100). Duplicate item types allowed (two Labor lines are fine).
- A **Draft** may be empty; a **Submit** with no priced line is rejected.
- Deposit: `Percent` value in range; `Amount` not exceeding grand total.

## Commands

- **`SaveOfferDraftCommand`** (aggregate save): get-or-create the caller's Draft for the request, `ReplaceItems`,
  set commercial terms, run the calculation service, persist, return the recomputed offer + a fresh concurrency
  token. **Optimistic concurrency**: the request carries the token; a stale token ⇒ reject `SR_OFFER_STALE` (no
  silent overwrite). One active offer per (provider, request).
- **`PreviewOfferCommand`**: same inputs, runs the calculation service, returns the computed offer **without
  persisting**.
- **`SubmitOfferCommand`**: **idempotent** (an `Idempotency-Key`); Draft→Submitted (`MarkSubmitted`), freezes
  content (a submitted offer is immutable — reject further `SaveOfferDraft` on it), rejects empty. Re-sending the
  same key returns the same offer, not a second one. `SR_OFFER_ALREADY_SUBMITTED`, `SR_OFFER_REQUEST_CLOSED`,
  `SR_OFFER_EMPTY`.
- Keep existing withdraw; ensure it keeps the row for history and is allowed only while open + not accepted.
- Provider identity from the assertion (`KeycloakTokenInfo.ProviderProfileId`), never from the DTO.

## Acceptance — observed, not asserted

- **A hand-computed fixture matches the server** for: multi-type items + a Percent discount spanning lines + KDV
  0.20, with round-per-line. Paste the fixture and the response.
- **A client-supplied `grandTotal` / `taxAmount` / `lineTotal` is ignored** — send wrong numbers, prove the
  server overwrites them.
- Decimal quantity (2.5 h) prices correctly. Mixed currency rejected. Empty offer cannot submit.
- **Stale save rejected** (concurrency). **Double submit (same key) → one offer.**
- Provider identity cannot be overridden by a client `providerProfileId` (send one; result unchanged).

## Constraints
- No float in the money path. No calculation in the BFF or the frontend as the source of truth.
- No `object`/`JsonElement` on the wire. Domain logic stays in the module.

## Report
Append to `REPORT_BACKEND.md` (section "10c"): the calculation fixture(s), the ignored-client-total proof, the
concurrency + idempotency evidence, and the discount %-unit + pro-rata decisions. Unfinished is **not done**.
