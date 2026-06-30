# Payment Event / Consumer / Job Architecture

**Project:** Inktavia Marine OS — Payment Module  
**Date:** 2026-06-30  
**Author:** Architecture Review (AI-assisted)  
**Status:** Design Finalized — Implementation Phase Pending

---

## A. Existing Infrastructure Found

### A1. Message Bus

- **Technology:** MassTransit 8.x over RabbitMQ
- **Base message class:** `AizenBaseMessage` (in `Aizen.Core.Messagebus.Abstraction.Messages`)
- **Publisher interface:** `IAizenMessagePublisher` — injected via DI, `PublishAsync<T>` pattern
- **Consumer base class:** `AizenBaseMessageConsumer<TMessage>` — 3-phase protocol (see A2)

### A2. Consumer Pattern

All consumers extend `AizenBaseMessageConsumer<TMessage>` and implement three override phases:

```
1. ExecutePrepareMessage(ctx) → bool
   - Idempotency check
   - Load related entities
   - Return false to skip without error (already processed)

2. ExecuteCommitMessage(ctx) → Task
   - Actual business logic
   - Must call SaveChangesAsync() directly (NO AizenCommandHandlerDecorator wrapping)

3. ExecuteRollbackMessage(ctx, ex) → Task
   - Error recovery
   - Compensating actions if needed
```

**Critical rule:** Consumers call `_dbContext.SaveChangesAsync()` or a repository method that includes it. They are NOT wrapped by the command handler decorator.

### A3. Scheduler / Jobs

- **Technology:** Hangfire (recurring job scheduler)
- **Base class:** `AizenRecurringJob` — abstract, implements `IAizenRecurringJob`
- **Required overrides:** `IsActive` (bool), `CronExpression` (5-field), `ProcessAsync(CancellationToken)`
- **DI pattern:** Jobs create their own scope via `ServiceProvider.CreateScope()` at the top of `ProcessAsync`
- **Discovery:** Auto-discovered by `AizenApplicationBuilder` via host assembly scanning — no manual Hangfire registration needed
- **Logger:** `IAizenSchedulerLogger` injected via base constructor, `Logger.WriteConsole(msg)` pattern

### A4. Existing Consumers (3)

| Consumer | Source Message | Action |
|---|---|---|
| `ServiceRequestCompletedConsumer` | `ServiceRequestCompletedMessage` | Releases escrow via gateway, creates `PayoutRecordEntity`, publishes `PaymentEscrowReleasedMessage` |
| `ServiceRequestCancelledConsumer` | `ServiceRequestCancelledMessage` | Handles SR cancellation, marks transaction appropriately |
| `CargoDryKitRenewalPaymentConsumer` | (CargoDry internal) | Records `PaymentTransactionEntity` for paid kit renewals; idempotency key: `RENEWAL-{KitCode}-{yyyyMM}` |

### A5. Existing Jobs (4)

| Job | Cron | Purpose |
|---|---|---|
| `PaymentEscrowTimeoutJob` | `*/30 * * * *` | Marks stale `PendingIntent` transactions as Failed; publishes `PaymentFailedMessage(IsRetryable=true)` |
| `PaymentReminderJob` | `15 * * * *` | Publishes `PaymentReminderRequestedMessage` for 1h and 24h windows |
| `PayoutProcessingJob` | `0 2 * * *` | Safety net: marks stuck Iyzico payouts older than 48h as Failed |
| `StaleEscrowCleanupJob` | `0 3 * * *` | Permanently cancels `PendingIntent` older than 7 days; publishes `PaymentFailedMessage(IsRetryable=false)` |

### A6. Existing Messages in `Payment.Abstraction/Message/`

| Message | Direction | Published By |
|---|---|---|
| `PaymentCapturedMessage` | Outbound | Gateway capture handler |
| `PaymentCancelledMessage` | Outbound | Cancel command handler |
| `PaymentEscrowReleasedMessage` | Outbound | `ServiceRequestCompletedConsumer` |
| `PaymentFailedMessage` | Outbound | `PaymentEscrowTimeoutJob`, `StaleEscrowCleanupJob` |
| `PaymentRefundedMessage` | Outbound | Refund handler |
| `PaymentReminderRequestedMessage` | Outbound | `PaymentReminderJob` |
| `PayoutCompletedMessage` | Outbound | Payout handler |
| `PartialRefundReversedMessage` | Outbound | Partial refund reversal |
| `PaymentCancellationReinstatedMessage` | Outbound | Reinstatement handler |
| `ServiceRequestCompletedMessage` | Inbound | ServiceRequest module |
| `ServiceRequestCancelledMessage` | Inbound | ServiceRequest module |

---

## B. Current Phase 1B Capability

Phase 1B has implemented the following invoice operations:

| Capability | Status |
|---|---|
| Create Invoice Draft (CQRS command + validator) | ✅ Done |
| Issue Invoice — assign number, transition to Issued | ✅ Done |
| Cancel Draft Invoice (idempotent) | ✅ Done |
| `InvoiceNumberService` — prefix mapping, optimistic retry (5x, 50ms backoff) | ✅ Done |
| Get Invoice by ID (header only / full with lines) | ✅ Done |
| Get Invoices Paged (admin, multi-filter) | ✅ Done |
| Get Invoices by Buyer | ✅ Done |
| `PaymentInvoiceController` (7 endpoints) | ✅ Done |
| DI registration (`InvoiceNumberService`) | ✅ Done |

**What Phase 1B does NOT yet do:**
- Auto-issue invoices on payment events (no consumer wires payment → invoice creation)
- Publish `InvoiceIssuedMessage` after issuing
- Mark invoices Overdue by schedule
- Link subscription billing cycles to invoice generation

---

## C. Scenarios Currently Supported / Not Supported

### Currently Supported ✅

1. **Manual admin invoice draft creation** — POST `/api/v1/payment/invoices` with full line data
2. **Manual admin invoice issuance** — POST `/api/v1/payment/invoices/{id}/issue`
3. **Manual admin draft cancellation** — DELETE `/api/v1/payment/invoices/{id}`
4. **ServiceRequest payment capture** — via `PaymentCapturedMessage` (published), SR module consumes
5. **ServiceRequest completion escrow release** — `ServiceRequestCompletedConsumer` → `PayoutRecordEntity` + `PaymentEscrowReleasedMessage`
6. **CargoDry kit renewal payment recording** — `CargoDryKitRenewalPaymentConsumer`
7. **Stale PendingIntent timeout** — `PaymentEscrowTimeoutJob` marks Failed, publishes event
8. **Iyzico payout safety net** — `PayoutProcessingJob` handles stuck records

### Not Yet Supported ❌

1. **Auto-invoice on ServiceRequest payment release** — needs `ServiceRequestPaymentReleasedConsumer`
2. **Auto-invoice on ServiceRequest payment capture** — needs `ServiceRequestPaymentCapturedConsumer`
3. **Subscription invoice generation** — no job or consumer creates `SubscriptionInvoice` on subscription creation
4. **Subscription renewal by schedule** — no `SubscriptionRenewalJob` exists yet
5. **Provider payout batch processing** — no `ProviderPayoutBatchJob` for periodic payouts
6. **Invoice overdue marking** — no `InvoiceOverdueMarkingJob`
7. **Webhook retry mechanism** — no `PaymentWebhookRetryJob`
8. **Reconciliation** — no `PaymentReconciliationJob`
9. **InkCoin expiration** — no `InkCoinExpirationJob`
10. **Monthly usage counter reset** — no `MonthlyUsageCounterResetJob`
11. **Downstream notification of invoice issuance** — no `InvoiceIssuedMessage` published
12. **Auto-release eligibility check** — no `PaymentAutoReleaseEligibilityJob`

---

## D. Required Integration Events

The following integration events need to be created in `Aizen.Modules.Payment.Abstraction/Message/`.

Events already existing are marked with ✅. All others require new `*.cs` files.

### D1. New Events to Create

#### `InvoiceIssuedMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/InvoiceIssuedMessage.cs`  
**Trigger:** `IssueInvoiceCommandHandler` after successful `invoice.Issue(number, userId)`  
**Consumers:** Notification module (email delivery to buyer), BFF invoice preview caching  
**Idempotency:** `INVOICE-ISSUED-{InvoiceId}` — deduplicate on consumer side  
**MVP:** Yes

```csharp
public sealed class InvoiceIssuedMessage : AizenBaseMessage
{
    public long    InvoiceId         { get; init; }
    public string  InvoiceNumber     { get; init; } = default!;
    public InvoiceType InvoiceType   { get; init; }
    public long?   BuyerUserId       { get; init; }
    public string  BuyerName         { get; init; } = default!;
    public long?   SellerUserId      { get; init; }
    public decimal TotalAmount       { get; init; }
    public string  Currency          { get; init; } = "TRY";
    public DateTime IssuedAtUtc      { get; init; }
    public long?   SourceId          { get; init; }       // ServiceRequestId / SubscriptionId
    public InvoiceSourceType SourceType { get; init; }
}
```

---

#### `SubscriptionPaymentSucceededMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/SubscriptionPaymentSucceededMessage.cs`  
**Trigger:** Subscription payment confirmed (gateway webhook / `ProcessIyzicoWebhookCommandHandler`)  
**Consumers:** `ProviderSubscriptionPaymentSucceededConsumer`, `ParticipantSubscriptionPaymentSucceededConsumer`  
**Idempotency:** `SUB-PAY-OK-{TransactionId}`  
**MVP:** Yes

```csharp
public sealed class SubscriptionPaymentSucceededMessage : AizenBaseMessage
{
    public long   TransactionId         { get; init; }
    public long   UserProfileId         { get; init; }
    public SubscriberType SubscriberType { get; init; } // Provider | Participant
    public long   SubscriptionId        { get; init; }
    public long   PlanId                { get; init; }
    public decimal PaidAmount           { get; init; }
    public string  CurrencyCode         { get; init; } = "TRY";
    public DateTime PeriodStart         { get; init; }
    public DateTime PeriodEnd           { get; init; }
    public DateTime PaidAtUtc           { get; init; }
}
```

---

#### `SubscriptionPaymentFailedMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/SubscriptionPaymentFailedMessage.cs`  
**Trigger:** Subscription payment failed or timed out  
**Consumers:** Notification module  
**Idempotency:** `SUB-PAY-FAIL-{TransactionId}`  
**MVP:** Yes

```csharp
public sealed class SubscriptionPaymentFailedMessage : AizenBaseMessage
{
    public long   TransactionId     { get; init; }
    public long   UserProfileId     { get; init; }
    public SubscriberType SubscriberType { get; init; }
    public long   SubscriptionId    { get; init; }
    public string FailureReason     { get; init; } = default!;
    public bool   IsRetryable       { get; init; }
    public DateTime FailedAtUtc     { get; init; }
}
```

---

#### `CommissionCalculatedMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/CommissionCalculatedMessage.cs`  
**Trigger:** `CommissionCalculationService.CalculateAsync()` completion  
**Consumers:** Reporting module (post-MVP), Invoice auto-generation  
**Idempotency:** `COMMISSION-{CalculationId}`  
**MVP:** Post-MVP (commission calculation already logs to DB; event is for downstream reporting)

```csharp
public sealed class CommissionCalculatedMessage : AizenBaseMessage
{
    public long   CalculationId         { get; init; }
    public long   ServiceRequestId      { get; init; }
    public long   ProviderProfileId     { get; init; }
    public decimal GrossAmount          { get; init; }
    public decimal CommissionAmount     { get; init; }
    public decimal NetPayoutAmount      { get; init; }
    public decimal VatAmount            { get; init; }
    public string  CurrencyCode         { get; init; } = "TRY";
    public DateTime CalculatedAtUtc     { get; init; }
}
```

---

#### `ProviderPayoutCreatedMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/ProviderPayoutCreatedMessage.cs`  
**Trigger:** `PayoutRecordEntity` created by `ServiceRequestCompletedConsumer`  
**Consumers:** Notification module (provider receives payout notification)  
**MVP:** Post-MVP

```csharp
public sealed class ProviderPayoutCreatedMessage : AizenBaseMessage
{
    public long   PayoutId              { get; init; }
    public long   ProviderProfileId     { get; init; }
    public decimal NetPayoutAmount      { get; init; }
    public string  CurrencyCode         { get; init; } = "TRY";
    public long?   ServiceRequestId     { get; init; }
    public DateTime CreatedAtUtc        { get; init; }
}
```

---

#### `CargoDryRenewalPaymentSucceededMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/CargoDryRenewalPaymentSucceededMessage.cs`  
**Trigger:** `CargoDryKitRenewalPaymentConsumer` — after transaction recorded  
**Consumers:** CargoDry module (extend kit expiry), Invoice module (auto-generate renewal invoice)  
**Idempotency:** Already handled in consumer with `RENEWAL-{KitCode}-{yyyyMM}`; event is published after commit  
**MVP:** Yes

```csharp
public sealed class CargoDryRenewalPaymentSucceededMessage : AizenBaseMessage
{
    public long   TransactionId     { get; init; }
    public string KitCode           { get; init; } = default!;
    public long   UserProfileId     { get; init; }
    public decimal PaidAmount       { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";
    public DateTime RenewedAtUtc    { get; init; }
    public DateTime NewExpiryDate   { get; init; }
}
```

---

#### `InkCoinLedgerEntryCreatedMessage`
**File:** `Aizen.Modules.Payment.Abstraction/Message/InkCoinLedgerEntryCreatedMessage.cs`  
**Trigger:** InkCoin credit/debit operations (post-MVP)  
**MVP:** Post-MVP

---

### D2. Events Already Existing (reuse / enrich)

| Event | Current Gap | Action |
|---|---|---|
| `PaymentCapturedMessage` | Not published after subscription payments | Enrich `ProcessIyzicoWebhookCommandHandler` to publish for subscription context |
| `PaymentRefundedMessage` | Exists. `PaymentRefundProcessedConsumer` needs to consume it | No message change needed |
| `PaymentFailedMessage` | Exists. Add subscription context to `ContextType` enum if missing | Check `TransactionContextType` enum |
| `PaymentEscrowReleasedMessage` | Exists and published. No change needed | — |

---

## E. Required Consumers

All consumers live in `Aizen.Modules.Payment/Consumers/` and are registered in the host DI.

### E1. `ServiceRequestPaymentCapturedConsumer`
**Consumes:** `PaymentCapturedMessage` where `ContextType == ServiceRequest`  
**Purpose:** When SR payment is captured (held in escrow), advance SR state to `PaymentReceived` if not already  
**Phase:** Prepare (load SR transaction, check idempotency) → Commit (publish internal SR state event or direct SR status update via repository) → Rollback (log)  
**Idempotency key:** `SR-PAY-CAPTURED-{TransactionId}`  
**MVP:** Yes

```
File: Aizen.Modules.Payment/Consumers/ServiceRequestPaymentCapturedConsumer.cs
Injects: IPaymentTransactionRepository, IAizenMessagePublisher
SaveChanges: Yes (update transaction record if needed)
```

### E2. `ServiceRequestPaymentReleasedConsumer`
**Consumes:** `PaymentEscrowReleasedMessage`  
**Purpose:** After escrow is released, auto-create a `CommissionInvoice` draft and immediately issue it. Also auto-create a `SalesInvoice` draft for the buyer if not already issued.  
**Idempotency key:** `SR-PAY-RELEASED-{TransactionId}`  
**MVP:** Yes — this is the primary auto-invoice trigger for SR completion

```
File: Aizen.Modules.Payment/Consumers/ServiceRequestPaymentReleasedConsumer.cs
Injects: IInvoiceRepository, InvoiceNumberService, IAizenMessagePublisher
Sequence:
  1. Load PaymentEscrowReleasedMessage
  2. Check if CommissionInvoice already exists for this TransactionId (idempotency)
  3. Create CommissionInvoice header entity, compute lines from CommissionAmount
  4. Call InvoiceNumberService.GenerateAsync(CommissionInvoice)
  5. Set status to Issued
  6. AddAsync + SaveChangesAsync
  7. Publish InvoiceIssuedMessage
```

### E3. `ProviderSubscriptionPaymentSucceededConsumer`
**Consumes:** `SubscriptionPaymentSucceededMessage` where `SubscriberType == Provider`  
**Purpose:** Activate/extend `ProviderPlanSubscriptionEntity`; create `SubscriptionInvoice` and issue it  
**Idempotency key:** `PROV-SUB-OK-{TransactionId}`  
**MVP:** Yes

```
File: Aizen.Modules.Payment/Consumers/ProviderSubscriptionPaymentSucceededConsumer.cs
Injects: IProviderPlanSubscriptionRepository, IInvoiceRepository, InvoiceNumberService
```

### E4. `ParticipantSubscriptionPaymentSucceededConsumer`
**Consumes:** `SubscriptionPaymentSucceededMessage` where `SubscriberType == Participant`  
**Purpose:** Activate/extend `ParticipantPlanSubscriptionEntity`; create and issue `SubscriptionInvoice`  
**Idempotency key:** `PART-SUB-OK-{TransactionId}`  
**MVP:** Yes

```
File: Aizen.Modules.Payment/Consumers/ParticipantSubscriptionPaymentSucceededConsumer.cs
```

### E5. `CargoDryRenewalPaymentSucceededConsumer` (extend existing consumer)
**Note:** `CargoDryKitRenewalPaymentConsumer` already handles the transaction record. What is missing is publishing `CargoDryRenewalPaymentSucceededMessage` from its `ExecuteCommitMessage` phase. The CargoDry module then consumes this to extend kit expiry.  
**Action:** Modify `CargoDryKitRenewalPaymentConsumer.ExecuteCommitMessage` to publish `CargoDryRenewalPaymentSucceededMessage` after SaveChanges.  
**MVP:** Yes

### E6. `PaymentRefundProcessedConsumer`
**Consumes:** `PaymentRefundedMessage`  
**Purpose:** Create `RefundInvoice` (CreditNote) for the refund amount; update original invoice `PaidAmount` / `RemainingAmount`  
**Idempotency key:** `REFUND-INVOICE-{TransactionId}`  
**MVP:** Post-MVP (refund flow exists but auto-invoice not required for MVP)

```
File: Aizen.Modules.Payment/Consumers/PaymentRefundProcessedConsumer.cs
```

### E7. `InvoiceIssuedConsumer`
**Consumes:** `InvoiceIssuedMessage`  
**Purpose:** Forward to Notification module (email PDF link to buyer). This consumer lives in the Notification module, not Payment — referenced here for completeness.  
**MVP:** Post-MVP (manual email sufficient at MVP)

---

## F. Required Jobs

All jobs live in `Aizen.Modules.Payment/Jobs/` and extend `AizenRecurringJob`.

### F1. `SubscriptionRenewalJob`
**Cron:** `0 6 * * *` (daily 06:00 UTC)  
**Purpose:** Find `ProviderPlanSubscriptionEntity` and `ParticipantPlanSubscriptionEntity` records where `SubscriptionPeriodEnd` is within 48h and `AutoRenew = true`. Initiate renewal payment via gateway.  
**Input:** Active subscriptions with `AutoRenew=true` nearing expiry  
**Output:** Publishes `SubscriptionPaymentSucceededMessage` or `SubscriptionPaymentFailedMessage`  
**Failure handling:** Log and mark `PastDue` if gateway call fails; publish `SubscriptionPaymentFailedMessage`  
**MVP:** Yes — required for subscription revenue continuity

```csharp
public sealed class SubscriptionRenewalJob : AizenRecurringJob
{
    public override bool   IsActive       => true;
    public override string CronExpression => "0 6 * * *";
}
```

### F2. `SubscriptionInvoiceGenerationJob`
**Cron:** `30 6 * * *` (daily 06:30 UTC — runs after `SubscriptionRenewalJob`)  
**Purpose:** Find subscriptions where payment succeeded but invoice has not been issued. Create and issue `SubscriptionInvoice` records.  
**Idempotency:** Check `InvoiceHeaderEntity` existence for `UserSubscriptionId` before creation  
**MVP:** Post-MVP (manual invoice creation acceptable at MVP; this job automates it)

### F3. `InvoiceOverdueMarkingJob`
**Cron:** `0 1 * * *` (daily 01:00 UTC)  
**Purpose:** Find `InvoiceHeaderEntity` records where `Status = Issued` and `DueDateUtc < UtcNow`. Mark them `Overdue`.  
**Domain method required:** `invoice.MarkOverdue()` — sets `Status = Overdue`  
**MVP:** Yes — required for accurate invoice lifecycle

```csharp
public sealed class InvoiceOverdueMarkingJob : AizenRecurringJob
{
    public override bool   IsActive       => true;
    public override string CronExpression => "0 1 * * *";
}
```

### F4. `ProviderPayoutBatchJob`
**Cron:** `0 4 * * 1` (every Monday 04:00 UTC)  
**Purpose:** Find approved `PayoutRecordEntity` records in `PendingGateway` status. Batch-submit to Iyzico payout API. Update status to `InProgress` or `Failed`.  
**Note:** Iyzico supports marketplace sub-merchant payouts via scheduled transfer; this job triggers it.  
**MVP:** Post-MVP (manual payout sufficient at MVP via admin panel)

### F5. `PaymentWebhookRetryJob`
**Cron:** `*/15 * * * *` (every 15 minutes)  
**Purpose:** Find payment transactions stuck in `PendingWebhook` state older than 10 minutes. Re-query gateway for status. Update accordingly.  
**Failure handling:** After 5 retries, mark `Failed` and publish `PaymentFailedMessage`  
**MVP:** Yes — webhook delivery is unreliable; retry is production-critical

```csharp
public sealed class PaymentWebhookRetryJob : AizenRecurringJob
{
    public override bool   IsActive       => true;
    public override string CronExpression => "*/15 * * * *";
}
```

### F6. `PaymentReconciliationJob`
**Cron:** `0 5 * * *` (daily 05:00 UTC)  
**Purpose:** Query Iyzico reporting API for all transactions in the previous calendar day. Compare against local `PaymentTransactionEntity` records. Flag any discrepancies as `ReconciliationFailed`.  
**Output:** Writes reconciliation report to DB or file; alerts admin via Notification module  
**MVP:** Post-MVP (critical for production but acceptable to skip at MVP with manual reconciliation)

### F7. `PaymentAutoReleaseEligibilityJob`
**Cron:** `0 * * * *` (every hour)  
**Purpose:** Find `PaymentTransactionEntity` records in `EscrowHeld` status where linked `ServiceRequest` has been in `CompletionSubmitted` state longer than the configured auto-release window (e.g., 72h with no dispute). Automatically release escrow.  
**Action:** Publishes `ServiceRequestCompletedMessage` internally to trigger `ServiceRequestCompletedConsumer`  
**MVP:** Yes — prevents indefinite escrow holds if buyer goes silent

```csharp
public sealed class PaymentAutoReleaseEligibilityJob : AizenRecurringJob
{
    public override bool   IsActive       => true;
    public override string CronExpression => "0 * * * *";
}
```

### F8. `PaymentOutboxDispatcherJob`
**Cron:** `*/5 * * * *` (every 5 minutes)  
**Purpose:** If an Outbox pattern table is added in the future, this job dispatches unpublished outbox entries to RabbitMQ.  
**Current status:** NOT NEEDED YET — current architecture publishes inline during command/consumer handlers. This job is required only if an explicit outbox table is introduced.  
**MVP:** Post-MVP — implement when reliability of inline publish is insufficient in production

### F9. `MonthlyUsageCounterResetJob`
**Cron:** `0 0 1 * *` (1st of each month at 00:00 UTC)  
**Purpose:** Reset monthly usage counters on subscription plans (API call counts, service request limits, feature limits). Apply to `ProviderPlanSubscriptionEntity` and `ParticipantPlanSubscriptionEntity` if counters are tracked there.  
**MVP:** Post-MVP (only needed when subscription plan limits are actively enforced)

### F10. `InkCoinExpirationJob`
**Cron:** `0 2 * * *` (daily 02:00 UTC)  
**Purpose:** Mark expired InkCoin ledger entries as consumed or void. Prevent expired coins from being used.  
**MVP:** Post-MVP (InkCoin module not yet implemented)

---

## G. Idempotency / Outbox Strategy

### G1. Current Approach

Idempotency is currently implemented **per-consumer** using deterministic string keys checked against a processed-messages store or by detecting existing DB records.

Pattern used in `CargoDryKitRenewalPaymentConsumer`:
```csharp
// In ExecutePrepareMessage:
var idempotencyKey = $"RENEWAL-{message.KitCode}-{DateTime.UtcNow:yyyyMM}";
var exists = await _transactions.ExistsByIdempotencyKeyAsync(idempotencyKey, ct);
if (exists) return false; // Skip — already processed
```

This is the correct pattern. All new consumers must follow the same approach.

### G2. Recommended Idempotency Key Conventions

| Consumer | Key Format |
|---|---|
| `ServiceRequestPaymentCapturedConsumer` | `SR-PAY-CAPTURED-{TransactionId}` |
| `ServiceRequestPaymentReleasedConsumer` | `SR-PAY-RELEASED-{TransactionId}` |
| `ProviderSubscriptionPaymentSucceededConsumer` | `PROV-SUB-OK-{TransactionId}` |
| `ParticipantSubscriptionPaymentSucceededConsumer` | `PART-SUB-OK-{TransactionId}` |
| `PaymentRefundProcessedConsumer` | `REFUND-INVOICE-{TransactionId}` |

### G3. Outbox Decision

**Current stance: No explicit Outbox table.** Publishers call `IAizenMessagePublisher.PublishAsync` inline inside handlers and consumers. This is acceptable for MVP.

**Risk:** If the database commit succeeds but RabbitMQ publish fails, the event is lost. This is a known trade-off.

**Mitigation available without an outbox:**
- Jobs act as compensating mechanisms (e.g., `InvoiceOverdueMarkingJob` catches invoices that missed events)
- `PaymentWebhookRetryJob` handles webhook re-querying
- `PaymentAutoReleaseEligibilityJob` catches stuck escrow records

**Post-MVP decision point:** If `IAizenMessagePublisher` internally uses MassTransit Outbox (transactional outbox over the same EF Core DbContext), an explicit `PaymentOutboxDispatcherJob` is not needed — MassTransit handles dispatch internally. Verify whether `AizenApplicationBuilder` configures MassTransit with the `UsingEntityFrameworkOutbox` extension. If yes, the current approach is already outbox-safe.

---

## H. InvoiceNumberService Risk Review

### H1. Gap Risk — Assessment

`InvoiceNumberService.GenerateAsync()` commits the sequence increment in an **independent `SaveChangesAsync` call** separate from the outer invoice UoW:

```csharp
// InvoiceNumberService:
sequence.Increment();
await _sequences.SaveChangesAsync(ct);  // ← committed independently
// ...outer handler then calls its own SaveChanges via decorator
```

**Risk:** If the outer invoice `SaveChanges` fails after the sequence has been incremented, a gap is created (e.g., `INV-2026-06-000042` is assigned but no invoice with that number ever exists).

### H2. Is This Acceptable?

**Yes.** Turkish e-fatura regulations require:
- No duplicate invoice numbers ✅
- No reordering of numbers within a sequence ✅
- Sequential numbering without gaps — **not strictly required for commercial invoices** (gaps are auditable and explainable)

The alternative (same-transaction increment) would cause the sequence to roll back on outer TX failure, meaning a concurrent retry gets the same number — which IS forbidden.

**Conclusion: The current independent-commit approach is correct. Gaps are acceptable; duplicates are not.**

### H3. Retry Atomicity in `IssueInvoiceCommandHandler`

The `IssueInvoice` handler:
1. Calls `InvoiceNumberService.GenerateAsync` → commits sequence
2. Calls `invoice.Issue(number, userId)` → domain method
3. `Update(invoice)` → decorator calls `SaveChanges`

If step 3 fails (network blip, optimistic concurrency on invoice), the number is orphaned. The retry would generate a new number via `InvoiceNumberService` (creating another orphan if step 3 fails again).

**Mitigation already in place:**
- `IssueInvoice` is idempotent — if `invoice.Status == Issued`, returns current state without regenerating
- The orphaned number is a gap, not a duplicate

**Recommendation:** Log a warning inside `IssueInvoiceCommandHandler` if retry is detected after an orphaned number (store last-attempted number on invoice entity as nullable `string? PendingInvoiceNumber`). This is a post-MVP refinement.

### H4. Concurrency Under Load

`InvoiceNumberService` uses optimistic retry (5 attempts, exponential backoff starting at 50ms). Under high load (>5 simultaneous invoice issuances of the same type), the 5th attempt will throw `AizenBusinessException(5033)`.

**For MVP this is acceptable.** Invoice issuance is an admin operation, not a high-concurrency path.

**Post-MVP:** Replace with a PostgreSQL sequence (`CREATE SEQUENCE payment.inv_seq`) and `NEXTVAL('payment.inv_seq')` — atomic, no retry needed, no gaps.

---

## I. Recommended Next Implementation Phase

### Phase 2A — Core Event Wiring (MVP-critical)

**Priority 1 — Publish `InvoiceIssuedMessage` from `IssueInvoiceCommandHandler`**

Modify `IssueInvoiceCommandHandler.Handle()` to publish `InvoiceIssuedMessage` after the invoice transitions to Issued. This is the foundation for downstream notification.

**Priority 2 — `ServiceRequestPaymentReleasedConsumer`**

Auto-generate `CommissionInvoice` on SR completion. This is the highest-value automation at MVP — every completed service request should automatically produce a trackable commission invoice.

**Priority 3 — `InvoiceOverdueMarkingJob`**

Add `MarkOverdue()` domain method to `InvoiceHeaderEntity`, implement the job. Required for accurate invoice lifecycle.

**Priority 4 — `PaymentWebhookRetryJob`**

Production-critical reliability. Webhook delivery from Iyzico is not guaranteed. Without this job, any missed webhook leaves a transaction permanently stuck in `PendingWebhook`.

**Priority 5 — `PaymentAutoReleaseEligibilityJob`**

Prevents indefinite escrow holds. Required before go-live.

### Phase 2B — Subscription Lifecycle (Post-MVP)

- `SubscriptionPaymentSucceededMessage` + `SubscriptionPaymentFailedMessage`
- `ProviderSubscriptionPaymentSucceededConsumer` + `ParticipantSubscriptionPaymentSucceededConsumer`
- `SubscriptionRenewalJob`
- `SubscriptionInvoiceGenerationJob`

### Phase 2C — Financial Completeness (Scale Phase)

- `ProviderPayoutBatchJob`
- `PaymentReconciliationJob`
- `CommissionCalculatedMessage` + downstream reporting consumers
- `InkCoinLedgerEntryCreatedMessage` + `InkCoinExpirationJob`
- Explicit outbox table (if inline publish proves insufficient)

---

## J. Exact Files To Create / Change

### New Message Files (create)

| File | Priority |
|---|---|
| `Aizen.Modules.Payment.Abstraction/Message/InvoiceIssuedMessage.cs` | MVP |
| `Aizen.Modules.Payment.Abstraction/Message/SubscriptionPaymentSucceededMessage.cs` | Post-MVP |
| `Aizen.Modules.Payment.Abstraction/Message/SubscriptionPaymentFailedMessage.cs` | Post-MVP |
| `Aizen.Modules.Payment.Abstraction/Message/CommissionCalculatedMessage.cs` | Post-MVP |
| `Aizen.Modules.Payment.Abstraction/Message/ProviderPayoutCreatedMessage.cs` | Post-MVP |
| `Aizen.Modules.Payment.Abstraction/Message/CargoDryRenewalPaymentSucceededMessage.cs` | MVP |
| `Aizen.Modules.Payment.Abstraction/Message/InkCoinLedgerEntryCreatedMessage.cs` | Post-MVP |

### New Consumer Files (create)

| File | Priority |
|---|---|
| `Aizen.Modules.Payment/Consumers/ServiceRequestPaymentCapturedConsumer.cs` | MVP |
| `Aizen.Modules.Payment/Consumers/ServiceRequestPaymentReleasedConsumer.cs` | MVP |
| `Aizen.Modules.Payment/Consumers/ProviderSubscriptionPaymentSucceededConsumer.cs` | Post-MVP |
| `Aizen.Modules.Payment/Consumers/ParticipantSubscriptionPaymentSucceededConsumer.cs` | Post-MVP |
| `Aizen.Modules.Payment/Consumers/PaymentRefundProcessedConsumer.cs` | Post-MVP |

### New Job Files (create)

| File | Priority |
|---|---|
| `Aizen.Modules.Payment/Jobs/InvoiceOverdueMarkingJob.cs` | MVP |
| `Aizen.Modules.Payment/Jobs/PaymentWebhookRetryJob.cs` | MVP |
| `Aizen.Modules.Payment/Jobs/PaymentAutoReleaseEligibilityJob.cs` | MVP |
| `Aizen.Modules.Payment/Jobs/SubscriptionRenewalJob.cs` | Post-MVP |
| `Aizen.Modules.Payment/Jobs/SubscriptionInvoiceGenerationJob.cs` | Post-MVP |
| `Aizen.Modules.Payment/Jobs/ProviderPayoutBatchJob.cs` | Post-MVP |
| `Aizen.Modules.Payment/Jobs/PaymentReconciliationJob.cs` | Post-MVP |
| `Aizen.Modules.Payment/Jobs/MonthlyUsageCounterResetJob.cs` | Post-MVP |
| `Aizen.Modules.Payment/Jobs/InkCoinExpirationJob.cs` | Post-MVP |

### Files to Modify (existing)

| File | Change |
|---|---|
| `Aizen.Modules.Payment.Application/Commands/IssueInvoice/IssueInvoiceCommandHandler.cs` | Inject `IAizenMessagePublisher`; publish `InvoiceIssuedMessage` after `invoice.Issue()` |
| `Aizen.Modules.Payment/Consumers/CargoDryKitRenewalPaymentConsumer.cs` | Publish `CargoDryRenewalPaymentSucceededMessage` in `ExecuteCommitMessage` after SaveChanges |
| `Aizen.Modules.Payment.Domain/Entities/Invoice/InvoiceHeaderEntity.cs` | Add `MarkOverdue()` domain method |
| `Aizen.Modules.Payment.Domain/Interface/Repository/IInvoiceRepository.cs` | Add `GetIssuedOverdueAsync(DateTime utcNow, int batchSize, CancellationToken)` |
| `Aizen.Modules.Payment/DependencyInjection.cs` (host project) | Register new consumers with MassTransit |
| `Aizen.Modules.Payment.Abstraction/Enum/SubscriberType.cs` | Create if not exists: `Provider`, `Participant` enum |

---

## Summary

Phase 1B delivers a fully functional **manual invoice CRUD API**. The next critical layer is **automated invoice generation triggered by business events** — primarily SR completion (commission invoices) and invoice lifecycle management (overdue marking). The 3 MVP-critical jobs (`InvoiceOverdueMarkingJob`, `PaymentWebhookRetryJob`, `PaymentAutoReleaseEligibilityJob`) and 2 MVP-critical consumers (`ServiceRequestPaymentCapturedConsumer`, `ServiceRequestPaymentReleasedConsumer`) are the immediate implementation targets before any Admin Web or BFF invoice screen work begins.
