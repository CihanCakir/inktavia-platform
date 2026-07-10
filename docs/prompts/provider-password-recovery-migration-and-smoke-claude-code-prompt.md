# Claude Code Prompt — Provider Password Recovery: Migration, Config & Smoke

The Identity-backed refactor is implemented. A subsequent **contract/layer cleanup** then changed: both password-
recovery controllers now bind Request DTOs (not command classes), the 4 domain result models moved to
`Aizen.Modules.Identity.Domain/Model/PasswordRecovery/PasswordRecoveryResults.cs`, a new BFF request-DTO file was
added, and appsettings were adjusted. Those cleanup edits were **not compiled yet** — so build first, then complete
the three live-environment steps: EF migration, the Keycloak admin config for identity-api, and an end-to-end smoke.
Do not change the frontend or the public BFF contract. Do not claim production readiness until the Notification gap
is closed.

## Step 0 — Rebuild after cleanup (catch any compile deltas)
```
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Aizen.Modules.Identity.Abstraction.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
```
Expect **0 errors**. Likely deltas to check if any error appears: the moved result models need
`using Aizen.Modules.Identity.Domain.Model.PasswordRecovery;` wherever they are named (the domain-service impl was
updated; handlers use `var`); the controllers must reference the Request DTO namespaces. Fix only wiring, not design.
Do NOT proceed to the migration until Step 0 is green.

## Step 1 — EF migration (new recovery table)
```
dotnet ef migrations add AddProviderPasswordRecoveryRequest \
  -p Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj \
  -s Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
```
- Verify the migration creates `provider_password_recovery_requests` with a **unique index on ResetRequestId** and
  the audit columns from `AizenEntityWithAudit`. Confirm no unintended changes to other tables.
- Apply it: `dotnet ef database update` (same -p/-s), or rely on migrate-on-start if identity-api runs migrations
  automatically. Confirm the table exists in the identity DB.

## Step 2 — identity-api Keycloak admin + recovery config (docker-compose)
The Identity password reset uses `IdentityKeycloakOptions` (client-credentials → Keycloak Admin API). Ensure
**identity-api** has these env vars (mirror the values the BFF already uses; the admin client must have the
`realm-management` roles `manage-users` + `view-users` so `reset-password` and `logout` are permitted):
```
IdentityKeycloak__Enabled: "true"
IdentityKeycloak__BaseUrl: http://keycloak:8080
IdentityKeycloak__Realm: inktavia-realm
IdentityKeycloak__AdminClientId: provider-portal-bff          # or a dedicated identity admin client
IdentityKeycloak__AdminClientSecret: ${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET:-local-dev-only-change-me}
IdentityKeycloak__RevokeSessionsOnSuspend: "true"
PasswordRecovery__OtpLength: 6
PasswordRecovery__OtpTtlSeconds: 300
PasswordRecovery__ResendCooldownSeconds: 60
PasswordRecovery__MaxAttempts: 5
PasswordRecovery__ResetTokenTtlSeconds: 300
PasswordRecovery__DevExposeOtp: "true"        # LOCAL ONLY — reads OTP from logs for the smoke; MUST be false elsewhere
```
- Confirm the `provider-portal-bff` service account already satisfies the Identity **IdentityWrite** policy (it does
  for existing BFF→Identity calls). The new `/provider-password-recovery/*` endpoints use `IdentityWrite`.
- If the chosen admin client lacks `manage-users`, grant it in Keycloak (do not weaken any other client config).

## Step 3 — End-to-end smoke (email channel)
Use an existing provider (Organizer profile) whose Keycloak user has a known email.
```
1. POST http://localhost:17002/api/v1/provider/auth/password/forgot   {"channel":"email","identifier":"<provider email>"}
     → accepted=true, resetRequestId, maskedTarget, otpLength=6, expiresInSeconds, resendAfterSeconds
2. Read the OTP from identity-api logs ([DEV-ONLY] line; DevExposeOtp=true).
3. POST /provider/auth/password/otp/verify   {"resetRequestId":"...","otpCode":"NNNNNN"}
     → verified=true, resetToken present, and NO access/login token in the response
4. POST /provider/auth/password/reset   {"resetToken":"...","newPassword":"NewStrongPass123!","confirmPassword":"NewStrongPass123!"}
     → success=true; the Keycloak password is updated; existing sessions revoked
5. Log in via Keycloak with the NEW password (works); the OLD password no longer works
```
Negative cases to confirm:
```
- Unknown email → same generic accepted shape, and NO recovery row is persisted (check the table).
- Wrong OTP → verified=false; attempts increment; locked at MaxAttempts.
- Expired OTP (wait past TTL) → verified=false.
- Reused reset token → reset fails (single-use; row deleted after success).
- Resend within cooldown → resent generic, no new code; after cooldown → new code replaces old.
```

## Step 4 — Report
Append a "Migration + Smoke" section to `docs/provider-password-recovery-identity-backed-refactor-report.md` with:
migration name + applied status, the identity-api config added, the 5-step happy-path result, the negative-case
results, and confirmation that no raw OTP/token/password is stored (inspect a live row) and that no login token is
returned on verify.

## Guardrails
- Set `PasswordRecovery__DevExposeOtp=false` for any shared/test/production environment before finishing.
- Do not change the frontend or the BFF public endpoints/response shapes.
- Do not mark production-ready: real email/SMS delivery via the Notification module + per-identifier/IP rate limiting
  are still outstanding.
