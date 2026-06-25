# 01 — Audit Current BFF Auth Boundary and Cache Usage

Inspect target projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Find:

- Current authentication middleware and policies.
- Current controllers requiring `Authorization` from browser.
- Existing extraction of `X-Aizen-User-Token`.
- Existing RemoteCall contracts and header forwarding.
- Any code forwarding incoming `Authorization` headers to internal APIs.
- Any direct Keycloak token handling.
- Any current cache usage in BFF or modules.
- Any existing Aizen Core/Cache registration.

Generate or update:

```text
docs/reports/admin-panel-bff-auth-boundary-audit-report.md
```

Do not change behavior in this step unless required to make the repository compile.
