# 00 — Master Prompt

Generate an AdminPanel BFF Postman package from `docs/admin-web-client/admin-panel-bff-endpoint-catalog.md`.

Apply the final Identity-only browser auth model:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not attach browser Keycloak Authorization headers to normal AdminPanel BFF requests.

Generate collection, environment, README, and reports.
