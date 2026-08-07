# REPORT — messagebus generic-consumer opt-out for domain-authored entities

> Executes `HARDENING_MESSAGEBUS_GENERIC_CONSUMER_OPTOUT.md`. Framework-wide, financially-sensitive → diagnostic-first,
> then decision, then a surgical guarded change. **Additive / behaviour-preserving** for every non-generic-sync path.
> **NOT committed** (per instruction).

---

## Phase 0 — diagnostic (blast radius)

Grepped the whole solution for `AizenGenericMessage`, `AizenInsert/Update/DeleteEntityCommand`,
`PublishAsync(new AizenGenericMessage`, `AizenGenericConsumer`, and the generic REST scaffolding.

**Who publishes `AizenGenericMessage<T>`?**
- The **only** publisher is the generic scaffolding controller `AizenGenericBffApi<TEntity>`
  (`Core/Starter/src/Aizen.Core.Starter.Bff/Generic/AizenGenericBffApi.cs`) — its `AddEntity`/`UpdateEntity`/`DeleteEntity`
  actions publish `Create`/`Update`/`Delete` generic messages.
- **No domain / feature code anywhere publishes `AizenGenericMessage<T>`.** No named business flow depends on the generic
  consumer.

**How reachable is it, though?** The generic CRUD stack is **auto-wired for every entity**, not dormant scaffolding sitting
unused:
- `AizenBffServiceConfiguration.BffRestControllerFeatureProvider` closes `AizenGenericBffApi<TEntity>` over **every**
  `AizenEntity` subclass in every domain assembly and adds it as an MVC controller → `[AllowAnonymous]`
  `POST/PUT/DELETE /api/{EntityName}` exists on every BFF host.
- `BuilderExtensions.AddAizenMessagebus` (the hazard, lines ~71–94) registers an `AizenGenericConsumer<TEntity>` for **every**
  `AizenEntity` subclass in worker hosts.
- The consumer turns the message straight into `AizenInsert/Update/DeleteEntityCommand<TEntity>`; those handlers
  (`Core/CQRS/.../GenericHandler/Metropol*EntityCommandHandler.cs`) call `repository.AddAsync/Update/Delete(entity)`
  **directly — no validating factory, no invariants.**

**How many entities get a generic consumer today?** ~**162** `AizenEntity` subclasses across the domain assemblies (heuristic
count of `: AizenEntity` / `: AizenEntityWith*` declarations). That is the scope of the auto-registration.

**Confirmed hazard for the financial set:** `PaymentEconomicsSnapshotEntity` + its line snapshots are `sealed`, all
properties `{ get; private set; }`, built only via a validating factory with exact 8-equality and no public mutators. A
generic `Update`/**`Delete`** would rewrite or erase an immutable financial snapshot with none of those guards.

### Finding → (A) or (B)?

Neither purely (A) nor (B): **no named flow** relies on the generic consumer (leans A), **but** the generic CRUD surface is
**live, auto-exposed, reachable HTTP infrastructure** for all 162 entities (leans B — there are effectively "users": the
generic REST endpoints themselves, and any external caller/tooling using them).

---

## Decision — **OPT-OUT** (`[NoMessagebusSync]` blocklist)

Chosen because the generic path is **auto-wired for every entity and reachable**, not inert:

- **Opt-in would be a broad behaviour change.** Registering the generic consumer only for `[MessagebusSync]` entities would
  silently disable the entire generic CRUD surface for **all 162 entities** — every auto-exposed BFF `POST/PUT/DELETE` would
  hang (no consumer answers the request-response) or fail. That violates the mandate: *"existing users keep working…
  behaviour-preserving for everything that isn't a generic-sync path."*
- **Opt-out is surgical and reversible.** A `[NoMessagebusSync]` marker removes **only** the domain-authored immutable
  financial entities from the generic consumer registration; the generic scaffolding stays fully functional for every other
  entity. Minimal blast radius, precisely targeted at the hazard.

**Defense-in-depth (both policies would get this anyway):** the generic Insert/Update/Delete **command handlers** *and* the
`AizenGenericConsumer` additionally **refuse** a marked entity at runtime — fail loud, before any repository is touched — so
the factory/invariants can never be bypassed even via a mis-registration or a hand-crafted message.

---

## Implementation (surgical)

### 1. Marker + shared policy — in `Aizen.Core.Domain`

- `Core/Domain/src/Aizen.Core.Domain/NoMessagebusSyncAttribute.cs` —
  `[AttributeUsage(AttributeTargets.Class, Inherited = true)] sealed class NoMessagebusSyncAttribute : Attribute`.
- `Core/Domain/src/Aizen.Core.Domain/MessagebusSyncPolicy.cs` —
  `static bool IsGenericSyncBlocked(Type)` = `Attribute.IsDefined(type, typeof(NoMessagebusSyncAttribute), inherit: true)`.
  Single source of truth so the registration filter and the two guards can never disagree.

**Placement rationale (deviation from the literal plan, which said `Aizen.Core.Messagebus.Abstraction`):** the marker lives
in `Aizen.Core.Domain` — the assembly that already defines `AizenEntity` and that **every** domain-entity project already
references (verified: `Aizen.Modules.Payment.Domain.csproj` references `Aizen.Core.Domain`, and **no** module `*.Domain`
project references `Messagebus.Abstraction`). Putting it here:
  - needs **zero new project references** anywhere (placing it in Messagebus.Abstraction would force a new ref — and a
    transitive MassTransit dependency — into every domain project that marks an entity);
  - is visible to **both** enforcement sites: `Aizen.Core.Messagebus` (impl) and `Aizen.Core.CQRS` both already
    `ProjectReference` `Aizen.Core.Domain`.

This fully satisfies the plan's stated intent ("keep it so domain projects can reference it without depending on the
messagebus implementation") — `Aizen.Core.Domain` has no messagebus dependency at all. The plan explicitly permitted a
marker interface / alternative placement.

### 2. Registration filter — `Core/Messagebus/.../Extentions/BuilderExtensions.cs`

In the `AizenEntity` loop (the hazard, ~71–94), one added clause:

```csharp
.Where(type => type is { IsClass: true, IsAbstract: false } && typeof(AizenEntity).IsAssignableFrom(type))
.Where(type => !MessagebusSyncPolicy.IsGenericSyncBlocked(type))   // ← added: skip [NoMessagebusSync]
.ForEach(type => { /* register AizenGenericConsumer<type> */ });
```

The non-entity consumer scan (lines 56–69), publishing, request-client wiring, and the two-phase commit path are **untouched**.

### 3. Defense-in-depth guards

- `Core/CQRS/.../GenericHandler/GenericEntitySyncGuard.cs` (new) — `EnsureAllowed<TEntity>()` throws
  `InvalidOperationException` when the type is blocked. Called at the **top** of `Handle` in all three generic handlers
  (`MetropolInsert/Update/DeleteEntityCommandHandler.cs`) — **before** any `UnitOfWork`/repository access, so no row is
  written. This layer also covers a **direct CQRS dispatch** of a generic command, not just the bus path.
- `Core/Messagebus/.../Consumers/AizenGenericConsumer.cs` — the same check at the top of `ExecutePrepareMessage`, so a
  mis-registered consumer refuses the message loudly on arrival (rolls back, no commit).

---

## Full list of entities marked `[NoMessagebusSync]` (17)

All are `sealed`, `AizenEntityWithAudit` (→ `AizenEntity`), all properties `{ get; private set; }`, built only via a
validating factory / private ctor — i.e. exactly the "validating-factory + no public mutator" class the plan targets.

| # | Entity | File | Role |
|---|--------|------|------|
| 1 | `PaymentEconomicsSnapshotEntity` | Entities/Economics/PaymentEconomicsSnapshotEntity.cs | immutable economics snapshot (root) |
| 2 | `OfferLineEconomicsSnapshotEntity` | Entities/Economics/OfferLineEconomicsSnapshotEntity.cs | line snapshot child |
| 3 | `CommissionAllocationSnapshotEntity` | Entities/Economics/CommissionAllocationSnapshotEntity.cs | line snapshot child |
| 4 | `DiscountAllocationSnapshotEntity` | Entities/Economics/DiscountAllocationSnapshotEntity.cs | line snapshot child |
| 5 | `TravelPricingSnapshotEntity` | Entities/Economics/TravelPricingSnapshotEntity.cs | line snapshot child (S4) |
| 6 | `OfferLineAttributeSnapshotEntity` | Entities/Economics/OfferLineAttributeSnapshotEntity.cs | line snapshot child (S2) |
| 7 | `FinancialLedgerEntryEntity` | Entities/Reporting/FinancialLedgerEntryEntity.cs | immutable ledger entry |
| 8 | `ProfitProtectionEvaluationLogEntity` | Entities/ProfitProtection/ProfitProtectionEvaluationLogEntity.cs | immutable evaluation log |
| 9 | `LineProfitProtectionEvaluationLogEntity` | Entities/ProfitProtection/LineProfitProtectionEvaluationLogEntity.cs | immutable evaluation log |
| 10 | `RefundAllocationEntity` | Entities/RefundAllocation/RefundAllocationEntity.cs | P10 immutable refund breakdown |
| 11 | `ChargebackRecordEntity` | Entities/RefundAllocation/RefundAllocationEntity.cs | P10 chargeback record |
| 12 | `ProviderBalanceEntity` | Entities/RefundAllocation/ProviderBalanceEntity.cs | P10 provider balance (versioned) |
| 13 | `ProviderBalanceMovementEntity` | Entities/RefundAllocation/ProviderBalanceEntity.cs | P10 balance ledger movement |
| 14 | `PremiumPurchaseEntity` | Entities/Premium/PremiumPurchaseEntity.cs | P11 purchase snapshot |
| 15 | `PremiumEntitlementEntity` | Entities/Premium/PremiumEntitlementEntity.cs | P11 entitlement snapshot |
| 16 | `PartCommercialTermEntity` | Entities/PartCommercialTerm/PartCommercialTermEntity.cs | S5 cost-confidential term |
| 17 | `TransactionRefundRecord` | Entities/Transaction/TransactionRefundRecord.cs | immutable refund record |

**`OfferLineFxSnapshot` (S3):** not a standalone `AizenEntity` — it is an **owned value object** embedded in
`OfferLineEconomicsSnapshotEntity` (verified: no `class`/`record` of that name exists; only a migration column). It never
gets its own generic consumer and is protected transitively via its parent (#2). No marker needed.

Chargeback (#11) and provider-balance (#12/#13) records are the plan's "P10 refund-allocation / chargeback / provider-balance
records"; premium (#14/#15) are the "P11 premium purchase/entitlement snapshots".

---

## Tests & verification

New test file: `Modules/Payment/tests/Aizen.Modules.Payment.Domain.UnitTests/MessagebusHardening/GenericConsumerOptOutTests.cs`
(xunit + FluentAssertions). **24 tests, all green.**

1. **Policy flags the marked set** — `IsGenericSyncBlocked` is `true` for each of the 17 marked entities (theory,
   17 cases) and `false` for an ordinary unmarked entity (`CommissionRuleEntity`).
2. **Registration filter excludes the marked set (no `AizenGenericConsumer` registered)** — reproduces the **exact**
   `BuilderExtensions` predicate over the Payment domain assembly; asserts the marked financial entities are **absent** from
   the registrable set, and that `PaymentEconomicsSnapshotEntity` specifically is absent → **hazard closed**.
3. **Unmarked entity still registers/works** — `CommissionRuleEntity` remains in the registrable set (behaviour-preserving).
4. **Generic Create/Update/Delete refused for a marked entity (fail loud, no write)** — the three handlers are constructed
   with an **empty** `IEnumerable<IAizenUnitOfWork>` and invoked for `PaymentEconomicsSnapshotEntity`; each throws
   `InvalidOperationException` ("…NoMessagebusSync…"). Because the guard fires **before** `UnitOfWork.GetRepository` (which
   would otherwise `NullReference`), asserting `InvalidOperationException` proves the refusal short-circuits **before any
   persistence**.

Build / regression:
- **Full solution build (`dotnet build Aizen.sln`): 0 Errors** (pre-existing nullable warnings only).
- `Aizen.Core.Messagebus` and `Aizen.Core.CQRS` build clean with the filter + guards.
- **Full `Aizen.Modules.Payment.Domain.UnitTests`: 360/360 pass** — no regression (the `[NoMessagebusSync]` attributes are
  inert to existing domain behaviour).

**Remaining manual verify (needs infra — RabbitMQ/Postgres/Redis):** boot a worker host and confirm (a) the bus starts and
(b) no `AizenGenericConsumer<PaymentEconomicsSnapshotEntity>` endpoint is created. This is fully predicted by test #2, which
exercises the identical registration predicate the host uses at startup; the change is a pure LINQ filter that cannot affect
bus startup for any other entity.

---

## Surgical scope / don't-break confirmation

- Changed: the entity-consumer registration **filter** (1 `.Where`), the **marker** + **policy** (2 new files in
  `Aizen.Core.Domain`), the **guard** (1 new helper + 3 one-line calls in the generic handlers + 1 guard in the consumer),
  and the **17 marker attributes**.
- Unchanged: non-entity consumer scanning, publishing, request-client wiring, the two-phase commit path, `AizenGenericApi`
  (read-only), and every existing message flow. Any real generic-sync user (the auto-exposed generic CRUD surface) keeps
  working for all non-marked entities (opt-out).
- Note (out of scope, future cleanup): the BFF feature provider still *exposes* generic `POST/PUT/DELETE` endpoints for the
  marked entities. Under opt-out those endpoints can no longer produce a write — the request has **no consumer** (times
  out/fails) and, defensively, both the consumer and the handlers refuse it. Filtering the BFF feature provider by the same
  policy would be a tidy follow-up but was intentionally left untouched to keep this change surgical.

**NOT committed.**
