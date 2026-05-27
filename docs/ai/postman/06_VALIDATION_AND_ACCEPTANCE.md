# Postman Validation and Acceptance Criteria

## Required Files

The implementation must create or update:

```text
infrastructure/postman/inktavia-local.postman_environment.json
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
docs/postman/POSTMAN_LOCAL_TESTING.md
tools/postman/sync-postman-collection.mjs
```

The implementation must also keep these AI task documents:

```text
docs/ai/postman/00_AGENT_ENTRYPOINT.md
docs/ai/postman/01_POSTMAN_ARCHITECTURE.md
docs/ai/postman/02_IMPLEMENTATION_TASKS.md
docs/ai/postman/03_POSTMAN_COLLECTION_SPEC.md
docs/ai/postman/04_POSTMAN_ENVIRONMENT_SPEC.md
docs/ai/postman/05_CONTROLLER_ENDPOINT_DISCOVERY.md
docs/ai/postman/06_VALIDATION_AND_ACCEPTANCE.md
```

## Import Validation

The following files must be importable into Postman:

```text
infrastructure/postman/inktavia-local.postman_environment.json
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
```

## Collection Validation

Collection must contain folders:

```text
00 - Keycloak Auth
01 - Identity API
02 - Profile API
03 - Payment API
04 - Auto-Discovered Endpoints
90 - Negative Authorization Tests
99 - Diagnostics
```

## Auth Validation

Token helper requests must exist:

```text
Get Mobile Token - inktavia-mobile
Get Customer Token - customer-panel
Get Admin Token - admin-panel
```

Each request must save the access token to the matching environment variable.

## Sync Command Validation

Run:

```bash
node tools/postman/sync-postman-collection.mjs
```

Expected output:

```text
- Scanned controller count
- Discovered endpoint count
- Created request count
- Updated request count
- Skipped request count
```

Expected file update:

```text
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
```

## Acceptance Checklist

```text
[ ] Environment JSON imports into Postman.
[ ] Collection JSON imports into Postman.
[ ] Token requests exist.
[ ] Token requests save environment variables.
[ ] API folders exist.
[ ] Negative test folder exists.
[ ] Diagnostics folder exists.
[ ] Sync tool scans src/MDYKE controller files.
[ ] Sync tool updates collection without duplicating generated requests.
[ ] Manual requests are preserved.
[ ] Generated requests are marked.
[ ] Documentation explains local testing.
```

## Final Agent Response

After implementation, summarize:

```text
- Files created
- Files updated
- Collection path
- Environment path
- Sync command
- How to import into Postman
- How to get tokens
- How to sync newly added endpoints
- Any assumptions made about API ports or route prefixes
```
