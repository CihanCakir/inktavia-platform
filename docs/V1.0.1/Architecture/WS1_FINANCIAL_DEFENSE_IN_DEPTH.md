# WS1 — financial defense-in-depth (unique constraints + money-after-persist + commit idempotency)

> **Repo:** `addesso-project` — **financial modules + DB only. Do NOT touch Core.Messagebus / the Prepare-Commit
> protocol** (that's WS2). First workstream of the exactly-once plan
> (`PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md`); basis + per-consumer detail in `REPORT_SPIKE_TWO_PHASE_BUS.md` (§7–8 +
> blast-radius table). Goal: make every financial side-effect **idempotent + duplicate-proof** so the two-phase
> double-commit (and any future redelivery) **cannot** double money — protection that stands **regardless** of WS2 and
> stays permanently. Latent today (those flows are unexercised); must be in before payments go live.

## The 6 financial consumers (from the spike)
Tier 1 (deterministic, no commit guard): `ProviderSubscriptionPaymentSucceeded`, `ParticipantSubscriptionPaymentSucceeded`
(→ SubscriptionInvoice), `ServiceRequestPaymentReleased` (→ CommissionInvoice + revenue event). Tier 2 (TOCTOU, gateway
before persist): `ServiceRequestCompleted` (→ escrow payout), `ServiceRequestCancelled` (→ refund),
`CargoDryKitRenewalPayment` (→ charge). Use the spike report's exact consumer files + natural keys.

## PART A — unique DB constraints (the hard duplicate-proof layer)
For each financial write, add a **unique index on its natural idempotency key** so a duplicate side-effect **cannot
persist**, and handle the unique-violation as a benign "already processed" (catch → no-op, don't throw).
- **`invoice_headers.PaymentTransactionId`** — today it has a **non-unique** partial index (`WHERE PaymentTransactionId
  IS NOT NULL`). Change it to a **partial UNIQUE** index (`IS NOT NULL`), covering both subscription + commission
  invoices (one invoice per payment transaction). This is the single highest-value constraint.
- **Payout** (escrow release / `ServiceRequestPaymentReleased` + `ServiceRequestCompleted`) — unique on its natural key
  (e.g. released transaction id / (ServiceRequestId + attribution)); one payout per release.
- **Refund** (`ServiceRequestCancelled`) — unique on (ServiceRequestId / refund source ref); one refund per cancellation.
- **CargoDry renewal charge** (`CargoDryKitRenewalPayment`) — unique on (KitId + renewal period / renewal txn); one
  charge per kit-period.
Derive the exact column(s) per consumer from the spike report + the entity. EF Core migrations; **check existing data for
pre-existing duplicates first** and dedupe/skip if any (flows are unexercised, so likely none — verify). Migrations must
apply cleanly on the running DB.

## PART B — money-after-persist (fix the Tier-2 TOCTOU)
In `ServiceRequestCompleted` (payout), `ServiceRequestCancelled` (refund), `CargoDryKitRenewalPayment` (charge): today the
**gateway call precedes the persist**, so a concurrent second commit re-hits the gateway before the first has recorded
it. Reorder to **persist the idempotency marker/record first, then call the gateway conditionally** ("only if not already
recorded"). So the second commit finds the marker and **skips the gateway entirely** (no double external money movement).
Keep the domain outcome identical for the single-commit case.

## PART C — commit-level idempotency (app guard above the DB)
In each of the 6 `ExecuteCommitMessage`, **check the natural key first** and return early if already processed — so the
common case never even attempts the duplicate insert (the DB unique index is the backstop for the race). Mirror the
pattern already used by `ServiceRequestMessageSyncConsumer` (dedupe) / the notification report's guard.

## Do NOT
- No change to Core.Messagebus, `CustomEndpointNameFormatter`, `AizenBaseMessageConsumer`, or the Prepare/Commit/Rollback
  protocol (WS2). No change to K=1 consumers. FE untouched.

## Verify
Since these flows are unexercised, verify by **driving each message through the bus** (as the spike did) and, where
possible, forcing the double-commit (K≥2 or a manual re-publish):
1. Each of the 6 flows produces **exactly one** invoice / payout / refund / charge row and **one** gateway call — even
   under a forced duplicate commit (the unique index + Part C guard hold; Part B prevents the double gateway hit).
2. A deliberately re-published/duplicated event hits the unique constraint and is swallowed benignly (no exception
   bubbling, no second row).
3. K=1 financial/other flows unchanged; P12 ledger unaffected; builds clean; migrations apply on the running DB;
   same-image redeploy.

## Report
`docs/V1.0.1/Architecture/REPORT_WS1_FINANCIAL_DEFENSE.md`: the unique indexes added (per table + the natural key), the
money-after-persist reorder per Tier-2 consumer, the commit-idempotency guards, the migration/data-dedup notes, and the
forced-duplicate proof (one row + one gateway call). Then WS2 (root directed-commit bus fix) is the next kickoff.
