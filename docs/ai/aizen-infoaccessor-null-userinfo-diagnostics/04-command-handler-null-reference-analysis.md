# 04 - Command Handler Null Reference Analysis

Do not modify code yet.

Inspect:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs
```

Error line:

```text
line 38
```

The failing access is likely:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

## Required Questions

Answer:

- What type is `_infoAccessor`?
- How is it injected?
- Is it the same interface registered by InfoAccessor package?
- Is `UserInfoAccessor` null?
- Is `UserInfo` null?
- Is `UserId` nullable or default?
- Is the handler executed during an HTTP request?
- Is the handler executed through MediatR/CQRS pipeline?
- Does the pipeline execute outside the HTTP request scope?
- Does any background job or bus handler use the same command?

## Required Safety Review

The handler should not use unsafe chains.

Instead of:

```csharp
var userId = _infoAccessor.UserInfoAccessor.UserInfo.UserId;
```

It should use a safe method or guard, for example:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor?.UserInfo;

if (userInfo is null || userInfo.UserId <= 0)
{
    throw new AizenBusinessException(...);
}
```

Use the project's existing exception/error pattern.

## Required Output

Produce:

```md
# ChangePasswordCommandHandler Analysis

## Handler Path
- ...

## Injected InfoAccessor Type
- ...

## Failing Line
- ...

## Null Object
- _infoAccessor / UserInfoAccessor / UserInfo / UserId

## Execution Scope
- HTTP request / Background / Unknown

## Required Handler Guard
- ...

## Related Error Code Needed
- Existing / New
```
