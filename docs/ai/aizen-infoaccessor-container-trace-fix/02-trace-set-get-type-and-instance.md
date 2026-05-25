# 02 - Trace Set/Get Type and Instance

Add temporary diagnostic logs or debugger-only traces if needed. Remove or convert to safe debug logs after fixing.

## Goal

Prove whether middleware and handler use the same:

- container instance
- DI scope
- `AizenUserInfo` type
- accessor path

## Trace in AizenUserInfoMiddleware

Immediately before and after `container.Set(userInfo)`, inspect/log safely:

```csharp
container.GetType().FullName
container.GetHashCode()
userInfo.GetType().FullName
userInfo.GetType().Assembly.FullName
userInfo.UserId
container.Get<AizenUserInfo>() immediately after Set
HttpContext.TraceIdentifier
```

Do not log raw tokens.

## Trace in UserInfoAccessor.UserInfo getter

Inspect/log safely:

```csharp
container.GetType().FullName
container.GetHashCode()
typeof(AizenUserInfo).FullName
typeof(AizenUserInfo).Assembly.FullName
container.Get<AizenUserInfo>() result
HttpContext.TraceIdentifier if accessible
```

## Trace in ChangePasswordCommandHandler

Inspect/log safely:

```csharp
_infoAccessor.GetType().FullName
_infoAccessor.UserInfoAccessor?.GetType().FullName
_infoAccessor.UserInfoAccessor?.UserInfo is null
HttpContext.TraceIdentifier if IHttpContextAccessor is available
```

## Interpretation

- If immediate Get after Set fails: container Set/Get or type mismatch.
- If immediate Get succeeds but accessor fails: different container instance/source/type.
- If accessor works in middleware but not handler: DI scope or CQRS child scope.
- If type assembly differs: duplicate `AizenUserInfo` type.

## Output Required

```md
# Trace Report

## Middleware Container
- Type:
- Hash:
- Immediate Get:

## Accessor Container
- Type:
- Hash:
- Get:

## Handler Accessor
- Type:
- UserInfo:

## Type Identity
- Middleware type:
- Accessor type:

## Conclusion
- ...
```
