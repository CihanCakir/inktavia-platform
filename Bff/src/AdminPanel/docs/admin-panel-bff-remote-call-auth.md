# Admin Panel BFF — Remote Call Auth Forwarding

## Overview

All admin endpoints require a valid Keycloak-issued JWT bearer token in the `Authorization` header. The BFF does **not** validate or decode the token itself — it reads and forwards it to each downstream microservice, which independently verifies it against Keycloak.

## Auth Flow

```
Admin UI
  │
  │  Authorization: Bearer <jwt>
  ▼
Admin Panel BFF  (validates role = "Admin" via middleware)
  │
  │  Reads header from HttpContext
  │  Passes as [AizenRemoteCallHeader("Authorization")] parameter
  ▼
Microservice  (Identity / Vessel / FileStorage / ServiceRequest)
  │
  │  Verifies JWT signature and claims with Keycloak
  ▼
Keycloak  (token introspection / JWKS)
```

## Step 1: Read from HttpContext in Controller

Every controller action reads the raw header value before constructing a CQRS query or command:

```csharp
var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
```

This includes the full `Bearer <token>` string with the prefix.

## Step 2: Pass to CQRS Query/Command

The auth string is threaded through the CQRS record constructor:

```csharp
var result = await _cqrs.ProcessAsync(
    new GetAdminVesselOverviewQuery(auth, pageIndex, pageSize, searchTerm, isArchived), ct);
```

## Step 3: Handler Forwards to Remote Call

The application layer handler injects the remote call interface and passes auth as a parameter:

```csharp
public sealed class GetAdminVesselOverviewQueryHandler
    : IQueryHandler<GetAdminVesselOverviewQuery, AdminVesselOverviewResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public async Task<AdminVesselOverviewResponse> HandleAsync(
        GetAdminVesselOverviewQuery query, CancellationToken ct)
    {
        var response = await _vessel.GetAdminVesselList(
            query.Authorization,
            query.PageIndex,
            query.PageSize,
            query.SearchTerm,
            query.IsArchived);
        // ... map response
    }
}
```

## Step 4: Remote Call Interface Sends Header

The `[AizenRemoteCallHeader("Authorization")]` attribute instructs the Refit-backed HTTP client to attach the value as an HTTP header:

```csharp
[AizenRemoteCallGet("/api/v1/admin/vessels")]
Task<AizenApiResponse<GetAllVesselsAdminResponse>> GetAdminVesselList(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    ...);
```

The downstream service receives `Authorization: Bearer <jwt>` and validates it.

## ReferenceData Exception

`AdminReferenceDataController` does **not** carry `[Authorize]` and the `IReferenceDataAdminBffRemoteCall` methods do not accept an `Authorization` parameter. Reference data endpoints are public/read-only lookup tables that require no auth.

## BFF-Level Authorization

The BFF applies `[Authorize(Roles = "Admin")]` at controller level. If the token is missing, expired, or lacks the `Admin` role, ASP.NET Core returns `401 Unauthorized` or `403 Forbidden` before the handler is reached.

## Environment Variables

| Variable | Example |
|---|---|
| `Keycloak__Authority` | `http://keycloak:8080/realms/inktavia` |
| `Keycloak__Audience` | `inktavia-api` |
| Identity base URL | Configured in `appsettings.json` under remote call named client `IIdentityAdminBffRemoteCall` |
