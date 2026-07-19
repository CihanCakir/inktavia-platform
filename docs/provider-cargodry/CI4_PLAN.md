# CI-4 — Provider stock-request action (replaces the header placeholders)

**Decision (project owner):** CargoDry kit **activation is user-side** (end user activates via QR/app). The provider
does **not** activate kits → the "Kit Aktive Et" header button is **removed**. "Stok Girişi" is repurposed into
**"Stok Talebi"**: the provider requests a new stock allocation from admin. This needs a **new provider-facing
command** (no existing CargoDry write command is provider-initiated — they're all admin/system/user).

## Phases
- **CI-4a — backend, provider side** (`CI4A_BACKEND_STOCK_REQUEST.md`): new `CargoDryStockRequestEntity` +
  status enum + migration; `CreateProviderStockRequestCommand` (+ validator/handler) and
  `GetProviderStockRequestsQuery`; provider controller `POST/GET /cargodry/provider/stock-requests`
  (+ `POST /{id}/cancel`) and a small `GET /cargodry/provider/products` (eligible products for the modal);
  BFF passthrough. This makes "Stok Talebi" functional and lists the provider's own requests.
- **CI-4b — backend, admin approval loop** (follow-up, `CI4B_BACKEND_ADMIN_APPROVAL.md` — write after 4a lands):
  admin list of pending requests + `Approve` / `Reject` commands; on approve, optionally chain
  `AllocateBatchToProvider` to fulfil. Closes the loop.
- **CI-4c — frontend** (implemented directly in `inktavia-marine-provider-web`, like CI-2b/CI-3b): remove
  "Kit Aktive Et"; "Stok Girişi" → "Stok Talebi" modal (product from the provider's holdings + quantity + note) +
  a "Stok Talepleri" list with status chips on the CargoDry Envanter page.

## Notes / reality
- Provider2 currently has inventory (STANDARD-90) but **no consignment agreement row** — so eligibility is based on
  the provider's **inventory products** (they can request more of what they hold), not on an agreement. Link an
  agreement id when one exists; don't hard-require it (would block the demo).
- Requests are **Pending** until admin acts (CI-4b). CI-4a alone lets the provider submit + see their request list;
  the approval loop is 4b.

Order: CI-4a → CI-4c (demo the request flow, requests show Pending) → CI-4b (approval). Verify on screen after 4c.
