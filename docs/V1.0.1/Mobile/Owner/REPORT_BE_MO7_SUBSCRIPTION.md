# REPORT — BE_MO7 owner participant membership (subscribe/plan) + discount transparency

> Owner Economics **MO7** — the owner views/subscribes to a **participant membership plan** and sees the **customer
> discount** their plan earns, transparently, on offers/checkout. Paid subscription payment is **iyzico-gated** (builds
> + dev-testable via the manual gateway, like MO3); free/launch plans are immediate. Additive; identity from token;
> cost-free. Reuses P4 pricing / P6 discount / subscription billing **unchanged**. **NOT committed.**
>
> Repos: `addesso-project` (Payment module + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile` (FE).

## Outcome
- **BE builds clean** — Payment module: 0 errors; Marine.Participant.Mobile BFF: 0 errors.
- **Tests: 88/88 green** in `Aizen.Modules.Payment.Repository.UnitTests` (6 new MO7 integration/cost-free cases over an
  in-memory DbContext + real handlers + the manual gateway).
- Plan pricing (P4), customer discount (P6), budget reserve/consume, and the subscription billing consumer are
  **unchanged** — MO7 exposes/labels them.

---

## Two decisive findings
1. **No token identity for participants in Payment — but the assertion already carries it.** The Payment subscription
   ops key on `ParticipantProfileId` and the only existing controllers are `[Authorize(Roles="Admin")]`. There is no
   `ParticipantProfileId` on the token accessor. **However**, the mobile BFF's delegating handler already asserts the
   resolved participant profile id in the shared `X-Aizen-Provider-Profile-Id` header, which the middleware surfaces on
   `KeycloakTokenInfo.ProviderProfileId`. So a new owner-facing Payment controller resolves the participant identity
   from the token exactly like `PaymentProviderController` — **no Core/InfoAccessor change needed**.
2. **Paid subscription = checkout → capture → consumer (like MO3 capture-at-accept).** `SubscribeParticipantPlan` is
   immediate and creates no transaction; the real paid path is a Subscription-context checkout whose **capture** fires
   the existing `ParticipantSubscriptionPaymentSucceededConsumer`, which builds the subscription + invoice. MO7's paid
   subscribe reuses that consumer + the gateway + the MO3 transaction-status poll — no billing logic added.

---

## BE — Payment module (additive; NO economics change)
- **New owner controller** `ParticipantMembershipController` (`api/v1/payment/participant`, `[Authorize]`) — resolves
  the participant profile id from the asserted token (mirrors `PaymentProviderController`). Endpoints: `GET plans`
  (reuses `GetParticipantPlansQuery(includeInactive:false)`), `GET subscription` (reuses
  `GetActiveParticipantSubscriptionQuery`), `POST subscription {planId}`, `DELETE subscription?reason=` (reuses
  `CancelParticipantSubscriptionCommand`), `GET subscription/payment-status/{transactionId}`.
- **New command** `SubscribeParticipantForOwnerCommand` — guards double-subscribe + plan-not-found (reused rules).
  **Free** plan (`MonthlyPriceTRY == 0`) → creates the subscription immediately (mirrors the admin subscribe with
  PaidAmount 0). **Paid** plan → creates a PendingIntent Subscription-context checkout (mirrors the P11 boost checkout:
  non-marketplace, no split, no escrow; `ContextId = plan id`, `PayerProfileId = participant`, `GrossAmount = the
  plan's price`) and initiates the gateway checkout — the **subscription is created by the capture consumer**. The
  price is always resolved from the plan server-side; **a client-supplied price/participant is never accepted** (the
  body carries only the plan id). Idempotent per `(participant, plan)` via the transaction idempotency key — a
  re-initiate reuses the pending checkout, never a second charge.
- **New query** `GetParticipantSubscriptionPaymentStatusForOwnerQuery` — owner-gates (the transaction must be a
  Subscription-context tx paid by this participant) and maps the raw status to the same MO3 owner lifecycle
  (None/Pending/Paid/Failed/Cancelled). Cost-free.
- **New cost-free result records** (`Abstraction/Model/Result`): `SubscribeParticipantForOwnerResult`,
  `ParticipantSubscriptionPaymentStatusResult` + the `SubscribeParticipantForOwnerRequest` (plan id only).

## BE — Marine.Participant.Mobile BFF (passthroughs, owner-asserted, cost-free)
- **New remote call** `IParticipantMembershipRemoteCall` → the Payment owner controller (plans / subscription /
  subscribe / cancel / payment-status). Plans deserialize straight into the cost-free mobile DTO (Refit maps the
  customer fields, drops the rest — `ParticipantPlanDto` lives in Payment.Application, not referenceable). Registered
  in DI with base URL `RemoteCalls:IParticipantMembershipRemoteCall:BaseUrl` (→ the payment-api host).
- **Cost-free mobile DTOs** (`Contracts/Membership/`): `MobileMembershipPlanDto` (+ `MobilePlanFeatureDto`),
  `MobileCurrentSubscriptionDto`, `MobileSubscribeResultDto`, `MobileMembershipPaymentStatusDto`,
  `MobileCancelSubscriptionDto`, `MobileSubscribeRequest`. Only customer price + perks (discount rate / earn
  multiplier / features) + the amount paid cross — never funding internals.
- **Handlers** (`Membership/`) — each resolves the participant (so the assertion carries the profile id) then proxies:
  `GetMobileMembershipPlansQuery`, `GetMobileCurrentSubscriptionQuery`, `SubscribeMobileMembershipCommand` (reads the
  fresh checkout status for the paid path), `CancelMobileMembershipCommand`, `GetMobileSubscriptionPaymentStatusQuery`.
- **New controller** `MembershipController` (`api/v1/mobile/membership`) — the 5 passthrough actions.

### Dev-testable vs live-iyzico (the money gate)
Same model as MO3/MO6. **Free plan** → immediate Active (no money). **Paid plan** → a PendingIntent Subscription
checkout; the manual gateway does not auto-capture, so the **dev path is an admin/manual capture** (`CapturePayment`)
which fires `ParticipantSubscriptionPaymentSucceededConsumer` → the subscription becomes Active and the poll flips to
Paid. **Live iyzico** capture (3DS webhook) is P9-gated — the FE polls the payment-status endpoint exactly as in MO3.
Runtime prerequisites for the paid live-round-trip: the payment-api must share the mobile BFF's
`BffAssertion:SharedSecret` and have `IParticipantMembershipRemoteCall:BaseUrl` reachable (env-gated, as with every MO
task).

## Discount transparency
No backend discount endpoint was added (keeps the owner payload cost-free — the P6 `CustomerDiscountBenefitInputs`
carries platform/provider funding-split internals). The offer/checkout already carry `DiscountTotal` (MO2/MO3); MO7
**labels** it as a plan benefit on the FE using the current subscription's plan name (`"{plan} indirimi: −₺X"`), with
an optional non-pushy nudge when the owner has no plan. `ResolveCustomerDiscount` remains available as a documented
extension but is intentionally not exposed to the owner.

---

## Tests (`ParticipantMembershipMo7Tests`, Payment Repository.UnitTests — 88/88 green)
1. **Free plan → immediate** — subscribe a ₺0 plan → Mode=Immediate, subscription Active now, **no transaction**.
2. **Paid plan → pending checkout at the plan price** — Mode=PaymentPending, a PendingIntent Subscription tx
   (`ContextId = plan id`, `PayerProfileId = participant`, `GrossAmount = 199`), **no subscription yet** (created on
   capture). Proves the price is server-resolved (test 6 — no client price).
3. **Double-subscribe blocked** — an existing active subscription → `SubscriptionAlreadyActive` (5024).
4. **Idempotent paid re-initiate** — a second subscribe for the same (participant, plan) reuses the pending
   transaction — exactly one charge.
5. **Payment status owner-scoped + lifecycle** — Pending before capture; another participant is rejected; Paid after
   capture (the MO3 map).
6. **Owner result DTOs cost-free** — reflection guard: no Funded/Funding/RevenueAllocation/Commission/NetPayout/Margin/
   PlatformFee member on the MO7 owner results.

### How the remaining acceptance criteria are enforced
- **(1) own subscription, not another's** — the owner controller resolves the participant from the token; `GET
  subscription` is scoped to that id; the payment-status query owner-gates by `PayerProfileId` (test 5).
- **(2) paid → Pending→active on capture (dev manual path)** — the paid subscribe creates the PendingIntent checkout
  (test 2); the existing capture consumer (unchanged) creates the subscription on capture; the FE polls the status.

---

## FE — inktavia-marine-mobile
Mirrors the MO3 accept→pay flow. **`npx tsc --noEmit` = 0 errors; i18n en/tr `membership` 34/34 + `services.offer`
47/47 (whole file 550/550); cost-free.** Not committed.

**New feature `src/features/membership/`** — `api/membershipApi.ts` (types + `fetchPlans`/`fetchCurrentSubscription`
(→null)/`subscribeToPlan`/`cancelSubscription`/`fetchSubscriptionPaymentStatus`), `api/useMembership.ts`
(`usePlans`/`useCurrentSubscription`/`useSubscribe`/`useCancelSubscription`/`useSubscriptionPaymentStatus` — the last
polls only while `Pending`), `screens/MembershipScreen.tsx` — a current-plan card (name/status/price/renewal/discount
benefit + Cancel) or a no-plan state, plus available-plan compare cards (₺ or Free, badge, highlighted discount-rate
benefit, features); a free plan subscribes immediately (toast); a paid plan opens an MO3-style `SubscribePaySheet`
(confirm → paying → pending → paid/failed, polling the payment-status while pending); Cancel via a confirm sheet.

**Wiring** — `endpoints.ts` (`MEMBERSHIP` group), `queryKeys.ts` (`membership`), Profile stack registration
(`types.ts` + `ProfileNavigator.tsx`), and `ProfileScreen` repoints the "Subscription Details" row → "Membership".

**Discount transparency** (`OwnerOfferDetailScreen`) — when the owner has an active plan, the offer's discount row
relabels to `services.offer.planDiscount` ("{plan} indirimi", highlighted) and the accept-confirm sheet shows the same
plan-benefit note; when the owner has no plan, a non-pushy nudge card links to Membership. Uses `useCurrentSubscription`.
Cost-free (only the discount amount + the plan code as the name).

**Mock parity** — new `membership.handlers.ts` (registered in `setupMocks.ts`): a FREE `LAUNCH` + a PAID `GOLD`
(10% discount) plan, empty initial subscription; free subscribe → Immediate/None; paid subscribe → PaymentPending
(auto-captured so the poll resolves to Paid) + activate; cancel → Cancelled. The existing MO2 offer seed already has
`discountTotal: 300`, so the plan-discount label shows once subscribed.

**Deviations** — no lint config (tsc is the sole gate); the plan-discount label uses the subscription's `planCode`
(e.g. "GOLD") as the display name (the cost-free current-subscription DTO carries no separate display name); the nudge
navigates cross-tab via `ProfileTab → Membership`.

---

## Don't-break / confidentiality
Additive: owner-facing reads + subscribe/cancel + a paid-checkout initiation + the payment-status poll + the FE
membership screen + discount labelling. P4 pricing, P6 discount, budget reserve/consume, and the subscription capture
consumer are untouched. Identity from token; cost-free (grep the owner payload — no funding split / platform-funded
internals).

## Next
**MO8** — maintenance schedules (owner self-service).
