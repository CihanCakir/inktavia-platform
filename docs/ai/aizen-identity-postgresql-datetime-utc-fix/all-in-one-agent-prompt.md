# All-in-One Agent Prompt - Aizen Identity PostgreSQL DateTime UTC Fix

Execute this task as a coding agent.

There is a `Microsoft.EntityFrameworkCore.DbUpdateException` during:

```csharp
await unitOfWork.SaveChangesAsync();
```

inside `AizenCommandHandlerDecorator`.

The first observed failing flow is `ChangePasswordCommandHandler`.

The innermost exception is:

```text
Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
```

Identity persistence uses:

```text
Aizen.Modules.Identity.Repository.Context.IdentityDbContext
```

Your task is to inspect the full Identity persistence flow and fix the actual Local DateTime source.

Do not use `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` as the final solution.

Find the exact entity/property, then implement a UTC-safe fix.

Follow every section in order.



---

# 00-context-and-error.md

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


---

# 01-discovery-savechanges-and-unitofwork.md

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


---

# 02-discovery-identity-dbcontext-and-mappings.md

# 02 - Discovery: IdentityDbContext and PostgreSQL Mappings

Do not modify code in this step.

Inspect:

```text
Aizen.Modules.Identity.Repository.Context
IdentityDbContext
Aizen.Modules.Identity.Repository
EntityTypeConfiguration
IEntityTypeConfiguration
OnModelCreating
HasColumnType
timestamp with time zone
timestamp without time zone
```

## Required Questions

- Where is `IdentityDbContext` defined?
- Which entities does it include?
- Which entity configurations are applied?
- Which DateTime properties are mapped to `timestamp with time zone`?
- Which DateTime properties are mapped to `timestamp without time zone`?
- Are there global conventions for DateTime?
- Are base entity fields configured globally?
- Are token date fields configured explicitly?

## Important Entities To Inspect

Search for:

```text
UserLoginTokenEntity
UserEntity
RoleEntity
UserDeviceEntity
UserProfileEntity
Agreement
RefreshToken
AccessToken
CreateDate
ModifyDate
CreatedAt
UpdatedAt
Expire
Expired
ExpiresAt
RefreshTokenExpire
```

## Required Output

Produce:

```md
# IdentityDbContext Mapping Report

## IdentityDbContext Path
- ...

## Registered DbSets
- ...

## DateTime Columns
| Entity | Property | Column Type | Nullable |
|---|---|---|---|

## timestamp with time zone Columns
- ...

## Mapping Risks
- ...
```


---

# 03-discovery-entities-and-datetime-fields.md

# 03 - Discovery: Entities and DateTime Fields

Do not modify code in this step.

Search all Identity domain/repository entities for DateTime properties.

Search patterns:

```text
DateTime
DateTime?
DateTimeOffset
DateTime.Now
DateTime.UtcNow
DateTime.Parse
Convert.ToDateTime
ToLocalTime
ToUniversalTime
```

## Required Entities

Inspect at least:

```text
UserLoginTokenEntity
UserEntity
UserDeviceEntity
UserProfileEntity
BaseEntity
AuditableEntity
Entity
```

Use actual project names.

## Required Questions

For each DateTime property:

- What is the property name?
- Is it set in constructor/factory?
- Is it set in repository update?
- Is it set by EF save changes?
- Is it mapped to PostgreSQL `timestamp with time zone`?
- Does it use `DateTime.Now`?
- Does it use `DateTime.UtcNow`?
- Does it parse a string into DateTime?
- Does it receive DateTime from token helper?

## Required Output

Produce:

```md
# Entity DateTime Field Report

| Entity | Property | Set Location | Current Source | Kind Risk |
|---|---|---|---|---|

## DateTime.Now Usages
- ...

## DateTime.Parse / Convert Usages
- ...

## High-Risk Fields
- ...
```


---

# 04-discovery-change-password-flow.md

# 04 - Discovery: ChangePassword Flow

Do not modify code in this step.

Inspect:

```text
ChangePasswordCommandHandler
ChangePasswordCommand
UserManager
PasswordHasher
UserLoginTokenEntity
TokenRepository
_tokenRepository
userManager.ChangePasswordAsync
```

## Current Relevant Code

The flow likely contains logic similar to:

```csharp
var tokens = await _tokenRepository.GetAllAsync(x => x.UserId == user.Id && x.IsRevoked == false);

if (tokens.Any())
{
    foreach (var entity in await tokens.ToListAsync(cancellationToken))
    {
        entity.IsRevoked = true;
        _tokenRepository.Update(entity);
    }
}
```

and then `SaveChangesAsync` is called by `AizenCommandHandlerDecorator`.

## Required Questions

- Which entities are modified in this handler?
- Does it update `UserEntity`?
- Does it update `UserLoginTokenEntity`?
- Does `_tokenRepository.Update(entity)` set audit fields?
- Does revoking token set `ModifyDate`, `UpdatedAt`, `RevokedAt`, `ExpireDate`, or similar?
- Does `ChangePasswordAsync` update Identity user fields with DateTime?
- Does the handler set any date manually?

## Required Output

Produce:

```md
# ChangePassword Flow Report

## Handler Path
- ...

## Modified Entities
- ...

## Repository Update Behavior
- ...

## DateTime Values Introduced
- ...

## Likely Failing Entity
- ...
```


---

# 05-discovery-token-and-audit-date-generation.md

# 05 - Discovery: Token and Audit Date Generation

Do not modify code in this step.

Inspect token generation and audit logic.

Search for:

```text
CreateLoginToken
CreateLoginTokenAsync
_tokenHelper
CreateAccessToken
CreateRefreshToken
UserLoginTokenEntity.Create
RefreshTokenExpiredDate
AccessTokenExpiredDate
DateTime.Now
DateTime.UtcNow
DateTime.Parse
ModifyDate
CreateDate
RevokedAt
```

## Token Entity

Inspect `UserLoginTokenEntity`.

Answer:

- What date fields does it contain?
- Which fields are saved to DB?
- Which fields are nullable?
- Which fields are set on create?
- Which fields are set on revoke?
- Are expiration dates UTC?

## Token Helper

Answer:

- Does `_tokenHelper` use `DateTime.Now` or `UtcNow`?
- Does it create expiration dates with local kind?
- Does it format dates as string then parse them back?
- Does the JWT claim `RefreshTokenExpire` use local date string?
- Does that claim value get stored back into entity DateTime?

## Audit Logic

Answer:

- Where are audit fields set?
- Is `DateTime.Now` used for `CreateDate` or `ModifyDate`?
- Are base entity date properties mapped as `timestamp with time zone`?

## Required Output

Produce:

```md
# Token and Audit Date Generation Report

## Token Date Sources
- ...

## Audit Date Sources
- ...

## DateTime.Now Sources
- ...

## DateTime.UtcNow Sources
- ...

## Fields That Must Be Normalized
- ...
```


---

# 06-root-cause-isolation.md

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


---

# 07-utc-strategy-design.md

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


---

# 08-implement-utc-fix.md

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


---

# 09-tests-and-verification.md

# 09 - Tests and Verification

Add or update tests if possible.

## Required Scenarios

### 1. Change Password Save

Run the `ChangePasswordCommandHandler` flow.

Expected:

- No Npgsql DateTime Kind error.
- Updated user/token entities save successfully.

### 2. Token Revocation

When login tokens are revoked:

Expected:

- Any date fields are UTC.
- SaveChanges succeeds.

### 3. Audit Fields

When any Identity entity is added/updated:

Expected:

- `CreateDate`, `ModifyDate`, `CreatedAt`, `UpdatedAt` or equivalent fields are UTC.

### 4. ChangeTracker Guard Test

If a test is possible, assert no tracked modified/added entity contains `DateTimeKind.Local` for persisted timestamp fields before save.

### 5. Build

Run:

```bash
dotnet build
```

### 6. Tests

Run:

```bash
dotnet test
```

## Manual Verification

Retry the API request that triggers ChangePassword.

Headers:

```http
Authorization: Bearer {{keycloakAccessToken}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
Content-Type: application/json
```

Body:

```json
{
  "oldPassword": "123456",
  "newPassword": "NewPassword123!",
  "newPasswordComfirm": "NewPassword123!"
}
```

## Required Output

Produce:

```md
# Verification Report

## Build
- ...

## Tests
- ...

## ChangePassword Result
- ...

## Failing Entity Rechecked
- ...

## UTC Confirmation
- ...

## Remaining Risks
- ...
```


---

# 10-final-report.md

# 10 - Final Report

Produce final report in this exact format:

```md
# Final Report - Identity PostgreSQL DateTime UTC Fix

## 1. Problem Summary

- Exception:
- First observed handler:
- Failing save layer:

## 2. Root Cause

- DbContext:
- Entity:
- Property:
- Assigned from:
- DateTime.Kind:

## 3. IdentityDbContext Mapping

- Relevant column:
- PostgreSQL type:
- Why UTC is required:

## 4. Changed Files

- `path/to/file.cs`
  - ...

## 5. UTC Strategy Applied

- ...

## 6. DateTime Sources Fixed

| Location | Before | After |
|---|---|---|

## 7. ChangePassword Flow Impact

- ...

## 8. UnitOfWork / SaveChanges Impact

- ...

## 9. Verification

### Build
- ...

### Tests
- ...

### Manual API Result
- ...

## 10. What Not To Do

- Do not use `Npgsql.EnableLegacyTimestampBehavior` as final solution.
- Do not persist `DateTime.Now` into `timestamp with time zone`.
- Do not ignore inner exception details.

## 11. Follow-up Recommendations

- ...
```
