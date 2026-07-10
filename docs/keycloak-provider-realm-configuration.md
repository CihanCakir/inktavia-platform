# Keycloak Provider Realm Configuration (Phase 31)

> Configuration reference for the MarineProvider Web/PWA + BFF on the existing `inktavia-realm`.
> **No real secrets in this document — use placeholders.** Secrets come from env/secret store.

## 1. Clients

### `provider-portal` (public SPA)
| Setting | Value |
|---|---|
| Client type | OpenID Connect, **public** |
| Standard Flow (Auth Code) | **Enabled** |
| PKCE | **Required** (`S256`) |
| Direct Access Grants | Disabled |
| Client authentication | Off (public) |
| Valid redirect URIs | `http://localhost:3002/*`, `https://provider.inktavia.com/*` |
| Web origins | `http://localhost:3002`, `https://provider.inktavia.com` |
| Root/base URL | provider web origin |

### `provider-portal-bff` (confidential resource server + service account)
| Setting | Value |
|---|---|
| Client type | OpenID Connect, **confidential** |
| Client authentication | **On** |
| Standard Flow | Disabled |
| Service Accounts | **Enabled** (client_credentials) |
| Secret | `${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}` (placeholder) |
| Audience | tokens for the BFF API should carry `provider-portal-bff` |
| Service account roles | realm-management: `manage-users`, `view-users`, `query-users`; plus module client roles the BFF calls downstream (e.g. `identity_read`) |

The BFF validates inbound provider access tokens (audience `provider-portal-bff`) **and** uses this
client's service-account token for (a) Keycloak Admin API and (b) BFF→module calls.

> To make provider tokens carry the `provider-portal-bff` audience, add an **Audience** protocol mapper
> on `provider-portal` (or a shared client scope) with Included Client Audience = `provider-portal-bff`.

## 2. Roles (realm roles)

| Role | Meaning |
|---|---|
| `provider_pending` | Registered / profile exists / pending approval. May call `/me/status`, onboarding/profile endpoints. |
| `provider_user` | Approved + active. May call operational provider endpoints (Phase 32+). |
| `provider_restricted` | Restricted/suspended-aware. Operational access still re-checked against Identity runtime status. |

Assignment: `provider_pending` on registration; promote to `provider_user` on admin approval; see role-sync
plan in the Identity blueprint. **Runtime Identity status is authoritative** for approval/suspension — roles are a coarse gate only.

## 3. Protocol mapper — `provider_profile_id`

Add a **User Attribute** mapper (on `provider-portal` or a shared client scope):

| Field | Value |
|---|---|
| Mapper type | User Attribute |
| User Attribute | `provider_profile_id` |
| Token Claim Name | `provider_profile_id` |
| Claim JSON Type | `String` (BFF parses to long) |
| Add to access token | **On** |
| Add to ID token | On (optional) |
| Multivalued | Off |

The BFF reads this claim (`MarineProviderKeycloak:ProviderProfileIdAttributeName`) to resolve `providerProfileId`.
The attribute is written by the BFF via Admin API once the Identity Organizer profile is provisioned/linked.

> **Required (Keycloak 24+): allow the `provider_profile_id` user attribute.** The declarative User Profile
> ships with **unmanaged attributes DISABLED**, so an Admin-API write of `provider_profile_id` is **silently
> dropped** (PUT returns 200 but the attribute doesn't persist → the token claim stays null). Fix either:
> (a) set `unmanagedAttributePolicy=ADMIN_EDIT` on the realm User Profile (the setup script does this:
> `kcadm update users/profile -r inktavia-realm -s 'unmanagedAttributePolicy=ADMIN_EDIT'`), or
> (b) declare `provider_profile_id` in Realm settings → User profile with admin edit permission.
> Note: the BFF's `ProviderProfileResolver` falls back to the by-subject lookup when the claim is absent, so
> this is a claim-population optimization — not a functional blocker.

## 4. Google Identity Provider (social login)

Realm → Identity Providers → **Add Google**:
| Field | Value |
|---|---|
| Client ID | `${GOOGLE_OAUTH_CLIENT_ID}` |
| Client Secret | `${GOOGLE_OAUTH_CLIENT_SECRET}` |
| Default scopes | `openid profile email` |
| Trust email | On |
| Redirect URI (Google console) | `${KEYCLOAK_BASE}/realms/inktavia-realm/broker/google/endpoint` |

First-login flow: Keycloak auto-creates the user (email from Google, `emailVerified=true`).
Provider Web then calls `POST /api/v1/provider/auth/ensure-profile` so the BFF provisions/links the
Identity Organizer profile. **Apple** is added later with the same pattern (Identity Provider `apple`).
The legacy Identity `IOAuthProviderClient` is NOT used for Provider Web.

## 5. Verify Email required action

- Realm → Authentication → Required Actions → **Verify Email**: Enabled.
- Realm → Login → **Verify email**: On.
- The BFF triggers it explicitly via Admin API `PUT /users/{id}/execute-actions-email?client_id=provider-portal&redirect_uri=...` with `["VERIFY_EMAIL"]`.

## 6. Forgot password

- Realm → Login → **Forgot password**: On. Email (SMTP) configured at realm level.
- Provider Web links to Keycloak's native reset (login page "Forgot password?" or the account console).
- The BFF does **not** reset passwords itself and exposes no password-reset endpoint.

## 7. Redirect URI examples

| Env | provider-portal redirect | Verify email redirect (`VerifyEmailRedirectUri`) |
|---|---|---|
| Local | `http://localhost:3002/*` | `http://localhost:3002/auth/verify-callback` |
| Dev | `https://provider.dev.inktavia.com/*` | `https://provider.dev.inktavia.com/auth/verify-callback` |
| Test | `https://provider.test.inktavia.com/*` | `https://provider.test.inktavia.com/auth/verify-callback` |
| Prod | `https://provider.inktavia.com/*` | `https://provider.inktavia.com/auth/verify-callback` |

## 8. Audience / client scope recommendations

- Provider access tokens: audience `provider-portal-bff` (via Audience mapper) so the BFF's JWT validation passes.
- Downstream module APIs must accept the `provider-portal-bff` **service-account** token — grant it the
  minimal module client roles the BFF actually calls. For Phase 31B, on `identity-api`:
  `identity_read` (GET organizers/profiles/{id}, by-subject) **and** `identity_write`
  (provision-from-keycloak, phone/mark-verified). Without these the new Identity endpoints return 401/403.
- Keep `provider_profile_id` in a dedicated client scope (e.g. `provider-scope`) assigned as default to
  both `provider-portal` and `provider-portal-bff` for consistency.

## 9. BFF configuration keys (`MarineProviderKeycloak`)

```
BaseUrl, Authority, MetadataAddress, Realm, Audience, RequireHttpsMetadata,
AdminClientId, AdminClientSecret,           # provider-portal-bff service account (client_credentials)
ProviderPortalClientId, ProviderPortalBffClientId,
ProviderPendingRole, ProviderUserRole, ProviderRestrictedRole,
ProviderProfileIdAttributeName, VerifyEmailRedirectUri
```
Secrets (`AdminClientSecret`) must be injected from environment/secret store — never committed.
