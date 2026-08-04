# REPORT — WS1: Financial defense-in-depth (unique constraints + money-after-persist + commit idempotency)

> **Kickoff:** `WS1_FINANCIAL_DEFENSE_IN_DEPTH.md` (gated by `PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md`;
> basis `REPORT_SPIKE_TWO_PHASE_BUS.md`). **Financial modules + DB only.** Core.Messagebus, the
> Prepare/Commit/Rollback protocol, `CustomEndpointNameFormatter`, `AizenBaseMessageConsumer`, and all K=1
> consumers were **not** touched — that is WS2.
> **Status:** ✅ **DONE.** Builds clean (0 errors); migration applied on the running `inktavia_store`;
> forced-duplicate proven at the DB for all 6 flows; P12 ledger + K=1 flows unaffected.
> **Environment:** live local docker stack (`postgres/inktavia_store`, `rabbitmq`), same evidence class as
> the spike (live Postgres constraint behaviour + source trace — the 6 flows remain unexercised, 0 real
> invoice/payout rows, so behaviour is proven at the natural-key layer they all funnel through).

---

## 0. Summary

Every financial side-effect from the spike's 6 consumers is now **idempotent + duplicate-proof** at three
layers, so the two-phase double-commit (and any future redelivery) **cannot** double money:

- **PART A — hard DB layer:** a partial-**unique** index on each natural idempotency key. A duplicate side
  effect physically cannot persist; the unique-violation (PostgreSQL SQLSTATE **23505**) is swallowed
  benignly as "already processed".
- **PART B — money-after-persist (Tier-2 TOCTOU fix):** for the two consumers that move external money
  (escrow payout, refund), the marker record is persisted **before** the gateway call, and the gateway call
  is reached only by the row that won the unique-key insert — so a concurrent second commit **skips the
  gateway entirely**.
- **PART C — app guard above the DB:** each `ExecuteCommitMessage` re-checks the natural key first and
  returns early if already processed (the DB unique index is the race backstop).

Net effect: **one invoice / payout / refund / charge row and one gateway call per event, even under a forced
duplicate commit.**

## 1. The 6 financial consumers + natural keys

| # | Consumer | Side-effect | Natural idempotency key | Enforcing unique index |
|---|---|---|---|---|
| 1 | `ProviderSubscriptionPaymentSucceededConsumer` (T1) | SubscriptionInvoice | `(PaymentTransactionId, InvoiceType=3)` | `IX_invoice_headers_PaymentTransactionId` |
| 2 | `ParticipantSubscriptionPaymentSucceededConsumer` (T1) | SubscriptionInvoice | `(PaymentTransactionId, InvoiceType=3)` | `IX_invoice_headers_PaymentTransactionId` |
| 3 | `ServiceRequestPaymentReleasedConsumer` (T1) | CommissionInvoice + `InvoiceIssuedMessage` | `(PaymentTransactionId, InvoiceType=2)` | `IX_invoice_headers_PaymentTransactionId` |
| 4 | `ServiceRequestCompletedConsumer` (T2) | **gateway escrow payout** + PayoutRecord | `payout.PaymentTransactionId` (non-Failed) | `UX_payout_records_PaymentTransactionId_Active` |
| 5 | `ServiceRequestCancelledConsumer` (T2) | **gateway full refund** + refund record | `(refund.PaymentTransactionId, RefundType=Full)` (non-Failed) | `UX_transaction_refund_records_FullRefund_Active` |
| 6 | `CargoDryKitRenewalPaymentConsumer` (T2) | renewal charge transaction (internal Capture) | `transactions.IdempotencyKey` = `RENEWAL-{KitCode}-{yyyyMM}` | `IX_transactions_IdempotencyKey` (pre-existing) |

## 2. PART A — unique DB constraints

**Migration:** `20260804105802_AddFinancialIdempotencyUniqueIndexes`
(`Modules/Payment/src/Aizen.Modules.Payment.Repository/Migrations/`). Applied cleanly on the running
`inktavia_store`; recorded in `public.__EFMigrationsHistory`. **Pre-existing duplicates checked first — zero
on every natural key** (the flows are unexercised), so no dedup step was needed.

| Table | Index | Change | Filter (partial) |
|---|---|---|---|
| `invoice_headers` | `IX_invoice_headers_PaymentTransactionId` | non-unique → **UNIQUE** | `"PaymentTransactionId" IS NOT NULL AND "InvoiceType" IN (2, 3)` |
| `payout_records` | `UX_payout_records_PaymentTransactionId_Active` | **new UNIQUE** | `"PaymentTransactionId" IS NOT NULL AND "Status" <> 4` |
| `transaction_refund_records` | `UX_transaction_refund_records_FullRefund_Active` | **new UNIQUE** (existing non-unique FK index kept) | `"RefundType" = 1 AND "Status" <> 4` |
| `transactions` (CargoDry) | `IX_transactions_IdempotencyKey` | already UNIQUE — no change | — |

**Why the invoice filter is scoped to `InvoiceType IN (2,3)`** — the single highest-value constraint, but it
must not false-collide. Several legitimate flows write `PaymentTransactionId` on an invoice referencing the
*same* transaction id as a commission invoice (`RefundPaymentCommandHandler`/`RecordChargebackCommandHandler`
write refund/chargeback **records**, not invoices; but `CreateInvoiceDraftCommandHandler` can create a
CreditNote/SalesInvoice for the same txn). Restricting uniqueness to the two auto-issued consumer types
(2=CommissionInvoice, 3=SubscriptionInvoice) makes it duplicate-proof for those consumers while a CreditNote
(5) / SalesInvoice (1) / CargoDryInvoice (4) / manual draft on the same txn is still allowed. A given txn id
maps to exactly one of {2,3} within the filter, so uniqueness always holds. **Proven** in §5.

**Why `Status <> 4` (Failed) on payout/refund** — the marker is inserted *before* the gateway (PART B). If
the gateway rejects, the marker is flipped to Failed and thereby leaves the partial index, so a later message
can retry (create a fresh Pending marker). Non-Failed rows (Pending/Processing/Completed/Processed) occupy the
key and block a duplicate. Audit trail is preserved (no hard deletes).

## 3. PART B — money-after-persist (Tier-2 TOCTOU fix)

The old order was **gateway → persist**, so two concurrent commit copies could both read the pre-terminal
state and both hit the gateway (double payout / refund). Reordered to **persist marker → gateway (conditional)
→ finalize**:

- **`ServiceRequestCompletedConsumer` (escrow payout):**
  1. PART C guard: `ActivePayoutExistsAsync(tx.Id)` → skip if a non-Failed payout already claims the txn.
  2. Create `PayoutRecordEntity` in **Pending**, `SaveChanges` → **claims** `UX_payout_records_…_Active`.
     If a concurrent copy won the insert, the 23505 is swallowed and **the gateway is not called**.
  3. `gateway.ReleaseEscrowAsync(...)` — reached only by the marker-holder.
  4. Success → `MarkCompleted` + `tx.Release()` + save + publish `PaymentEscrowReleasedMessage`.
     Failure → `MarkFailed(...)` (marker leaves the index; retry-able), no `tx.Release()`.

- **`ServiceRequestCancelledConsumer` (full refund):** identical shape —
  `FullRefundExistsAsync(tx.Id)` guard → insert **Pending** `TransactionRefundRecord(RefundType.Full)` +
  `SaveChanges` (claims `UX_transaction_refund_records_FullRefund_Active`; 23505 → benign skip, no gateway) →
  `gateway.RefundAsync(...)` → success `MarkProcessed` + `tx.ApplyRefund` + `RefundAllocationService.ApplyAsync`
  + save + publish; failure `MarkFailed` (retry-able). The `MarkProcessed → ApplyRefund → allocation` order is
  preserved exactly.

- **`CargoDryKitRenewalPaymentConsumer` (renewal charge):** the charge is recorded by an **internal**
  `transaction.Capture()` — there is **no external gateway money movement** in this consumer (the renewal was
  paid before the message). So "money-after-persist" is already the case; the protection is the pre-existing
  unique `IdempotencyKey` plus the benign-catch. No reorder needed.

## 4. PART C — commit-level idempotency + benign unique-violation handling

Each `ExecuteCommitMessage` now checks the natural key first (mirrors `ServiceRequestMessageSyncConsumer`'s
dedupe pattern) and the claiming `SaveChanges` is wrapped to swallow 23505:

- **Invoice consumers (1–3):** `_invoices.ExistsByTransactionIdAndTypeAsync(txId, <type>)` → early return;
  the final invoice `SaveChanges` catches 23505 → benign skip (no second invoice number, no second outbound
  event).
- **Payout (4):** `_payouts.ActivePayoutExistsAsync(txId)` guard; marker-insert `SaveChanges` catches 23505.
- **Refund (5):** `_transactions.FullRefundExistsAsync(txId)` guard; marker-insert `SaveChanges` catches 23505.
- **CargoDry (6):** existing `GetByIdempotencyKeyAsync` guard (Prepare + Commit) retained; charge `SaveChanges`
  now catches 23505.

**New repo methods** (interface + EF impl): `IInvoiceRepository.ExistsByTransactionIdAndTypeAsync`,
`IPaymentTransactionRepository.FullRefundExistsAsync`, `IPayoutRecordRepository.ActivePayoutExistsAsync`.
**Helper:** `Consumers/PaymentIdempotency.IsUniqueViolation(DbUpdateException)` (matches SQLSTATE `23505`,
mirroring Identity's `UserMessagePermissionRepository`). No other implementers of these interfaces exist
(no mocks/fakes to update).

## 5. Verify — forced-duplicate proof (live Postgres)

Each natural-key duplicate was forced inside a rolled-back transaction; every one is rejected with **23505**
(the exact code the consumers swallow), and every legitimate non-colliding row is still allowed:

```
INVOICE   dup CommissionInvoice(type2) same txn      → ERROR 23505 IX_invoice_headers_PaymentTransactionId ✓ REJECT
          CreditNote(5)+SalesInvoice(1)+Commission(2) same txn → 3 rows coexist                            ✓ ALLOW (no false-collision)
PAYOUT    2nd non-Failed payout same txn             → ERROR 23505 UX_payout_records_PaymentTransactionId_Active ✓ REJECT
          Failed(4) + Pending(1) same txn            → both persist                                        ✓ ALLOW (retry after gateway failure)
REFUND    2nd non-Failed FULL refund same txn        → ERROR 23505 UX_transaction_refund_records_FullRefund_Active ✓ REJECT
          Full(Processed) + Partial(Processed)       → both persist                                        ✓ ALLOW (multiple partials)
          Full(Failed) + new Full(Pending)           → both persist                                        ✓ ALLOW (retry after gateway failure)
CARGODRY  dup transactions.IdempotencyKey            → ERROR 23505 IX_transactions_IdempotencyKey          ✓ REJECT
```

Mapping to the acceptance criteria:
1. **Exactly one row + one gateway call under a forced duplicate** — the unique index guarantees at most one
   persisted marker (proven above); PART B guarantees the gateway is reached only by the insert-winner, so the
   losing concurrent commit returns before any external call.
2. **Re-published/duplicated event hits the unique constraint and is swallowed benignly** — the 23505 is caught
   by `PaymentIdempotency.IsUniqueViolation` in each claiming `SaveChanges`; no exception bubbles, no second row.
3. **K=1 flows + P12 ledger unaffected** — no K=1 consumer or shared infra was touched; the migration adds only
   the three financial indexes (`financial_ledger_entries` index footprint unchanged = 6). **Builds clean**
   (0 errors); **migration applies on the running DB**; changes are module-local so a **same-image redeploy**
   carries them (no split-brain risk).

## 6. Files changed

**EF config** (`…Repository/Persistence/Configurations/`): `InvoiceHeaderConfiguration.cs`,
`PayoutRecordConfiguration.cs`, `TransactionRefundRecordConfiguration.cs`.
**Migration:** `…Repository/Migrations/20260804105802_AddFinancialIdempotencyUniqueIndexes.cs` (+ Designer + snapshot).
**Repos:** `IInvoiceRepository.cs`/`InvoiceRepository.cs`, `IPaymentTransactionRepository.cs`/`PaymentTransactionRepository.cs`,
`IPayoutRecordRepository.cs`/`PayoutRecordRepository.cs`.
**Consumers** (`…/Aizen.Modules.Payment/Consumers/`): `Subscription/ProviderSubscriptionPaymentSucceededConsumer.cs`,
`Subscription/ParticipantSubscriptionPaymentSucceededConsumer.cs`, `ServiceRequest/ServiceRequestPaymentReleasedConsumer.cs`,
`ServiceRequest/ServiceRequestCompletedConsumer.cs`, `ServiceRequest/ServiceRequestCancelledConsumer.cs`,
`CargoDry/CargoDryKitRenewalPaymentConsumer.cs`, and new `Consumers/PaymentIdempotency.cs`.

## 7. Scope boundary + residual notes

- **Not touched (WS2):** Core.Messagebus, `CustomEndpointNameFormatter`, `AizenBaseMessageConsumer`, the
  Prepare/Commit/Rollback protocol, K=1 consumers, FE. WS1 neutralises the financial blast radius
  **independently of** and **permanently alongside** the WS2 root fix.
- **Residual (narrow, backstopped):** the marker→gateway claim closes the two-phase double-commit fanout (P
  concurrent commits of one publish: only the insert-winner reaches the gateway). A hypothetical *concurrent
  redelivery* of the same event **after** a prior gateway failure would re-run the same unique-insert claim, so
  it is likewise single-gateway. The DB unique index is the ultimate backstop in every case.
- **Next:** WS2 (root directed-commit bus fix) per `PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md` — WS1's DB
  constraints stay permanently as defense-in-depth.
