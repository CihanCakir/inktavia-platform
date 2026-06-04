# 09 - Postman Docs and Validation

Regenerate AdminPanel BFF Postman documentation.

## Required files

```text
Bff/src/AdminPanel/docs/postman/admin-panel-bff-endpoint-inventory.md
Bff/src/AdminPanel/docs/postman/AdminPanelBff.ControllerApiTests.postman_collection.json
Bff/src/AdminPanel/docs/postman/AdminPanelBff.CrossModuleScenarios.postman_collection.json
Bff/src/AdminPanel/docs/postman/admin-panel-bff-validation-report.md
```

## Postman rules

- Use `{{admin_panel_bff_base_url}}`.
- Use `Authorization: Bearer {{active_access_token}}`.
- Use `X-Aizen-User-Token: {{X-Aizen-User-Token}}`.
- Request bodies must be based on actual request DTOs.
- Response tests must be based on actual response DTOs.
- Do not use `{}` for endpoints with request bodies.
- Include scripts to save IDs where relevant.
- Include Identity authentication/authorization endpoints.
- Include ReferenceData endpoints.
- Include Vessel, FileStorage and ServiceRequest endpoints relevant to AdminPanel.
