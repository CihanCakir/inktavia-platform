# Auth Scheme and AdminPanelAccess Policy Requirements

The BFF has protected endpoints but the local report shows:

```text
No authenticationScheme was specified, and there was no DefaultChallengeScheme found.
```

Fix this at BFF startup/pipeline level.

## Requirements

- AdminPanel BFF must authenticate incoming requests using `X-Aizen-User-Token`.
- Add a default authentication/challenge scheme compatible with existing Aizen Identity token validation.
- Add an `AdminPanelAccess` authorization policy.
- Do not use browser Keycloak as the default BFF auth scheme.

## Required policy behavior

`AdminPanelAccess` must verify:

```text
Identity token exists
Identity token is valid
User is active if this can be checked
The token/user/profile has AdminPanel/Admin context
Admin permission/profile exists
Device/session is valid if the Identity contract supports this
```

If Identity token claim names are unclear, inspect the Identity module token generation code.

Do not assume any valid Identity token may access AdminPanel BFF.
