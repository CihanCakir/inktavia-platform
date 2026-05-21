# 03 - Auth Package Analysis

This step focuses on the `Aizen.Core.Auth` package.

## Target Area

Inspect:

```text
Core/Auth/src/Aizen.Core.Auth
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

Find:

```csharp
AddAizenAuth
```

## Goal

Understand the current Keycloak/JWT implementation and decide how to extend it so all authorization policy behavior is centralized in this package.

## Required Analysis

Answer these questions:

### Authentication

- Does `AddAizenAuth` call `AddAuthentication`?
- Does it use `JwtBearerDefaults.AuthenticationScheme`?
- Does it call `AddJwtBearer`?
- Where are Keycloak options loaded from?
- Are Authority, Audience, ClientId, Realm, MetadataAddress or similar settings used?
- Is configuration hard-coded or options-based?

### Token Validation

Check:

- `ValidateIssuer`
- `ValidateAudience`
- `ValidateLifetime`
- `ValidateIssuerSigningKey`
- `ClockSkew`
- `RequireHttpsMetadata`
- `MapInboundClaims`
- Claims mapping compatibility

### Authorization

Check:

- Does `AddAizenAuth` call `AddAuthorization`?
- Are there named policies?
- Is there a fallback policy?
- Is there a default policy?
- Is there any custom authorization handler?

### User Context Compatibility

Search for current user context/accessor classes and claim usage.

Do not break existing claim names such as:

```text
sub
userId
jti
version
refreshTokenExpire
role
scope
preferred_username
```

## Required Output

Produce:

```md
# Aizen.Core.Auth Analysis Report

## AddAizenAuth Current Behavior
- ...

## Keycloak Configuration Source
- ...

## Token Validation Current State
- ...

## Authorization Current State
- ...

## Missing Pieces
- ...

## Safe Extension Plan
- ...
```
