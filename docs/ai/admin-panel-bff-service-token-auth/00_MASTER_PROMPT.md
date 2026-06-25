# 00 — Master Prompt

Implement the AdminPanel BFF cache-backed service-token auth model.

Use the final architecture:

```text
React Admin Web -> AdminPanel BFF:
  X-Aizen-User-Token

AdminPanel BFF -> Internal APIs:
  Authorization: Bearer <cached admin-panel-bff Keycloak service token>
  X-Aizen-User-Token
```

Keycloak service tokens must be acquired server-side and cached using existing Aizen Core/Cache infrastructure.

Do not implement browser-side Keycloak auth.

Do not pair Keycloak service token with Identity token as a single credential.

Follow steps 01 through 10 in order.
