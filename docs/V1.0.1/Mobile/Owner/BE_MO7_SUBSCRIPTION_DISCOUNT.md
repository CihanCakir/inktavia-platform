# BE_MO7 — owner participant membership (subscribe/plan) + discount transparency (mobile)

> **Repos:** `addesso-project` (Payment module + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile`. Owner Economics
> track **MO7**: the owner views/subscribes to a **participant membership plan** and sees the **customer discount** their
> plan earns them, transparently, on offers/checkout. Paid subscription payment is **iyzico-gated** (builds + dev-testable
> via the manual gateway, like MO3); free/launch plans are immediate. Additive; identity from token; cost-free. **Do not
> commit.**

## Baseline (investigated)
- **Participant plan/subscription exists (Payment):** queries `GetParticipantPlans` (`?includeInactive`),
  `GetParticipantPlanById`, `GetActiveParticipantSubscription`; commands `SubscribeParticipantPlan` (blocks if already
  active / plan-not-found; snapshots the resolved price per P4), `CancelParticipantSubscription`. `ParticipantPlanController`
  at `api/v1/payment/participant-plans` is **admin-oriented** (Create/Update/Activate/Deactivate) — MO7 needs the
  **owner-facing** read + subscribe/cancel.
- **Customer discount (P6) exists:** `ResolveCustomerDiscount` + `CustomerBenefitBudget`. The **offer/checkout already carry
  the discount** (`DiscountTotal`, shown in MO2/MO3) — MO7 makes it **legible as a plan benefit**, it does not re-compute it.
- **I3:** the participant identity/profile exists; the subscription lives in Payment (`ParticipantPlanSubscription`). MO7 is
  the owner-facing subscription surface + discount transparency; no new identity link needed beyond the token's participant.

## BE — owner membership surface (reuse; no economics change)
1. **Plans (read):** owner-facing list of **active** participant plans (name, price in ₺, benefit summary — e.g. the
   discount rate/perks) via `GetParticipantPlans(includeInactive:false)`.
2. **Current subscription (read):** `GetActiveParticipantSubscription` for the token's participant — plan, status, price,
   renewal/next-charge date, launch-vs-list price.
3. **Subscribe:** reuse `SubscribeParticipantPlan` (owner = token participant, never from body). **Paid plan → iyzico-gated**:
   reuse the **MO3 checkout/payment-status pattern** for the subscription charge (dev via manual gateway; live via iyzico +
   P9 keys). **Free/launch plan → immediate** (no payment). Block double-subscribe (existing guard).
4. **Cancel:** reuse `CancelParticipantSubscription`.
5. **Discount transparency (read):** surface the customer discount the plan yields — reuse the existing `DiscountTotal` on
   offers/checkout, **labelled as a plan benefit**; optionally call `ResolveCustomerDiscount` to show "your {plan} saved
   ₺X" and the remaining benefit budget. Cost-free (customer-facing figures only; no platform/provider funding internals).
6. **Mobile BFF:** passthroughs — plans, current-subscription, subscribe (+ payment-status for the paid charge), cancel;
   owner identity via BffAssertion; typed, envelope-correct; cost-free (never accept a client-supplied price/plan-price).

## FE — membership + discount (`inktavia-marine-mobile`)
- A **Membership** screen (profile/settings area): **current plan** (name, status, price, renewal, benefits) or a
  "no plan" state; **available plans** to compare (price + benefits/discount); **Subscribe** → for a paid plan, the
  MO3-style pay flow (confirm ₺ → iyzico WebView / manual dev → payment-status → active); for free/launch, immediate;
  **Cancel** with confirm.
- **Discount transparency:** on the MO2 offer detail + MO3 checkout, when a customer discount applies, label it clearly as
  a **plan benefit** ("{plan} indirimi: −₺X") next to the existing DiscountTotal; if no plan, an optional nudge that a plan
  would save money (non-pushy). Cost-free; tr+en; mock parity.

## Don't-break / QA
- Additive: owner-facing reads + subscribe/cancel passthroughs + FE membership screen + discount labelling. The plan
  pricing (P4), customer discount (P6), budget reserve/consume, and subscription billing logic are **unchanged** — MO7
  exposes/labels them. Paid-subscription live charge iyzico-gated (dev manual gateway; document the split). Identity from
  token; cost-free (grep — no funding split / platform-funded internals in the owner payload). BE builds clean; FE
  tsc+lint; tr+en.
- Tests: (1) owner sees active plans + their own current subscription (not another participant's); (2) subscribe to a
  free/launch plan → active immediately; a paid plan → payment-gated (Pending→active on capture; dev manual path);
  (3) double-subscribe blocked (existing guard); (4) cancel → subscription cancelled; (5) discount surfaces as a plan
  benefit on an offer, cost-free (no funding internals); (6) no client-supplied price accepted.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO7_SUBSCRIPTION.md`: the owner plans/current-subscription/subscribe(+paid iyzico-gated
via MO3 pattern)/cancel, the discount-as-plan-benefit transparency (reusing P6 DiscountTotal + ResolveCustomerDiscount), the
FE membership screen, the dev-vs-iyzico-gated split, and the tests. Then MO8 (maintenance schedules — owner self-service).
