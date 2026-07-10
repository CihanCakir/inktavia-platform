# Claude Code Prompt — Phase 32 Item 3: Resolver cleanup rebuild + regression

A follow-up cleanup was applied to the MarineProvider BFF. Rebuild and confirm no regression against the provider
jobs slice. This is a verification-only task — do not add features.

## What changed (already applied to the repo)
`Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Common/Services/ProviderProfileResolver.cs`
- Removed the primary `GetOrganizerProfileById(claimId)` resolution branch. That endpoint is admin-only and always
  returned 403 for the BFF service account (a swallowed warning on every provider request).
- `ResolveAsync` now resolves **only** via `GetOrganizerProfileByKeycloakSubject(subject)` — the non-admin
  endpoint the BFF is authorized for. It returns the profile id + UserId used to populate `IProviderIdentityHolder`
  (which drives the `X-Aizen-Bff-Assertion` / `X-Aizen-Provider-Profile-Id` headers). Behavior is otherwise
  identical; the old code already fell through to this path.

## Steps
1. **Build:** `dotnet build Aizen.sln -c Debug` → expect **0 errors**. If `GetOrganizerProfileById` is now unused on
   `IProviderIdentityRemoteCall`, leave the interface method in place (harmless); do not delete it in this task.
2. **Bring up** (reuse the running stack if healthy): `keycloak identity-api service-request-api bff-marineprovider`
   plus infra. Ensure `AIZEN_BFF_ASSERTION_SECRET` is still set on both BFF and service-request-api.
3. **Regression — re-run only 5a and 5b** from the Item-3 smoke, same test data (Provider A profileId=100006 with
   assignment id=4; Provider B profileId=100007, no assignments):
   - **5a:** `GET http://localhost:17002/api/v1/provider/jobs` as Provider A → HTTP 200,
     `hasProfileLink=true`, `items=[{assignmentId:4}]`, `warnings=[]`.
   - **5b:** same as Provider B → HTTP 200, `hasProfileLink=true`, `items=[]`, `totalReturned=0`.
4. **Log check:** confirm the BFF no longer logs the "Resolve provider profile by claim id failed" warning on these
   requests (the whole point of the cleanup).
5. **Report:** append a short "Resolver cleanup verification" section to
   `docs/reports/phase32-item3-provider-jobs-smoke-report.md` with build result, 5a/5b PASS/FAIL, and confirmation
   the claim-id warning is gone.

## Guardrails
- Do not change resolution semantics further; by-subject stays authoritative.
- Do not accept a provider profile id from query/body anywhere.
- If 5a/5b regress, report the exact response + logs; do not refactor around it.
