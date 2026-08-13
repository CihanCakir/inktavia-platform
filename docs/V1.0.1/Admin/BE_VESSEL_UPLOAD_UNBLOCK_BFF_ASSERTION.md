# BE — Unblock vessel document/media register (BFF assertion + in-module file identity)

Context: `BE_VESSEL_RESOLVE_AND_DOC_ACTIONS` added the vessel upload/replace endpoints (presigned PUT two-step), but
the **register** step fails: the Vessel module validates the uploaded file via a nested FileStorage call using
`UserInfo.AccessToken`, which is **empty on the admin BFF assertion path** (the trusted-BFF identity assertion is
disabled in dev), so that inner call 401s. This kickoff makes the assertion path carry identity end-to-end so the
register step (documents + media) works. Standing rules apply; **DO NOT COMMIT**.

## Root findings (verified)
1. **Assertion disabled in dev.** `docker-compose.yaml`: `AIZEN_BFF_ASSERTION_SECRET` defaults empty (`:-`), which
   disables the trusted-BFF assertion on every host. Modules currently accept BFF calls without it, so reads work —
   but `UserInfo` is never populated from an assertion, so `UserInfo.AccessToken` is empty on module→module calls.
2. **vessel-api allow-list is wrong.** `vessel-api` block: `BffAssertion__AllowedClientIds__0: marine-mobile-bff`
   **only** — it does **not** list `admin-panel-bff` (or `provider-portal-bff`). `file-storage-api` correctly lists
   all three (provider-portal-bff, admin-panel-bff, marine-mobile-bff). So the moment the secret is enabled,
   admin-panel-bff → vessel-api calls would be **rejected**. This must be fixed first.

## Steps

### 1. Fix vessel-api allow-list (blocker)
In `docker-compose.yaml` (and the equivalent k8s manifest/Helm values for vessel-api), extend vessel-api's assertion
allow-list to match file-storage-api:
```
BffAssertion__AllowedClientIds__0: provider-portal-bff
BffAssertion__AllowedClientIds__1: admin-panel-bff
BffAssertion__AllowedClientIds__2: marine-mobile-bff
```
(Preserve marine-mobile-bff; add the two BFFs.) Grep every service's `BffAssertion__AllowedClientIds` and confirm
each module that the admin BFF calls includes `admin-panel-bff`.

### 2. Enable the assertion secret
Set `AIZEN_BFF_ASSERTION_SECRET` to a non-empty value for dev (`.env` / compose override — **not** committed to the
repo) and document that prod/staging supply it via k8s secret. This activates the admin-panel-bff → module trusted
assertion so `UserInfo` (subject + roles) is populated module-side. Note: this also fixes the earlier
`UserInfo.Roles` empty issue that forced the `[Authorize(Roles=Admin)]` workaround, and unblocks profile-approval
uploads (same class). Cross-ref `[[notification_module_needs_bff_assertion_secret]]`.

### 3. Fix the in-module FileStorage validation identity (root cause of register 401)
In the Vessel module's document/media register command handler, the nested FileStorage validation/finalize call
authenticates with `UserInfo.AccessToken`. On the assertion path this is empty. Change that inner call to
authenticate the **same way other module→module calls do** — propagate the trusted BFF assertion (or use the module
service token / client-credentials), not the end-user access token. Mirror how Vessel already calls FileStorage for
the **read** presign path (`CreateReadUrl` works via the shared `IFileStorageRemoteCall`), and use that same
authenticated remote-call for the register-time validation instead of a raw token.

### 4. Verify (with the secret enabled)
- **Regression:** existing admin vessel **reads** still work (list, detail, documents, media) now that vessel-api's
  allow-list includes admin-panel-bff — confirm no 401/403 after enabling the secret.
- **Upload docs:** `POST …/documents/upload-url` → browser PUT to MinIO → `POST …/documents` (register) succeeds and
  the new document appears in `GET …/documents`.
- **Upload media:** same via `…/media/upload-url` → PUT → `…/media`.
- **Replace:** `POST …/documents/{id}/versions` adds a version (isCurrent flips).
- Best-effort/presigned reads unaffected; `dotnet build` 0 errors; any new tests green.
- **No commit.**

## Handoff
Once this lands, run `QA_VESSELS_V6_FE_UPLOAD_WIRE` (admin-web) to re-enable the Upload/Replace buttons against these
routes.
