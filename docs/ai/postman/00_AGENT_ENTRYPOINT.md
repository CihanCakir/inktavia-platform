# Postman Integration Agent Entrypoint

You are implementing and maintaining an importable Postman testing package for this repository.

Read all files in this folder in order:

```text
docs/ai/postman/01_POSTMAN_ARCHITECTURE.md
docs/ai/postman/02_IMPLEMENTATION_TASKS.md
docs/ai/postman/03_POSTMAN_COLLECTION_SPEC.md
docs/ai/postman/04_POSTMAN_ENVIRONMENT_SPEC.md
docs/ai/postman/05_CONTROLLER_ENDPOINT_DISCOVERY.md
docs/ai/postman/06_VALIDATION_AND_ACCEPTANCE.md
```

## Goal

Create and maintain a Postman package that tests the Keycloak based authentication and authorization setup for APIs under:

```text
src/MDYKE
```

API endpoint definitions are located in controller files matching:

```text
src/MDYKE/**/Controller/V1/**/*Controller.cs
src/MDYKE/**/Controllers/V1/**/*Controller.cs
```

The package must generate or update an importable Postman collection JSON and environment JSON.

## Required Importable Files

Create or update:

```text
infrastructure/postman/inktavia-local.postman_environment.json
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
docs/postman/POSTMAN_LOCAL_TESTING.md
```

## Required Sync Command

Create a repeatable command that scans new `*Controller.cs` files and updates the Postman collection.

Preferred command:

```bash
node tools/postman/sync-postman-collection.mjs
```

Also add package.json script if package.json exists:

```json
{
  "scripts": {
    "postman:sync": "node tools/postman/sync-postman-collection.mjs"
  }
}
```

If package.json does not exist, keep the direct node command documented.

## Keycloak Values

Use the existing Keycloak realm configuration.

Realm:

```text
inktavia-realm
```

API/resource clients:

```text
identity-api
profile-api
payment-api
```

Application/login clients:

```text
inktavia-mobile
customer-panel
admin-panel
```

Test users:

```text
mobile.user@inktavia.com
customer.user@inktavia.com
admin.user@inktavia.com
```

Default local password:

```text
Password123!
```

## Important Authentication Model

Do not invent a separate authentication model for Postman.

Postman must use the same Keycloak model:

```text
Application client -> user login -> access token -> API audience -> API request -> role/policy validation
```

## Required Client-to-API Mapping

```text
inktavia-mobile:
  user: mobile.user@inktavia.com
  token variable: mobile_access_token
  allowed API audiences:
    - identity-api
    - profile-api

customer-panel:
  user: customer.user@inktavia.com
  token variable: customer_access_token
  allowed API audiences:
    - identity-api
    - profile-api
    - payment-api
  expected write limitations:
    - payment_write not allowed
    - identity_write not allowed
    - profile_write not allowed unless explicitly changed

admin-panel:
  user: admin.user@inktavia.com
  token variable: admin_access_token
  allowed API audiences:
    - identity-api
    - profile-api
    - payment-api
```

## Execution Rule

Do not ask for confirmation unless the repository structure cannot be inferred.

Implement or update the required files directly.
