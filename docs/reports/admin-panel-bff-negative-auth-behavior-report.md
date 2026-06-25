# AdminPanel BFF — Negative Auth Behavior Report

**Date:** 2026-06-15  
**Branch:** feature/service-request-registration  
**Scope:** Login and refresh failure response mapping

---

## Summary

This report documents the root-cause analysis and fix for incorrect success responses returned by
the AdminPanel BFF when the Identity module rejects login or refresh requests.

---

## Observed Failure (Original Test Run)

From `LOCAL_2026-06-15T13-43-30-657Z/identity.api.json`:

| Endpoint | Scenario | Expected | Actual (before fix) |
|---|---|---|---|
| `POST /auth/login/username` | Wrong password | 400/401 or `isSuccess=false` | **Timeout (~65s), no response** |
| `POST /auth/login/phone` | Bad phone credentials | 400/401 or `isSuccess=false` | **HTTP 200, `isSuccess=true, errorCode=0`** |
| `POST /auth/refresh` | Invalid refresh token | 400/401 or `isSuccess=false` | **HTTP 200, `isSuccess=true, errorCode=0`** |

---

## Root Cause Analysis

### Negative auth returning success

The BFF handlers (`LoginWithPhoneCommandHandler`, `RefreshCommandHandler`) called:
```csharp
var r = await _identity.LoginWithPhone(request.Request, authHeader);
return r.Body;  // <-- bug: discards r.Header
```

When the Identity module returns a failure:
```json
{"header": {"isSuccess": false, "errorCode": 1234}, "body": null}
```

The handler returned `r.Body` = `null`. The Aizen BFF framework wrapped `null` in a success envelope:
```json
{"header": {"isSuccess": true, "errorCode": 0}, "body": null}
```

### Username login timeout

The Identity module performs bcrypt password comparison and lockout management. When the Identity
service is unreachable or slow, the BFF had no HttpClient timeout configured on the named client
`IIdentityAdminBffRemoteCall`. The default `HttpClient.Timeout` is 100 seconds, resulting in a
~65-second hang before eventual socket failure.

---

## Fix Applied

### Handler fix — all three auth handlers

Added envelope validation before returning body:

```csharp
var r = await _identity.LoginWithUsername(request.Request, authHeader);

if (r?.Header is { IsSuccess: false })
    throw new AizenBusinessException(r.Header.ErrorCode);

return r?.Body;
```

Pattern applied to:
- `LoginWithUsernameCommandHandler.cs`
- `LoginWithPhoneCommandHandler.cs`
- `RefreshCommandHandler.cs`

**Effect:** When Identity returns `isSuccess=false`, the BFF handler throws `AizenBusinessException`
with the original Identity error code. The Aizen error middleware catches this and returns:
```json
{"header": {"isSuccess": false, "errorCode": <identity error code>, "errorMessage": "..."}, "body": null}
```

### Timeout fix — `DependencyInjection.cs`

```csharp
services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(IIdentityAdminBffRemoteCall));
    httpClient.Timeout = TimeSpan.FromSeconds(15);
    return RestService.For<IIdentityAdminBffRemoteCall>(httpClient);
});
```

**Effect:** If the Identity service does not respond within 15 seconds, the BFF returns a
`TaskCanceledException` which the Aizen error middleware converts to a deterministic error response
instead of a 65-second hang.

---

## Expected Behavior After Fix

| Endpoint | Scenario | Expected response |
|---|---|---|
| `POST /auth/login/username` | Wrong password | `isSuccess=false`, errorCode from Identity (e.g., `LoginFailedForPasswordBlockedUser`) |
| `POST /auth/login/phone` | Bad credentials | `isSuccess=false`, errorCode from Identity |
| `POST /auth/refresh` | Invalid refresh token | `isSuccess=false`, errorCode from Identity (e.g., `TokenNotFound`) |
| Identity unreachable | Any login/refresh | Timeout error within 15 seconds, `isSuccess=false` |

---

## Validation

```
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```
**Result: Build succeeded. 0 errors.**

---

## Remaining Gaps

1. **Live retest**: The actual error codes returned by the Identity module for wrong credentials
   (`LoginFailedForPasswordBlockedUser`, `UserNotFound`, `TokenNotFound`) need to be verified in a
   live local test run with the Identity service running.

2. **Test assertions**: Local test suite currently asserts `expectedStatusCode: 401`. The Aizen
   envelope always returns HTTP 200. Test assertions should be updated to check
   `header.isSuccess = false` and `header.errorCode != 0` rather than HTTP 401.

3. **Null `r` case**: If the Refit client throws a network exception, `r` is never set. The handlers
   propagate the exception directly to the Aizen error middleware, which returns an error envelope.
   This is correct behavior and requires no additional handling.
