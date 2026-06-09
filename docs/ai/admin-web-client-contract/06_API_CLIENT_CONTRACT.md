# 06 — API Client Contract

Create:

```text
docs/admin-web-client/admin-web-api-client-contract.md
```

Define how the React app should call AdminPanel BFF:

- Base URL from environment variable.
- Request interceptor attaches Keycloak and Identity tokens.
- Response interceptor handles common API response envelope.
- Error mapping for 400/401/403/404/500.
- Pagination contract.
- Cancellation support.
- File read URL handling.
- Token refresh once before redirecting to login.

Provide TypeScript examples in markdown only unless an Admin Web project already exists and the task explicitly requires source generation.
