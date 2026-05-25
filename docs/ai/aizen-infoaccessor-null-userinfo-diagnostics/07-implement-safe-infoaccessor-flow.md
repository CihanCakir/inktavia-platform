# 07 - Implement Safe InfoAccessor Flow

Implement the root-cause fix.

## Target Area

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

## Required Implementation Checks

### 1. DI Lifetime

Ensure request-scoped state services are registered as scoped.

Examples:

```csharp
services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();
services.AddScoped<IAizenUserInfoAccessor, AizenUserInfoAccessor>();
```

Use actual interface/class names.

Do not register request-scoped user state as singleton.

### 2. Accessor Initialization

Ensure `UserInfoAccessor` is initialized.

If `AizenInfoAccessor` contains `UserInfoAccessor`, initialize it in constructor or inject it.

Example:

```csharp
public AizenInfoAccessor(IAizenUserInfoAccessor userInfoAccessor)
{
    UserInfoAccessor = userInfoAccessor;
}
```

### 3. UserInfo Assignment

Ensure middleware assigns parsed user info to the same scoped accessor consumed by handlers.

Example:

```csharp
_userInfoAccessor.SetUserInfo(userInfo);
```

or:

```csharp
_infoAccessor.UserInfoAccessor.SetUserInfo(userInfo);
```

Use actual architecture.

### 4. Missing Header

If `X-Aizen-User-Token` is missing:

- Do not throw NullReference.
- Clear/set empty user info.
- Continue or throw controlled error depending on existing options.

### 5. Invalid Token

If invalid:

- Controlled behavior.
- No raw token logging.

### 6. Middleware Timing

Ensure assignment happens before:

```csharp
await _next(context);
```

if downstream handlers need the value.

## Required Output

Produce:

```md
# Safe InfoAccessor Flow Implementation

## DI Lifetime Changes
- ...

## Initialization Changes
- ...

## Middleware Assignment Changes
- ...

## Missing Token Behavior
- ...

## Invalid Token Behavior
- ...

## Files Changed
- ...
```
