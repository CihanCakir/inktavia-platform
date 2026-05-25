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
