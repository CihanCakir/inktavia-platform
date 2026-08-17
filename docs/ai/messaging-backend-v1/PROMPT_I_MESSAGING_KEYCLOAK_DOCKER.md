# PROMPT I — Messaging Module: Keycloak & Docker Integration
# Files: `infrastructure/keycloak/inktavia-realm-realm.json` + `docker-compose.yaml`

## Overview

Bu prompt iki dosyayı günceller:
1. **Keycloak Realm** — `messaging-api` resource server, client rolleri, audience mapper'lar, kullanıcı rolleri
2. **Docker Compose** — `messaging-api` servisi + `bff-adminpanel` dependency/env güncellemesi

Her iki değişiklik de **additive** — mevcut içerik silinmez, sadece ekleme yapılır.

---

## PART 1 — Keycloak Realm (`infrastructure/keycloak/inktavia-realm-realm.json`)

### 1.1 — Realm Rolleri Ekle

`roles.realm` dizisine aşağıdaki iki rolü ekle (mevcut rollerin sonuna):

```json
{ "name": "messaging_read",  "description": "Read access to Messaging API" },
{ "name": "messaging_write", "description": "Write access to Messaging API" }
```

**Sonuç:** `roles.realm` 19 eleman içerecek (mevcut 17 + 2 yeni).

---

### 1.2 — Messaging API Client Rolleri Ekle

`roles.client` objesine `"messaging-api"` key'ini ekle (mevcut `"service-request-api"` bloğunun hemen arkasına):

```json
"messaging-api": [
  {
    "name": "messaging.read",
    "description": "Read conversations and messages"
  },
  {
    "name": "messaging.write",
    "description": "Send messages and create conversations"
  },
  {
    "name": "messaging.admin",
    "description": "Full administrative access — view internal notes, all contexts"
  },
  {
    "name": "messaging.moderation.manage",
    "description": "Review and action content moderation queue"
  },
  {
    "name": "messaging.reporting.read",
    "description": "Access provider response time and channel usage reports"
  }
]
```

---

### 1.3 — Messaging API Resource Server Client Ekle

`clients` dizisine aşağıdaki client'ı ekle (`"reference-data-api"` client bloğunun hemen arkasına, `"inktavia-mobile"` bloğundan önce):

```json
{
  "clientId": "messaging-api",
  "name": "Messaging API",
  "description": "Resource server for Messaging API audience validation",
  "enabled": true,
  "publicClient": false,
  "standardFlowEnabled": false,
  "directAccessGrantsEnabled": false,
  "serviceAccountsEnabled": false,
  "protocol": "openid-connect"
}
```

---

### 1.4 — Mobile Client Audience Mapper Ekle

`inktavia-mobile` client'ının `protocolMappers` dizisine ekle (mevcut son mapper'ın — `audience-reference-data-api` — arkasına):

```json
{
  "name": "audience-messaging-api",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-audience-mapper",
  "consentRequired": false,
  "config": {
    "included.client.audience": "messaging-api",
    "id.token.claim": "false",
    "access.token.claim": "true"
  }
}
```

---

### 1.5 — Customer Panel Audience Mapper Ekle

`customer-panel` client'ının `protocolMappers` dizisine ekle (mevcut son mapper'ın arkasına):

```json
{
  "name": "audience-messaging-api",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-audience-mapper",
  "consentRequired": false,
  "config": {
    "included.client.audience": "messaging-api",
    "id.token.claim": "false",
    "access.token.claim": "true"
  }
}
```

---

### 1.6 — AdminPanel BFF Audience Mapper Ekle

`admin-panel-bff` client'ının `protocolMappers` dizisine ekle (mevcut son mapper'ın — `audience-service-request-api` — arkasına):

```json
{
  "name": "audience-messaging-api",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-audience-mapper",
  "consentRequired": false,
  "config": {
    "included.client.audience": "messaging-api",
    "id.token.claim": "false",
    "access.token.claim": "true",
    "userinfo.token.claim": "false",
    "introspection.token.claim": "true"
  }
}
```

---

### 1.7 — Kullanıcı Realm Rolleri Güncelle

#### `mobile.user@inktavia.com`

`realmRoles` dizisine ekle:
```json
"messaging_read",
"messaging_write"
```

#### `customer.user@inktavia.com`

`realmRoles` dizisine ekle:
```json
"messaging_read",
"messaging_write"
```

#### `admin.user@inktavia.com`

`realmRoles` dizisine ekle:
```json
"messaging_read",
"messaging_write"
```

---

### 1.8 — BFF Service Account Client Rolleri Güncelle

`service-account-admin-panel-bff` kullanıcısının `clientRoles` objesine `"messaging-api"` key'ini ekle (mevcut `"service-request-api"` bloğunun arkasına):

```json
"messaging-api": [
  "messaging.read",
  "messaging.write",
  "messaging.admin",
  "messaging.moderation.manage",
  "messaging.reporting.read"
]
```

---

### 1.9 — Tam Değişiklik Özeti (diff formatında)

Keycloak realm dosyasının değiştirilecek bölümleri:

```
roles.realm                          +2 rol  (messaging_read, messaging_write)
roles.client                         +1 client blok  (messaging-api: 5 rol)
clients[]                            +1 client  (messaging-api resource server)
clients[inktavia-mobile].protocolMappers    +1 mapper  (audience-messaging-api)
clients[customer-panel].protocolMappers     +1 mapper  (audience-messaging-api)
clients[admin-panel-bff].protocolMappers    +1 mapper  (audience-messaging-api)
users[mobile.user].realmRoles               +2 rol
users[customer.user].realmRoles             +2 rol
users[admin.user].realmRoles                +2 rol
users[service-account-admin-panel-bff]      +1 clientRoles blok  (messaging-api: 5 rol)
```

---

### 1.10 — Doğrulama

Keycloak güncellemesi sonrası:

```bash
# Docker'ı yeniden başlat (realm import her seferinde çalışır)
docker compose down keycloak keycloak-db && docker compose up -d keycloak-db keycloak

# Realm'de messaging-api client'ı mevcut mu?
curl -s http://localhost:8080/realms/inktavia-realm/.well-known/openid-configuration | jq '.issuer'
# Expected: "http://localhost:8080/realms/inktavia-realm"

# admin-panel-bff token al ve messaging-api audience kontrolü
TOKEN=$(curl -s -X POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=admin-panel-bff" \
  -d "client_secret=local-dev-only-change-me" \
  | jq -r '.access_token')

# Token'da messaging-api audience var mı?
echo $TOKEN | cut -d. -f2 | base64 -d 2>/dev/null | jq '.aud' | grep messaging-api
# Expected: "messaging-api" (in the aud array)
```

---

## PART 2 — Docker Compose (`docker-compose.yaml`)

### 2.1 — messaging-api Servisi Ekle

`service-request-api` servisinin hemen arkasına (k6 bloğundan önce) ekle:

```yaml
  messaging-api:
    build:
      context: .
      dockerfile: ./Modules/Messaging/build/Dockerfile
    container_name: messaging-api
    depends_on:
      - postgres
      - redis
      - keycloak
      - rabbitmq
      - file-storage-api
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: Host=postgres;Database=${POSTGRES_DB:-aizen};Username=${POSTGRES_USER:-aizen};Password=${POSTGRES_PASSWORD:-aizenpw}
      Redis__Host: redis:6379
      Messaging__RabbitMQ__Host: rabbitmq
      Messaging__RabbitMQ__Port: 5672
      Messaging__RabbitMQ__User: ${RABBITMQ_USER:-aizen}
      Messaging__RabbitMQ__Password: ${RABBITMQ_PASSWORD:-aizenpw}
      KEYCLOAK_AUTHORITY: http://localhost:8080/realms/inktavia-realm
      KEYCLOAK_METADATA_ADDRESS: http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration
      KEYCLOAK_AUDIENCE: messaging-api
      KEYCLOAK_REQUIRE_HTTPS_METADATA: "false"
      # FileStorage MassTransit bağlantısı (RabbitMQ üzerinden)
      # file-storage-api'ye MassTransit request/response için ayrıca endpoint gerekmez
      # Anthropic LLM Content Moderation (isteğe bağlı — boş bırakıldığında devre dışı)
      Messaging__LlmModeration__Enabled: ${MESSAGING_LLM_MODERATION_ENABLED:-false}
      Messaging__LlmModeration__ApiKey: ${ANTHROPIC_API_KEY:-}
      Messaging__LlmModeration__Model: claude-haiku-4-5-20251001
    ports:
      - "${MESSAGING_API_PORT:-7108}:8080"
    networks: [aizen]
```

> **Not:** Port `7108` seçildi — mevcut servisler `7101, 7104–7107` aralığındadır.

> **Not:** `file-storage-api` bağımlılığı eklenmiştir çünkü `MessagingFileStorageService`, FileStorage modülüne RabbitMQ üzerinden `IAizenMessagePublisher.SendAsync` ile MassTransit mesajları gönderir. RabbitMQ'nun hazır olması yeterlidir; ancak `file-storage-api`'nin de ayakta olması gereklidir ki consumer'lar mesajları işleyebilsin.

---

### 2.2 — bff-adminpanel Servisini Güncelle

#### 2.2.a — `depends_on` Listesine Ekle

`bff-adminpanel.depends_on` listesine ekle:
```yaml
      - messaging-api
```

#### 2.2.b — Environment Variables'a Ekle

`bff-adminpanel.environment` bloğuna ekle (mevcut `RemoteCalls__IReferenceDataAdminBffRemoteCall__BaseUrl` satırının arkasına):
```yaml
      RemoteCalls__IMessagingAdminBffRemoteCall__BaseUrl: http://messaging-api:8080
```

---

### 2.3 — Dockerfile Oluştur

`messaging-api` servisi için Dockerfile oluştur:

**Dosya:** `Modules/Messaging/build/Dockerfile`

Mevcut diğer modüllerin Dockerfile'larıyla aynı pattern'i takip et. Referans olarak `Modules/ServiceRequest/build/Dockerfile` dosyasını incele. Temel yapı:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY ["Modules/Messaging/src/Aizen.Modules.Messaging/Aizen.Modules.Messaging.csproj", "Modules/Messaging/src/Aizen.Modules.Messaging/"]
COPY ["Modules/Messaging/src/Aizen.Modules.Messaging.Application/Aizen.Modules.Messaging.Application.csproj", "Modules/Messaging/src/Aizen.Modules.Messaging.Application/"]
COPY ["Modules/Messaging/src/Aizen.Modules.Messaging.Domain/Aizen.Modules.Messaging.Domain.csproj", "Modules/Messaging/src/Aizen.Modules.Messaging.Domain/"]
COPY ["Modules/Messaging/src/Aizen.Modules.Messaging.Abstraction/Aizen.Modules.Messaging.Abstraction.csproj", "Modules/Messaging/src/Aizen.Modules.Messaging.Abstraction/"]
COPY ["Modules/Messaging/src/Aizen.Modules.Messaging.Repository/Aizen.Modules.Messaging.Repository.csproj", "Modules/Messaging/src/Aizen.Modules.Messaging.Repository/"]

# Copy Core and shared infrastructure projects
# (Copy the same Core/* projects as other modules — check ServiceRequest Dockerfile for exact list)

RUN dotnet restore "Modules/Messaging/src/Aizen.Modules.Messaging/Aizen.Modules.Messaging.csproj"

COPY . .

WORKDIR "/src/Modules/Messaging/src/Aizen.Modules.Messaging"
RUN dotnet build "Aizen.Modules.Messaging.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "Aizen.Modules.Messaging.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aizen.Modules.Messaging.dll"]
```

> **Kritik:** Core, FileStorage.Abstraction ve diğer shared proje referanslarını `Modules/ServiceRequest/build/Dockerfile` dosyasındaki `COPY` satırlarını kopyalayarak ekle. Sadece Messaging projesi için olan bölümü değiştir.

---

### 2.4 — .env Güncelle (isteğe bağlı)

`.env` veya `.env.local` dosyasına ekle:

```env
# Messaging API
MESSAGING_API_PORT=7108
MESSAGING_LLM_MODERATION_ENABLED=false
ANTHROPIC_API_KEY=
```

---

### 2.5 — Doğrulama

```bash
# Tüm servisleri ayağa kaldır
docker compose up -d --build messaging-api

# Sağlık kontrolü
curl -s http://localhost:7108/health
# Expected: {"status":"Healthy"} veya benzeri

# Swagger kontrol
curl -s http://localhost:7108/swagger/index.html | head -5
# Expected: HTML başlığı görünür

# Konuşma listesi endpoint kontrol (auth token olmadan 401 beklenir)
curl -s -o /dev/null -w "%{http_code}" http://localhost:7108/api/v1/conversations
# Expected: 401

# BFF üzerinden kontrol (BFF token ile)
TOKEN=$(curl -s -X POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=admin-panel-bff" \
  -d "client_secret=local-dev-only-change-me" \
  | jq -r '.access_token')

curl -s -H "Authorization: Bearer $TOKEN" \
  http://localhost:17001/api/v1/admin-panel/messaging/conversations
# Expected: 200 OK + JSON (boş liste veya seed data)

# SignalR hub erişilebilir mi?
curl -s -o /dev/null -w "%{http_code}" http://localhost:7108/hubs/messaging
# Expected: 200 veya 101 (WebSocket upgrade negotiate)
```

---

## PART 3 — Messaging API appsettings.json

Messaging modülünün `appsettings.json` dosyasında aşağıdaki blokların mevcut olduğundan emin ol (PROMPT_F ve PROMPT_G'de tanımlandı, buraya referans olarak tekrar eklendi):

```json
{
  "Messaging": {
    "LlmModeration": {
      "Enabled": false,
      "ApiKey": "",
      "Model": "claude-haiku-4-5-20251001",
      "MaxTokens": 200,
      "TemperatureThreshold": 0.3
    }
  },
  "KEYCLOAK_AUDIENCE": "messaging-api",
  "KEYCLOAK_AUTHORITY": "http://localhost:8080/realms/inktavia-realm",
  "KEYCLOAK_METADATA_ADDRESS": "http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration",
  "KEYCLOAK_REQUIRE_HTTPS_METADATA": false
}
```

Docker Compose'daki environment variables bu değerleri override eder — sadece local dev için appsettings'de default değerler bulunması yeterlidir.

---

## Quality Gates

- [ ] `docker compose up -d keycloak` sonrası realm import başarılı
- [ ] `messaging-api` client Keycloak admin panel'de görünür (http://localhost:8080)
- [ ] `messaging-api` client 5 role sahip: `messaging.read/write/admin/moderation.manage/reporting.read`
- [ ] `admin-panel-bff` service account token'ında `aud` array'inde `messaging-api` var
- [ ] `mobile.user` token'ında `messaging_read`, `messaging_write` realm rolleri var
- [ ] `docker compose up -d messaging-api` sonrası container ayakta (`docker ps`)
- [ ] `curl http://localhost:7108/health` → 200
- [ ] `curl http://localhost:7108/api/v1/conversations` → 401 (auth korumalı)
- [ ] BFF admin token ile `http://localhost:17001/api/v1/admin-panel/messaging/conversations` → 200
- [ ] BFF `depends_on` listesinde `messaging-api` var
- [ ] BFF `RemoteCalls__IMessagingAdminBffRemoteCall__BaseUrl` env var set edilmiş
