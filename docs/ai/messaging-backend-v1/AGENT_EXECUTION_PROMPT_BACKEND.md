# AGENT EXECUTION PROMPT — Messaging Module v1 (Backend)
# Kapsam: PROMPT_F + PROMPT_G + PROMPT_I
# `Aizen.Modules.Messaging` · `Aizen.Bff.AdminPanel` · Keycloak · Docker

## Ön Koşullar

Bu prompt paketi **A–E promptlarının tamamlandığını** varsayar:
- ✅ PROMPT_A — Domain entities, enums, repository interfaces
- ✅ PROMPT_B — Application layer (commands, queries, content policy)
- ✅ PROMPT_C — Repository, DbContext, EF migration, DI
- ✅ PROMPT_D — API layer (Hub, controllers, Program.cs)
- ✅ PROMPT_E — Seed data (3 conversation, 12 message)

**Frontend (PROMPT_H)** ayrı bir pakette yönetilmektedir — bu prompt paketiyle ilişkisi yoktur.

---

## Mimari Sözleşme (Değişmez Kurallar)

| Kural | Değer |
|-------|-------|
| Entity ID tipi | `long` — GUID YASAK |
| Entity base class | `AizenEntityWithAudit` |
| Command/Query handler base | `AizenCommandHandler<>` / `AizenQueryHandler<>` |
| Controller base | `AizenWebApiController` |
| Return type | `AizenApiResponse<T?>` via `SetResponse(result)` |
| CQRS processor | `IAizenCQRSProcessor.ProcessAsync<T>(command, ct)` |
| Hub base | `DomainHubBase` (DomainName = "messaging") |
| MassTransit | `IAizenMessagePublisher.SendAsync<TMessage, TResponse>()` |
| DB schema | `"messaging"` (PostgreSQL) |
| DateTime | Daima UTC, `NormalizeDateTimeProperties()` |
| FileStorage OwnerModule | `"Messaging"` (string, enum içinde yok) |
| Keycloak audience | `messaging-api` |
| Docker port | `7108:8080` |

---

## PHASE 6 — MVP Extensions
**Dosya:** `PROMPT_F_MESSAGING_MVP_EXTENSIONS.md`

### Sıra:

1. **Part 1 — Enum Güncelle**
   - `MessageType.Location = 6` ekle (Abstraction/Enum/)
   - `MessagingRealtimeEventType.AttachmentReady = 8` ekle

2. **Part 2 — FileStorage Entegrasyonu**
   - `IMessagingFileStorageService` interface oluştur (Domain/Interface/)
   - `MessagingFileStorageService` implement et (Application/Services/)
     - `CreateAttachmentUploadUrlAsync()` → `IAizenMessagePublisher.SendAsync<CreateUploadSessionProcessMessage, CreateUploadSessionProcessMessageResult>()`
     - `CompleteAttachmentUploadAsync()` → `IAizenMessagePublisher.SendAsync<CompleteUploadSessionProcessMessage, CompleteUploadSessionProcessMessageResult>()`
     - `GetAttachmentReadUrlAsync()` → `IAizenMessagePublisher.SendAsync<CreateFileReadUrlProcessMessage, CreateFileReadUrlProcessMessageResult>()`
   - `RequestAttachmentUploadUrlCommand` + Handler + Validator oluştur
   - `SendMessageCommand`'ı güncelle: `UploadSessionCode (string?)`, `Checksum (string?)` ekle
   - `AttachmentDto`'ya `ReadUrl (string?)` ekle
   - Endpoint: `POST /api/v1/conversations/{id}/messages/attachment-upload-url`

3. **Part 3 — Location Mesaj Tipi**
   - `LocationContentDto` record oluştur: `Lat, Lng, Label, Accuracy?, GoogleMapsUrl, YandexMapsUrl, GeoUri`
   - `MessageType.Location` için content policy skip ekle
   - `SendMessageRequest`'e `LocationJson (string?)` ekle
   - `ConversationMessageDto`'ya `Location (LocationContentDto?)` ekle
   - Handler'da Location için GoogleMapsUrl ve YandexMapsUrl oluştur:
     ```csharp
     GoogleMapsUrl = $"https://maps.google.com/?q={lat},{lng}"
     YandexMapsUrl = $"https://yandex.com/maps/?ll={lng},{lat}&pt={lng},{lat}"
     GeoUri        = $"geo:{lat},{lng}"
     ```

4. **Part 4 — LLM Content Moderation**
   - `ILlmContentAnalyzer` interface oluştur (Domain/Interface/)
   - `AnthropicLlmContentAnalyzer` implement et (Application/Services/)
     - Model: `claude-haiku-4-5-20251001`
     - Fire-and-forget: yeni `IServiceScope` aç, `Task.Run()` ile çalıştır
     - Asla mesaj teslimatını engelleme — tüm exception'ları yakala ve logla
     - Feature flag: `Messaging:LlmModeration:Enabled` false ise skip
   - `SendMessageCommandHandler` içinde LLM'i mesaj kaydedildikten sonra çağır
   - `HttpClient "anthropic"` DI kaydı ekle

5. **Part 5 — Raporlama**
   - `GetProviderResponseTimeReportQuery` + Handler
     - SQL: `MIN(sentAt WHERE senderRole='Provider') - MIN(sentAt WHERE senderRole='Owner')` per conversation
   - `GetChannelUsageReportQuery` + Handler
     - SQL: context_type bazında message/conversation count, daily trend, peak hour
   - `MessagingReportingController` at `api/v1/reporting/messaging`
     - `GET /provider-response-time`
     - `GET /channel-usage`

6. **Part 6 — DI Güncellemeleri**
   - `AddMessagingServices()` içine ekle: `IMessagingFileStorageService`, `ILlmContentAnalyzer`, HttpClient "anthropic"

**Gate:**
```bash
dotnet build Aizen.Modules.Messaging
# 0 error
# Swagger: /api/v1/conversations/{id}/messages/attachment-upload-url görünür
# Swagger: /api/v1/reporting/messaging/provider-response-time görünür
# Swagger: /api/v1/reporting/messaging/channel-usage görünür
```

---

## PHASE 7 — BFF Layer
**Dosya:** `PROMPT_G_MESSAGING_BFF.md`

### Hedef projeler:
- `Aizen.Bff.AdminPanel.Application`
- `Aizen.Bff.AdminPanel`

### Sıra:

1. **IMessagingAdminBffRemoteCall oluştur** (Application/Common/RemoteClients/)
   - Tüm endpoint'ler Refit attribute pattern ile: `[AizenRemoteCallGet]`, `[AizenRemoteCallPost]`, `[AizenRemoteCallPatch]`
   - Header'lar: `[AizenRemoteCallHeader("Authorization")]`, `[AizenRemoteCallHeader("X-Aizen-User-Token")]`

2. **BFF DTO'ları oluştur** (Application/Common/Dto/Messaging/)
   - `AdminConversationListBffResponse` — `items`, `total`, `hubUrl` (string) içerir
   - `AdminConversationDetailBffResponse` — messages + participants içerir
   - `AdminMessageDto` — `location` field'ı `LocationContentDto`'dan JSON parse edilir
   - `AdminAttachmentDto` — `readUrl (string?)` içerir
   - `AdminModerationQueueBffResponse`
   - `AdminMessagingReportBffResponse` — `providerResponseTime[]` + `channelUsage[]`

3. **Query Handler'ları oluştur** (Application/Features/Messaging/)
   - `GetAdminConversationListQueryHandler`
   - `GetAdminConversationDetailQueryHandler` — Location JSON parse et
   - `GetAdminModerationQueueQueryHandler`
   - `GetAdminMessagingReportQueryHandler` — iki raporu paralel `Task.WhenAll` ile çek

4. **Command Handler'ları oluştur**
   - `SendAdminMessageCommandHandler`
   - `ModerateMessageCommandHandler`

5. **AdminMessagingController oluştur** (Controllers/V1/)
   - Base route: `api/v1/admin-panel/messaging`
   - `[Authorize(Policy = "AdminPanelAccess")]`
   - Header'dan `UserToken` al: `Request.Headers["X-Aizen-User-Token"]`
   - Her action: `_cqrs.ProcessAsync() → SetResponse(result)`

6. **Temizlik:**
   - `IServiceRequestAdminBffRemoteCall` içindeki stale messaging method'larını sil (eğer varsa `GetAdminConversations`, `GetAdminConversationDetail`)

7. **DI:**
   - `AddAizenRemoteCall<IMessagingAdminBffRemoteCall>()` ekle
   - `appsettings.json`: `RemoteServices:Messaging:BaseUrl` ve `Messaging:HubUrl` ekle

**Gate:**
```bash
dotnet build Aizen.Bff.AdminPanel
# 0 error
# Swagger (BFF): GET /api/v1/admin-panel/messaging/conversations görünür
# Swagger (BFF): GET /api/v1/admin-panel/messaging/reports görünür
```

---

## PHASE 8 (Son) — Keycloak + Docker
**Dosya:** `PROMPT_I_MESSAGING_KEYCLOAK_DOCKER.md`

### Sıra:

1. **`infrastructure/keycloak/inktavia-realm-realm.json` güncelle**
   - `roles.realm`: `messaging_read`, `messaging_write` ekle
   - `roles.client`: `"messaging-api"` bloku ekle (5 rol)
   - `clients[]`: `messaging-api` resource server ekle (publicClient: false, flow'suz)
   - `inktavia-mobile.protocolMappers`: `audience-messaging-api` mapper ekle
   - `customer-panel.protocolMappers`: `audience-messaging-api` mapper ekle
   - `admin-panel-bff.protocolMappers`: `audience-messaging-api` mapper ekle
   - `mobile.user.realmRoles`: `messaging_read`, `messaging_write` ekle
   - `customer.user.realmRoles`: `messaging_read`, `messaging_write` ekle
   - `admin.user.realmRoles`: `messaging_read`, `messaging_write` ekle
   - `service-account-admin-panel-bff.clientRoles`: `"messaging-api"` bloku ekle (5 rol)

2. **`docker-compose.yaml` güncelle**
   - `messaging-api` servisi ekle (port 7108, depends_on: postgres, redis, keycloak, rabbitmq, file-storage-api)
   - `bff-adminpanel.depends_on`'a `messaging-api` ekle
   - `bff-adminpanel.environment`'a `RemoteCalls__IMessagingAdminBffRemoteCall__BaseUrl` ekle

3. **`Modules/Messaging/build/Dockerfile` oluştur**
   - `Modules/ServiceRequest/build/Dockerfile` referans alınarak oluştur
   - Sadece project path'lerini Messaging için güncelle

**Gate:**
```bash
# Keycloak yeniden başlat
docker compose down keycloak && docker compose up -d keycloak

# BFF token'ında messaging-api audience var mı?
TOKEN=$(curl -s -X POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=admin-panel-bff" \
  -d "client_secret=local-dev-only-change-me" \
  | jq -r '.access_token')
echo $TOKEN | cut -d. -f2 | base64 -d 2>/dev/null | jq '.aud' | grep messaging-api

# messaging-api container ayakta mı?
docker compose up -d --build messaging-api
curl -s -o /dev/null -w "%{http_code}" http://localhost:7108/health
# Expected: 200

# BFF üzerinden erişim
curl -s -H "Authorization: Bearer $TOKEN" \
  http://localhost:17001/api/v1/admin-panel/messaging/conversations | jq '.header.isSuccess'
# Expected: true
```

---

## Bilinen Tehlikeler

1. **`OwnerModule` string'dir, enum değil.** `FileStorage.Abstraction`'da `FileOwnerModule` enum'unda `Messaging` yoktur. `CreateUploadSessionProcessMessage.OwnerModule = "Messaging"` (string?) olarak set et.

2. **LLM moderation asla bloklama yapmamalı.** `try-catch` ile tümünü sar. `IServiceScope` içinde yeni `DbContext` instance kullan — parent scope'taki EF context completion sonrası dispose olmuş olabilir.

3. **`HubUrl` BFF'te `appsettings`'den gelir.** Messaging servisinin hub URL'ini BFF kendisi oluşturur: `$"{_messagingHubUrl}/hubs/messaging"`. Bu değer `IConfiguration["Messaging:HubUrl"]`'den alınır.

4. **Keycloak import deterministic değildir.** Realm dosyası `--import-realm` ile import edilir. Eğer realm zaten mevcutsa Keycloak bazı versiyonlarda import'u atlar. Temiz test için: `docker compose down keycloak keycloak-db -v && docker compose up -d keycloak-db keycloak`.

5. **BFF `RemoteCalls__IMessagingAdminBffRemoteCall__BaseUrl` convention'ı.** Bu key, diğer remote call'larla aynı pattern'i izler — Refit kayıt mekanizması bu convention'ı kullanır. Tam olarak interface class adıyla eşleşmeli.

---

## Definition of Done

### Phase 6 — MVP Extensions
- [ ] `dotnet build Aizen.Modules.Messaging` — 0 error
- [ ] `MessageType.Location = 6` enum'da mevcut
- [ ] `POST /api/v1/conversations/{id}/messages/attachment-upload-url` → 200 + `{uploadUrl, fileId, expiresAt}`
- [ ] Location mesajı gönderildiğinde `googleMapsUrl` response'da dolu
- [ ] `Messaging:LlmModeration:Enabled=false` iken hiç Anthropic HTTP isteği gitmez
- [ ] `Messaging:LlmModeration:Enabled=true` iken SendMessage 200 döner, Anthropic async çağrılır
- [ ] `GET /api/v1/reporting/messaging/provider-response-time` → 200 + data
- [ ] `GET /api/v1/reporting/messaging/channel-usage` → 200 + data

### Phase 7 — BFF
- [ ] `dotnet build Aizen.Bff.AdminPanel` — 0 error
- [ ] `GET /api/v1/admin-panel/messaging/conversations` → 200 + `{items[], total, hubUrl}`
- [ ] `GET /api/v1/admin-panel/messaging/conversations/{id}` → 200 + messages + location parsed
- [ ] `POST /api/v1/admin-panel/messaging/conversations/{id}/messages` → 201
- [ ] `GET /api/v1/admin-panel/messaging/moderation/queue` → 200
- [ ] `GET /api/v1/admin-panel/messaging/reports` → 200 + providerResponseTime + channelUsage
- [ ] Location mesajlarında `location.googleMapsUrl` ve `location.yandexMapsUrl` dolu

### Phase 8 — Keycloak + Docker
- [ ] `messaging-api` Keycloak client'ı mevcut
- [ ] `admin-panel-bff` token'ı `aud` array'inde `messaging-api` içeriyor
- [ ] `messaging-api` Docker container `7108` portunda sağlıklı çalışıyor
- [ ] `docker compose up -d messaging-api` — container crash yok
- [ ] BFF `depends_on` + `RemoteCalls` env var set edilmiş
- [ ] End-to-end: BFF token → `http://localhost:17001/api/v1/admin-panel/messaging/conversations` → 200
