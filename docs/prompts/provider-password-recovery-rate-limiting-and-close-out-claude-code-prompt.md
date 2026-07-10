# Claude Code Prompt — Provider Password Recovery: Rate Limiting + Feature Close-Out

Final piece to close the provider password recovery feature: add abuse protection (IP-based at the BFF edge +
per-identifier at Identity) and complete the production checklist. Do not change the frontend, the public BFF
contract, or any recovery security control. Keep all responses non-enumerating.

## Architecture
```
Browser → BFF /provider/auth/password/*   → [IP rate limit — ASP.NET rate limiter, 429 on abuse]
        → Identity /provider-password-recovery/request|resend  → [per-identifier limit — IAizenDistributedCache]
```
Two independent layers: the BFF edge caps volumetric abuse from one client (real client IP); Identity caps abuse
targeting a specific account regardless of IP rotation. The existing resend cooldown + OTP `MaxAttempts` stay.

## Part 1 — BFF edge IP rate limiting (mirror the CargoDry pattern)
Reference: `Modules/CargoDry/src/Aizen.Modules.CargoDry/Program.cs` (`AddRateLimiter` + `UseRateLimiter`) and
`CargoDryPublicController` (`[EnableRateLimiting("validate-ip")]`).
- In `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Program.cs` add `builder.Services.AddRateLimiter(...)` with a
  policy `pwd-recovery-ip`: partition by client IP (`httpContext.Connection.RemoteIpAddress`), fixed or sliding
  window, e.g. **10 requests / 5 minutes** per IP, `QueueLimit = 0`, rejection status **429**. Add
  `app.UseRateLimiter()` in the pipeline (after routing, before endpoints — match CargoDry ordering).
- Ensure the real client IP is used behind the gateway: configure `ForwardedHeaders`
  (`ForwardedHeaders.XForwardedFor`) so `RemoteIpAddress` reflects the browser, not the gateway, or partition on the
  `X-Forwarded-For` leftmost hop. Restrict `KnownProxies`/`KnownNetworks` to the gateway.
- Apply `[EnableRateLimiting("pwd-recovery-ip")]` on the BFF `ProviderPasswordRecoveryController` (covers all four
  endpoints). The 429 body should be generic (no account info).
- Make the limits configurable via a small options section (e.g. `RateLimiting:PasswordRecovery:PermitPerWindow`,
  `WindowSeconds`) with the above defaults; values from config/env.

## Part 2 — Identity per-identifier throttle (IAizenDistributedCache)
Reference: `IAizenDistributedCache` usage in `OAuthProviderClient` / the participant external-login handlers.
- In `ProviderPasswordRecoveryDomainService.RequestAsync` and `ResendAsync`, before generating/dispatching an OTP,
  increment a Redis counter keyed by a **hash of the normalized identifier** (never the raw email/phone), e.g.
  `provider:pwreset:rl:{sha256(normalizedIdentifier)}`, with a sliding/fixed window (e.g. **5 requests / hour**).
- If the limit is exceeded: **return the same generic accepted response** (masked target, timings) but **do not**
  persist a new recovery row or dispatch an OTP. This stays non-enumerating and caps targeted abuse.
- Add the limit to `PasswordRecoveryOptions` (`MaxRequestsPerIdentifierPerWindow`, `IdentifierWindowSeconds`) with
  safe defaults; bind from config/env. Reuse the existing `PasswordRecoverySecurity` hashing for the key.
- Counter failures (cache down) must **fail open to the normal flow** (do not block legitimate resets on a cache
  outage) — log a warning.

## Part 3 — Config
- BFF `appsettings.json` + docker-compose: `RateLimiting__PasswordRecovery__PermitPerWindow`, `__WindowSeconds`
  (defaults 10 / 300). Identity: `PasswordRecovery__MaxRequestsPerIdentifierPerWindow`, `__IdentifierWindowSeconds`
  (defaults 5 / 3600). No secrets.

## Part 4 — Build + test
```
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/... and .../Aizen.Bff.MarineProvider/...
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/... and .../Aizen.Modules.Identity/...
```
Runtime:
- Hammer `POST /provider/auth/password/forgot` from one IP past the window → **429** after the limit; other IPs
  unaffected.
- Repeat forgot for the **same identifier** from rotating IPs past the identifier window → still generic accepted,
  but **no new recovery row / no OTP dispatched** after the limit (verify in DB + logs).
- Confirm a normal single reset still completes end-to-end (forgot → email OTP → verify → reset → Keycloak).

## Part 5 — Production close-out checklist (document in the report)
Update `docs/provider-password-recovery-identity-backed-refactor-report.md` §12 to **production-ready once the
following ops items are set** (they are deploy/config, not code):
```
[ ] Real SMTP provider configured on notification-api (Email__* env); email OTP smoke-tested in the target env
[ ] PasswordRecovery__DevExposeOtp = false (and PASSWORD_RECOVERY_LOG_LEVEL not Debug) in shared/test/prod
[ ] IdentityKeycloak admin client has realm-management manage-users + view-users; secret from secret store
[ ] Rate limiting enabled (this part) with tuned limits; gateway forwards real client IP
[ ] (Optional) ISmsSender implemented if phone-channel recovery is offered (template already seeded)
```
Flip the report status to "production-ready pending the ops checklist above" and mark the **code** work complete.

## Guardrails
- No frontend / BFF public-contract change. Responses stay non-enumerating (429 is generic; identifier throttle
  degrades to generic accepted).
- Per-identifier key is a hash — never store/log the raw identifier. Fail open on cache outage.
- Keep OTP off the BFF/browser and redacted at rest (unchanged).
```
