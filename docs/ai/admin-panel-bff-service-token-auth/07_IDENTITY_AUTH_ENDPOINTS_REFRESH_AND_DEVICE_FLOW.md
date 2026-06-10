# 07 — Identity Auth Endpoints, Refresh and Device Flow

Ensure AdminPanel BFF exposes real Identity auth facade endpoints only if backed by internal Identity contracts.

Expected endpoint family:

```http
POST /auth/login/username
POST /auth/login/phone
POST /auth/login/otp
POST /auth/otp/send
POST /auth/otp/check
POST /auth/refresh
POST /auth/password/change
```

Rules:

- Do not invent `/auth/session`, `/auth/admin/login`, or `/auth/identity/refresh` unless real contracts exist.
- Device information must be forwarded to Identity if required by Identity request models.
- Preserve existing request/response DTOs from Abstraction layers.
- Do not return raw `object`.
- Do not put Identity domain rules in BFF.
- Refresh behavior must go through Identity refresh contract.

Generate or update:

```text
docs/reports/admin-panel-bff-token-refresh-boundary-report.md
```
