# FIN-DATERANGE — Provider Finance list endpoints: add date-range filter — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment` (+ CargoDry settlements). The Finance UI (provider portal)
> now has per-tab filters and needs a shared **date-range** filter (from/to) across the list surfaces. Three provider
> list endpoints must accept optional `from`/`to` (UTC, inclusive-from / exclusive-to). Read-only, additive, non-breaking.

## Scope — three endpoints
1. **Transactions** — `GET /api/v1/payment/provider/transactions` (PAY-3). Filter on `CreatedAt`.
2. **Invoices** — `GET /api/v1/payment/provider/invoices` (PAY-4). Filter on `IssueDateUtc` (fallback `CreatedAt` when
   `IssueDateUtc` is null, i.e. drafts — but provider list already excludes drafts in practice; use `IssueDateUtc`).
3. **Settlements** — `GET /api/v1/.../cargodry/settlements` (Finance/CargoDry). Filter on the settlement period
   (`PeriodStartUtc`) or `CreatedAt` — pick the column the existing list already orders by; keep consistent.

## Changes per endpoint (repeat the same shape for each)
- **Repo:** add two optional params `DateTime? fromUtc, DateTime? toUtc` to the provider-scoped paged method
  (`GetProviderPagedAsync` / `GetProviderInvoicesPagedAsync` / settlement equivalent). Apply:
  `&& (fromUtc == null || x.<DateCol> >= fromUtc) && (toUtc == null || x.<DateCol> < toUtc)`.
  Keep existing deterministic ordering. **Do not** change existing param order in a breaking way — append the new
  params at the end (with defaults) or add an overload.
- **Query:** add `DateTimeOffset? From`, `DateTimeOffset? To` to the query object; pass through to the repo (convert to
  UTC `DateTime`).
- **Controller:** add `[FromQuery] DateTime? from`, `[FromQuery] DateTime? to`. **PostgreSQL-safe:** coerce to UTC
  (`DateTime.SpecifyKind(x, DateTimeKind.Utc)`) before querying — Npgsql rejects non-UTC `timestamptz` comparisons.
- **BFF:** add `[Query] string? from, [Query] string? to` (ISO-8601) to the Refit call + BFF query + controller; pass
  through untouched. Routes unchanged.

## Semantics
- Both bounds optional and independent. `from` inclusive, `to` exclusive (so a single-day filter passes
  `from=2026-07-01&to=2026-07-02`).
- Invalid/unparseable dates → ignore that bound (treat as null), never 400 the whole request.
- Combines with existing `status`/`type` filters (AND).

## Verify — run and PASTE output (container + DB)
provider2 = 100011. For each endpoint:
1. Build module + BFF: 0 errors; restart `payment-api` + `bff-marineprovider`.
2. HTTP smoke (provider2 token):
   ```
   GET .../transactions?from=2026-07-01&to=2026-08-01     # only July rows
   GET .../transactions?to=2026-07-26                     # only rows before 26 Jul
   GET .../invoices?from=2026-07-01                        # only invoices issued on/after 1 Jul
   GET .../cargodry/settlements?from=2026-07-01&to=2026-08-01
   ```
   Confirm counts shrink correctly vs unfiltered; confirm combining with `status`/`type` still works; confirm a
   non-UTC/garbage `from` does not 500.

## Acceptance
- All three provider list endpoints accept optional `from`/`to`, UTC-safe, inclusive-from/exclusive-to, additive to
  existing filters, non-breaking repo signatures, typed + PRT preserved. Build clean; HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("FIN-DATERANGE"): date-range added to transactions/invoices/settlements provider endpoints
(repo + query + controller + BFF), UTC-safe. Verified: filtered counts, filter combination, bad-input tolerance.
FE then wires a shared date-range control (bu ay / son 3 ay / özel) across the Finance tabs.
