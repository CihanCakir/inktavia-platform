# Admin Auth Modernizasyonu — AdminPanel BFF + admin-web'i Provider (Keycloak RS256 / OTP) pattern'ine hizala

> **Geliştirme borcu:** AdminPanel tarafı **eski "köprü"** auth kullanıyor (Identity modülünün HS256 shared-secret token'ı,
> inline `SymmetricSecurityKey`). En güncel + korunaklı yapı **MarineProvider BFF ↔ inktavia-marine-provider-web ↔ Keycloak
> (RS256, OTP login)**. Bu doküman admin tarafını o pattern'e hizalar. **Kök sorun aynı zamanda Wave-2/4 admin runtime'ını
> bloke eden `KEYCLOAK_BFF_CLIENT_SECRET`/zero-length-key 500 faultudur** — Faz A onu çözer.

## 1. Hedef pattern (Provider — kodda doğrulandı, aynen aynalanacak)
- **BFF inbound auth:** `Bff/src/MarineProvider/.../Extensions/AuthenticationExtensions.cs :
  AddMarineProviderAuthentication(config)` → **Keycloak full IdP, RS256**: `options.Authority` = `{BaseUrl}/realms/{Realm}`
  (veya `MetadataAddress`), `RequireHttpsMetadata` config, `TokenValidationParameters` (ValidIssuer=authority,
  ValidateAudience=audience `ProviderPortalBffClientId`), **JWKS ile RS256 doğrulama** (SymmetricSecurityKey YOK),
  `OnTokenValidated = MapKeycloakRealmRoles` → Keycloak `realm_access.roles` (nested JSON) → `ClaimTypes.Role`.
  Config bloğu: `MarineProviderKeycloak:{BaseUrl,Realm,Authority,Audience,ProviderPortalBffClientId,MetadataAddress,RequireHttpsMetadata}`.
- **Login akışı:** `Controllers/V1/Auth/OtpLoginController` (`/api/v1/provider/auth/otp-login` → `request`/`verify`/`resend`)
  + `AuthController` + `PasswordRecoveryController`. Handler'lar: `RequestOtpLogin`/`VerifyOtpLogin`/`ResendOtpLogin`,
  register, password reset (OTP). **OTP → Keycloak token.**
- **FE:** `inktavia-marine-provider-web/src/features/auth` (`otpLoginApi` request/verify/resend, `useAuth`, `AuthGate`,
  `otpLoginTypes/Storage`, `LoginPage`) — token saklama + `AuthGate` guard.

## 2. Mevcut admin durumu (kodda doğrulandı — değiştirilecek)
- **BFF inbound auth (ESKİ):** `Bff/src/AdminPanel/.../Program.cs` inline `AddJwtBearer` →
  `IssuerSigningKey = new SymmetricSecurityKey(UTF8.GetBytes(config["TokenOption:SecurityKey"] ?? ""))` (**HS256**,
  Identity modülü token'ı), `TokenOption:{Issuer,Audience,SecurityKey}`, user token `X-Aizen-User-Token` header'dan
  (Authorization = BFF→module Keycloak **service token**, `KeycloakServiceTokenOptions.ClientSecret` +
  `AdminPanelBffKeycloakServiceTokenProvider`). **SecurityKey/secret boş → zero-length key → HER admin isteği 500.**
- **Login akışı (ESKİ):** `Controllers/V1/AuthController` (`/api/v1/admin-panel/auth`) → `login/username`, `login/phone`,
  `login/otp`, `otp/send`, `otp/check`, `refresh`, `password/change`; handler'lar `LoginWithUsername/Phone/Otp`,
  `SendOtp`/`CheckOtp` → **Identity modülünün HS256 token'ını** üretiyor (Keycloak değil).
- **admin-web FE:** `src/shared/auth/{keycloakClient,authService,authStore,tokenProvider,jwtUtils,permissions,roles,deviceId}`
  + `features/auth/{AuthGate,useAuth,authApi}` + `pages/public/{LoginPage,AuthCallbackPage}` + route `LOGIN`+`OTP`.
  `authHeaders.test.ts`: "identity mode" → `X-Aizen-User-Token: Bearer <id-token>`. **Geçiş halinde:** hem keycloakClient
  hem identity-token makinesi var; login hâlâ eski Identity akışı.

## 3. Migration — güvenli fazlar

### Faz A — 🔴 BFF inbound auth: HS256 → Keycloak RS256 (500'ü çözer, kök borç)
- `Bff/src/AdminPanel/.../Extensions/AuthenticationExtensions.cs` **oluştur**: `AddAdminPanelAuthentication(config)` —
  provider extension'ının **birebir kopyası**, config prefix `AdminPanelKeycloak:*` (BaseUrl/Realm/Authority/Audience/
  `AdminPanelBffClientId`/MetadataAddress/RequireHttpsMetadata), `MapKeycloakRealmRoles` (realm_access.roles→Role).
  `X-Aizen-User-Token` header'dan okuma (`OnMessageReceived`) korunur (BFF pattern; Authorization = service token).
- `Program.cs`'teki inline `AddJwtBearer` + `SymmetricSecurityKey` **kaldırılır** → `.AddAdminPanelAuthentication(config)`.
  `AddAuthorization("AdminPanelAccess")` policy'si korunur ama artık Keycloak `Admin` realm-role claim'ine bakar (provider
  policy'siyle aynı mantık).
- Config: `appsettings*.json`'a `AdminPanelKeycloak:*` (provider'daki `MarineProviderKeycloak:*` şablonu). **Keycloak'ta
  admin-panel-bff client'ı** (audience) + realm — kullanıcı sağlar. `TokenOption:*` HS256 kaldırılır/legacy.
- **Sonuç:** boş-secret 500 gider; admin, provider'la aynı RS256 Keycloak token doğrulamasını yapar; Wave-2/4 admin
  endpoint'leri runtime doğrulanabilir hale gelir.

### Faz B — BFF login/OTP: Keycloak-backed OTP akışı (provider'la aynı)
- Admin login handler'ları (`LoginWithOtp` vb.) **Keycloak token** üretmeli (Identity HS256 değil) — provider'ın OTP→Keycloak
  akışını izle (`VerifyOtpLogin` handler'ı referans). Controller yüzeyi `login/otp`+`otp/send/check`+`refresh` korunabilir
  ama arkada **Keycloak token exchange**. Register/password-reset provider pattern'iyle hizalanır.
- Not: Identity modülü admin kullanıcılarını Keycloak realm'inde `Admin` rolüyle temsil etmeli (realm_access.roles). Bu,
  Identity ↔ Keycloak eşlemesi — mevcut provider realm eşlemesini referans al.

### Faz C — admin-web FE: provider-web login/OTP pattern'i
- `admin-web/src/features/auth` + `shared/auth`'u provider-web `features/auth`'a hizala: `otpLoginApi` (request/verify/resend),
  `useAuth`, `AuthGate`, OTP page akışı; token saklama; `X-Aizen-User-Token` gönderimi korunur. Mevcut `keycloakClient`/
  `authStore` iskeleti buna bağlanır; eski Identity-login ekranı OTP akışıyla değiştirilir. Provider-web'in `LoginPage`/
  `otpLoginTypes` referans.

### Faz D — Keycloak config + doğrulama
- Keycloak realm'inde **admin-panel-bff client** (+ audience mapper, realm-role `Admin`), `AdminPanelKeycloak:*` config,
  `KEYCLOAK_BFF_CLIENT_SECRET` (service token için) doğru set. Uçtan uca: admin-web OTP login → Keycloak RS256 token →
  admin BFF doğrular (401/403 doğru) → module service-token ile çağrı.

## 4. Sıra + risk
- **Faz A önce** (self-contained, en yüksek değer: 500'ü çözer + validation'ı hizalar; login akışı henüz Identity token
  üretse bile RS256'ya geçince Keycloak token gerekir → Faz A+B birlikte anlamlı ama A altyapıyı kurar). Güvenlik-kritik:
  auth extension'ı provider'dan **birebir** kopyalanır, icat edilmez.
- **Faz B** login akışını Keycloak'a taşır (Identity↔Keycloak eşlemesi gerektirir).
- **Faz C** FE'yi hizalar (provider-web referans).
- **Faz D** Keycloak client + config + uçtan uca smoke.
- **BFF-Wave 5 (offer economics + P12 reporting) bu borçtan SONRA.** Sonra FE dalgaları.

## 5. Kabul kriteri
- Admin BFF, provider'la **aynı Keycloak RS256** inbound auth'unu kullanır (SymmetricSecurityKey kaldırıldı); `AdminPanelAccess`
  Keycloak `Admin` realm-role'üne bakar; boş-secret 500 gider. Login OTP akışı Keycloak-backed. admin-web login/OTP provider-web
  pattern'inde. Mevcut admin endpoint'leri (Wave 2/4 dahil) uçtan uca çalışır. Provider tarafı **hiç değişmez** (yalnız referans).

> **İlk icra: Faz A** (admin BFF Keycloak RS256 auth extension, provider'dan birebir). Kullanıcı Keycloak admin-panel-bff
> client + realm bilgisini sağlamalı (config). Sonra Faz B→D.
