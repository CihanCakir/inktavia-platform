# 03 - Standardize Auth Token Headers

Generate or update environment and collection auth scripts according to the existing two-token model.

Required behavior:

- Keycloak token requests store `active_access_token`.
- Identity login requests store `identityAccessToken` and `X-Aizen-User-Token`.
- API requests use Bearer `{{active_access_token}}`.
- User-context requests include `X-Aizen-User-Token: {{X-Aizen-User-Token}}`.

Add helper token switch requests:

- Set Active Token - Mobile
- Set Active Token - Customer
- Set Active Token - Admin
- Set Active Identity User - Owner
- Set Active Identity User - Provider if supported
- Set Active Identity User - Admin

Create:

```text
docs/postman/auth-token-contract.md
```
