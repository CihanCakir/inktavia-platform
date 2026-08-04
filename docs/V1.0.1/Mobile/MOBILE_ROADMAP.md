# Mobile (Participant / Owner) BFF — V1.0.1 Roadmap

> Mobil uygulama (Owner/Participant) yüzeyinin backend-for-frontend katmanı. **Kanonik referanslar:** BFF deseni için `Bff/src/MarineProvider` (birebir mirror, **yeniden yazılmaz**, genişletilir); istemci sözleşmesi için `inktavia-marine-mobile` (`src/core/api/endpoints.ts` + `src/core/auth/*` + `docs/API_CLIENT_GUIDE.md`, `docs/MOBILE_ARCHITECTURE.md`). Keycloak/altyapı için `docker-compose.yaml` + `infrastructure/keycloak/`. Her faz → ayrı `BE_M<n>_*.md` backend promptu; her faz raporu → `REPORT_BE_M<n>_*.md`. Kural: **additive, envelope korunur, MarineProvider/AdminPanel/provider-web/CargoDry byte-for-byte dokunulmaz.**

Bu BFF, `Aizen.Bff.Marine.Participant.Mobile` (Web host) + `.Application` ikilisidir (net9.0). Mobil istemci **Keycloak ile doğrudan konuşmaz** (`keycloak-js` yok); tüm auth ve modül erişimi bu BFF üzerinden brokerlanır. İstemci token'ı `Authorization: Bearer` (+ dökümante edilen `X-Aizen-User-Token`) ile taşır, envelope olarak `ApiResponse<T>` / `PagedResponse<T>` bekler.

---

## Backend fazları

| Faz | Kapsam | Referans / mirror | Bağımlılık |
|---|---|---|---|
| **M1** | **Foundation.** Framework bootstrap (`AizenApplicationBuilder`, `Aizen.Core.Starter.Bff`), `MarineMobileKeycloak` options + inbound JWT doğrulama (`inktavia-realm`, audience `marine-mobile-bff`), participant authorization policy'leri, RemoteCall plumbing + BFF-assertion delegating handler + Keycloak service-token, config (`configuration/appsettings*`), `docker-compose` `bff-marine-mobile` servisi, csproj/Dockerfile düzeltmeleri, `GET /api/v1/mobile/me` (claims) + health. | `MarineProvider` Program.cs/DI/Auth deseni | — (pilot) |
| **M2** | **Auth surface.** `/mobile/auth/otp/send`+`/verify` (Keycloak OTP-login SPI + identity ticket), `/mobile/auth/google`, `/mobile/auth/apple` (IdP brokering / realm'e Google+Apple IdP ekleme), `/auth/login` (password), `/auth/register`, `/auth/forgot-password`+`/verify-otp`+`/set-new-password`, `/auth/refresh`, `/auth/logout`. | `MarineProvider` `Auth/` + Identity OTP-login | **M1**; Identity OTP SPI; realm IdP kararı (üretim kapısı) |
| **M3** | **Profile & ReferenceData.** `GET/PUT /profile`, `POST /profile/avatar`, runtime Identity profil re-check (M1'de ertelenen), reference lookuplar (`/reference/vessel-types|flags|service-categories|fuel-types|engine-types|materials`). | `MarineProvider` `Me/` + ReferenceData clients | **M1**, **M2** |
| **M4** | **Vessels.** `GET/POST /vessels`, `GET/PUT /vessels/{id}`, `/vessels/{id}/documents`, `/vessels/{id}/cargodry`; teknik/motor spec ve doküman upload akışları. | Vessel + FileStorage clients | **M3** |
| **M5** | **CargoDry.** `/cargodry`, `/cargodry/kits`, `/cargodry/kits/{id}`, `POST /cargodry/activate` (QR), `POST /cargodry/kits/{id}/renew`, `/cargodry/recommendations`. | CargoDry client | **M3** (M4 önerilir) |
| **M6** | **ServiceRequests.** 3 adımlı create, list/detail, `/offers` + `/offers/{offerId}` accept, `/chat` (Messaging), `POST /complete`, `POST /dispute`, assignment progress. | ServiceRequest + Messaging clients | **M3**, **M4** |
| **M7** | **Notifications & Realtime.** `/notifications`, `/notifications/{id}`, `read`/`read-all`, `/preferences`, `POST /notifications/register-token` (Expo push); SignalR hub `/hubs/notifications` → `ReceiveNotification`, `CargoDryAlert`, `ServiceUpdate`; Redis backplane + bus consumer (`TypeInclude={Worker}`). | `MarineProvider` Realtime (ADR realtime-edge) | **M1**; M5/M6 (event kaynakları) |
| **M8** | **Files.** `POST /files/upload`, `POST /files/upload-session`, `GET /files/{id}` (presigned). | FileStorage client | **M3** |

> Kapsam dışı (MVP sonrası): `commerce` ve `discovery` yüzeyleri istemcide yalnızca ekran-stub; BFF fazı açılmadan önce ürün kararı bekler (bkz. Açık kararlar).

---

## Yatay kurallar (tüm fazlar)

- **Envelope:** İstemci `ApiResponse<T>` = `{ data, message?, success, statusCode }`, listeler `PagedResponse<T>` bekler; auth uçları `{ data: { accessToken, refreshToken, expiresIn, tokenType } }` döner. BFF içi CQRS `AizenApiResponse<T>` üretir; controller `SetResponse(...)` ile sarar. İki envelope arası eşlemenin istemci beklentisiyle birebir tutulması **her fazın kabul kriteridir**.
- **Kimlik güvenliği:** `participantProfileId`/`userId` **asla** request body/query'den alınmaz; her zaman doğrulanmış token claim'lerinden (`sub`, `participant_profile_id`) sunucu tarafında çözülür. Modüllere geçiş `X-Aizen-Bff-Assertion` (+ `X-Aizen-User-Id`, `X-Aizen-Participant-Profile-Id`) ile `ModuleAssertionSecret` üzerinden yapılır; inbound kullanıcı JWT'si modüllere forward edilmez.
- **Keycloak:** Realm `inktavia-realm`; BFF audience `marine-mobile-bff` (yeni confidential service-account client); mobil uygulama public client `inktavia-mobile` (mevcut). `Authority` browser-facing issuer, `MetadataAddress` in-cluster JWKS, `RequireHttpsMetadata=false` (dev).
- **Sırlar:** `AdminClientSecret`, `ModuleAssertionSecret`, `Cors:AllowedOrigins`, remote BaseUrl'ler **env/secret** ile enjekte edilir; committed config yalnızca `__FROM_ENV__` placeholder taşır. Sır loglanmaz.
- **Bozmama:** Yalnızca `Bff/src/Marine.Participant.Mobile/` + kendi Dockerfile'ı + additive `docker-compose` servisi + (M2'de) realm client'ı değişir. Diğer BFF'ler, modüller ve web panelleri git-clean kalır. Build 0 hata; her faz kendi `REPORT_BE_M<n>_*.md` raporunu yazar.

---

## İstemci yüzeyi (`inktavia-marine-mobile`) — faz eşlemesi

- **auth:** LoginScreen (email+password), RegisterScreen, ForgotPassword/OTP reset, OTPLoginScreen (phone+OTP), SocialLoginButtons (Google/Apple) → **M1/M2**.
- **home:** dashboard metrikleri (vessels/cargodry/service-requests özetleri) → **M3+** (aggregate).
- **vessels:** liste, detay, ekleme stepper (basic/technical/engine/documents), per-vessel cargodry → **M4**.
- **cargodry:** overview, kit detail, QR activation, recommendation → **M5**.
- **service-requests:** 3 adımlı create, provider offers, chat, assignment progress, completion review, dispute → **M6**.
- **notifications:** center, detail, preferences, push token, realtime bell → **M7**.
- **profile/settings:** profil görüntü/güncelle, avatar, ayarlar → **M3**.

---

## Durum

- **M0 (skeleton) — mevcut durum (2026-08-04):** `Marine.Participant.Mobile` BFF ikilisi + Helm chart (`Bff/deploy/aizen-bff-marine.participant.mobile`) + `Dockerfile.Marine.Participant.Mobile` oluşturuldu; ancak Web projesi hâlâ `dotnet new webapi` **weatherforecast** stub'ı, `.Application` projesinde kod yok, host csproj'da **çift `.Application.Application`** path bug'ı, root appsettings boş, `docker-compose`'da servis yok. `docs/V1.0.1/Mobile` boştu.
- **M1 (Foundation) promptu (arşiv)** (`BE_M1_FOUNDATION_KICKOFF.md`): MarineProvider Foundation dikey dilimini (Program.cs/DI/Auth/RemoteCall plumbing/config) mirror+rename ile kurar; `bff-marine-mobile` compose servisi + `marine-mobile-bff` realm client'ı + `GET /api/v1/mobile/me` kabul kriteri.
- **M1 (Foundation) — iskelet YAZILDI, derleme DOĞRULANMADI ⏳ (2026-08-04):** MarineProvider Foundation dosyaları `Marine.Participant.Mobile`'a rename map ile kopyalandı (`Program.cs`, `DependencyInjection.cs`, `AuthenticationExtensions`, `ParticipantAuthorization`, `MarineMobileKeycloakOptions`, `MarineMobileBffAuthDelegatingHandler`, `ParticipantContext`/`IdentityHolder`/`ServiceTokenProvider`, `IIdentityRemoteCall`+`IReferenceDataRemoteCall`, `Me` slice + `GET /api/v1/mobile/me`, `configuration/appsettings*`). csproj çift-path + Dockerfile `EXPOSE 80→8080`/`WORKDIR /app→/src` düzeltildi; `bff-marine-mobile` servisi compose'a additive eklendi. **Design note:** realtime/SignalR ve `TypeInclude={Worker}` Foundation'dan çıkarıldı (M7); policy'ler ikiye indirildi (`ParticipantAuthenticated`, `ParticipantActive` = claims-based `mobile_user`), provider'ın runtime Identity re-check'i (`ProviderProfileRequirement`) ve `KeycloakAdminClient`/`ProfileResolver` **M3'e ertelendi**; yalnızca Identity+ReferenceData remote client'ı register edildi. **Flag (derleme):** sandbox'ta .NET SDK yok → `dotnet build` çalıştırılamadı; Mac'te `dotnet build Bff/src/Marine.Participant.Mobile/.../Aizen.Bff.Marine.Participant.Mobile.csproj -c Debug` ile doğrulanmalı. **Açık karar (realm):** `marine-mobile-bff` confidential client + `inktavia-mobile`'a `marine-mobile-bff` audience mapper realm JSON'a henüz eklenmedi (BE_M1 §2'de hazır JSON, uygulanması M1'i kapatır). Sonraki: **BE-M2 auth surface**.

---

## Açık kararlar (Mobile)

- **Google/Apple IdP (ÜRETİM KAPISI):** `inktavia-realm`'de Google/Apple identity provider **yok** (Google yalnızca provider-realm partial'ında). Karar: (a) realm'e Google+Apple IdP eklenip BFF token-exchange mi yapacak, yoksa (b) BFF native `idToken`/`identityToken`'ı kendi doğrulayıp Keycloak kullanıcısını admin-API ile provision + token mi mint edecek? M2 social akışları bu karara bağlı; üretime çıkmadan çözülmeli.
- **Realm string uyuşmazlığı:** istemci default `KEYCLOAK_REALM=inktavia` iken gerçek realm `inktavia-realm`. İstemci env'i düzeltilmeli veya BFF/gateway normalize etmeli (istemci BFF üzerinden gittiği için düşük risk, yine de netleştirilmeli).
- **`X-Aizen-User-Token` header:** istemci mimari dokümanında var, interceptor'da henüz wire edilmemiş. BFF'in bu header'ı bekleyip beklemeyeceği (yoksa yalnız `Authorization` mı) M2'de netleşmeli.
- **Refresh sahipliği:** istemcide refresh-token çağrısı implement değil; BFF `/auth/refresh` sahipliği M2 kapsamı.
- **Push:** Expo push token → `POST /notifications/register-token`; Notification modülünün Expo push kanalı desteği M7 ön koşulu.
- **commerce / discovery:** istemci ekran-stub; BFF fazı ürün önceliğine bağlı, MVP dışı.
