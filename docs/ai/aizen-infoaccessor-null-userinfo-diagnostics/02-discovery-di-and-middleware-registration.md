# 02 - Discovery: DI and Middleware Registration

Do not modify code in this step.

Search for:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
AizenUserInfoMiddleware
UseMiddleware<AizenUserInfoMiddleware>
IHttpContextAccessor
AddHttpContextAccessor
services.AddScoped
services.AddSingleton
services.AddTransient
IAizenInfoAccessor
IAizenUserInfoAccessor
UserInfoAccessor
```

## Required Questions

### DI Registration

- Where is InfoAccessor registered?
- What lifetime is used?
- Where is UserInfoAccessor registered?
- What lifetime is used?
- Is `IHttpContextAccessor` registered?
- Is there more than one registration for the same interface?
- Does any module override the registration?

### Middleware Registration

- Where is `AizenUserInfoMiddleware` registered in the pipeline?
- Is it registered through `UseAizenInfoAccessor` or manually?
- Does the operation starter call it?
- What is its order relative to:
  - `UseRouting`
  - `UseAuthentication`
  - `UseAuthorization`
  - `MapControllers`

### Scope Consistency

- Does middleware receive the same scoped accessor instance that command handlers receive?
- Is the accessor accidentally singleton while holding request-specific state?
- Is the accessor transient, causing middleware and handler to get different instances?

## Required Output

Produce:

```md
# DI and Middleware Registration Report

## InfoAccessor Registration
- ...

## UserInfoAccessor Registration
- ...

## IHttpContextAccessor Registration
- ...

## Duplicate Registrations
- ...

## Middleware Registration
- ...

## Middleware Order
- ...

## Scope Consistency Assessment
- ...

## Suspected DI Issue
- ...
```
