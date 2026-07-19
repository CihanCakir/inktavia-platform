# Phase 7 — final reorg pass (Phone + confirmation sweep)

The remaining controllers (Me, Auth, OtpLogin, PasswordRecovery, Files, Onboarding) are **already gold standard** —
typed `AizenApiResponse<T>` + `[ProducesResponseType]` on every endpoint, no `IActionResult`, no inline DTOs. Their
Application handlers are already under `{Feature}/Command|Query/{Op}/` from Phase 1C. This phase closes the last gap
and does a confirmation sweep. Small, mechanical, no behaviour change.

## 1. Phone — insert the missing `Command/` level
`Phone/` is still flat: `Phone/SendProviderPhoneOtp/` and `Phone/VerifyProviderPhoneOtp/` sit directly under the
feature (unlike every other feature which has `Command/`/`Query/`). Move both operations (both are Commands —
`*Command.cs`, `*CommandHandler.cs`, `*CommandValidator.cs`) into:
```
Phone/Command/SendProviderPhoneOtp/
Phone/Command/VerifyProviderPhoneOtp/
```
Namespace stays `Aizen.Bff.MarineProvider.Application.Phone`. Update any `using`/references. (The `MeController`
phone endpoints already import these — confirm they still resolve.)

## 2. Confirmation sweep (fix only if something is off)
Across `Me`, `Auth`, `Files`, `Onboarding`, `Phone`:
- **One class per file:** each operation folder has separate `*Query.cs`/`*Command.cs`, `*Handler.cs`, and
  `*Validator.cs` — no message+handler crammed together. (These predate the Phase-2 cramming issue and are expected
  to be clean; fix any that aren't.)
- **Validators present** for validatable commands (register, password reset/forgot/verify/resend, otp-login
  request/verify/resend, onboarding save/submit/attach, phone send/verify, file complete/create). Add any missing.
- **No response/request DTO declared inline** in the BFF Application — any such type moves to the relevant module
  Abstraction and is referenced. (Controllers already have none; check the handlers/messages.)
- **Controllers unchanged** — they're already typed + PRT; do not touch them beyond fixing a broken `using` if the
  Phone move requires it.

## Acceptance
- `Phone/Command/{SendProviderPhoneOtp,VerifyProviderPhoneOtp}/` exist; nothing left flat directly under `Phone/`.
- Every provider BFF feature now follows `{Feature}/Command|Query/{Op}/` uniformly; one class per file; validators
  present; no inline DTOs anywhere in `Controllers/**` or the BFF Application.
- Builds. After rebuild: login (password + Google + OTP), password recovery, register, onboarding save/submit/docs,
  me/profile/status, phone send/verify OTP, and file upload session/complete all behave exactly as before.

## Report
Append to `REPORT_BACKEND.md` ("Phase 7"): moved Phone under `Phone/Command/**`; confirmed Me/Auth/Files/Onboarding
already gold standard (typed + PRT, one-class-per-file, no inline DTOs). MarineProvider BFF refactor complete —
every controller prefix-free + typed + PRT, every operation under `{Feature}/Command|Query/{Op}/`, all remote-calls
in handlers, DTOs from module Abstractions.
