# 06 — Fix Auth Negative Cases and Refresh Flow

Fix Identity auth failures:

- wrong username/password must not timeout,
- bad phone credentials must not return success,
- invalid refresh token must not return success.

Inspect:

```text
POST /auth/login/username
POST /auth/login/phone
POST /auth/refresh
```

Requirements:

1. Failed credentials return repository-standard 400/401/403 or error envelope.
2. Do not return `header.isSuccess = true` for invalid credentials.
3. Add cancellation/timeout handling to avoid 25s hangs.
4. Preserve valid login behavior.

If the existing Identity module intentionally returns HTTP 200 with `header.isSuccess=false`, align tests and BFF mapping accordingly. Never report invalid auth as success.

Generate auth-specific findings in the final gap report.
