# HARDENING (plan-first) — messagebus generic-consumer opt-out for domain-authored entities

> **Repo:** `addesso-project` — framework `Core/Messagebus/src/Aizen.Core.Messagebus` (+ marker apply across Payment/other
> domain entities). A **framework-wide, financially-sensitive** change → **diagnostic-first, then decision, then a small
> guarded implementation.** Closes a **latent** hazard (currently dormant). Additive/behaviour-preserving for everything
> that isn't a generic-sync path. **Do not commit** until the user says.

## The hazard (confirmed in source)
`BuilderExtensions.cs` (lines ~71–94) enumerates **every `AizenEntity` subclass in every domain assembly** and registers an
`AizenGenericConsumer<TEntity>` for each — **no opt-out**. `AizenGenericConsumer<TEntity>` turns an inbound
`AizenGenericMessage<TEntity>` (Operation Create/Update/Delete + a deserialized `Entity`) directly into an
`AizenInsertEntityCommand` / `AizenUpdateEntityCommand` / `AizenDeleteEntityCommand<TEntity>` — a **generic persistence that
bypasses the domain factory + invariants**. For immutable, invariant-guarded financial entities (e.g.
`PaymentEconomicsSnapshotEntity` and its line snapshots — built only via a validating factory, no public mutators, exact
8-equality) this means: if anyone ever publishes `AizenGenericMessage<PaymentEconomicsSnapshotEntity>`, the bus would
insert/update/**delete** a financial snapshot with **none** of the guards. It's **dormant** (no code publishes these
today), but it's the same family of latent financial hazard as the two-phase-bus double-commit — worth closing before
go-live.

## Phase 0 — diagnostic (do this first; it decides opt-out vs opt-in)
Establish the blast radius before changing the framework:
1. **Who publishes `AizenGenericMessage<T>`?** Grep the whole solution for `AizenGenericMessage`, `AizenInsertEntityCommand`,
   `PublishAsync(new AizenGenericMessage`, and any use of the generic insert/update/delete commands. Determine whether **any
   live flow** relies on the generic consumer (entity sync across modules), or whether it is **entirely unused**.
2. **How many entities** get a generic consumer today (count `AizenEntity` subclasses across domain assemblies) — the scope
   of the auto-registration.
3. Record the finding: **(A) fully dormant** (nothing publishes) → the safest fix is broad (opt-in or disable-by-default);
   **(B) some real generic-sync users** → must be opt-out (blocklist the sensitive entities only, leave the users working).

## The decision (gated on Phase 0)
- **If (A) dormant:** prefer **opt-in** — the generic consumer registers **only** for entities explicitly marked
  `[MessagebusSync]` (or an allowlist), so *nothing* is generically writable unless a developer opts in. Lowest long-term
  risk; but confirm truly nothing breaks first.
- **If (B) has users:** implement **opt-out** — a `[NoMessagebusSync]` marker (attribute or `INoMessagebusSync` interface)
  that `BuilderExtensions` skips, and apply it to the sensitive set; the existing generic-sync users keep working.
- Either way, **defense-in-depth:** the generic insert/update/delete **command handlers** should additionally **refuse** an
  entity that is marked domain-authored/immutable (a second guard so a mis-registration or a hand-crafted message can't
  bypass the factory even if the consumer somehow runs). Prefer failing loud over silent no-op for a financial entity.

## Implementation (once the decision is made)
1. **Marker** in `Aizen.Core.Messagebus.Abstraction`: `NoMessagebusSyncAttribute` (opt-out) and/or `MessagebusSyncAttribute`
   (opt-in) — pick per Phase 0. A marker interface is also acceptable; keep it in the abstraction so domain projects can
   reference it without depending on the messagebus implementation.
2. **`BuilderExtensions.cs`:** in the `AizenEntity` registration loop (71–94), **skip** entities per the chosen policy
   (opt-out: skip if `[NoMessagebusSync]`; opt-in: register only if `[MessagebusSync]`). No other registration behaviour
   changes; non-entity consumer scanning (56–69) is untouched.
3. **Consumer/handler guard (defense-in-depth):** in `AizenGenericConsumer` (or the generic insert/update/delete command
   handlers), reject a marked entity with a clear exception — so the invariant can never be bypassed via the generic path.
4. **Apply the marker** to the domain-authored, immutable/invariant-guarded entities — at minimum the **financial** set:
   `PaymentEconomicsSnapshotEntity` + its immutable children (`OfferLineEconomicsSnapshot`, `CommissionAllocationSnapshot`,
   `DiscountAllocationSnapshot`, `TravelPricingSnapshot`, `OfferLineAttributeSnapshot`, `OfferLineFxSnapshot`),
   `FinancialLedgerEntryEntity`, `ProfitProtectionEvaluationLog`, `LineProfitProtectionEvaluationLog`, the P10
   refund-allocation / chargeback / provider-balance records, and the P11 premium purchase/entitlement snapshots. (Extend to
   any other entity with a validating factory + no public mutators.) List the full applied set in the report.

## Don't-break / QA
- Framework change is **surgical**: only the entity-consumer registration filter + a marker + a guard. Non-entity bus
  consumers, publishing, the two-phase commit path, and every existing message flow are **unchanged**. If Phase 0 shows
  real generic-sync users, they must still work (opt-out path).
- Tests: (1) a marked financial entity has **no** `AizenGenericConsumer` registered (assert via the DI/bus registration);
  (2) a generic Create/Update/Delete message targeting a marked entity is **refused** by the guard (fails loud, no write);
  (3) an unmarked entity (or an explicit opt-in one) still registers/works as before; (4) the solution builds + boots, bus
  starts, no regression in existing consumers. Prove the hazard is closed for `PaymentEconomicsSnapshotEntity` specifically.

## Verify
1. Boot a host: the bus starts; the sensitive financial entities are **not** generically consumable (registration absent);
   existing consumers + publishing still work.
2. Attempt (in a test) to drive a generic insert/update/delete for `PaymentEconomicsSnapshotEntity` → refused, no row
   written/mutated/deleted — the factory/invariants can no longer be bypassed.
3. Any Phase-0-identified real generic-sync flow still functions (opt-out) or is intentionally opt-in.

## Report
`docs/V1.0.1/Architecture/REPORT_HARDENING_MESSAGEBUS_OPTOUT.md`: the Phase-0 finding (who publishes generic messages / how
many entities), the opt-out-vs-opt-in decision + rationale, the marker + BuilderExtensions filter + the defense-in-depth
guard, the **full list of entities marked**, and the tests proving the financial-snapshot bypass is closed with no
regression. **Do NOT commit.**
