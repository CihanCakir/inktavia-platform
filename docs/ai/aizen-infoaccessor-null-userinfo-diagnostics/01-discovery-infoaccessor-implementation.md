# 01 - Discovery: InfoAccessor Implementation

Do not modify code in this step.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search for:

```text
InfoAccessor
IInfoAccessor
IAizenInfoAccessor
AizenInfoAccessor
UserInfoAccessor
IUserInfoAccessor
IAizenUserInfoAccessor
AizenUserInfoAccessor
AizenUserInfo
CurrentUser
UserInfo
SetUserInfo
GetUserInfo
```

## Required Questions

Answer:

### Interfaces

- What is the main InfoAccessor interface?
- What is the type of `_infoAccessor` used in command handlers?
- Does it expose `UserInfoAccessor`?
- What is the type of `UserInfoAccessor`?
- Does `UserInfoAccessor` expose `UserInfo` as nullable or non-nullable?

### Implementations

- Where is the InfoAccessor concrete implementation?
- Where is UserInfoAccessor concrete implementation?
- Does the implementation initialize `UserInfoAccessor`?
- Does the implementation initialize `UserInfo`?
- Are there setters or methods for setting user info?

### Models

- Where is `AizenUserInfo`?
- What properties and flags does it have?
- Does it have an empty/default instance factory?
- Does it have required constructor parameters?

## Required Output

Produce:

```md
# InfoAccessor Implementation Report

## Main Interface
- ...

## Concrete Implementation
- ...

## UserInfoAccessor Interface
- ...

## UserInfoAccessor Implementation
- ...

## UserInfo Initialization Behavior
- ...

## AizenUserInfo Model
- ...

## Null Risk Points
- ...
```
