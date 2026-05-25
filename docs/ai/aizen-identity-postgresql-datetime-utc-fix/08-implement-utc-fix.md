# 08 - Implement UTC Fix

Implement the selected UTC strategy.

## Required Implementation Rules

### 1. Replace Local Date Sources

Replace persisted `DateTime.Now` with:

```csharp
DateTime.UtcNow
```

where the value maps to PostgreSQL `timestamp with time zone`.

### 2. Normalize Incoming DateTime Values

If values come from methods/DTOs/token helper:

```csharp
EnsureUtc(value)
```

before assigning to entity properties mapped to `timestamp with time zone`.

### 3. Fix Audit Fields

If base entity audit fields use local time, change them to UTC.

Examples:

```csharp
CreateDate = DateTime.UtcNow;
ModifyDate = DateTime.UtcNow;
```

### 4. Fix Token Entity Dates

For token expiration/revocation dates:

```csharp
AccessTokenExpiredDate = DateTime.UtcNow.AddMinutes(...)
RefreshTokenExpiredDate = DateTime.UtcNow.AddDays(...)
RevokedAt = DateTime.UtcNow
```

Use actual property names.

### 5. Optional Safety Net

If consistent with architecture, add DbContext-level normalization before save.

Example:

```csharp
foreach (var entry in ChangeTracker.Entries())
{
    foreach (var property in entry.Properties)
    {
        if (property.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
        {
            property.CurrentValue = dt.Kind == DateTimeKind.Local
                ? dt.ToUniversalTime()
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
    }
}
```

Use only for DateTime properties mapped to `timestamp with time zone` or if acceptable globally.

### 6. Remove Temporary Diagnostics

Do not leave noisy temporary traces.

Keep only useful debug logging or tests.

## Required Output

Produce:

```md
# UTC Fix Implementation Result

## Changed Files
- ...

## DateTime.Now Replacements
- ...

## Normalization Helpers
- ...

## Entity/Repository Fixes
- ...

## DbContext Fixes
- ...

## Notes
- ...
```
