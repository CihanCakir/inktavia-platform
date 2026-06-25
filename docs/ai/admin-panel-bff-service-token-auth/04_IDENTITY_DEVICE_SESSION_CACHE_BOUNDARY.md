# 04 — Identity Device Session Cache Boundary

Inspect current Identity login/refresh model and AdminPanel BFF auth endpoints.

Expected endpoints:

```http
POST /auth/login/username
POST /auth/login/phone
POST /auth/login/otp
POST /auth/otp/send
POST /auth/otp/check
POST /auth/refresh
POST /auth/password/change
```

Implement Identity session/cache only if supported by real contracts.

Rules:

- Identity remains source of user/profile/device/panel context.
- Do not silently extend Identity token without Identity validation.
- If refresh token is not held server-side, return 401 and let React call `/auth/refresh`.
- If server-side refresh token storage is implemented, bind it to `UserId + DeviceId + SessionId`.
- Store protected token values only if approved protection/encryption exists.
- Otherwise store non-sensitive session metadata only.
- Hash token values if token lookup is necessary.

Generate or update:

```text
docs/reports/admin-panel-bff-identity-device-session-cache-report.md
```
