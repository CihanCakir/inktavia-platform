# 07 - UTC Strategy Design

Design the correct fix.

## Preferred Strategy

All DateTime values written to PostgreSQL `timestamp with time zone` columns should be UTC.

Use:

```csharp
DateTime.UtcNow
```

for new timestamps.

Normalize existing input DateTime values using:

```csharp
private static DateTime EnsureUtc(DateTime value)
{
    return value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
```

Use `DateTimeOffset` only if the project architecture supports it and migration impact is acceptable.

## Fix Locations

Choose the minimal but correct fix location based on root cause.

Possible locations:

### 1. Entity Factory / Domain Methods

Preferred when dates are set in entity methods.

Example:

```csharp
entity.RevokedAt = DateTime.UtcNow;
```

### 2. Repository Update / Audit Logic

Preferred if `ModifyDate` or `UpdateDate` is set globally.

Example:

```csharp
entity.ModifyDate = DateTime.UtcNow;
```

### 3. DbContext SaveChanges Interceptor / Override

Useful as a safety net.

Normalize DateTime values before SaveChanges only if it fits framework architecture.

### 4. EF Core Value Converter

Can normalize DateTime properties globally, but must be used carefully.

### 5. Column Type Change

If the system intentionally stores local/unspecified dates, map column as:

```text
timestamp without time zone
```

But this is usually not recommended for instant timestamps.

## Do Not

Do not use:

```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

as final fix.

Do not call `DateTime.Now` for persisted timestamps.

Do not mix UTC and Local values in arrays/ranges.

## Required Output

Produce:

```md
# UTC Strategy Design

## Selected Fix Location
- ...

## Why
- ...

## DateTime Normalization Rule
- ...

## Columns Kept As timestamp with time zone
- ...

## Code Changes Planned
- ...
```
