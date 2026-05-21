# 08 - Backward Compatibility and Consumers

Check existing consumers of InfoAccessor.

## Goal

Do not break current code that depends on user info accessor interfaces.

## Search Consumers

Search usages of:

```text
IAizenUserInfoAccessor
IAizenInfoAccessor
AizenUserInfo
CurrentUser
UserId
GetUserId
MetropolUserId
PhoneNumber
Version
RefreshTokenExpire
```

## Required Compatibility Decisions

If current consumers expect user info to always exist, choose one safe pattern:

### Option A - Existing behavior preserved for user endpoints

Protected user-level endpoints must provide `X-Aizen-User-Token`. If missing, accessor returns empty and consumer-level business rules handle it.

### Option B - Explicit user context required

For endpoints that require a user token, add a reusable guard/policy/filter later. Do not implement this broadly unless requested.

### Option C - Accessor exposes availability

Add properties/methods such as:

```csharp
bool IsAuthenticatedUserAvailable { get; }
bool IsClientContextAvailable { get; }
```

Use existing style.

## Important

Do not make Keycloak client token impersonate an application user.

Do not fill `UserId` from `client_id`, `azp`, or `preferred_username` unless the system explicitly defines that mapping.

## Required Output

Produce:

```md
# Backward Compatibility Result

## Consumers Found
- ...

## Potential Breaking Points
- ...

## Compatibility Strategy
- ...

## Additional Work Needed
- ...
```
