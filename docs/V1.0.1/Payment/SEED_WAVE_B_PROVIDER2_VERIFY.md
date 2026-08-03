# SEED + VERIFY — light up Wave B's positive branches for Provider 2 (economics breakdown, disputed/refund, negative balance)

> **Goal:** on-screen proof of the two Wave B branches that had no data — Part A "Kazanç kırılımı" (economics breakdown)
> and Part C (İtirazlı chip + refund breakdown + negative-balance strip) — for the provider used in verification
> (**"PROVIDER 2 AS"**). The FE is already implemented + verified for Part B (live) and A/C legacy (null-safe); this only
> adds the missing **data** and drives the screen.
>
> **Seed via the domain factories / real construction paths — NOT raw column INSERTs.** `PaymentEconomicsSnapshot` is an
> immutable, invariant-guarded entity (the 8 equalities / zero-tolerance); build it with
> `PaymentEconomicsSnapshotEntity.CreateFromLines(...)` so the invariants hold. Same discipline for RefundAllocation
> (use the allocation calculator) and `ProviderBalanceEntity` (use its domain methods). **Dev/Local-guarded, idempotent
> (find-or-create), no migration.** Do NOT touch product code, the FE, the admin panel, provider-web, or CargoDry — this
> is seed data + a bring-up + verification only.

## Ground truth
- Existing payment mock-seed infra to extend (same pattern, `EnvironmentGuard` Local/Development, idempotent):
  `Modules/Payment/.../Repository/Seed/` — `TransactionRefundMockSeed`, `PayoutRecordMockSeed`, `SubscriptionMockSeed`,
  `InvoiceMockSeed`, etc. Provider 2 already has mock transactions/payouts (the legacy tx `TXN-20260610-P202` renders),
  so **extend those** rather than inventing a new provider.
- Factories/entities: `PaymentEconomicsSnapshotEntity.CreateFromLines` (Economics), `ProviderBalanceEntity`
  (RefundAllocation), the RefundAllocation calculator/repos (`IRefundAllocationRepositories`).
- The provider BFF read-model enriches the FE fields FROM these: `ProviderTransactionDto.EconomicsBreakdown` ← the
  linked `PaymentEconomicsSnapshot` (via `transactions.EconomicsSnapshotId`); `RefundSummary`/`DisputedAt` ← the
  RefundAllocation + tx; `ProviderPayoutPagedResultDto.NegativeBalance` ← the provider's `ProviderBalance`. So the seed
  must set those links, not just insert loose rows.

## Seed (dev-only, idempotent, Provider 2)
Resolve Provider 2's identity the same way the existing mock seeds do (the "PROVIDER 2 AS" provider profile / the
provider used by `PayoutRecordMockSeed`). Then ensure:

1. **A Released service transaction WITH an economics snapshot** (lights up Part A):
   - Build a `PaymentEconomicsSnapshot` via `CreateFromLines` with realistic, balanced numbers so the breakdown reads
     cleanly and the invariants pass — e.g. CustomerTotal ≈ 20 000, a service line + platform fee (net/VAT/gross),
     commission base, commission, provider net, platform gross share (pick values that satisfy the 8 equalities; mirror
     an existing BE-P8/S8 test's line inputs if easier).
   - Create/attach a **Released** provider transaction for Provider 2 with `EconomicsSnapshotId` = that snapshot (mirror
     how `PayoutRecordMockSeed`/`TransactionRefundMockSeed` build a tx; set status Released so it shows under settled).
   - Result: the FE `EconomicsBreakdown` is non-null → the "Kazanç kırılımı" walk renders to gold ProviderNet.

2. **A disputed, release-after refunded transaction + an over-limit negative balance** (lights up Part C):
   - Create a settled Provider-2 transaction, then apply a **release-after** refund via the RefundAllocation path so a
     `RefundAllocation` exists with `PlatformAdvancedRefundAmount` / `ProviderRecoveryAmount` /
     `RemainingProviderNegativeBalance`; set the tx `DisputedAt`.
   - Push Provider 2's `ProviderBalance` **over its `NegativeBalanceLimit`** (so `IsOverLimit = true`) via the balance
     domain methods.
   - Result: the FE shows the **"İtirazlı"** chip + the refund breakdown in the drawer, and the payouts **negative-balance
     strip** in its over-limit (danger) state.

Keep both records clearly demo-labelled (notes/reference) and idempotent so re-running the seed doesn't duplicate.

## Bring-up
Ensure the **provider BFF (`bff-marineprovider`, :17002)** and `payment-api` are running (rebuild payment-api if the seed
is added to its startup): `docker compose up -d --build payment-api && docker compose up -d bff-marineprovider`. Ensure
`keycloak-init` has run. (One service at a time to avoid the OOM.)

## Verify on-screen (provider-web, Provider 2)
Log into provider-web as Provider 2 (OTP from `docker compose logs identity-api | grep DEV-ONLY`). Confirm:
1. **Part A:** open the seeded Released transaction → the **"Kazanç kırılımı"** section walks
   CustomerTotal → ServiceAmount → PlatformFee (gross, with net+VAT sub-line) → CommissionBase → Commission →
   **gold ProviderNet**; the legacy tx still shows summary-only (no regression).
2. **Part C:** the seeded disputed tx shows the **"İtirazlı"** chip + the refund breakdown (platform advanced / provider
   recovery / remaining negative balance) in the drawer; the **Payouts** tab shows the negative-balance strip in its
   over-limit warning state.
3. **Part B** still shows the Launch/List badge (unchanged).

## Report
Append a "Live positive-branch verification" section to `docs/V1.0.1/Payment/REPORT_FE_PROVIDER_WAVE_B.md`: what was
seeded (entities + the snapshot line inputs + the balance/refund values), how (factories, not raw SQL), the on-screen
transcript for A + C positive branches, and how to remove the demo seed if desired. Nothing else touched.
