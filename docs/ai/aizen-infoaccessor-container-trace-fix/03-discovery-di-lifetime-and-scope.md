# 03 - Discovery: DI Lifetime and Scope

Do not modify code until the registration map is clear.

Search for all registrations of:

```text
IAizenInfoContainer
AizenInfoContainer
AizenInfoContainerForScoped
AizenInfoContainerForSigleton
IAizenInfoAccessor
IAizenUserInfoAccessor
AddInfoAccessor
AddAizenInfoAccessor
AddScoped
AddTransient
AddSingleton
```

## Questions

- What lifetime is used for `IAizenInfoContainer`?
- What lifetime is used for `IAizenInfoAccessor`?
- What lifetime is used for `IAizenUserInfoAccessor`?
- Are there duplicate registrations?
- Which registration wins?
- Are API, Operation, BFF starters registering different implementations?
- Does any module override these registrations?

## Correct Direction

Request-specific user info should be request-bound. Usually this means:

```csharp
services.AddScoped<IAizenInfoContainer, AizenInfoContainerForScoped>();
services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();
services.AddScoped<IAizenUserInfoAccessor, AizenUserInfoAccessor>();
```

But follow existing project conventions.

## Output Required

```md
# DI Lifetime Report

## Registrations Found
- ...

## Duplicate Registrations
- ...

## API/Operation/BFF Differences
- ...

## Lifetime Issue
- ...
```
