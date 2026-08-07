# Provider QA — CargoDry (P-QA8)

## Static findings
`cargodry` has 3 pages (products, renewals, inventory routes) + api + hooks, but `CargoDryProductsPage` shows **0 query
hooks** (EmptyState). Nav marks it `planned`. **Decision on record:** provider CargoDry is **read-oriented** — kit
activation is user-side (QR/app), not provider; there is no provider CargoDry write command. So the provider surface is
sales/commercial + read (products, renewals, inventory view), not activation.

## Live walkthrough checklist (localhost:3002/app/cargodry, /renewals, /products, /inventory)
- [ ] Products/inventory/renewals load real read data (loading/empty/error) — or a clear "no CargoDry activity" empty state.
- [ ] No "Kit Aktive Et" / activation write action on the provider side (activation is user-side).
- [ ] Renewals reflect real upcoming/settlement data; commercial/attribution figures correct (tier bonus is
      settlement-affecting per decision).
- [ ] Nav flag reconciled once verified.

## Fix candidates
Wire the read pages if they're bare EmptyState while data exists → `FIX_*` here; otherwise confirm the read-only scope is
intentional and the empty states are correct.
