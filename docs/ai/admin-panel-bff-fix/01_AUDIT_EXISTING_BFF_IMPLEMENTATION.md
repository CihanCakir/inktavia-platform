# 01 - Audit Existing AdminPanel BFF Implementation

Inspect the existing AdminPanel BFF implementation.

Target paths:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Produce a checklist of:

- Existing controllers
- Existing Application commands/queries
- Incorrect same-file Query/Handler definitions
- Incorrect same-file Command/Handler definitions
- Existing remote services
- Existing AizenRemoteCall usage
- Existing appsettings entries
- Existing docs/postman files
- Missing or suspicious areas

Generate:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-current-state-audit.md
```
