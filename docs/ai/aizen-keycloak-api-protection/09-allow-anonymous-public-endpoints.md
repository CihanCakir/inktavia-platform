# 09 - Allow Anonymous Public Endpoints

This step prevents intentionally public endpoints from being blocked by the new default protection.

## Goal

Add `[AllowAnonymous]` only to endpoints that must be accessible without a token.

## Search Targets

Search all `*Controller.cs` files and identify public endpoint categories.

Possible public endpoints include:

- Login
- Username login
- Phone login
- OTP send
- OTP verify
- Register
- External login start
- External login callback
- Token acquisition
- Refresh token if the existing design expects it to be called without access token
- Public agreement
- Public version
- Public app config
- Health-check controller if implemented as controller

## Important Security Rule

Do not mark an endpoint public just because it currently fails.

If it reads authenticated user context, user profile, active profile, role, device, payment, participant, organizer, venue owner or admin data, it must remain protected.

## Required Attribute

Use:

```csharp
using Microsoft.AspNetCore.Authorization;
```

and:

```csharp
[AllowAnonymous]
```

on the specific action.

Prefer action-level `[AllowAnonymous]` over controller-level unless the entire controller is truly public.

## Required Output

Produce:

```md
# AllowAnonymous Result

## Public Actions Marked
- `POST /api/v1/...` => reason

## Controllers Not Made Public
- `...Controller` => reason

## Files Changed
- ...
```
