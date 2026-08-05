# BE_M2g — Phone password-login (resolve phone → user before ROPC)

**Date:** 2026-08-06 · **Repo:** `addesso-project` · **Scope:** mobile BFF + Identity (OTP resolver reuse). Build 0 errors, redeployed, HTTP-verified.

## Problem
On-device QA opened phone password-login on the client, but the mobile BFF `LoginParticipantCommandHandler`
ran a raw Keycloak ROPC with `username = <identifier>`. Keycloak's username is the **email**, and the phone
is never written to Keycloak (register stores it only on the Identity participant profile as `ContactPhone`).
So `POST /login { email: "+905551002026", … }` → ROPC username `+90…` → no match → **401**.

## Fix (reuse the OTP-login resolver — no duplicate lookup logic)
When the login identifier is a **phone** (same `'@'`→email / else→phone rule the OTP endpoint uses), resolve
it to the participant's canonical **email (= Keycloak username)** via Identity *before* the ROPC. The resolver
is the **exact same lookup + active-Participant gate** OTP-login already uses — extracted into a shared method
so both paths run identical code (normalize phone → `PhoneNumber` match → `GetActiveProfileIdAsync(Participant)`).
No new resolver written; no OTP is sent on this path.

### Flow
```
POST /api/v1/mobile/auth/login { email:"+905551002026", password }   (BFF)
  └─ identifier has no '@' → channel=phone
     └─ IIdentityRemoteCall.ResolveParticipantIdentifier { channel:"phone", identifier:"+905551002026" }
          → Identity POST /api/v1/identity/auth/participant-otp-login/resolve-identifier  (IdentityWrite, S2S)
             └─ ParticipantOtpLoginDomainService.ResolveByIdentifierAsync
                  └─ LookupActiveParticipantAsync  ← SAME method OTP-login's RequestAsync now calls
                       NormalizePhone("+905551002026") → PhoneNumber match → Participant gate
                  → { Found:true, Email:"qa.owner.aug5@inktavia.com", KeycloakSubjectId }
     └─ ropcUsername = resolved email
  └─ VerifyPasswordGetSubAsync(email, password)  → sub   (unchanged ROPC)
  └─ MintParticipantLoginTicket(sub) → ParticipantSessionHandoff → inktavia-mobile tokens   (unchanged handoff)
```
Email identifiers skip the resolve step entirely → **existing behavior unchanged**. Unresolved phone → the
existing `null` → invalid-credentials → 401 (anti-enumeration; not a 500). The phone is never logged in plain text.

## Changed files
**Identity (resolver reuse — shared lookup, new read-only S2S endpoint):**
- `…Domain/Model/OtpLogin/OtpLoginResults.cs` — new `ParticipantIdentifierResolution` model.
- `…Domain/Interface/Service/IParticipantOtpLoginDomainService.cs` — new `ResolveByIdentifierAsync`.
- `…Repository/Service/OtpLogin/ParticipantOtpLoginDomainService.cs` — extracted **`LookupActiveParticipantAsync`**
  (normalize + email/phone lookup + Participant gate); `RequestAsync` now calls it (behaviour identical);
  new `ResolveByIdentifierAsync` returns email/subject, **no OTP, no rate-limit side effect**.
- `…Abstraction/Dto/OtpLogin/ParticipantMintTicketDtos.cs` — `ResolveParticipantIdentifierRequest/Response`.
- `…Controller/V1/Identity/ParticipantOtpLoginController.cs` — `POST resolve-identifier` (mirrors `mint-ticket`:
  IdentityWrite, direct domain-service call).

**Mobile BFF:**
- `…Common/RemoteClients/IIdentityRemoteCall.cs` — `ResolveParticipantIdentifier` remote call.
- `…Auth/Command/LoginParticipant/LoginParticipantCommandHandler.cs` — phone → resolve → ROPC-with-email;
  email path unchanged.

## Verification (redeployed images; `http://localhost:17003`, mock off; user `qa.owner.aug5@inktavia.com`
/ phone `+905551002026` / password `QaReset2026!!`)

`POST /api/v1/mobile/auth/login` (token masked to prefix + length):

| Case | identifier | HTTP | token |
|---|---|---|---|
| email (regression) | `qa.owner.aug5@inktavia.com` | **200** | `eyJhbGciOiJSUz…` len 1749 |
| phone +90 E.164 | `+905551002026` | **200** | `eyJhbGciOiJSUz…` len 1749 |
| phone bare 10-digit | `5551002026` | **200** | `eyJhbGciOiJSUz…` len 1749 |
| phone 0-prefixed | `05551002026` | **200** | `eyJhbGciOiJSUz…` len 1749 |
| unknown phone | `+905559999999` | **401** | none |
| correct phone, wrong pw | `+905551002026` | **401** | none |
| unknown email | `nobody@inktavia.com` | **401** | none |

Phone-login token claims (decoded) — identical shape to email login:
```
aud  ⊇ marine-mobile-bff        azp  = inktavia-mobile
realm_access.roles ⊇ mobile_user
name = "QA Owner Aug5"          preferred_username = qa.owner.aug5@inktavia.com
```
Phone variants (`5551002026`, `05551002026`) normalize to the same `+90…` via the OTP resolver's `NormalizePhone`
→ same participant → 200. Unknown phone and wrong-password both yield a clean **401**, no 500.

## Scope (git status — M2g only)
```
 M Bff/.../Auth/Command/LoginParticipant/LoginParticipantCommandHandler.cs
 M Bff/.../Common/RemoteClients/IIdentityRemoteCall.cs
 M Modules/Identity/.../Abstraction/Dto/OtpLogin/ParticipantMintTicketDtos.cs
 M Modules/Identity/.../Domain/Interface/Service/IParticipantOtpLoginDomainService.cs
 M Modules/Identity/.../Domain/Model/OtpLogin/OtpLoginResults.cs
 M Modules/Identity/.../Repository/Service/OtpLogin/ParticipantOtpLoginDomainService.cs
 M Modules/Identity/.../Controller/V1/Identity/ParticipantOtpLoginController.cs
```
Only the mobile BFF + the Identity OTP-login resolver were touched. Provider/Admin and the unrelated
in-flight Payment/ServiceRequest/Travel-pricing parallel work were **not** modified. Not committed.
