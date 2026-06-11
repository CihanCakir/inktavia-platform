# BFF Postman Target Model

Generate a Postman collection for AdminPanel BFF only.

The collection must target:

```text
{{admin_panel_bff_base_url}}
```

Where:

```text
admin_panel_bff_root_url = http://localhost:<ADMIN_BFF_PORT>
admin_panel_bff_base_url = {{admin_panel_bff_root_url}}/api/v1/admin-panel
```

The collection must be controller/folder based and must be generated from:

```text
docs/admin-web-client/admin-panel-bff-endpoint-catalog.md
```

If the catalog is incomplete or inconsistent with controllers, scan the actual controller files under:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Do not create Postman requests for internal module APIs directly.
