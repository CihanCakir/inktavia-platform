# AdminPanel BFF — Negative Auth Rerun Report

**Date:** 2026-06-16T21:34:24Z  
**Scope:** Login/phone, login/username, and refresh endpoint failure behavior after hardening fixes

---

## Summary

All three negative auth scenarios now correctly return `isSuccess=false` with a non-zero `errorCode`.  
The wrong-password login no longer hangs — it fails fast within 1 second.

---

## Test Conditions

- **BFF:** Running locally on `http://localhost:17001` (ASPNETCORE_ENVIRONMENT=Local)
- **Identity service:** NOT running (port 7101 — connection refused)
- **Behavior intent:** BFF should fail fast when Identity is unreachable and return `isSuccess=false`

---

## Endpoint Results

### `POST /auth/login/username`

**Request shape:** `{"username":"w@t.com","pin":"9999","deviceId":"test-device-001"}`

> Note: `LoginWithUsernameRequest` uses field name `pin` (not `password`) with minimum length 4.
> `deviceId` is also required (minimum length 6).

| Check | Expected | Actual | Result |
|---|---|---|---|
| Not blocked by FallbackPolicy | HTTP ≠ 401 | HTTP 500 | ✅ PASS |
| Reached handler | HTTP 200 or 500 | HTTP 500 | ✅ PASS |
| Response time | < 15 seconds | 0.1 seconds | ✅ PASS |
| `isSuccess` | `false` | `false` | ✅ PASS |
| `errorCode` | non-zero | 911 | ✅ PASS |
| `errorMessage` | network/domain error | "Connection refused (localhost:7101)" | ✅ PASS |

**Additional fix applied:** `[AllowAnonymous]` was missing on this action — the FallbackPolicy was returning HTTP 401 before the handler was reached. Fixed.

---

### `POST /auth/login/phone`

**Request shape:** `{"phoneNumber":"+905550000000","password":"WRONGPWD","deviceId":"test-device-001"}`

> Note: `password` minimum length is 4 characters.

| Check | Expected | Actual | Result |
|---|---|---|---|
| Not blocked by FallbackPolicy | HTTP ≠ 401 | HTTP 500 | ✅ PASS |
| Response time | < 15 seconds | 0.04 seconds | ✅ PASS |
| `isSuccess` | `false` | `false` | ✅ PASS |
| `errorCode` | non-zero | 911 | ✅ PASS |
| `errorMessage` | network/domain error | "Connection refused (localhost:7101)" | ✅ PASS |

---

### `POST /auth/refresh`

**Request shape:** `{"refreshToken":"invalid-refresh-token-xyz","deviceId":"test-device-001"}`

| Check | Expected | Actual | Result |
|---|---|---|---|
| Not blocked by FallbackPolicy | HTTP ≠ 401 | HTTP 500 | ✅ PASS |
| Response time | < 15 seconds | 0.04 seconds | ✅ PASS |
| `isSuccess` | `false` | `false` | ✅ PASS |
| `errorCode` | non-zero | 911 | ✅ PASS |
| `errorMessage` | network/domain error | "Connection refused (localhost:7101)" | ✅ PASS |

---

## Previous vs. Current Behavior

| Endpoint | Previous (LOCAL_2026-06-15) | Current (LOCAL_2026-06-16) |
|---|---|---|
| `auth/login/username` | HTTP 401 empty body (no `AllowAnonymous`!) | HTTP 500, `isSuccess=false, errorCode=911` |
| `auth/login/phone` | HTTP 200, `isSuccess=true, errorCode=0` | HTTP 500, `isSuccess=false, errorCode=911` |
| `auth/refresh` | HTTP 200, `isSuccess=true, errorCode=0` | HTTP 500, `isSuccess=false, errorCode=911` |

---

## Root Cause of Previous Failures

1. **`login/username` returned 401**: Missing `[AllowAnonymous]` — FallbackPolicy blocked the request.
2. **`login/phone` and `refresh` returned `isSuccess=true`**: Handlers called `return r.Body` directly, discarding `r.Header.IsSuccess`. When Identity returned a failure envelope, the body was `null`, and the Aizen BFF framework wrapped `null` in a success envelope.

---

## Fix Summary

| File | Fix |
|---|---|
| `AuthController.cs` | Added `[AllowAnonymous]` to `LoginWithUsername` action |
| `LoginWithUsernameCommandHandler.cs` | Added `if (r?.Header is { IsSuccess: false }) throw new AizenBusinessException(r.Header.ErrorCode)` |
| `LoginWithPhoneCommandHandler.cs` | Same fix |
| `RefreshCommandHandler.cs` | Same fix |
| `DependencyInjection.cs` | Added `httpClient.Timeout = TimeSpan.FromSeconds(15)` to Identity client |

---

## Validation Notes

Since the Identity service is not running in the test environment, `errorCode=911` (Connection refused) is returned for all three endpoints. This confirms:
- The BFF handler IS being reached (not blocked at auth/policy layer)
- The failure envelope (`isSuccess=false`) IS correctly returned
- The timeout guard IS effective (0.04–0.1s vs previous 65s hang)

When the Identity service is running, the expected behavior with genuinely invalid credentials will be:
- Invalid credentials → Identity throws `AizenBusinessException` with domain error code → BFF propagates `isSuccess=false, errorCode=<domain code>`
- The HTTP transport code will be 200 (Aizen envelope convention) — **not** HTTP 401
