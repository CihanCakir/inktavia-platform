# 01 - Discovery: Container and Accessor Architecture

Do not modify code yet.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
Core/InfoAccessor/src/Aizen.Core.InfoAccessor.Abstraction
```

Search for:

```text
IAizenInfoContainer
AizenInfoContainer
AizenInfoContainerForScoped
AizenInfoContainerForSigleton
AizenInfoAccessor
IAizenInfoAccessor
IAizenUserInfoAccessor
AizenUserInfoAccessor
AizenUserInfo
Set(
Get<
```

## Questions

### Container

- How does `IAizenInfoContainer.Set<T>` work?
- How does `IAizenInfoContainer.Get<T>` work?
- Is `typeof(T)` used as key?
- Is storage instance-level, static, `AsyncLocal`, `HttpContext.Items`, or dictionary?
- What is the difference between scoped and singleton container implementations?

### Accessors

- What is the concrete `IAizenInfoAccessor`?
- What is the concrete `IAizenUserInfoAccessor`?
- Does `UserInfoAccessor.UserInfo` read `container.Get<AizenUserInfo>()`?
- Is `UserInfoAccessor` injected into `AizenInfoAccessor` or manually constructed?
- Is `UserInfo` initialized to a null-object or left nullable?

### Type Identity

Search all definitions:

```bash
find . -name "*.cs" -exec grep -n "class AizenUserInfo\|record AizenUserInfo" {} \;
```

Check whether middleware sets one `AizenUserInfo` type while accessor gets another namespace/assembly type.

## Output Required

```md
# Container and Accessor Architecture Report

## Container Implementations
- ...

## Set/Get Mechanism
- ...

## InfoAccessor Implementation
- ...

## UserInfoAccessor Implementation
- ...

## AizenUserInfo Type Identity
- ...

## Mismatch Risks
- ...
```
