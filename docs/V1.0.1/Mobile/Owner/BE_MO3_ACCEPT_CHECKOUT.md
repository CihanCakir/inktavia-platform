# BE_MO3 — owner accept offer → checkout → payment result (mobile)

> **Repos:** `addesso-project` (ServiceRequest + Payment + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile`.
> Owner Economics track **MO3** — the first phase where **money moves**: the owner accepts a provider offer, pays via the
> gateway, and the SR becomes paid/assigned. This is the **core economic transaction**. **Diagnostic-first + iyzico-gated**:
> the flow builds and is **dev-testable via the manual gateway**; **live** iyzico capture needs the sandbox keys (the P9
> production gate). **The escrow / split / economics are OWNED by P8/P9 and must NOT change** — MO3 only exposes the
> owner-facing accept + pay + result. Additive; identity from token; cost-free. **Do not commit.**

## Baseline (investigated)
- **Accept exists:** SR `AcceptServiceRequestOffer` runs **P8 `CalculateServiceRequestEconomics`** (plan → S7 → P3 fee → P5
  gate → S8 snapshot → **escrow**) *before* committing; Rejected/ConfigError → block (no half-accept); success → SR
  `OfferAccepted` + `SetPaymentTransaction(txId)` + publishes `ServiceRequestOfferAcceptedMessage`. It returns
  `AcceptServiceRequestOfferResponse(offerId, srId)` — **no checkout payload today.**
- **Checkout exists at the gateway:** `IPaymentGatewayProvider.InitiateCheckoutAsync(CheckoutInitInput) → CheckoutInitResult`
  ("redirect/embed URL and gateway reference"); the **iyzico** provider returns `CheckoutFormContent` (the buyer's payment
  form; **Capture** is the MVP default). A **`ManualPaymentGatewayProvider`** also exists → **dev/testing without iyzico
  keys**. Webhook → `PaymentCaptured`; release-on-completion is later.
- **P9 gate:** iyzico live keys are placeholder (production gate). MO3's live capture waits on them; everything else builds
  and runs on the manual gateway.

## Phase 0 — diagnose the checkout wiring (decides the shape)
Determine **how the owner obtains the payment form for an accepted SR**:
1. Does anything already call `InitiateCheckoutAsync` for an SR acceptance (returning `CheckoutFormContent`/redirect), or is
   it only wired for P11 boost? 
2. Is the intended flow **(a) accept-then-checkout** (accept creates the escrow; a **separate** owner "pay" step initiates
   the checkout form) or **(b) accept-returns-checkout** (accept also returns the form payload)? Pick the one that fits the
   existing escrow/capture lifecycle — prefer **(a)** if the escrow is authorize-first and capture happens on the buyer's
   payment (cleaner: accept = commit + escrow; pay = a distinct, retriable step).
3. Which gateway is selected per environment (iyzico vs manual) and how (`IPaymentGatewayProvider` DI)?
Record the finding; build to it.

## BE — owner accept + checkout + result
- **Accept:** reuse the existing SR `AcceptServiceRequestOffer` (owner). Do **not** change its P8/escrow logic.
- **Checkout init:** ensure an owner-callable **"initiate checkout for my accepted SR"** path that returns the gateway's
  `CheckoutInitResult` (iyzico `CheckoutFormContent` / redirect URL + gateway reference; manual gateway returns a
  dev-completable stub). If Phase 0 shows this isn't wired for SR acceptance, add a thin command/endpoint that calls
  `InitiateCheckoutAsync` with the accepted SR's transaction/snapshot (reusing P9's basket + split-math guard — **do not
  reimplement** the basket/split). Owner-scoped (the caller owns the SR).
- **Result/confirm:** an owner-callable **payment-status** read (poll the transaction state: Pending → Captured/Failed),
  driven by the existing webhook → `PaymentCaptured`. No new capture logic — reuse.
- **Mobile BFF:** passthroughs — accept, initiate-checkout (returns form content / redirect + ref), payment-status; owner
  identity from the token via BffAssertion; typed, envelope-correct. Never accept a client-supplied amount/provider id.

## FE — owner accept → pay → result (`inktavia-marine-mobile`)
- Replace the **MO2 "Accept = coming-soon" stub** with the real flow: **Accept** → a confirm sheet showing the **frozen
  customer total** (what they'll pay, ₺) → call accept → then **checkout**:
  - **iyzico:** open the `CheckoutFormContent` in a **WebView** (or the redirect URL) → 3DS → on return/callback, poll
    payment-status → success/failure screen.
  - **manual gateway (dev):** a dev-completable confirm so the flow is exercisable without iyzico keys.
- On success: the SR/offer shows **Accepted/Paid + assigned**, with the frozen economics (the S3/S8 amounts — cost-free);
  link to the job/assignment. On failure/cancel: clear error, offer retry (the escrow/accept state must not be left
  inconsistent — a failed payment should be retriable, not a dead accepted-but-unpaid SR; confirm the lifecycle).
- Cost-free throughout (customer total + line breakdown only; no commission/cost).

## Don't-break / QA
- **P8 economics, S8 snapshot, P9 split/escrow, the 8-equality, and capture/webhook logic are UNCHANGED** — MO3 exposes
  the owner accept + pay + status only, reusing them. Additive BFF passthroughs + FE. Identity from token; amounts
  server-owned; cost-free. Builds clean; FE tsc+lint; tr+en; mock parity.
- **Live gate:** clearly separate "builds + dev-testable via manual gateway" from "live iyzico capture (needs sandbox
  keys + the P9 gate)". Do not hardcode/print keys.
- Tests: (1) accept runs P8 + escrow, blocks on Rejected/ConfigError (unchanged); (2) checkout-init returns a form/redirect
  for the owner's own accepted SR (rejects a non-owner); (3) manual-gateway path completes a dev payment → status Captured →
  SR Paid; (4) payment-status poll reflects Pending→Captured/Failed; (5) a failed/cancelled payment leaves a retriable
  state, no orphaned accepted-unpaid SR; (6) cost-free payload.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO3_CHECKOUT.md`: the Phase-0 checkout-wiring finding (accept-then-checkout vs
accept-returns-checkout; gateway selection), the accept + checkout-init + payment-status surface (reusing P8/P9, nothing
re-implemented), the FE accept→pay→result flow (iyzico WebView + manual dev path), the failure/retry lifecycle, and the
dev-testable-vs-live-gated split. Note the iyzico live verification remains gated on the sandbox keys (P9). Then MO4
(completion review + auto-approve countdown).
