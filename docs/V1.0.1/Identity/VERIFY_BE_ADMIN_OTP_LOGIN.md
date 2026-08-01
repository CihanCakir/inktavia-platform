# VERIFY — BE_ADMIN_OTP_LOGIN (Kickoff 1 smoke): prove the admin OTP code reaches the logs

> **Repo:** `addesso-project`. **Goal:** confirm the Kickoff-1 admin OTP backend actually generates a code and mints a
> login ticket, end-to-end through the running docker stack. This is a **verification-only** task — do NOT change
> product code. The only edits allowed are throwaway diagnostics you clean up, and (if needed) `.env` dev flags.
>
> **Do NOT touch** provider-otp-login, MarineProvider BFF, provider-web, or CargoDry. **Never print secrets** (Keycloak
> client secrets, `OTP_LOGIN_*_SECRET`) — mask them in any output.

## Runtime facts (already inspected — use these, don't re-derive)
- Everything runs via `docker-compose.yaml`. Containers: **`identity-api`** (host port `7101:8080`),
  **`bff-adminpanel`** (host port `17001:8080`), plus `postgres`, `redis`, `rabbitmq`, `keycloak`.
- Container env is `ASPNETCORE_ENVIRONMENT=Development` (NOT `Local`) — so `appsettings.Local.json` is irrelevant here.
  The operative dev flags come from **`.env`** and are **already set correctly**:
  `PASSWORD_RECOVERY_DELIVERY_MODE=Logging`, `PASSWORD_RECOVERY_DEV_EXPOSE_OTP=true`,
  `PASSWORD_RECOVERY_LOG_LEVEL=Debug`, `OTP_LOGIN_TICKET_SECRET`/`OTP_LOGIN_CONSUME_SECRET` present.
  (If any of these is missing/wrong, fix it in `.env`, note it, and continue.)
- Admin login surface (anonymous): `bff-adminpanel` →
  `POST http://localhost:17001/api/v1/admin-panel/auth/otp-login/{request,verify,resend}` (`[AllowAnonymous]`).
  The BFF proxies to `identity-api` `POST /api/v1/identity/auth/admin-otp-login/{request,verify,resend}`.
- The OTP code is logged by the Identity notifier at **Debug**:
  `[DEV-ONLY] OTP login code for {maskedTarget}: {otp}` (category
  `Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin`).
- The seeded OTP-ready admin identifier is `Seed:Admin:Email` (default **`admin@inktavia.local`**). The seed assigns a
  **placeholder** `KeycloakSubjectId` unless `Seed:Admin:KeycloakSubjectId` is set — so login **will not fully
  complete** (handoff needs the real subject, that's Kickoff 2). This smoke only proves code-generation + ticket-mint.

## Steps

### 1. Rebuild + restart the changed services
Kickoff 1 added Identity code + a migration + a seed change, and the admin BFF slice. Rebuild and restart so they're
live (migration applies + seed runs on startup — `MockData.RunOnStartup=true`):
```bash
docker compose up -d --build identity-api bff-adminpanel
docker compose logs -f identity-api   # watch until "Application started" / seed completes, then Ctrl-C
```
Confirm the containers are healthy (`docker compose ps`).

### 2. Confirm the migration applied
The new table must exist in `inktavia_store`:
```bash
docker compose exec postgres psql -U aizen -d inktavia_store -c "\dt *admin_otp_login*"
docker compose exec postgres psql -U aizen -d inktavia_store -c "\d admin_otp_login_requests" | head -40
```
Also confirm the migration is recorded in `__EFMigrationsHistory` (name `20260729182024_AddAdminOtpLoginRequest`).

### 3. Confirm the seed made the admin OTP-ready
`admin@inktavia.local` must now have a **non-empty `KeycloakSubjectId`**, the **Admin role**, and (ideally) an
`WorkshopRoleContext.Admin` profile. Query it (adjust table/column names to the real schema — inspect if unsure):
```bash
docker compose exec postgres psql -U aizen -d inktavia_store -c \
"select id, email, (keycloak_subject_id is not null and keycloak_subject_id <> '') as has_sub from \"Users\" where email='admin@inktavia.local';"
```
Then confirm the Admin role assignment and (optional) Admin-context profile for that user. If `has_sub` is `false`,
the seed's OTP-ready update path did not run against the pre-existing admin row — **stop and report this**, because
without a subject id the request returns the synthetic response and no code is produced. (Fix direction: ensure the
seed updates the existing admin even under `SeedMode=InsertMissingOnly`.)

### 4. Trigger the OTP request (through the admin BFF, as the UI does)
```bash
curl -sS -X POST http://localhost:17001/api/v1/admin-panel/auth/otp-login/request \
  -H "Content-Type: application/json" \
  -d '{"channel":"email","identifier":"admin@inktavia.local"}' | tee /tmp/otp_request.json
```
Expect HTTP 200 with an enveloped body: `accepted:true`, a masked target, a `loginRequestId`, `otpLength`,
`expiresInSeconds`, `resendAfterSeconds`. Capture the `loginRequestId`.

### 5. Read the code from the Identity logs
```bash
docker compose logs --since 2m identity-api | grep -i "DEV-ONLY. OTP login code" | tail -5
```
Expect a line like `[DEV-ONLY] OTP login code for a***@inktavia.local: 123456`. Extract the 6-digit code.
- If you see `OTP login code published to message bus ... (Logging mode)` / `dispatched ... (Logging mode)` but **no
  `[DEV-ONLY]` line**, the Debug level isn't taking effect → confirm `PASSWORD_RECOVERY_LOG_LEVEL=Debug` in `.env` and
  that `identity-api` was recreated (not just restarted) so the env var is picked up; then repeat step 4.

### 6. Verify the code → expect a login ticket
```bash
curl -sS -X POST http://localhost:17001/api/v1/admin-panel/auth/otp-login/verify \
  -H "Content-Type: application/json" \
  -d '{"loginRequestId":"<from step 4>","otpCode":"<from step 5>"}' | tee /tmp/otp_verify.json
```
Expect `verified:true`, `nextAction:"redirect_to_keycloak_handoff"`, and a **non-empty `loginTicket`**.
(If `nextAction:"keycloak_handoff_required"` with no ticket, the ticket-mint failed — check that
`OTP_LOGIN_TICKET_SECRET` is set for `identity-api`; report it.)

### 7. Negative checks (anti-enumeration + brute-force guard)
- Unknown identifier → still `accepted:true` (synthetic), **no** `[DEV-ONLY]` log line, **no** new row in
  `admin_otp_login_requests`:
  ```bash
  curl -sS -X POST http://localhost:17001/api/v1/admin-panel/auth/otp-login/request \
    -H "Content-Type: application/json" -d '{"channel":"email","identifier":"nobody@nowhere.test"}'
  ```
- Wrong code on a fresh request → `verified:false`; a few repeats increment attempts and then invalidate at
  `MaxAttempts` (5).
- Row inspection:
  ```bash
  docker compose exec postgres psql -U aizen -d inktavia_store -c \
  "select login_request_id, channel, attempts, consumed_at_utc, otp_expires_at_utc from admin_otp_login_requests order by 1 desc limit 5;"
  ```

### 8. Regression: provider path untouched
Smoke the provider request still behaves (synthetic for an unknown provider identifier is fine — just confirm the
endpoint responds 200 and the provider tables/behavior are unchanged):
```bash
curl -sS -X POST http://localhost:17001/api/v1/admin-panel/auth/otp-login/request -H "Content-Type: application/json" -d '{"channel":"email","identifier":"admin@inktavia.local"}' >/dev/null && echo "admin ok"
```
Confirm `git status` shows no changes to provider-otp-login files, MarineProvider BFF, provider-web, or CargoDry.

## Report
Append a "Verification" section to `docs/V1.0.1/Identity/REPORT_BE_ADMIN_OTP_LOGIN.md` with: the migration/table
confirmation, the seed `has_sub` result, the **masked** request/verify transcripts (mask the OTP and the
loginTicket — show only that they were present), the negative-check outcomes, and a one-line status:
**PASS** (code appeared in logs + ticket minted) or **BLOCKED** (with the exact failing step). Clean up `/tmp/*.json`
and any diagnostic edits. Do not commit `.env`.
