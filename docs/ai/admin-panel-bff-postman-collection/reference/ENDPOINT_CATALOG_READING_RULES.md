# Endpoint Catalog Reading Rules

Read:

```text
docs/admin-web-client/admin-panel-bff-endpoint-catalog.md
```

Expected endpoint groups:

```text
Auth
Dashboard
Identity - General Profiles
Identity - Organizer Profiles
Identity - Venue Profiles
Identity - Participant Profiles
Files
Vessels
Service Requests
Reference Data
```

If the catalog says protected endpoints need `Authorization: Bearer <keycloakAccessToken>`, treat that as legacy text and apply the final Identity-only browser auth model.

If endpoint path, method, body, or query params are ambiguous, inspect actual controller files and request DTOs. If still uncertain, mark it in the gap report instead of inventing behavior.
