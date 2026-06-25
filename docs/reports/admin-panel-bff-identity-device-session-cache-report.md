# AdminPanel BFF Identity Device Session Cache Report

## Assessment Date
2026-06-10

---

## Identity Remains the User Authority

Identity module owns login, refresh, user profile, device context, and panel/domain authorization. The AdminPanel BFF does not extend or invent Identity tokens.

---

## Existing Identity Auth Endpoints (BFF facade)

| Endpoint | Auth | Notes |
|---|---|---|
| `POST /api/v1/admin-panel/auth/login/username` | Anonymous | Proxied to Identity |
| `POST /api/v1/admin-panel/auth/login/phone` | Anonymous | Proxied to Identity |
| `POST /api/v1/admin-panel/auth/login/otp` | Anonymous | Proxied to Identity |
| `POST /api/v1/admin-panel/auth/otp/send` | Anonymous | Proxied to Identity |
| `POST /api/v1/admin-panel/auth/otp/check` | Anonymous | Proxied to Identity |
| `POST /api/v1/admin-panel/auth/refresh` | Anonymous | Proxied to Identity (React calls this when token expires) |
| `POST /api/v1/admin-panel/auth/password/change` | Authorized | Proxied to Identity; forwards `X-Aizen-User-Token` + BFF service token |

All auth endpoints delegate to the real Identity module via `IIdentityAdminBffRemoteCall`. No Identity domain rules are in the BFF.

---

## Device-Aware Identity Session Cache

**Decision: Not implemented.**

Reasons:
1. The Identity module contract does not expose a device/session identifier in its response DTOs (reviewed `UserLoginResponse`, `UserProfileDetailDto` — no `DeviceId` field returned to BFF).
2. `IAizenInfoAccessor` is available in the framework but no `DeviceId` claim was found in the BFF's current auth pipeline.
3. Implementing a BFF-side device session cache without a real Identity contract binding would invent behaviour.

If the Identity module adds device-session context to its token or response in the future, the BFF should implement:
```text
inktavia:admin-panel-bff:identity-session-by-device:{userIdHash}:{deviceIdHash}
```

---

## Refresh Boundary

- Refresh goes through the real Identity refresh contract at `POST /api/v1/auth/refresh` on the Identity module.
- The BFF does not hold a server-side refresh token.
- When the React client's Identity access token expires, it calls `POST /api/v1/admin-panel/auth/refresh` with its refresh token.
- The BFF proxies this to Identity and returns the new access token.
- No silent token renewal or BFF-side refresh exists.

---

## Token Storage in BFF Session

The BFF does not store Identity tokens server-side. Identity tokens are:
- Received by the browser from Identity login responses.
- Sent by the browser per-request in `X-Aizen-User-Token`.
- Forwarded unchanged by the BFF to internal APIs.

This is intentional and correct per the target security model.

---

## Remaining Gaps

- No device/session cache (by design, pending Identity contract).
- No protected/encrypted storage for Identity token values (not applicable since BFF does not store them).
- `IAizenInfoAccessor` DeviceId availability in BFF request pipeline: not verified — follow-up when device context is added to Identity JWT claims.
