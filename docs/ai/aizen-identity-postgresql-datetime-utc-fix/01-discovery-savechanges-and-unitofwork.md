# 01 - Discovery: SaveChanges and UnitOfWork

Do not modify code in this step.

Inspect:

```text
Core/UnitOfWork
AizenCommandHandlerDecorator
IAizenUnitOfWork
AizenUnitOfWork
SaveChangesAsync
DbContext
```

## Required Questions

### Command Decorator

- How does `AizenCommandHandlerDecorator` iterate over unit of works?
- Does it call `SaveChangesAsync` for all DbContexts?
- Does the failing DbContext clearly correspond to IdentityDbContext?
- Are transactions opened for all unit of works?

### UnitOfWork

- What does `SaveChangesAsync` do?
- Does it update audit fields before saving?
- Does it call `DateTime.Now` anywhere?
- Does it call `DateTime.UtcNow` anywhere?
- Does it override cancellation token?

### DbContext SaveChanges

Search for overrides:

```text
SaveChanges
SaveChangesAsync
ChangeTracker
EntityState.Modified
EntityState.Added
CreateDate
ModifyDate
UpdatedAt
CreatedAt
```

Answer:

- Is there global audit date assignment?
- Does it use local dates?

## Required Diagnostic Addition

If necessary, add a temporary diagnostic method before save that inspects tracked entries and identifies Local DateTime values.

Example diagnostic logic:

```csharp
foreach (var entry in dbContext.ChangeTracker.Entries())
{
    foreach (var property in entry.Properties)
    {
        if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
        {
            // log entity type, property name, value, state
        }
    }
}
```

Do not log sensitive data.

## Required Output

Produce:

```md
# SaveChanges and UnitOfWork Report

## Decorator Behavior
- ...

## UnitOfWork SaveChanges Behavior
- ...

## Audit Date Assignment
- ...

## DateTime.Now Usages
- ...

## Suspected Save Source
- ...
```
