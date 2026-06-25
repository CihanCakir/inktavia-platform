# Final Auth and Token Model

## AdminPanel BFF browser-facing model

React/Admin Web and Postman-simulated browser requests send only:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The AdminPanel BFF does not require browser-provided Keycloak tokens for normal protected BFF requests.

## Server-side BFF model

AdminPanel BFF obtains the Keycloak token server-side using:

```text
grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<secret>
```

Then internal API calls use:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

## Postman implication

Normal AdminPanel BFF folders must not attach `Authorization: Bearer {{adminPanelBffServiceToken}}`.

Only the optional Keycloak validation folder may obtain and decode the BFF service token.
