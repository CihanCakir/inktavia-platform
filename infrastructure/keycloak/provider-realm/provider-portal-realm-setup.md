# Provider Realm — Detailed Setup & Token Validation

Realm: `inktavia-realm`. Execute with `setup-provider-realm.sh`, `provider-portal-clients.json` partial import,
or the Admin Console. **This was prepared, not executed** in the authoring environment.

## 1. What gets created

| Item | Value |
|---|---|
| Client `provider-portal` | public SPA, Auth Code + PKCE (S256), redirect `http://localhost:3002/*` (+prod), web origins set |
| Client `provider-portal-bff` | confidential, service account on, secret `${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}` |
| Realm roles | `provider_pending`, `provider_user`, `provider_restricted` |
| Mapper `provider_profile_id` | user-attribute → claim `provider_profile_id` (String) in access/id/userinfo |
| Mapper `aud-provider-portal-bff` | audience mapper → tokens include `provider-portal-bff` |
| Google IdP `google` | social login, trustEmail=true, scopes `openid profile email` |
| Realm login | Verify Email = on, Forgot Password = on |

## 2. Service-account roles (must be granted — not covered by partial import)

On the `provider-portal-bff` service account user (`service-account-provider-portal-bff`):
- **realm-management**: `view-users`, `query-users`, `manage-users` (create user, set attribute, assign role, verify-email, logout).
- **identity-api**: `identity_read` (by-subject + profile reads), `identity_write` (provision-from-keycloak, phone/mark-verified).

> If `identity-api` client roles `identity_read`/`identity_write` don't exist yet, create them on the
> `identity-api` client first, then grant. Without these the new Identity endpoints return 401/403.

## 3. Environment variables / secrets

| Var | Used by |
|---|---|
| `KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET` | Keycloak client secret; BFF `MarineProviderKeycloak:AdminClientSecret`; Identity `IdentityKeycloak:AdminClientSecret` |
| `GOOGLE_OAUTH_CLIENT_ID` / `GOOGLE_OAUTH_CLIENT_SECRET` | Google IdP config |
| `PROVIDER_WEB_BASE` | SPA redirect/web-origins |

BFF `MarineProviderKeycloak` and Identity `IdentityKeycloak` sections must point at the same realm/base URL.

## 4. Expected provider access token

```json
{
  "aud": ["provider-portal-bff"],
  "realm_access": { "roles": ["provider_pending"] },
  "provider_profile_id": "12345",
  "email": "provider@example.com",
  "email_verified": true,
  "preferred_username": "provider@example.com",
  "sub": "b3f1...-keycloak-user-id"
}
```

Notes:
- `provider_profile_id` is a **string**; the BFF parses it to `long`.
- It appears **only after** Identity provisioning + the BFF writes the Keycloak user attribute — so a
  brand-new token (before provisioning) may omit it; the BFF then resolves via the by-subject lookup.
- Role changes (e.g. `provider_pending → provider_user` on approval) require a **token refresh / re-login**
  to appear in the token — but **runtime Identity ApprovalStatus/ProfileStatus is checked on every request**,
  so access reflects the true status immediately regardless of the cached token role.

## 5. Smoke checklist (Phase 31C)
1. Register via BFF `POST /api/v1/provider/auth/register` → Keycloak user created, Identity profile provisioned, `provider_profile_id` attribute written, verify-email sent.
2. Google login → `POST /api/v1/provider/auth/ensure-profile` links/creates the profile.
3. `GET /api/v1/provider/me/status` returns `AwaitApproval` for a pending provider.
4. Admin approves → role sync flips `provider_pending → provider_user`; after re-login the token shows `provider_user`; status returns `EnterWorkspace`.
5. Phone OTP send/verify → `PhoneVerifiedPersisted=true`.
