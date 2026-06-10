# Security Validation Checklist

Validate the following:

- Browser requests to AdminPanel BFF do not require Keycloak bearer token.
- AdminPanel BFF does not forward incoming browser Authorization headers to internal APIs.
- AdminPanel BFF uses server-side `admin-panel-bff` client credentials.
- Keycloak client secret is not committed.
- Keycloak service token is cached through Aizen Core/Cache.
- Keycloak service token cache key is not user-token based.
- Raw tokens are not used as Redis keys.
- Token values are not logged.
- Identity token is forwarded to internal APIs as `X-Aizen-User-Token`.
- Identity refresh only uses real Identity refresh contract.
- Device/session handling uses existing Identity/Aizen context conventions.
- Token stampede protection is used if available.
- Reports document whether Redis token values are protected/encrypted.
- `dotnet restore`, `dotnet build`, and `dotnet test` are executed where possible.
