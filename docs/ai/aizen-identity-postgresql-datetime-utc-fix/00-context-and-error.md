# 00 - Context and Error

## Current Error

During command execution, the following line fails:

```csharp
await unitOfWork.SaveChangesAsync();
```

Location:

```text
AizenCommandHandlerDecorator
```

First observed command:

```text
ChangePasswordCommandHandler
```

Exception:

```text
Microsoft.EntityFrameworkCore.DbUpdateException:
An error occurred while saving the entity changes. See the inner exception for details.
```

Innermost exception:

```text
System.ArgumentException:
Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
Note that it's not possible to mix DateTimes with different Kinds in an array, range, or multirange. (Parameter 'value')
```

## Meaning

Npgsql refuses to write a `DateTime` whose `Kind` is `Local` into a PostgreSQL `timestamp with time zone` column.

PostgreSQL `timestamp with time zone` must receive UTC `DateTime` values when using Npgsql's modern timestamp behavior.

## Main Goal

Find exactly which tracked entity and which property contains a `DateTimeKind.Local` value during `SaveChangesAsync`.

Then fix the source of that value.

## Do Not

Do not use this as the final solution:

```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

Do not only patch the currently failing command handler.

The issue must be fixed at the source:

- entity factory
- repository update
- audit field setter
- token helper
- DbContext conversion
- mapping
- unit of work
- or identity persistence logic

## Required Output Before Changes

Produce:

```md
# Initial Error Understanding

## Failing Layer
- ...

## Failing Command
- ...

## DbContext Involved
- ...

## PostgreSQL Column Type Concern
- ...

## Initial Hypotheses
- ...
```
