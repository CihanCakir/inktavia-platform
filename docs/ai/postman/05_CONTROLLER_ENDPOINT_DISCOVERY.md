# Controller Endpoint Discovery and Postman Sync

## Purpose

The repository contains API endpoint definitions in C# controller files.

The Postman sync command must scan these files and keep the Postman collection updated.

## Controller Search Paths

Scan:

```text
src/MDYKE/**/Controller/V1/**/*Controller.cs
src/MDYKE/**/Controllers/V1/**/*Controller.cs
```

## Supported Attribute Patterns

The parser must detect controller route attributes:

```csharp
[Route("api/v1/[controller]")]
[Route("api/v1/identity")]
[ApiController]
```

The parser must detect action HTTP method attributes:

```csharp
[HttpGet]
[HttpGet("me")]
[HttpPost]
[HttpPost("login")]
[HttpPut("{id}")]
[HttpPatch("{id}")]
[HttpDelete("{id}")]
```

## Authorization Attribute Detection

Detect these when available:

```csharp
[Authorize]
[Authorize(Roles = "admin_user")]
[Authorize(Policy = "PaymentRead")]
[AllowAnonymous]
```

Use this information to choose a default token.

Suggested mapping:

```text
Policy IdentityRead  -> admin_access_token or customer_access_token depending endpoint
Policy IdentityWrite -> admin_access_token
Policy ProfileRead   -> mobile_access_token
Policy ProfileWrite  -> mobile_access_token or admin_access_token
Policy PaymentRead   -> customer_access_token
Policy PaymentWrite  -> admin_access_token
AdminOnly            -> admin_access_token
AllowAnonymous       -> no Authorization header
Unknown protected    -> active_access_token
```

## Module Detection

Detect module from file path first.

Examples:

```text
src/MDYKE/...Identity.../Controller/V1/... -> Identity
src/MDYKE/...Profile.../Controller/V1/... -> Profile
src/MDYKE/...Payment.../Controller/V1/... -> Payment
```

If path detection fails, infer from controller name.

Fallback:

```text
Unknown
```

## Base URL Mapping

Use module to choose base URL variable:

```text
Identity -> {{identity_api_base_url}}
Profile  -> {{profile_api_base_url}}
Payment  -> {{payment_api_base_url}}
Unknown  -> {{active_api_base_url}}
```

If `active_api_base_url` does not exist, create it in the environment with an empty or fallback value.

## Route Resolution

Resolve:

```text
[controller]
```

to controller name without `Controller`.

Example:

```text
UserController -> user
PaymentTransactionsController -> paymenttransactions
```

If the project uses custom route naming conventions, preserve the explicit route when available.

Combine controller route and action route.

## Parameter Handling

If route contains parameters:

```text
{id}
{transactionId}
```

Convert them to Postman variables:

```text
{{id}}
{{transactionId}}
```

Also add these variables to the environment if missing, with sample values:

```text
id=1
transactionId=1
```

## Request Body Handling

For POST/PUT/PATCH requests:

- If action has `[FromBody] SomeRequest request`, create a placeholder JSON body.
- If request DTO cannot be resolved, use:

```json
{}
```

## Duplicate Handling

Generated requests must be identified by:

```text
method + resolved URL + source controller + action name
```

If a generated request already exists, update it.

Do not duplicate it.

## Manual Request Preservation

Do not delete manually created requests.

Only update requests that contain generated marker:

```text
Generated from Controller
```

or header:

```text
x-generated-by: tools/postman/sync-postman-collection.mjs
```

## Required Command

Create:

```text
tools/postman/sync-postman-collection.mjs
```

Command:

```bash
node tools/postman/sync-postman-collection.mjs
```

Optional package.json script:

```bash
npm run postman:sync
```

## Final Sync Output

The command should print:

```text
- Scanned controller count
- Discovered endpoint count
- Created request count
- Updated request count
- Skipped request count
- Output collection path
```
