# Architecture Alignment

## Current target architecture

React Admin Web calls AdminPanel BFF. AdminPanel BFF calls internal modules.

Browser to BFF:

```http
X-Aizen-User-Token: Bearer <identity-user-token>
```

BFF to modules:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identity-user-token>
```

Do not require the browser to acquire or send a Keycloak service token.

## Route convention

Source documents use `/bff/vessels`. The current project uses AdminPanel BFF routes under `/api/v1/admin-panel`.

Implement using existing controller conventions. If existing `AdminVesselsController` already exists, extend it instead of creating a conflicting route/controller.

## Response convention

Use existing `AizenBffResponse<T>` / Aizen envelope helpers. Do not return raw entities. Do not invent a second envelope.

## Internal module boundary

Vessel owns vessel data. CargoDry owns kit activation data. ServiceRequest owns service history. BFF aggregates; it does not write into another module database.
