# 06 - Root Cause Isolation

Now isolate the exact failing entity/property.

## Required Technique

Before `SaveChangesAsync`, inspect the ChangeTracker of the failing DbContext.

If useful, add temporary diagnostic code in UnitOfWork or IdentityDbContext.

Example:

```csharp
private static void ThrowIfLocalDateTimeExists(DbContext dbContext)
{
    var localDateTimes = dbContext.ChangeTracker
        .Entries()
        .SelectMany(entry => entry.Properties.Select(property => new
        {
            Entity = entry.Entity.GetType().Name,
            State = entry.State,
            Property = property.Metadata.Name,
            Value = property.CurrentValue
        }))
        .Where(x => x.Value is DateTime dt && dt.Kind == DateTimeKind.Local)
        .ToList();

    if (localDateTimes.Any())
    {
        throw new InvalidOperationException(
            string.Join(Environment.NewLine,
                localDateTimes.Select(x =>
                    $"{x.Entity}.{x.Property} contains Local DateTime")));
    }
}
```

Use logging if preferred. Do not keep temporary exception code in final unless converted into a safe guard or unit test.

## Required Outcome

Identify:

- DbContext
- Entity
- Property
- Value
- DateTime.Kind
- Where that value was assigned

## Required Output

Produce:

```md
# Root Cause Isolation Report

## Failing DbContext
- ...

## Failing Entity
- ...

## Failing Property
- ...

## DateTime Kind
- ...

## Assigned From
- ...

## Exact Root Cause
- ...
```
