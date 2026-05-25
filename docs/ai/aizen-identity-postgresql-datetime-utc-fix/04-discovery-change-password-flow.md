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
