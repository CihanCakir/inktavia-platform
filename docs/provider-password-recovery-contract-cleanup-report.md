# Provider Password Recovery — Contract / Layer Cleanup Report

Cleanup pass over the already-implemented Identity-backed refactor (no re-refactor, no frontend change, no EF
migration — no schema change was introduced). Not build-verified in this environment (no .NET SDK); the edits follow
the audited conventions and must be compiled by the .NET build.

## 1. Controller body command cleanup

Both controllers bound `[FromBody]` directly to CQRS **command** classes; both now bind **Request DTOs** and map to
commands inside the action (command types are no longer HTTP contracts):

- Identity `ProviderPasswordRecoveryController` (`api/v1/identity/auth/provider-password-recovery/*`) → binds
  `RequestProviderPasswordRecoveryRequest` / `VerifyProviderPasswordRecoveryOtpRequest` / `ResetProviderPasswordRequest`
  / `ResendProviderPasswordRecoveryOtpRequest` (Identity Abstraction DTOs).
- BFF `ProviderPasswordRecoveryController` (`api/v1/provider/auth/password/*`) → binds new BFF request DTOs
  (`ForgotProviderPasswordRequest` / `VerifyProviderPasswordOtpRequest` / `ResetProviderPasswordRequest` /
  `ResendProviderPasswordOtpRequest`). Public paths + wire shapes unchanged (frontend unaffected).

Command validators are unchanged — they still validate the mapped commands in the CQRS pipeline.

## 2. Request models added / where

- Identity request DTOs already existed in
  `Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Dto/PasswordRecovery/ProviderPasswordRecoveryDtos.cs`
  (reused; no new files).
- BFF request DTOs added:
  `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Contracts/Auth/Password/PasswordRecoveryRequests.cs`.

## 3. Result / response models moved out of service interface file

Yes. The 4 domain result classes (`PasswordRecoveryRequestResult`, `…VerifyResult`, `…ResetResult`, `…ResendResult`)
were removed from `IProviderPasswordRecoveryDomainService.cs` and moved to a dedicated file
`Modules/Identity/src/Aizen.Modules.Identity.Domain/Model/PasswordRecovery/PasswordRecoveryResults.cs`. The interface
file now contains only the interface (+ a `using` for the new Model namespace). `using` added to the domain-service
impl; the Application handlers use `var` and needed no change.

## 4. Domain reference to Abstraction vs domain-result mapping

**Domain does NOT reference Abstraction.** The domain service returns domain-internal result models
(`Domain/Model/PasswordRecovery`); the **Application handlers** map those to the cross-boundary Abstraction response
DTOs (`Identity.Abstraction/Dto/PasswordRecovery`). This preserves the dependency direction
(Domain → nothing; Application → Domain + Abstraction) with no circular reference.

## 5. IProviderIdentityRemoteCall DTO usage

Already correct — the BFF remote-call methods (`RequestProviderPasswordRecovery`, `VerifyProviderPasswordRecoveryOtp`,
`ResetProviderPassword`, `ResendProviderPasswordRecoveryOtp`) use the **Identity Abstraction** request/response DTOs
(`Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery`), via `[AizenRemoteCallBody]` + the service token injected
by `MarineProviderBffAuthDelegatingHandler`. No BFF command classes or anonymous models. No change needed.

## 6. Notification / message-bus integration status

**Gap — deferred (not a cleanup item).** The Notification module is MassTransit-based (event consumers) but has **no
email/SMS OTP delivery path** (only FCM push, itself stubbed via `FcmSenderStub`). Wiring OTP delivery is a new
integration (new bus message + consumer + email/SMS provider), out of scope here. `LoggingProviderPasswordRecoveryNotifier`
remains the Identity-side stub: logs delivery **intent only** by default, and logs the code at Debug **only** when
`PasswordRecovery:DevExposeOtp=true` (local dev). Production is blocked on this integration.

## 7. Can raw OTP reach the BFF?

**No.** The Identity `RequestProviderPasswordRecoveryResponse` (and all recovery responses) contain no OTP field; the
OTP exists only inside Identity (generated + hashed + handed to the notifier). The BFF receives only
accepted/verified/reset + masked target + reset token + timings. The BFF never receives, logs, persists, or sends the
raw OTP.

## 8. Identity appsettings changes

`Modules/Identity/src/Aizen.Modules.Identity/configuration/`:
- `appsettings.json` (base): added `PasswordRecovery` (`OtpLength`, `OtpTtlSeconds`, `ResendCooldownSeconds`,
  `MaxAttempts`, `ResetTokenTtlSeconds`, `DevExposeOtp:false`) and `IdentityKeycloak`
  (`Enabled:false`, empty `BaseUrl`/`AdminClientSecret`, `Realm`, `AdminClientId`, `RevokeSessionsOnSuspend`) as
  env-driven placeholders — no secrets committed.
- `appsettings.Local.json`: `PasswordRecovery.DevExposeOtp: true` (local dev only).

Field names match the actual Identity `PasswordRecoveryOptions` (`OtpTtlSeconds` etc.), not generic names.

## 9. MarineProvider BFF appsettings changes

Removed the **orphaned** `PasswordRecovery` block from
`Bff/src/MarineProvider/Aizen.Bff.MarineProvider/configuration/appsettings.json` (the BFF-side `PasswordRecoveryOptions`
class was deleted in the refactor; zero C# references remain). No OTP/TTL/hash/DevExposeOtp config lives in the BFF
anymore — single ownership in Identity. Remote-call + Keycloak service-account config unchanged.

## 10. Production config safety

- `DevExposeOtp` defaults to **false** in Identity base config; only the **Local** file sets it true.
- No secrets committed: `IdentityKeycloak:AdminClientSecret` is empty in files; supply via env
  (`IdentityKeycloak__AdminClientSecret`). Keycloak reset requires `IdentityKeycloak__Enabled=true` +
  BaseUrl/secret at runtime (fails closed otherwise).
- BFF remote-call base URLs come from env (`__FROM_ENV__`).
- **Action for prod deploy:** set `IdentityKeycloak__*` (with a client holding `manage-users`) and confirm
  `PasswordRecovery__DevExposeOtp` is unset/false. Delivery still requires the Notification integration.

## 11. Build results

**Not run here (no .NET SDK).** Edits are surgical and convention-following. Run the 7-project build to verify:
```
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/...
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/...
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/...
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/...
dotnet build Modules/Identity/src/Aizen.Modules.Identity/...
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/...
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/...
```
Frontend untouched — no `npm run build/lint` needed. JSON of all three edited appsettings validated.

## 12. Remaining gaps

Notification (email/SMS) OTP delivery; per-identifier/IP rate limiting; the EF migration
(`AddProviderPasswordRecoveryRequest`) + runtime smoke (see
`docs/prompts/provider-password-recovery-migration-and-smoke-claude-code-prompt.md`). Not production-ready.

## 13. Recommended next step

Build the 7 projects to confirm 0 errors, then run the migration + smoke prompt. Afterwards, implement the
Notification email/SMS OTP delivery (new bus message + consumer + provider) to close the last production blocker.
