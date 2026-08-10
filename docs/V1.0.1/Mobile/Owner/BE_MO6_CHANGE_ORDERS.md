# BE_MO6 — owner change-order review + approve/reject → incremental checkout (mobile)

> **Repos:** `addesso-project` (ServiceRequest + Payment + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile`. Owner
> Economics track **MO6** — the piece **S11 left blocked on the owner surface**: the provider proposes extra work
> (change order) on an accepted SR; the **owner reviews and approves/rejects**. Approve runs the **incremental** economics
> (new snapshot + incremental capture/split) or a P10 refund for a decrease — **reusing S11/P8/P9/P10 unchanged**. Money
> moves → **iyzico-gated** (builds + dev-testable via the manual gateway; live capture waits on P9 keys). Additive; identity
> from token; cost-free. **Do not commit.**

## Baseline (investigated — S11 already built the engine)
- `ServiceChangeOrderController` at `api/v1/service-requests/{srId}/change-orders`: **Propose** (provider), **Approve**
  (`PATCH {coId}/approve` → `ApproveServiceChangeOrderCommand`), **Reject** (`PATCH {coId}/reject`), + list
  (`GetServiceChangeOrders`).
- **Approve handler (reuse verbatim):** `Increase` → P8 `CalculateServiceRequestEconomics` over the CO lines under a
  **distinct context ref `SR-{sr}-OFFER-{offer}-CO-{id}`** → a **new** immutable snapshot + a **new incremental
  escrow/split** (the accepted snapshot is untouched); `Decrease` → a **P10 refund** of the delta (no new snapshot);
  **idempotent** on the CO context ref (re-apply never double-charges); a P5/S9 breach → **Rejected**. This is the same
  **capture-at-approve** model as MO3's capture-at-accept.
- **Owner-gate:** the module approve/reject aren't owner-scoped (like accept/dispute) → **BFF owner-gates** via
  `EnsureOwnedAsync` (consistent with MO1–MO5).

## BE — owner change-order surface (all reuse; no economics change)
1. **List/detail:** owner-scoped read of the SR's change orders — direction (Increase/Decrease), changed lines
   (added/removed, offer-item-shaped, **cost-free**), reason, the **incremental customer amount** (what the owner would pay
   / be refunded), status. Reuse `GetServiceChangeOrders`; map to a **cost-free** mobile DTO (no provider cost/commission/
   net; no S5 margin).
2. **Approve:** reuse `ApproveServiceChangeOrder` (Increase → incremental P8 snapshot+capture; Decrease → P10 refund) —
   **do not touch** the economics. Capture-at-approve (matching MO3); for a live Increase the incremental payment is
   iyzico-gated (dev via the manual gateway); reuse the **MO3 payment-status seam** to report the incremental capture
   result (Pending→Captured/Failed).
3. **Reject:** reuse `RejectServiceChangeOrder` — terminal; no economics.
4. **Mobile BFF:** passthroughs — change-order list + approve + reject, **owner-gated (EnsureOwnedAsync)**, cost-free, owner
   identity via BffAssertion; reuse the payment-status passthrough for the incremental result.

## FE — owner change orders (`inktavia-marine-mobile`)
- On the accepted-SR detail, a **pending change orders** section: for each, show **what changed** (added/removed lines) +
  the **incremental ₺** (extra to pay for Increase / to be refunded for Decrease) + reason + status; loading/empty/error.
- **Approve** → a confirm sheet with the incremental amount →
  - **Increase:** capture-at-approve; for live iyzico the incremental payment opens the form (WebView) / manual dev path;
    poll payment-status → success/failure; on success the SR's effective total = original + increment (cost-free display).
  - **Decrease:** confirm → P10 refund of the delta; show the refunded amount.
- **Reject** → terminal, clear state. Cost-free throughout (customer amounts only). tr+en; mock parity.

## Don't-break / QA
- **S11 apply logic, P8 incremental snapshot, P9 split, P10 refund, the accepted snapshot's immutability, idempotency, and
  the P5/S9 breach gate are UNCHANGED** — MO6 exposes owner review + approve/reject, reusing them. Additive BFF+FE;
  owner-gated; identity from token; cost-free (grep the CO payload — no provider cost/commission/net/margin). Builds clean;
  FE tsc+lint; tr+en. Live-gate split documented (dev manual gateway vs live iyzico + P9 keys).
- Tests: (1) owner lists change orders on their own SR (non-owner rejected); (2) approve Increase → new incremental
  snapshot + capture (idempotent re-apply, no double-charge), original snapshot untouched; (3) approve Decrease → P10 refund
  of the delta; (4) a P5/S9-breaching CO → Rejected, no capture; (5) reject → terminal; (6) cost-free payload.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO6_CHANGE_ORDERS.md`: the owner change-order list + approve(incremental capture /
decrease-refund)/reject (reusing S11/P8/P9/P10), the BFF owner-gate, the FE review→approve/reject flow, the
dev-testable-vs-iyzico-gated split, and the tests. Then MO7 (subscription/plan + discount transparency).
