# Auth, RemoteCall, and Boundary Rules

## Browser to AdminPanel BFF

The browser sends only the Identity token to AdminPanel BFF:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

If an `Authorization` header exists from old tools/tests, AdminPanel BFF must not treat it as a browser-owned service token.

## AdminPanel BFF to ServiceRequest module

BFF must send:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

ServiceRequest module validates:
1. Keycloak service token audience/client authorization.
2. User Identity token role/context through the existing Aizen user info / claims transformation mechanism.

## AdminPanel protected endpoints

AdminPanel BFF endpoints must use the existing AdminPanel policy, usually:

```csharp
[Authorize(Policy = "AdminPanelAccess")]
```

Expected auth results:
- no token: 401
- participant/customer-only token: 403
- valid admin identity token: allowed

## Do not move domain logic into BFF

ServiceRequest domain decisions stay in ServiceRequest module. BFF maps, aggregates, normalizes, and handles degraded responses only.
