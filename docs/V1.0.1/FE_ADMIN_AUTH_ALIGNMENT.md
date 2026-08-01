# FE_ADMIN Auth Alignment (Faz C) — admin-web login/OTP'yi provider-web (BFF-aracılı Keycloak OTP) pattern'ine hizala

> **Repo:** `inktavia-marine-admin-web`. **Bağımlılık:** Admin BFF Faz A+B (`ADMIN_AUTH_ALIGNMENT.md`) — bu spec **A+B
> REPORT_BFF.md'deki gerçek admin auth endpoint'leri + token şekli + header sözleşmesine** dayanır (varsayım değil).
> **Hedef pattern (provider-web, aynen aynalanır):** BFF-aracılı **passwordless OTP** (`otpLoginApi` request/verify/resend →
> BFF → Keycloak token; **browser redirect YOK**), `authInterceptors` token'ı ekler, `AuthGate` guard.
> **Provider-web'e HİÇ dokunulmaz** (referans).

## 1. Hedef (provider-web — kodda doğrulandı)
- `src/features/auth/api/otpLoginApi.ts`: `request({identifier, channel})` → `{loginRequestId, resendAfterSeconds}` ·
  `verify({loginRequestId, otpCode})` → token/`nextAction` · `resend({loginRequestId})`. **`publicHttpClient`** (auth
  interceptor'sız) ile `endpoints.auth.otpLogin.{request,verify,resend}`.
- `src/shared/auth/authInterceptors.ts`: authenticated `httpClient`'a token ekler; `publicHttpClient` eklemez.
- `useAuth`, `AuthGate`, `otpLoginTypes/Storage`, `LoginPage` — OTP akışı + token saklama + guard.

## 2. Mevcut admin-web (değişecek — iskelet var, akış eski)
- `src/features/auth/api/authApi.ts`: **username+PIN → OTP** akışı; `BffTokenDto{accessToken, refreshToken, *ExpiredDate}`;
  `LoginWithUsernameBody = LoginSuccessBody | OtpRequiredBody` (`isOtpRequired`); login username/phone/otp + sendOtp/checkOtp.
- `src/shared/auth/authStore.ts` (zustand + persist): `identityToken/identityRefreshToken/identityExpiresAt` + `user`;
  `setSession(...)`. **identity token** saklıyor.
- `src/pages/public/LoginPage.tsx`: 2-stage `credentials`→`otp`, `authService.loginWithCredentials(username, pin)` →
  `requiresOtp/sessionId` → OTP stage.
- Route: `LOGIN`, **`AUTH_CALLBACK`** (redirect kalıntısı), `OTP`. i18n `auth.json` var. `shared/auth/{keycloakClient,
  authService,tokenProvider,jwtUtils,permissions,roles,deviceId}` — geçiş iskeleti.
- Header: `authHeaders.test.ts` → identity mode `X-Aizen-User-Token: Bearer <id-token>`.

## 3. Hizalama (A+B raporuna göre kesinleşir)
1. **authApi → OTP-login pattern'i:** admin BFF'in modernize auth endpoint'lerine bağla (A+B raporundan; muhtemelen
   `/api/v1/admin-panel/auth/otp-login request/verify/resend` VEYA mevcut `login/otp`+`otp/send/check` yüzeyi ama
   **Keycloak token** dönen). `publicHttpClient` ile çağır (auth interceptor'sız). `BffTokenDto` → Keycloak access/refresh
   token şekline hizala (A+B token DTO'su ne ise).
2. **authStore → Keycloak token:** `identityToken` yerine (veya yanında) **Keycloak access token** sakla; `setSession`
   Keycloak token + expiry. Persist aynı kalır.
3. **Header sözleşmesi:** A+B'nin admin BFF'i inbound token'ı **nereden okuyorsa** ona hizala — provider `Authorization:
   Bearer` (default); admin A+B `X-Aizen-User-Token` OnMessageReceived'i koruyorsa o header. **A+B raporundaki header'ı
   kullan** (authInterceptors'ı ona göre ayarla). `authHeaders.test.ts` güncellenir.
4. **LoginPage → provider OTP UX:** provider-web `LoginPage` + `otpLoginTypes` referansıyla; 2-stage yerine passwordless OTP
   (identifier→OTP→verify) VEYA A+B'nin desteklediği akış. `resendAfterSeconds` sayaç, `nextAction` handling.
5. **Redirect kalıntısı:** BFF-aracılı OTP redirect gerektirmez → `AuthCallbackPage` + `keycloakClient` **legacy** (kaldır
   veya devre dışı; kaldırmadan önce başka kullanan var mı kontrol et). `AUTH_CALLBACK` route'u temizlenir.
6. **AuthGate/permissions:** Keycloak `Admin` realm-role claim'ine göre (A+B policy'siyle simetrik); `roles.ts/permissions.ts`
   Keycloak realm_access.roles'e hizalanır.

## 4. Bozmama / geçiş yaklaşımı
- Auth bir **cutover**'dır (additive değil) — ama güvenli geçiş için: yeni OTP-login akışını kur, eski credentials akışını
  **feature-flag/branch arkasında** tut, A+B BFF canlıyken **tek noktadan cutover**. Cutover öncesi eski akış çalışır kalır.
- `shared/auth` iskeleti (authStore/authService/tokenProvider) **yeniden kullanılır**, yeniden yazılmaz; yalnız token
  kaynağı Keycloak'a döner. Admin design system (Stitch) + mevcut LoginPage layout korunur; yalnız akış/alanlar değişir.
- i18n `auth.json` (tr+en) OTP anahtarlarıyla genişletilir (provider `otpLogin` anahtarları referans).

## 5. Kabul kriteri
- admin-web login = **BFF-aracılı Keycloak OTP** (provider-web pattern'i); `authStore` Keycloak token saklar; header A+B BFF
  sözleşmesiyle eşleşir; `AuthGate` Keycloak `Admin` realm-role'üne bakar; redirect/keycloakClient kalıntısı temizlenir;
  eski credentials akışı cutover'da kaldırılır. typecheck/build 0 hata; `authHeaders.test` güncel; provider-web dokunulmadı.

## 6. Ön koşul + sıra
1. **Faz A+B (BFF) REPORT_BFF.md** — gerçek admin auth endpoint'leri + token DTO + inbound header. Bu spec ona hizalanır.
2. Faz C (bu spec) uygulanır.
3. **Faz D:** Keycloak `inktavia-realm` admin client (`admin-panel-web` public + `admin-panel-bff` confidential secret) +
   uçtan uca smoke (admin-web OTP login → Keycloak RS256 token → admin BFF 200; non-admin → 403).

> **Not:** Header (`Authorization: Bearer` vs `X-Aizen-User-Token`) ve endpoint adları **A+B raporundan** kesinleşecek;
> bu spec o rapor gelince tek geçişte netleştirilir. admin-web `shared/auth` iskeleti + Stitch layout korunur.
