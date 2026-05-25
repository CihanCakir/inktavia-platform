# Aizen Identity PostgreSQL DateTime UTC Fix

## Purpose

A `DbUpdateException` occurs during:

```csharp
await unitOfWork.SaveChangesAsync();
```

inside:

```text
AizenCommandHandlerDecorator
```

The first observed place is:

```text
ChangePasswordCommandHandler
```

The innermost exception is:

```text
System.ArgumentException:
Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
Note that it's not possible to mix DateTimes with different Kinds in an array, range, or multirange. (Parameter 'value')
```

The Identity module uses:

```text
Aizen.Modules.Identity.Repository.Context
IdentityDbContext
```

The task is to inspect the Identity DbContext, entity mappings, repositories, base entity/audit logic, token creation/update logic, and command handler flow to identify which `DateTime` property is written with `DateTimeKind.Local` into a PostgreSQL `timestamp with time zone` column.

## Main Goal

Find the exact entity and property causing the error, then implement a clean UTC DateTime strategy without breaking the existing Aizen architecture.

## Important Direction

Do not apply only a local patch in `ChangePasswordCommandHandler`.

The root cause may be in:

- Base entity audit fields
- Repository `Update` method
- UnitOfWork save behavior
- Identity token entity date fields
- Token creation helper
- `DateTime.Now` usage
- `DateTime.Parse` producing Local/Unspecified DateTime
- EF Core / Npgsql model configuration
- IdentityDbContext mapping
- Multiple UnitOfWork save loop
- UserManager/Identity entity date fields

## Expected Result

All DateTime values written to PostgreSQL `timestamp with time zone` columns must be UTC.

Use:

```csharp
DateTime.UtcNow
```

or normalize values with:

```csharp
DateTime.SpecifyKind(value, DateTimeKind.Utc)
value.ToUniversalTime()
```

where appropriate.

Do not use `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` as the primary fix. It may be mentioned only as a temporary diagnostic/workaround, not the final solution.
