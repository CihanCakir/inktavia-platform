# Keycloak Service Token Validation Folder

Create a separate folder:

```text
00 - Keycloak BFF Service Token Validation (Optional)
```

Requests:

1. `Get admin-panel-bff service token`
   - POST `{{keycloak_token_url}}`
   - Body: x-www-form-urlencoded
     - `grant_type=client_credentials`
     - `client_id={{admin_panel_bff_client_id}}`
     - `client_secret={{admin_panel_bff_client_secret}}`
   - Test script captures `access_token` into `adminPanelBffServiceToken`.

2. `Decode token instructions`
   - This may be a documentation-only request or README section.

This folder is optional and must be clearly marked as not part of normal browser-to-BFF calls.

Do not attach `adminPanelBffServiceToken` as Authorization to normal AdminPanel BFF requests.
