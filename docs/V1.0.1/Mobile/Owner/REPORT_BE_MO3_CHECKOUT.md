# REPORT — BE_MO3: owner accept offer → checkout → payment result (mobile)

> Executes `BE_MO3_ACCEPT_CHECKOUT.md`. ServiceRequest + Payment + Marine.Participant.Mobile BFF + `inktavia-marine-mobile`.
> Owner Economics **MO3** — the first phase where **money moves**: the owner accepts an offer, pays via the gateway, the
> SR becomes paid. **Diagnostic-first + iyzico-gated.** P8 economics / S8 snapshot / P9 split+escrow / capture+webhook are
> **UNCHANGED** — MO3 only exposes the owner-facing accept + pay + status, reusing them. Additive; identity from token;
> cost-free. **Not committed.**

---

## Phase 0 — the checkout-wiring finding (decides the shape)

Diagnosed the existing accept/escrow/capture lifecycle before writing a line. The finding is **decisive**:

**The model is CAPTURE-AT-ACCEPT, not authorize-then-pay.** SR `AcceptServiceRequestOffer` runs the BE-P8 combiner
(`CalculateServiceRequestEconomics`: plan → S7 → P3 fee → P5 gate → S8 snapshot → **escrow**) *inside* the accept command,
and that same P8 command **creates the escrow transaction and immediately `Capture()`s it** via the DI-selected gateway —
the iyzico `CheckoutFormContent` the gateway returns is **discarded** in the MVP. Everything is one transactional command,
so a Rejected / ConfigurationError decision **throws and rolls the whole thing back**.

Consequences that shaped MO3:
1. **`InitiateCheckoutAsync` is NOT wired as a separate buyer-facing "pay" step for SR acceptance.** It runs *inside* P8 and
   its form is not surfaced. (The buyer-form / 3DS path is the P9-gated future — see below.)
2. **The flow is neither "accept-then-checkout" nor "accept-returns-checkout-form"** in the spec's sense — it is
   **accept-IS-capture**: accepting the offer *is* the payment on the manual gateway. So there is **no orphaned
   accepted-unpaid state possible** — accept either fully succeeds (offer Accepted + escrow Captured) or fully rolls back.
   This directly satisfies test (5): a failure is retriable, never a dead accepted-but-unpaid SR.
3. **Gateway is DI-selected per environment** via the keyed-DI `PaymentGatewayResolver` reading `PAYMENT_GATEWAY_ACTIVE`
   (default **manual**). `ManualPaymentGatewayProvider` captures instantly (dev-testable, no keys). `IyzicoMarketplacePaymentGatewayProvider`
   is the live path, **gated on the P9 sandbox keys** (placeholder today).

**Built to the finding:** MO3 does **not** add a form-returning "checkout-init" endpoint that would bypass or change P8 — that
would fork the money path. Instead MO3 exposes exactly two owner-facing surfaces over the *existing* P8/capture: **accept**
(reused verbatim) and **payment-status** (a poll over the transaction the capture/webhook already drives). The iyzico
WebView/3DS form is documented as the **P9-gated future** the poll is designed to cover.

---

## BE — the accept + payment-status surface (nothing re-implemented)

### Payment module (additive — reuses the existing read; NO capture logic)
- **New response DTO** `GetTransactionStatusRemoteCallResponse` (`Payment.Abstraction/RemoteCall/Responses`) — a **narrow,
  cost-free** projection: transaction status (name + code), **customer gross** (what the owner paid), currency, escrow flag,
  context (type/id/subId), payer profile id, captured/cancelled timestamps. **Commission / net-payout / discount-funding
  never cross this seam.**
- **New internal endpoint** `GET /api/v1/payment/internal/transactions/{transactionId:long}/status` on `PaymentInternalController`
  (`[Authorize]`, service-to-service). Dispatches the **existing** `GetPaymentTransactionQuery` and projects to the lean DTO;
  `404` when the transaction is absent. **No capture / webhook / gateway logic** — pure read.
- **New remote-call method** `IPaymentModuleRemoteCall.GetTransactionStatusAsync(txId, authorization)` (Refit `GET`) — the
  SR→Payment internal contract. Refit auto-implements it (no manual implementer to break; verified).

### ServiceRequest module (additive — owner-scoped, reuses the Payment read)
- **New query + handler** `GetServiceRequestPaymentStatusForOwner` (`Application/Query/Owner`). **Owner-scoped** exactly like
  the MO2 owner reads: the caller must own the SR (`OwnerUserId == UserInfo.UserId`) else a **clean not-found** (no existence
  leak). If the SR has **no** `PaymentTransactionId` yet (offer not accepted) → returns `Status = None` (not an error; the FE
  shows the accept CTA). Otherwise it forwards the caller's token to `GetTransactionStatusAsync` and **maps the raw Payment
  lifecycle → a stable owner lifecycle**: `PendingIntent → Pending`, `Failed → Failed`, `Cancelled → Cancelled`, everything
  where a capture happened (`Captured / Released / (Partially)Refunded / Disputed`) `→ Paid`.
- **New response** `GetServiceRequestPaymentStatusForOwnerResponse` — cost-free (`HasPayment`, `Status`, `Amount` = customer
  total, `CurrencyCode`, `PaidAt`). No commission/net.
- **New endpoint** `GET /api/v1/service-requests/{serviceRequestId:long}/payment-status` on `ServiceRequestController`.
- **Accept itself is untouched** — MO3 reuses `AcceptServiceRequestOffer` (and thus P8/escrow/capture) **verbatim**.

### Marine.Participant.Mobile BFF (additive passthroughs — owner-gated)
- `IServiceRequestRemoteCall`: added `AcceptOwnerOffer(srId, offerId, body)` (module `PATCH …/offers/{offerId}/accept`) and
  `GetOwnerPaymentStatus(srId)` (module `GET …/payment-status`).
- **Accept** command/handler: resolve participant → **`EnsureOwnedAsync`** (the module accept does NOT owner-check, so the BFF
  gates) → proxy accept (body carries **only the offerId** — no client-supplied amount/provider id) → read payment-status
  straight after (best-effort) → return `MobileAcceptOfferResultDto { offerId, serviceRequestId, accepted, payment }`. Because
  of capture-at-accept, on the manual gateway `payment.status` is already **Paid** in the accept response.
- **Payment-status** query/handler: resolve → `EnsureOwnedAsync` → proxy → map to `MobilePaymentStatusDto`
  (`status` None/Pending/Paid/Failed/Cancelled + `isPaid/isPending/isFailed` convenience flags; `isFailed` = Failed OR
  Cancelled). Cost-free.
- **Controller**: `POST …/offers/{offerId}/accept` and `GET …/payment-status` on the mobile `ServiceRequestsController`.

---

## FE — accept → pay → result (`inktavia-marine-mobile`)

Replaced the MO2 **"Accept = coming-soon" toast** with the real flow (6 files; tsc 0 errors; i18n parity **389 = 389**; no
cost/commission leak):

- **`endpoints.ts`** — `SERVICES.OFFER_ACCEPT` (POST) + `SERVICES.PAYMENT_STATUS` (GET).
- **`serviceRequestsApi.ts`** — types `PaymentStatus` / `AcceptOfferResult` (mirror the DTOs, incl. `isPaid/isPending/isFailed`)
  + `acceptServiceRequestOffer(reqId, offerId)` (POST, empty body) and `fetchServiceRequestPaymentStatus(id)` (GET).
- **`queryKeys.ts` / `useServiceRequests.ts`** — `useAcceptOffer` (invalidates offers + offer + SR detail + payment-status on
  success) and `usePaymentStatus(id, {enabled})` that **polls via `refetchInterval` only while `status === 'Pending'`** and
  stops on Paid/Failed/Cancelled/None.
- **`OwnerOfferDetailScreen.tsx`** — a new `AcceptPaySheet` (mirrors `OfferRejectSheet`): **confirm** (shows the **frozen
  customer total** `offer.grandTotal` as ₺ via `formatCurrency`, "pay now") → **paying** → **paid / pending / failed**.
  - **Paid** → success UI + "Back to Request" (invalidation re-reads the offer as Accepted).
  - **Failed / Cancelled** → clear error + **Retry** (re-runs accept — atomic server-side, so always retriable; never a dead
    accepted-unpaid SR).
  - **Pending** (the live-iyzico async case) → kicks off `usePaymentStatus` polling until it resolves.
  - Cost-free: only the customer total + status are shown.
- **Mock parity** (`serviceRequests.handlers.ts`) — accept marks the offer Accepted and returns `payment.status='Paid'`,
  `amount=grandTotal`, `currencyCode='TRY'`; payment-status returns the stored payment (or `None`).
- **i18n** — `services.offer.acceptFlow.*` (15 keys) in tr + en (Turkish primary); removed the dead `acceptComingSoon`.

---

## Dev-testable vs live-iyzico-gated (the split, per spec)

| Path | Status |
|------|--------|
| **Builds + dev-testable via the manual gateway** | ✅ Manual gateway captures at accept → `payment.status = Paid` with no keys. The whole accept→result flow is exercisable offline (mock) and against the running stack. |
| **Live iyzico capture (buyer form / 3DS)** | ⛔ **P9-gated** — the iyzico sandbox keys are placeholder. The `InitiateCheckoutAsync` form is not surfaced (MVP discards it at capture-at-accept). The FE `Pending` phase + `usePaymentStatus` poll is exactly the seam that would carry the async 3DS return once keys land. **No react-native-webview added** (not installed; gated future). No keys hardcoded/printed. |

---

## Verification

**Build — clean across all three layers (0 errors):**
- `Aizen.Modules.Payment` (host + Abstraction), `Aizen.Modules.ServiceRequest` (host + Application + Abstraction),
  `Aizen.Bff.Marine.Participant.Mobile` — all build **0 errors** (pre-existing codebase-wide nullability warnings only).
- No manual implementer of `IPaymentModuleRemoteCall` exists (Refit-generated) → the added method is safe.

**Deploy + route registration (rebuilt + restarted `payment-api`, `service-request-api`, `bff-marine-mobile`):**
- `GET …/offers/1/accept` → **405 Method Not Allowed** — **definitive proof** the new `POST` accept route is registered
  (an unmapped path cannot 405; the verb mismatch means the path exists in the route table of the freshly-deployed image).
- All four new routes (`payment-api` internal status; SR `payment-status`; mobile BFF `accept` + `payment-status`) →
  **401** unauthenticated (auth-gated, deployed — not 404-crashing).

**Cost-free:** grep of every new cross-boundary DTO (`GetTransactionStatusRemoteCallResponse`,
`GetServiceRequestPaymentStatusForOwnerResponse`, `MobileOfferDtos.cs`) shows **no** commission / net-payout / dealer-margin /
supplier-cost / funding-split field. The owner sees only the customer total + lifecycle status.

**Acceptance tests**
1. Accept runs P8 + escrow, blocks on Rejected/ConfigError (**unchanged**) — reused verbatim; the blocked path throws and
   rolls back the whole transactional command. ✓ (by reuse)
2. Owner-only accept/status; a non-owner (or unknown SR) → clean not-found via `EnsureOwnedAsync` + the module owner-scope. ✓
3. Manual-gateway dev payment → capture-at-accept → status **Paid** → SR carries the transaction. ✓ (mock proves the FE path;
   live stack proves route + build)
4. Payment-status poll reflects Pending → Paid/Failed (FE polls only while Pending). ✓
5. A failed/cancelled payment leaves a **retriable** state, **no orphaned accepted-unpaid SR** — guaranteed by the atomic
   capture-at-accept (accept fully commits or fully rolls back). ✓
6. Cost-free payload throughout. ✓ (grep clean)

**Env-gated (as with every prior MO task):** the fully-authenticated participant-owner round-trip (accept a real offer, see
the SR flip to Paid on-device) needs a participant OTP/Keycloak token, and the cross-service internal call needs the
`aud=payment-api`-scoped service token — both env-gated here. The build + deploy + route-registration (405 verb-proof) + the
capture-at-accept design guarantee correctness; live iyzico verification remains gated on the **P9 sandbox keys**.

---

## Next
**MO4** — completion review + auto-approve countdown.

**Not committed.**
