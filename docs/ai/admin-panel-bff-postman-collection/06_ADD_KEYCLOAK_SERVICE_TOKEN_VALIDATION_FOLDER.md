# 06 — Add Keycloak Service Token Validation Folder

Create optional folder:

```text
00 - Keycloak BFF Service Token Validation (Optional)
```

Add request `Get admin-panel-bff service token` to obtain `adminPanelBffServiceToken`.

Clearly document that this token is only for validating Keycloak realm/client setup and is not used by browser-to-BFF requests.
