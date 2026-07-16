# 10b — Backend: offer migrations & domain (Claude Code)

Run **first** of the offer-domain phases. P1 (detail read model) is done and verified. This phase adds the
**fields** the itemized offer needs; **10c** adds the calculation/lifecycle logic; **10d** adds the BFF endpoints.
Run these three in order, verifying between them — **do not run them as one job.** (The one-shot run of the
umbrella prompt produced only P1; that is why we split.)

No calculation logic in this phase — only schema + domain-method plumbing. Do not touch the frontend.

## Facts verified in source (2026-07-15)

- `ServiceRequestOfferItemEntity`: `ItemType`, `Title`, `Description`, **`Quantity` (int)**, `UnitPrice`
  (decimal), `CurrencyCode`, `SortOrder`, `IsDiscount` (bool). **No UnitCode, no tax, no line totals, no discount
  fields.**
- `ServiceRequestOfferEntity`: `Status`, `TotalAmount` (decimal), `CurrencyCode`, `Description`, `ProviderNotes`,
  `EstimatedStart/End`, `EstimatedDurationMinutes`, `ExpiresAt`, Accepted/Rejected/Withdrawn At+reasons.
  `RecalculateTotal() => Items.Sum(Quantity * UnitPrice)`. **No tax totals, no deposit, no SubmittedAt/ViewedAt/
  RevisionRequestedAt, no concurrency token, no Version.**
- Offer status enum: Draft/Submitted/UnderReview/Accepted/Rejected/Withdrawn/Expired.
- **A migration without a `.Designer.cs` is not discovered by EF** — this bit us on `AddContentUpdatedAt`. Every
  migration here must generate its Designer; verify the columns exist **in the database**, not just in C#.

## Decisions (doc 06) — do not relitigate

Money = `decimal`, never float. Quantity = `decimal(12,3)`. Tax per line. Discount = explicit
`DiscountType{Amount,Percent}` + `DiscountValue`, applied pre-tax. EmergencyFee taxable. Deposit = structured
(type+value) + free-text payment/warranty notes. Submitted immutable; formal versioning post-MVP.

## Work — schema + domain only

### Offer item — new columns
`ServiceRequestOfferItemEntity`:
- **`Quantity`: `int` → `decimal(12,3)`** (migration + entity + `Create`/`Update` signatures).
- add `UnitCode` (`string?`), `TaxRate` (`decimal`, numeric(9,4); fraction e.g. 0.20), and the **computed
  snapshots** `LineSubtotal`, `TaxAmount`, `LineTotal`, `DiscountAmount` (all numeric(18,2)) — set by the
  calculation service in 10c, but the columns and private setters live here.
- add discount inputs `DiscountType` (enum `{ None=0, Amount=1, Percent=2 }`, default None) and `DiscountValue`
  (`decimal?`). Keep `IsDiscount` for now (the `ItemType == Discount` line) or replace it — state which; prefer
  driving off `ItemType == Discount` + `DiscountType/Value` and dropping `IsDiscount`.

### Offer — new columns
`ServiceRequestOfferEntity`:
- totals (set in 10c): `Subtotal`, `TaxTotal`, `DiscountTotal`, `GrandTotal` (numeric(18,2)). Keep `TotalAmount`
  as an alias for `GrandTotal` **or** migrate callers — state which; do not leave two independent totals that can
  disagree.
- per-category totals: either 8 columns (`ServiceTotal`, `ProductTotal`, `LaborTotal`, `InstallationTotal`,
  `InspectionTotal`, `DeliveryTotal`, `EmergencyFeeTotal`, `OtherTotal`) **or** compute-on-read in the projection.
  Recommend **compute-in-the-calculation-service and store**, so the read is cheap and consistent.
- commercial terms: `DepositType` (enum `{ None, Amount, Percent }`), `DepositValue` (`decimal?`),
  `PaymentTermsNote` (`string?`), `WarrantyNote` (`string?`).
- lifecycle timestamps: `SubmittedAt`, `ViewedAt`, `RevisionRequestedAt` (all `DateTime?`).
- **concurrency token**: a `RowVersion` (`byte[]`, `[Timestamp]` / xmin) or an incrementing `int ConcurrencyStamp`
  — pick the project's existing pattern if one exists; else Postgres `xmin` via EF `IsRowVersion()`.

### Domain methods
- `ReplaceItems(IEnumerable<...>)` — atomically swap the draft's item set (the aggregate-save shape from doc 07).
- `MarkSubmitted(DateTime utc)` sets `Status=Submitted` + `SubmittedAt`; `MarkViewed`, `MarkRevisionRequested`.
- Keep `Update(...)` but ensure a **submitted** offer cannot be mutated (guard: only Draft is editable) — the
  actual enforcement is 10c, but the domain method should not silently allow it.

### Migrations
One migration (or a small set), **each with its Designer**. `Quantity int→decimal` needs a `USING` cast in the
raw SQL if EF's default cast is rejected by Postgres — verify it applies. **No backfill of totals here** (10c
computes them); existing offers get null/zero totals until re-saved — note that.

## Acceptance — observed against the DATABASE
- `\d service_request_offer_items` and `\d service_request_offers` show the new columns with the right types
  (paste it). `Quantity` is `numeric`.
- A round-trip: create a draft offer with a decimal quantity (2.5) and a tax rate; it persists and reads back.
- Build succeeds (module + BFF). The existing create/update/withdraw offer flow still compiles (adjust its
  callers for the decimal quantity + new fields, but **do not** add calculation here).

## Constraints
- No calculation logic (that is 10c). No BFF endpoint changes (10d). No frontend.
- No float. No `object`/`JsonElement` on the wire. Migrations must have Designers and be verified in the DB.

## Report
Append to `REPORT_BACKEND.md` (section "10b"): the two `\d` outputs, the decimal round-trip, and which choice you
made for `IsDiscount`, the `TotalAmount`/`GrandTotal` relationship, and per-category totals. Unfinished is **not
done**.
