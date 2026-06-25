# 01 — Analyze AdminPanel BFF

Inspect the AdminPanel BFF implementation.

Scan:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/**/*Controller.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/**/Controllers/**/*.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/**/*.cs
Bff/src/AdminPanel/docs/postman/**/*.md
Bff/src/AdminPanel/docs/postman/**/*.json
```

Identify:

- Controllers
- Routes
- HTTP methods
- Action names
- Request DTOs
- Response DTOs
- Command/query handlers
- Downstream module calls
- Auth attributes/policies
- Missing response types
- Endpoint gaps relevant to Admin Web

Do not modify source code in this step. Generate findings into:

```text
docs/admin-web-client/admin-panel-bff-analysis-notes.md
```
