# Keycloak Realm Import Specification

Create this file:

```text
infrastructure/keycloak/inktavia-realm-realm.json
```

The file must be importable by Keycloak 25.0.0 using:

```text
start-dev --import-realm
```

## Realm

```text
inktavia-realm
```

## Required API Clients

Create these API/resource clients:

```text
identity-api
payment-api
profile-api
```

These clients are used as API audiences.

They must not be used for user login.

Expected behavior:

```text
identity-api:
  Used by Identity API as JWT audience.

payment-api:
  Used by Payment API as JWT audience.

profile-api:
  Used by Profile API as JWT audience.
```

Suggested config:

```text
enabled: true
publicClient: false
standardFlowEnabled: false
directAccessGrantsEnabled: false
serviceAccountsEnabled: false
```

If Keycloak 25 supports `bearerOnly`, use it if appropriate. If not, keep the clients as non-public clients with login flows disabled.

## Required Application Clients

Create:

```text
inktavia-mobile
customer-panel
admin-panel
```

### inktavia-mobile

Purpose:

```text
Mobile application login client.
```

Allowed API audiences:

```text
identity-api
profile-api
```

Not allowed:

```text
payment-api
```

Roles expected for mobile users:

```text
mobile_user
profile_read
profile_write
```

Redirect URIs:

```text
inktavia://auth/callback
com.inktavia.mobile://auth/callback
http://localhost:19006/auth/callback
```

Settings:

```text
publicClient: true
standardFlowEnabled: true
directAccessGrantsEnabled: false
serviceAccountsEnabled: false
PKCE: S256
```

### customer-panel

Purpose:

```text
Customer panel login client.
```

Allowed API audiences:

```text
identity-api
profile-api
payment-api
```

Payment access:

```text
Read only
```

Roles expected for customer users:

```text
customer_user
profile_read
payment_read
```

Redirect URIs:

```text
http://localhost:3000/*
https://customer.inktavia.com/*
```

Settings:

```text
publicClient: true
standardFlowEnabled: true
directAccessGrantsEnabled: false
serviceAccountsEnabled: false
PKCE: S256
```

### admin-panel

Purpose:

```text
Admin panel login client.
```

Allowed API audiences:

```text
identity-api
profile-api
payment-api
```

Roles expected for admin users:

```text
admin_user
identity_read
identity_write
profile_read
profile_write
payment_read
payment_write
```

Redirect URIs:

```text
http://localhost:3001/*
https://admin.inktavia.com/*
```

Settings:

```text
publicClient: true
standardFlowEnabled: true
directAccessGrantsEnabled: false
serviceAccountsEnabled: false
PKCE: S256
```

## Realm Roles

Create these realm roles:

```text
mobile_user
customer_user
admin_user

identity_read
identity_write

profile_read
profile_write

payment_read
payment_write
```

## Test Users

Create these users:

```text
mobile.user@inktavia.com
customer.user@inktavia.com
admin.user@inktavia.com
```

Temporary password must be false.

Password:

```text
Password123!
```

### mobile.user@inktavia.com

Realm roles:

```text
mobile_user
profile_read
profile_write
```

Expected API access:

```text
identity-api
profile-api
```

### customer.user@inktavia.com

Realm roles:

```text
customer_user
profile_read
payment_read
```

Expected API access:

```text
identity-api
profile-api
payment-api
```

Payment write is not allowed.

### admin.user@inktavia.com

Realm roles:

```text
admin_user
identity_read
identity_write
profile_read
profile_write
payment_read
payment_write
```

Expected API access:

```text
identity-api
profile-api
payment-api
```

## Required Protocol Mappers

Each application client must include audience mappers.

### inktavia-mobile audience mappers

```text
audience-identity-api -> identity-api
audience-profile-api -> profile-api
```

### customer-panel audience mappers

```text
audience-identity-api -> identity-api
audience-profile-api -> profile-api
audience-payment-api -> payment-api
```

### admin-panel audience mappers

```text
audience-identity-api -> identity-api
audience-profile-api -> profile-api
audience-payment-api -> payment-api
```

Each audience mapper must add the API client as an access token audience.

Expected mapper behavior:

```text
id.token.claim: false
access.token.claim: true
included.client.audience: target API client id
```

## Realm Import JSON Shape

Use this as the base shape and adjust only if Keycloak 25 requires a compatible field format:

```json
{
  "realm": "inktavia-realm",
  "enabled": true,
  "roles": {
    "realm": [
      { "name": "mobile_user" },
      { "name": "customer_user" },
      { "name": "admin_user" },
      { "name": "identity_read" },
      { "name": "identity_write" },
      { "name": "profile_read" },
      { "name": "profile_write" },
      { "name": "payment_read" },
      { "name": "payment_write" }
    ]
  },
  "clients": [
    {
      "clientId": "identity-api",
      "name": "Identity API",
      "enabled": true,
      "publicClient": false,
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false
    },
    {
      "clientId": "payment-api",
      "name": "Payment API",
      "enabled": true,
      "publicClient": false,
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false
    },
    {
      "clientId": "profile-api",
      "name": "Profile API",
      "enabled": true,
      "publicClient": false,
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false
    },
    {
      "clientId": "inktavia-mobile",
      "name": "Inktavia Mobile",
      "enabled": true,
      "publicClient": true,
      "standardFlowEnabled": true,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false,
      "redirectUris": [
        "inktavia://auth/callback",
        "com.inktavia.mobile://auth/callback",
        "http://localhost:19006/auth/callback"
      ],
      "webOrigins": ["+"],
      "attributes": {
        "pkce.code.challenge.method": "S256"
      },
      "protocolMappers": [
        {
          "name": "audience-identity-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "identity-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        },
        {
          "name": "audience-profile-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "profile-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        }
      ]
    },
    {
      "clientId": "customer-panel",
      "name": "Customer Panel",
      "enabled": true,
      "publicClient": true,
      "standardFlowEnabled": true,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false,
      "redirectUris": [
        "http://localhost:3000/*",
        "https://customer.inktavia.com/*"
      ],
      "webOrigins": [
        "http://localhost:3000",
        "https://customer.inktavia.com"
      ],
      "attributes": {
        "pkce.code.challenge.method": "S256"
      },
      "protocolMappers": [
        {
          "name": "audience-identity-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "identity-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        },
        {
          "name": "audience-profile-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "profile-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        },
        {
          "name": "audience-payment-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "payment-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        }
      ]
    },
    {
      "clientId": "admin-panel",
      "name": "Admin Panel",
      "enabled": true,
      "publicClient": true,
      "standardFlowEnabled": true,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false,
      "redirectUris": [
        "http://localhost:3001/*",
        "https://admin.inktavia.com/*"
      ],
      "webOrigins": [
        "http://localhost:3001",
        "https://admin.inktavia.com"
      ],
      "attributes": {
        "pkce.code.challenge.method": "S256"
      },
      "protocolMappers": [
        {
          "name": "audience-identity-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "identity-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        },
        {
          "name": "audience-profile-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "profile-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        },
        {
          "name": "audience-payment-api",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "payment-api",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        }
      ]
    }
  ],
  "users": [
    {
      "username": "mobile.user@inktavia.com",
      "email": "mobile.user@inktavia.com",
      "enabled": true,
      "emailVerified": true,
      "credentials": [
        {
          "type": "password",
          "value": "Password123!",
          "temporary": false
        }
      ],
      "realmRoles": [
        "mobile_user",
        "profile_read",
        "profile_write"
      ]
    },
    {
      "username": "customer.user@inktavia.com",
      "email": "customer.user@inktavia.com",
      "enabled": true,
      "emailVerified": true,
      "credentials": [
        {
          "type": "password",
          "value": "Password123!",
          "temporary": false
        }
      ],
      "realmRoles": [
        "customer_user",
        "profile_read",
        "payment_read"
      ]
    },
    {
      "username": "admin.user@inktavia.com",
      "email": "admin.user@inktavia.com",
      "enabled": true,
      "emailVerified": true,
      "credentials": [
        {
          "type": "password",
          "value": "Password123!",
          "temporary": false
        }
      ],
      "realmRoles": [
        "admin_user",
        "identity_read",
        "identity_write",
        "profile_read",
        "profile_write",
        "payment_read",
        "payment_write"
      ]
    }
  ]
}
```

## Compatibility Note

If Keycloak 25 rejects any field in the import file, adjust the JSON to the current Keycloak export/import schema while preserving:

```text
- Realm name
- Roles
- API clients
- Application clients
- Audience mappers
- Test users
- Role mappings
```
