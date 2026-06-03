# Existing Postman Export Analysis Requirements

Before generating new artifacts, inspect the existing Postman collection and environment exports.

The current collection contains Keycloak auth requests for mobile, customer, and admin clients. These requests parse `response.access_token` and store client-specific access token variables plus `active_access_token`.

The current Identity login requests use the Keycloak token as Bearer Authorization and then parse the application-level Identity token from `response.body.token.accessToken`.

The Identity login script stores:

- `X-Aizen-User-Token` with `Bearer ${identityAccessToken}`
- `identityAccessToken`
- `identityAccessTokenExpiredDate`
- `identityRefreshToken`
- `identityRefreshTokenExpiredDate`
- profile helper variables such as `identityUserId`, `identityUserEmail`, `identityUserName`, `identityUserSurname`

The environment already includes:

- Keycloak URL variables
- Identity and ReferenceData base URL variables
- client ids
- usernames/password
- `active_access_token`
- `X-Aizen-User-Token`
- `identityAccessToken` helper variables

The generated package must extend this environment with Vessel, FileStorage, and ServiceRequest variables, not replace the existing model.
