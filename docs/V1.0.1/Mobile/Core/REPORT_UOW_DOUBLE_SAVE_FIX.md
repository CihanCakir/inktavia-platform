# REPORT — UoW double-save fix (platform-wide, standalone)

**Scope:** shared Core only. Isolated from the vessel FE/BFF/M4b diff so it can be reviewed and signed off on its own.
**File changed:** `Core/UnitOfWork/src/Aizen.Core.UnitOfWork/Extention/BuilderExtensions.cs` (one registration).
**Class of bug:** command handlers persisted **twice per request** (duplicate rows / doubled side-effects) — the "double-commit" class.

---

## Root cause

`AddAizenUnitOfWork<TContext>` registered the **non-generic** `IAizenUnitOfWork` twice:

```csharp
services.AddScoped<IAizenUnitOfWork, AizenUnitOfWork<TContext>>();          // (1) explicit
services.AddScoped<IAizenUnitOfWork<TContext>, AizenUnitOfWork<TContext>>();
services.Scan(scanner =>
{
    scanner.AddTypes(typeof(AizenUnitOfWork<>))
        .AsImplementedInterfaces()   // (2) re-registers IAizenUnitOfWork (and the generic) AGAIN
        .WithScopedLifetime();
});
```

`AizenUnitOfWork<TContext>` implements exactly two interfaces — `IAizenUnitOfWork<TContext>` and `IAizenUnitOfWork` — both already registered explicitly at (1). The `Scan(...).AsImplementedInterfaces()` at (2) registered them a **second** time.

The command pipeline commits via `AizenCommandHandlerDecorator`, which injects **`IEnumerable<IAizenUnitOfWork>`** and loops:

```csharp
var result = await _decorated.Handle(command, ct);   // handler stages the new entity (AddAsync)
foreach (var unitOfWork in _unitOfWorks)             // <-- TWO entries (both the same DbContext)
    await unitOfWork.SaveChangesAsync();
```

With two `IAizenUnitOfWork` registrations, `_unitOfWorks` had **two** entries over the **same** scoped `DbContext`, so `SaveChangesAsync()` fired **twice**. The second save re-persisted the still-tracked, newly-added entity → a duplicate INSERT (and doubled downstream side-effects).

### Why it went unnoticed until now
The second insert only throws where a **unique index** guards a value the handler sets at insert time. The first participant write to do that was **vessel create** (`vessel.vessels` has a unique `Slug` derived from the name) → `23505 duplicate key value violates unique constraint "IX_vessels_Slug"` *after* the row had already committed. Elsewhere the second save had been silently creating duplicate rows / firing side-effects twice (the same class as the two-phase-bus double-commit epic). All 10 modules route through this one Core method, so the defect was platform-wide.

---

## The fix (one registration; UoW semantics unchanged)

Exclude the non-generic `IAizenUnitOfWork` from the scan (it stays registered exactly once, via the explicit `AddScoped`). The scan block is preserved; no manual `SaveChanges` added; the UoW remains the **sole** save, firing **once** per request.

```csharp
scanner.AddTypes(typeof(AizenUnitOfWork<>))
    .AsImplementedInterfaces(t => t != typeof(IAizenUnitOfWork))   // <-- exclude the duplicate
    .WithScopedLifetime();
```

Result: `IEnumerable<IAizenUnitOfWork>` resolves to **one** entry → the decorator's `foreach … SaveChangesAsync()` runs **once**. `IAizenUnitOfWork<TContext>` (injected by query handlers) is untouched — still registered (explicitly + scanned), single instance per scope.

Scrutor 4.2.2 `AsImplementedInterfaces(Func<Type,bool>)` overload; Core.UnitOfWork compiles clean.

---

## Regression evidence (cross-module — writes persist EXACTLY once)

All 12 API/BFF images rebuilt with the Core fix and recreated. Each write below persisted **exactly once** (no duplicate rows / doubled side-effects).

| Module | Write driven | Signal | Result |
|---|---|---|---|
| **Vessel** | mobile `POST /vessels` (create → spec → engine) | rows for the new vessel | `vessels=1, owners=1, specifications=1, engines=1` |
| **Vessel** | same create | `INSERT` statements in vessel-api SQL log | `vessels:1  owners:1  spec:1  engine:1` (one per table) |
| **File-storage** | mobile `POST /profile/avatar` | `file_storage.files` count | `5 → 6` (Δ = **1**) |
| **Identity** | mobile `POST /auth/register` | `public.UserProfiles` count | `42 → 43` (Δ = **1**) |

The vessel case is the strongest single proof: one command inserts **four** distinct entity types, and each landed exactly once — before the fix, the second save re-inserted the vessel and threw `23505` on `IX_vessels_Slug` *after* the first insert had already committed. Post-fix, the vessel-api SQL log shows a single `INSERT` per table per request.

**Before → after**, same create request:
- Before: `INSERT vessels` ×2 (2nd → duplicate-slug 500, row already committed) — double save.
- After: `INSERT vessels` ×1, `INSERT vessel_owners` ×1, `INSERT vessel_specifications` ×1, `INSERT vessel_engines` ×1 — single save.

---

## Confirmation

- The `AizenUnitOfWork` single-save architecture is **preserved**: exactly one `SaveChangesAsync` per request; no bypass, no manual save, no semantic change.
- The only change is de-duplicating a DI registration so the command decorator no longer enumerates the same UoW twice.
