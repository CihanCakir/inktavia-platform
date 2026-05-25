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
