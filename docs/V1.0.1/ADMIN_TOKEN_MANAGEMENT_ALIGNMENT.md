# Admin Token Yönetimi — `X-Aizen-User-Token`'ı tamamen kaldır, MarineProvider↔FE token yönetimini birebir uygula

> **Direktif:** AdminPanel'de `X-Aizen-User-Token` yönetimini **tamamen kaldır**; FE↔BFF ve BFF→module token akışını
> **MarineProvider ile birebir aynı** yap. **Provider'a HİÇ dokunulmaz** (referans). Keycloak realm:
> `infrastructure/keycloak/inktavia-realm-realm.json` (client'lar: `admin-panel` [FE public], `admin-panel-bff`
> [confidential], `payment-api`… module audience'ları).
> **Kritik bulgu:** `X-Aizen-User-Token` **merkezi** yönetiliyor (auth extension + delegating handler + middleware + DI) —
> **hiçbir Command/Query içinde iş mantığı olarak geçmiyor.** Yani kaldırma tek noktadan, temiz.

## 1. Provider token akışı (HEDEF — kodda doğrulandı)
- **Inbound (FE→BFF):** admin-web/provider-web **`Authorization: Bearer <Keycloak user token>`**. Provider
  `AuthenticationExtensions` default JwtBearer (Authorization) okur; `OnMessageReceived` yalnız **SignalR WebSocket**
  (query-string `access_token`) için — HTTP'de default Authorization.
- **Outbound (BFF→module):** `MarineProviderBffAuthDelegatingHandler` — (1) Authorization boşsa **Keycloak SERVICE token**
  (`IProviderKeycloakServiceTokenProvider`, client_credentials, per-module audience) ekler; (2) **BFF-assertion**:
  `X-Aizen-Bff-Assertion` (shared secret, `MarineProviderKeycloak` opsiyonlarından) + `X-Aizen-User-Id` +
  `X-Aizen-Provider-Profile-Id` (`IProviderIdentityHolder`'dan). **Identity `X-Aizen-User-Token` ASLA forward edilmez, JWT
  fabrikanmaz.** Modüller assertion'ı `BffAssertion:SharedSecret` ile doğrulayıp asserted kimliği onurlandırır.
- `IProviderIdentityHolder` (UserId/ProfileId, `Set(...)`) per-request; `ProviderProfileResolver` doğrulanmış Keycloak
  principal'ından doldurur.

## 2. Admin mevcut akış (KALDIRILACAK)
- **Inbound:** A+B `AuthenticationExtensions` user token'ı **`X-Aizen-User-Token` header'ından** okuyor
  (`OnMessageReceived` → `ctx.Token`). Authorization inbound'da kullanılmıyor.
- **Outbound:** `AdminPanelBffAuthDelegatingHandler` — (1) service token → Authorization (kalır); (2) **ham user JWT'yi
  `X-Aizen-User-Token` olarak forward ediyor** (`IAizenUserInfoAccessor.AccessToken`, `AizenUserInfoMiddleware`). ← ESKİ KÖPRÜ.
- `AuthorizationForwardingHandler` (inbound Authorization'ı forward eder) — module pipeline'ında kullanılıyorsa user token
  audience'ı module'e uymaz; provider bunu module çağrılarında kullanmıyor.
- **Admin module endpoint'leri `[Authorize(Roles="Admin")]`** (service token'ın Admin rolüyle yetkilenir; A+B'de service
  account'a Admin rolü verildi). Yani **yetki service token'da; kullanıcı kimliği yalnız audit** → provider'ın by-subject
  zorunluluğundan daha basit.

## 3. Değişiklik seti (X-Aizen'ı sil, provider'ı aynala)
1. **`AuthenticationExtensions.cs` (inbound):** `X-Aizen-User-Token` `OnMessageReceived` bloğunu **kaldır** → default
   `Authorization: Bearer` oku (provider gibi). Admin SignalR hub'ı varsa yalnız query-string WebSocket dalını koru
   (provider'daki gibi), yoksa hiç.
2. **`AdminPanelBffAuthDelegatingHandler.cs` (outbound):** §2 `X-Aizen-User-Token` forward bloğunu **kaldır**; yerine
   provider'ın **BFF-assertion** bloğunu ekle: `X-Aizen-Bff-Assertion` (shared secret, `AdminPanelKeycloak` opsiyonundan) +
   `X-Aizen-User-Id` (`IAdminIdentityHolder`'dan). **ProfileId YOK** (admin provider değil). Service token bloğu (§1) kalır.
3. **`IAdminIdentityHolder` + `AdminIdentityHolder`** oluştur (provider `IProviderIdentityHolder` aynası, **yalnız UserId**);
   doğrulanmış Keycloak principal'ından (sub/user-id claim) dolduran bir resolver/middleware (provider `ProviderProfileResolver`
   deseni; admin için ProfileId olmadan). `AizenUserInfoMiddleware`/`IAizenUserInfoAccessor.AccessToken` bağımlılığı outbound'dan kalkar.
4. **`AdminPanelBffKeycloakServiceTokenProvider` / `CachedKeycloakServiceToken` / `IAdminPanelBffKeycloakServiceTokenProvider`:**
   provider `ProviderKeycloakServiceTokenProvider` ile **davranış eşitliği** doğrula (client_credentials, Redis cache, per-module
   audience). Değişmesi gerekmiyorsa dokunma; yalnız assertion secret opsiyonunu ekle/oku.
5. **`DependencyInjection.cs` + `Program.cs`:** `IAdminIdentityHolder` + resolver kaydı; `AdminPanelKeycloak` assertion secret
   opsiyonu; artık kullanılmayan `AizenUserInfoMiddleware`/X-Aizen pipeline'ını kaldır (başka kullanan yoksa).
6. **RemoteClients (IAdminPaymentBffRemoteCall vb.):** doc/comment'lerdeki "Authorization + X-Aizen-User-Token" ifadesini
   güncelle; **per-method `X-Aizen-User-Token` header param'ı varsa kaldır** (enjeksiyon delegating handler'da).
7. **Module tarafı:** admin endpoint'leri zaten `[Authorize(Roles="Admin")]` (service token) → **yetki için değişiklik yok.**
   Assertion **audit** amaçlı: acting-admin id'sini isteyen module handler'ları `X-Aizen-User-Id`'yi `BffAssertion:SharedSecret`
   doğrulamasıyla okur (provider deseni). Module'lerin `BffAssertion:SharedSecret` config'i admin secret'ıyla eşleşmeli
   (env). **X-Aizen-User-Token bekleyen module kodu varsa** (audit) assertion'a geçir.
8. **FE (admin-web) — Faz C header kararı NETLEŞTİ:** user Keycloak token'ı **`Authorization: Bearer`** ile gönder (provider
   `authInterceptors` gibi), **`X-Aizen-User-Token` gönderme.** `authHeaders.test.ts` güncellenir (X-Aizen → Authorization).
   `FE_ADMIN_AUTH_ALIGNMENT.md` §3.3'teki açık header sorusu = **Authorization: Bearer.**

## 4. Doğrulama
- Admin BFF inbound: `Authorization: Bearer <Keycloak Admin token>` → 200; `X-Aizen-User-Token` header'ı artık **hiçbir yerde
  okunmuyor/yazılmıyor** (grep temiz: extension/handler/DI/RemoteClient/middleware). No-token→401, non-Admin→403 korunur.
- Outbound: module çağrılarında Authorization = **service token** (per-module audience), + `X-Aizen-Bff-Assertion` +
  `X-Aizen-User-Id`; user token module'e gitmiyor.
- MarineProvider BFF ve provider-web **hiç değişmedi**. Build 0 hata; admin endpoint'leri (Wave 1–4) uçtan uca 200.
- FE: admin-web `Authorization: Bearer` gönderiyor; `authHeaders.test` yeşil.

## 5. Sıra
1. **BFF token akışı (bu doküman §3.1–3.7)** — inbound Authorization + outbound assertion + X-Aizen tamamen kaldır.
2. **FE (§3.8 = Faz C header)** — admin-web `Authorization: Bearer`.
3. **Keycloak/config:** `admin-panel` (FE public) audience mapper `admin-panel-bff` eklesin; `AdminPanelKeycloak` assertion
   secret + module `BffAssertion:SharedSecret` eşle; `admin-panel-bff` client secret set. Uçtan uca smoke.

> **Kaldırılacak "X-Aizen yöneten" yapılar (Command/Query DEĞİL, altyapı):** `AuthenticationExtensions` (OnMessageReceived
> X-Aizen), `AdminPanelBffAuthDelegatingHandler` (§2 forward), `AizenUserInfoMiddleware`/`IAizenUserInfoAccessor` outbound
> kullanımı, `DependencyInjection` X-Aizen kaydı, RemoteClient doc/param. Yerine: `IAdminIdentityHolder` + assertion (provider
> aynası). Bu, MarineProvider↔FE token yönetiminin birebir eşidir.
