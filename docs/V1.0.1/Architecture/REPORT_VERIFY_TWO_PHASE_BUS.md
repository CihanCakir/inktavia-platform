# REPORT — verify the two-phase-bus epic (WS1 + WS2) is intact

> Verification only (no code change). Confirms the WS2 directed-commit is committed, the WS1 financial-idempotency
> constraints are applied in `inktavia_store`, and a K≥2 fanout message produces exactly one notification row (no
> double-commit). **All three checks PASS. Nothing committed.**

## (1) WS2 directed-commit committed in HEAD — PASS
Both Core.Messagebus base consumers direct the two-phase Commit/Rollback to the **preparer's own input queue**
(`context.ReceiveContext.InputAddress`) instead of publishing to the shared, consumer-agnostic exchange (which fanned
the commit to every consumer of the message → side effects ×P):
- `Core/Messagebus/src/Aizen.Core.Messagebus.Abstraction/Consumers/AizenBaseMessageConsumer.cs` — uses
  `context.GetSendEndpoint(context.ReceiveContext.InputAddress)` for the directed commit (2 `InputAddress` refs).
- `Core/Messagebus/src/Aizen.Core.Messagebus.Abstraction/Consumers/AizenBaseMessageConsumerWithResult.cs` — uses
  `CreateRequestClient<T>(context.ReceiveContext.InputAddress)` for the directed request/response commit (2 refs).

Both files' HEAD versions contain the `InputAddress` directed-commit, and the working tree is **byte-identical to HEAD**
(`git diff HEAD` = 0 lines) — so WS2 is committed, not a stray working-tree edit.

## (2) WS1 migration `20260804105802` constraints applied in `inktavia_store` — PASS
`Migrations/20260804105802_AddFinancialIdempotencyUniqueIndexes` is recorded in `public."__EFMigrationsHistory"`, and all
**3 partial-unique indexes exist in the `payment` schema** with the exact filters from the migration (verified via
`pg_indexes`):

| Index | Table | Definition (live) |
|-------|-------|-------------------|
| `UX_transaction_refund_records_FullRefund_Active` | `payment.transaction_refund_records` | `UNIQUE (PaymentTransactionId) WHERE RefundType = 1 AND Status <> 4` |
| `UX_payout_records_PaymentTransactionId_Active` | `payment.payout_records` | `UNIQUE (PaymentTransactionId) WHERE PaymentTransactionId IS NOT NULL AND Status <> 4` |
| `IX_invoice_headers_PaymentTransactionId` | `payment.invoice_headers` | `UNIQUE (PaymentTransactionId) WHERE PaymentTransactionId IS NOT NULL AND InvoiceType IN (2, 3)` |

These are the DB-level idempotency guards (WS1 defense-in-depth) that make the financial consumers duplicate-proof
(23505 benign-catch on redelivery).

## (3) K≥2 fanout message → notification rows cnt=1 — PASS
`MessagingMessageSentMessage` has **K≥3 consumers** — the Notification `MessagingMessageSentConsumer`, the AdminPanel
`AdminMessagingRealtimeConsumer`, and the MarineProvider realtime consumer — so a chat send is a genuine K≥2 fanout (the
exact shape that, pre-WS2, fanned the commit and multiplied the side effect).

Triggered **one** owner chat message (SR 55) → HTTP 200. The Notification consumer created **exactly one** notification
row for the recipient:
- New row **Id 197**, `RecipientUserId=100011`, `Type=200` (NewMessageReceived) — **cnt = 1** per recipient (not K=3).

So the directed commit reaches only the preparer; the notification side effect is committed **once**. No double-commit /
side-effect multiplication.

## Conclusion
The two-phase-bus epic is intact end-to-end: WS2 directed-commit (committed in both base consumers), WS1 financial
partial-unique constraints (applied in `inktavia_store`), and the live K≥2 fanout yields P≡1 (one notification row).
Nothing was changed or committed.
