# VESSEL_BFF_API_CONTRACT.md
# Inktavia Marine OS — Vessel Module BFF API Contract
# Source: React Admin Stitch V5 (AF + AG + AH)

Bu döküman, yeni Vessel UI tasarımlarının (Vessel Management List, Vessel Detail Overview,
Vessel Documents & Media) ihtiyaç duyduğu tüm BFF endpoint'lerini, request/response şekillerini,
hangi backend modülüne gittiğini ve hangi yeni alanların backend'de eklenmesi gerektiğini tanımlar.

---

## 1. GENEL MİMARİ

```
React Admin Web
    │
    │  (Dual auth headers on every request)
    │  Authorization: Bearer <keycloakAccessToken>
    │  X-Aizen-User-Token: Bearer <identityAccessToken>
    │
    ▼
AdminPanel BFF  (.NET 8 — Aizen.AdminPanel.BFF)
    │
    ├──► Aizen.Modules.Vessel          (vessel CRUD, documents, media)
    ├──► Aizen.Modules.CargoDry        (kit activation status, efficiency)
    ├──► Aizen.Modules.ServiceRequest  (service history per vessel)
    └──► Aizen.Modules.ReferenceData   (country/currency lookups)
```

BFF'in görevi: Frontend'e tek bir çağrıyla aggregate edilmiş, flat ve UI-ready response döndürmek.
BFF hiçbir zaman frontend'e raw domain entity döndürmez.

---

## 2. ORTAK RESPONSE ENVELOPE

Tüm BFF response'ları mevcut `AizenBffResponse<T>` envelope'unu kullanır:

```json
{
  "header": {
    "isSuccess": true,
    "errorCode": null,
    "errorMessage": null,
    "validationErrors": []
  },
  "body": { ... }
}
```

Hata durumunda `header.isSuccess = false`, `header.errorCode` ve `header.errorMessage` dolu gelir.

---

## 3. ORTAK PAGINATION SHAPE

Tüm sayfalanmış listeler aşağıdaki yapıyı döndürür (mevcut vessel list response ile uyumlu):

```json
{
  "from": 0,
  "index": 0,
  "size": 20,
  "count": 142,
  "pages": 8,
  "hasPrevious": false,
  "hasNext": true,
  "items": [ ... ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| `from` | int | Başlangıç offset (index * size) |
| `index` | int | 0-tabanlı sayfa numarası (BFF input'u da 0-tabanlı) |
| `size` | int | Sayfa boyutu |
| `count` | int | Toplam kayıt sayısı |
| `pages` | int | Toplam sayfa sayısı |
| `hasPrevious` | bool | Önceki sayfa var mı |
| `hasNext` | bool | Sonraki sayfa var mı |
| `items` | array | Sayfa içeriği |

---

## 4. ENDPOINTLERİN DETAYI

---

### 4.1 — GET /bff/vessels

**Kaynak Sayfa:** AF — Vessel Management List  
**Amaç:** Sayfalanmış, filtrelenebilir vessel listesi (tablo görünümü için)

#### Request

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| `index` | int | Hayır | 0-tabanlı sayfa no (default: 0) |
| `size` | int | Hayır | Sayfa boyutu (default: 20, max: 100) |
| `search` | string | Hayır | Ad, kod veya sahip adına göre arama |
| `assetTypes` | int[] | Hayır | 1=MotorYacht, 2=SailingYacht, 3=Superyacht, 4=Catamaran, 5=RIB, 6=Commercial |
| `ownershipStatuses` | int[] | Hayır | 1=Private, 2=Charter, 3=Corporate |
| `operationalStatuses` | int[] | Hayır | 1=InService, 2=Refit, 3=Idle, 4=Decommissioned |

#### Response Body

```json
{
  "vessels": {
    "from": 0,
    "index": 0,
    "size": 20,
    "count": 142,
    "pages": 8,
    "hasPrevious": false,
    "hasNext": true,
    "items": [
      {
        "id": 1001,
        "vesselCode": "INK-2024-001",
        "name": "Serenity IV",
        "slug": "serenity-iv",
        "vesselTypeCode": "MOTOR_YACHT",
        "flagCountryCode": "TR",
        "thumbnailUrl": "https://cdn.inktavia.com/vessels/1001/thumb.jpg",
        "ownerName": "Ahmet Yılmaz",
        "ownerAvatarUrl": "https://cdn.inktavia.com/users/owner-avatar.jpg",
        "lengthMeters": 28.5,
        "grossTonnage": 120,
        "latitude": 36.8500,
        "longitude": 27.2500,
        "lastPositionDate": "2026-06-18T14:30:00Z",
        "operationalStatus": 1,
        "assetType": 1,
        "ownershipStatus": 1,
        "status": 1,
        "isArchived": false,
        "createDate": "2024-03-15T10:00:00Z"
      }
    ]
  }
}
```

#### VesselListItemBffDto — Alan Gereksinimleri

| Alan | Backend Kaynağı | Yeni mi? |
|------|----------------|----------|
| `id` | Vessel.Id | Mevcut |
| `vesselCode` | Vessel.VesselCode | Mevcut |
| `name` | Vessel.Name | Mevcut |
| `slug` | Vessel.Slug | Mevcut |
| `vesselTypeCode` | Vessel.VesselTypeCode | Mevcut |
| `flagCountryCode` | Vessel.FlagCountryCode | Mevcut |
| `thumbnailUrl` | VesselMedia (isPrimary=true, mediaType=Image) | **YENİ — join gerekli** |
| `ownerName` | VesselOwnership.Owner.FullName | Mevcut (join) |
| `ownerAvatarUrl` | Identity.User.AvatarUrl | **YENİ — identity join** |
| `lengthMeters` | VesselSpec.LengthMeters | Mevcut |
| `grossTonnage` | VesselSpec.GrossTonnage | Mevcut |
| `latitude` | VesselLocationSnapshot.Latitude (latest) | Mevcut |
| `longitude` | VesselLocationSnapshot.Longitude (latest) | Mevcut |
| `lastPositionDate` | VesselLocationSnapshot.RecordedAt (latest) | Mevcut |
| `operationalStatus` | Vessel.OperationalStatus | **YENİ ALAN — Vessel entity'ye eklenecek** |
| `assetType` | Vessel.AssetType | **YENİ ALAN — Vessel entity'ye eklenecek** |
| `ownershipStatus` | VesselOwnership.OwnershipStatus | **YENİ ALAN** |
| `status` | Vessel.Status | Mevcut |
| `isArchived` | Vessel.IsArchived | Mevcut |
| `createDate` | Vessel.CreateDate | Mevcut |

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselQueryService.GetVesselListAsync(VesselListQuery)
```

VesselListQuery ek parametreleri: `AssetTypes`, `OwnershipStatuses`, `OperationalStatuses`, `Search`

---

### 4.2 — GET /bff/vessels/{id}

**Kaynak Sayfa:** AG — Vessel Detail Overview  
**Amaç:** Tek vessel'ın tüm detaylarını, engine specs, CargoDry kits, servis geçmişi ve döküman özetleriyle birlikte aggregate eden endpoint.

#### Request

| Parametre | Tip | Zorunlu |
|-----------|-----|---------|
| `id` | int (path) | Evet |

#### Response Body

```json
{
  "vessel": {
    "id": 1001,
    "vesselCode": "INK-2024-001",
    "name": "Serenity IV",
    "slug": "serenity-iv",
    "vesselTypeCode": "MOTOR_YACHT",
    "flagCountryCode": "TR",
    "heroImageUrl": "https://cdn.inktavia.com/vessels/1001/hero.jpg",
    "yearBuilt": 2018,
    "builderName": "Ferretti Group",
    "buildCountry": "IT",
    "hullMaterial": "GRP",
    "superstructureMaterial": "Aluminium",
    "beamMeters": 7.2,
    "draftMeters": 1.8,
    "lengthMeters": 28.5,
    "grossTonnage": 120,
    "netTonnage": 80,
    "passengerCapacity": 10,
    "crewCapacity": 4,
    "imoCertificateNo": "IMO-9876543",
    "mmsiNumber": "271001234",
    "callSign": "TCXYZ",
    "homePort": "Bodrum, TR",
    "latitude": 36.8500,
    "longitude": 27.2500,
    "lastPositionDate": "2026-06-18T14:30:00Z",
    "operationalStatus": 1,
    "status": 1,
    "isArchived": false,
    "createDate": "2024-03-15T10:00:00Z",

    "engine": {
      "engineType": "Diesel Inboard",
      "engineCount": 2,
      "enginePowerKw": 1100,
      "engineModel": "MTU 12V 2000 M96",
      "propulsionType": "Fixed Pitch",
      "fuelType": "Marine Diesel",
      "fuelCapacityL": 8000,
      "maxSpeedKnots": 32,
      "cruisingSpeedKnots": 24,
      "rangeNm": 600
    },

    "cargoDryKits": [
      {
        "kitId": "kit-uuid-001",
        "kitCode": "CD-2024-INK-001",
        "productName": "CargoDry Marine Pro",
        "activatedDate": "2024-06-01T00:00:00Z",
        "expiryDate": "2026-06-01T00:00:00Z",
        "daysUntilExpiry": 347,
        "efficiencyPercent": 87,
        "status": "active"
      }
    ],

    "serviceHistory": [
      {
        "id": "svc-uuid-001",
        "date": "2026-03-10T00:00:00Z",
        "serviceType": "Annual Survey",
        "provider": "Turkish Lloyd Maritime",
        "location": "Bodrum Marina",
        "notes": "All certificates renewed",
        "status": "completed"
      }
    ],

    "documentSummaries": [
      {
        "id": "doc-uuid-001",
        "documentType": "Registration Certificate",
        "expiryDate": "2027-03-15T00:00:00Z",
        "daysUntilExpiry": 270,
        "documentStatus": "valid"
      },
      {
        "id": "doc-uuid-002",
        "documentType": "Safety Equipment Certificate",
        "expiryDate": "2026-08-10T00:00:00Z",
        "daysUntilExpiry": 52,
        "documentStatus": "expiring"
      }
    ]
  }
}
```

#### Alan Gereksinimleri & Backend Kaynakları

**Temel vessel alanları:** Mevcut (büyük çoğunluk Vessel entity'de)

| Alan | Backend Kaynağı | Yeni mi? |
|------|----------------|----------|
| `heroImageUrl` | VesselMedia (isPrimary=true, mediaType=Image).Url | **YENİ join** |
| `yearBuilt` | VesselSpec.YearBuilt | Mevcut |
| `builderName` | VesselSpec.BuilderName | Mevcut veya eklenecek |
| `buildCountry` | VesselSpec.BuildCountry | Mevcut veya eklenecek |
| `hullMaterial` | VesselSpec.HullMaterial | Mevcut veya eklenecek |
| `superstructureMaterial` | VesselSpec.SuperstructureMaterial | Mevcut veya eklenecek |
| `imoCertificateNo` | Vessel.ImoCertificateNo | Mevcut |
| `mmsiNumber` | Vessel.MmsiNumber | Mevcut |
| `callSign` | Vessel.CallSign | Mevcut |
| `homePort` | Vessel.HomePort | Mevcut |
| `engine.*` | VesselEngine entity | **YENİ TABLO — VesselEngine** |
| `cargoDryKits[]` | CargoDry.KitActivation (vesselId ile filter) | **YENİ — cross-module join** |
| `serviceHistory[]` | ServiceRequest.ServiceHistory (vesselId ile filter) | **YENİ — cross-module** |
| `documentSummaries[]` | VesselDocument (top 5, summary fields only) | Mevcut (kısmi) |

#### Backend Modülü Çağrıları (aggregate)

```
BFF → Aizen.Modules.Vessel → IVesselQueryService.GetVesselDetailAsync(id)
BFF → Aizen.Modules.CargoDry → ICargoDryQueryService.GetKitsByVesselAsync(vesselId)
BFF → Aizen.Modules.ServiceRequest → IServiceRequestQueryService.GetServiceHistoryByVesselAsync(vesselId)
```

**Not:** BFF bu 3 çağrıyı `Task.WhenAll` ile paralel yapar, tek response'a birleştirir.

---

### 4.3 — GET /bff/vessels/{id}/documents

**Kaynak Sayfa:** AH — Vessel Documents & Media (Documents tab)  
**Amaç:** Vessel'a ait tüm dökümanları versiyon geçmişiyle birlikte döndürür.

#### Request

| Parametre | Tip | Zorunlu |
|-----------|-----|---------|
| `id` | int (path) | Evet |
| `status` | string? (query) | Hayır | `valid/expiring/expired/archived` filter |

#### Response Body

```json
{
  "documents": [
    {
      "id": "doc-uuid-001",
      "documentType": "Registration Certificate",
      "documentCategory": "Legal",
      "issueDate": "2022-03-15T00:00:00Z",
      "expiryDate": "2027-03-15T00:00:00Z",
      "daysUntilExpiry": 270,
      "issuingAuthority": "Turkish Maritime Authority",
      "documentStatus": "valid",
      "fileUrl": "https://cdn.inktavia.com/documents/doc-001.pdf",
      "thumbnailUrl": "https://cdn.inktavia.com/documents/doc-001-thumb.jpg",
      "mimeType": "application/pdf",
      "fileSizeBytes": 245760,
      "approvedAt": "2022-03-20T10:00:00Z",
      "approvedByName": "Admin User",
      "versions": [
        {
          "versionId": "ver-uuid-001",
          "versionNumber": 2,
          "uploadedAt": "2022-03-15T09:00:00Z",
          "uploadedByName": "Admin User",
          "fileSizeBytes": 245760,
          "notes": "Annual renewal",
          "isCurrent": true
        },
        {
          "versionId": "ver-uuid-000",
          "versionNumber": 1,
          "uploadedAt": "2020-03-15T09:00:00Z",
          "uploadedByName": "Admin User",
          "fileSizeBytes": 238000,
          "notes": "Initial upload",
          "isCurrent": false
        }
      ]
    }
  ]
}
```

#### Alan Gereksinimleri

| Alan | Backend Kaynağı | Yeni mi? |
|------|----------------|----------|
| `documentType` | VesselDocument.DocumentType | Mevcut |
| `documentCategory` | VesselDocument.Category | **YENİ ALAN** |
| `issueDate` | VesselDocument.IssueDate | Mevcut |
| `expiryDate` | VesselDocument.ExpiryDate | Mevcut |
| `daysUntilExpiry` | BFF hesaplar: `(ExpiryDate - Today).Days` | BFF tarafında hesaplanır |
| `documentStatus` | BFF hesaplar: expired/expiring(≤90gün)/valid | BFF tarafında hesaplanır |
| `issuingAuthority` | VesselDocument.IssuingAuthority | **YENİ ALAN** |
| `fileUrl` | VesselDocumentVersion.FileUrl (current) | Mevcut |
| `thumbnailUrl` | VesselDocumentVersion.ThumbnailUrl | **YENİ ALAN** |
| `mimeType` | VesselDocumentVersion.MimeType | Mevcut veya eklenecek |
| `fileSizeBytes` | VesselDocumentVersion.FileSizeBytes | Mevcut veya eklenecek |
| `approvedAt` | VesselDocument.ApprovedAt | **YENİ ALAN** |
| `approvedByName` | Identity.User.FullName (ApprovedBy FK) | **YENİ join** |
| `versions[]` | VesselDocumentVersion tüm kayıtlar | Mevcut (kısmi) |
| `versions[].isCurrent` | VesselDocument.CurrentVersionId ile karşılaştır | BFF hesaplar |

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselDocumentQueryService.GetDocumentsByVesselAsync(vesselId, statusFilter?)
```

---

### 4.4 — GET /bff/vessels/{id}/media

**Kaynak Sayfa:** AH — Vessel Documents & Media (Media tab)  
**Amaç:** Vessel'a ait tüm medya dosyaları (fotoğraf + video)

#### Request

| Parametre | Tip | Zorunlu |
|-----------|-----|---------|
| `id` | int (path) | Evet |
| `mediaType` | string? (query) | Hayır | `image/video` |

#### Response Body

```json
{
  "media": [
    {
      "id": "media-uuid-001",
      "mediaType": "image",
      "title": "Exterior Port Side",
      "description": "Full exterior view from port side",
      "url": "https://cdn.inktavia.com/vessels/1001/media/exterior.jpg",
      "thumbnailUrl": "https://cdn.inktavia.com/vessels/1001/media/exterior-thumb.jpg",
      "mimeType": "image/jpeg",
      "fileSizeBytes": 3145728,
      "uploadedAt": "2024-03-20T10:00:00Z",
      "uploadedByName": "Admin User",
      "isPrimary": true,
      "sortOrder": 1
    }
  ]
}
```

#### Alan Gereksinimleri

| Alan | Backend Kaynağı | Yeni mi? |
|------|----------------|----------|
| `mediaType` | VesselMedia.MediaType | Mevcut |
| `title` | VesselMedia.Title | **YENİ ALAN** |
| `description` | VesselMedia.Description | **YENİ ALAN** |
| `url` | VesselMedia.Url | Mevcut |
| `thumbnailUrl` | VesselMedia.ThumbnailUrl | **YENİ ALAN** |
| `mimeType` | VesselMedia.MimeType | Mevcut veya eklenecek |
| `fileSizeBytes` | VesselMedia.FileSizeBytes | **YENİ ALAN** |
| `uploadedAt` | VesselMedia.CreateDate | Mevcut |
| `uploadedByName` | Identity.User.FullName (UploadedBy FK) | **YENİ join** |
| `isPrimary` | VesselMedia.IsPrimary | Mevcut |
| `sortOrder` | VesselMedia.SortOrder | **YENİ ALAN** |

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselMediaQueryService.GetMediaByVesselAsync(vesselId, mediaType?)
```

---

### 4.5 — POST /bff/vessels/{id}/documents/{documentId}/approve

**Kaynak Sayfa:** AH — DocumentSlidePanel "Approve" butonu  
**Amaç:** Dökümanı onayla

#### Request

```
POST /bff/vessels/{vesselId}/documents/{documentId}/approve
Body: {} (boş body)
```

#### Response Body

```json
{
  "document": {
    "id": "doc-uuid-001",
    "approvedAt": "2026-06-19T12:00:00Z",
    "approvedByName": "Admin User"
  }
}
```

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselDocumentCommandService.ApproveDocumentAsync(vesselId, documentId, adminUserId)
```

**Backend'de ApproveDocument command**: `VesselDocument.ApprovedAt = now`, `VesselDocument.ApprovedByUserId = adminUserId`

---

### 4.6 — POST /bff/vessels/{id}/documents/{documentId}/replace

**Kaynak Sayfa:** AH — DocumentSlidePanel "Replace" butonu  
**Amaç:** Dökümanın yeni versiyonunu yükle

#### Request

```
POST /bff/vessels/{vesselId}/documents/{documentId}/replace
Content-Type: multipart/form-data

file: [binary]
notes: "Renewal 2026"
```

#### Response Body

```json
{
  "document": {
    "id": "doc-uuid-001",
    "currentVersionNumber": 3,
    "fileUrl": "https://cdn.inktavia.com/documents/doc-001-v3.pdf",
    "fileSizeBytes": 251000,
    "uploadedAt": "2026-06-19T12:00:00Z"
  }
}
```

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselDocumentCommandService.ReplaceDocumentAsync(vesselId, documentId, file, notes, adminUserId)
```

**Backend'de**: Yeni `VesselDocumentVersion` kaydı oluştur, `VesselDocument.CurrentVersionId` güncelle.

---

### 4.7 — POST /bff/vessels

**Kaynak Sayfa:** AF — "Registry Register" butonu  
**Amaç:** Yeni vessel kaydı (form submit)

#### Request Body

```json
{
  "name": "Serenity V",
  "vesselTypeCode": "MOTOR_YACHT",
  "flagCountryCode": "TR",
  "yearBuilt": 2022,
  "builderName": "Azimut Benetti",
  "lengthMeters": 32.0,
  "grossTonnage": 145,
  "mmsiNumber": "271009999",
  "callSign": "TCABC",
  "homePort": "Bodrum, TR",
  "assetType": 1,
  "operationalStatus": 1
}
```

#### Response Body

```json
{
  "vessel": {
    "id": 1042,
    "vesselCode": "INK-2026-042",
    "name": "Serenity V",
    "slug": "serenity-v",
    "createDate": "2026-06-19T12:00:00Z"
  }
}
```

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselCommandService.RegisterVesselAsync(RegisterVesselCommand)
```

---

### 4.8 — GET /bff/vessels/export

**Kaynak Sayfa:** AF — "Export Registry" butonu  
**Amaç:** Filtreli vessel listesini Excel/CSV olarak indir

#### Request (query params — list endpoint ile aynı filtreler)

| Parametre | Tip |
|-----------|-----|
| `format` | string | `csv` veya `xlsx` (default: `xlsx`) |
| `search` | string? | |
| `assetTypes` | int[]? | |
| `ownershipStatuses` | int[]? | |
| `operationalStatuses` | int[]? | |

#### Response

```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="vessel-registry-2026-06-19.xlsx"
[binary file]
```

#### Backend Modülü Çağrısı

```
BFF → Aizen.Modules.Vessel → IVesselQueryService.ExportVesselListAsync(VesselExportQuery)
```

---

## 5. YENİ BACKEND ALANLARI ÖZETİ

Aşağıdaki alanlar mevcut backend entity'lerinde yoksa eklenmesi gerekir:

### Aizen.Modules.Vessel — Entity Değişiklikleri

#### Vessel entity
```csharp
public int OperationalStatus { get; private set; }  // 1=InService, 2=Refit, 3=Idle, 4=Decommissioned
public int AssetType { get; private set; }           // 1=MotorYacht, ... 6=Commercial
public string? HomePort { get; private set; }
```

#### VesselSpec entity (mevcut ise extend, yoksa oluştur)
```csharp
public string? BuilderName { get; private set; }
public string? BuildCountry { get; private set; }
public string? HullMaterial { get; private set; }
public string? SuperstructureMaterial { get; private set; }
```

#### VesselOwnership entity
```csharp
public int OwnershipStatus { get; private set; }  // 1=Private, 2=Charter, 3=Corporate
```

#### VesselDocument entity
```csharp
public string? DocumentCategory { get; private set; }
public string? IssuingAuthority { get; private set; }
public DateTime? ApprovedAt { get; private set; }
public Guid? ApprovedByUserId { get; private set; }
```

#### VesselDocumentVersion entity
```csharp
public string? ThumbnailUrl { get; private set; }
public string? MimeType { get; private set; }
public long? FileSizeBytes { get; private set; }
public Guid? UploadedByUserId { get; private set; }
public string? Notes { get; private set; }
```

#### VesselMedia entity
```csharp
public string? Title { get; private set; }
public string? Description { get; private set; }
public string? ThumbnailUrl { get; private set; }
public string? MimeType { get; private set; }
public long? FileSizeBytes { get; private set; }
public Guid? UploadedByUserId { get; private set; }
public int SortOrder { get; private set; }
```

#### **YENİ** VesselEngine entity (yeni tablo)
```csharp
public class VesselEngine
{
    public int Id { get; private set; }
    public int VesselId { get; private set; }
    public string? EngineType { get; private set; }
    public int? EngineCount { get; private set; }
    public decimal? EnginePowerKw { get; private set; }
    public string? EngineModel { get; private set; }
    public string? PropulsionType { get; private set; }
    public string? FuelType { get; private set; }
    public int? FuelCapacityL { get; private set; }
    public decimal? MaxSpeedKnots { get; private set; }
    public decimal? CruisingSpeedKnots { get; private set; }
    public int? RangeNm { get; private set; }
}
```

### Aizen.Modules.CargoDry — Gereken Query

Mevcut kit entity'sine `VesselId` bağlantısı varsa:
```
IQueryable<KitActivation>.Where(k => k.VesselId == vesselId)
```

Eğer yoksa: `KitActivation.VesselId` foreign key alanı eklenmeli.

CargoDry → BFF DTO:
```csharp
public record CargoDryKitBffDto(
    string KitId,
    string KitCode,
    string ProductName,
    DateTime ActivatedDate,
    DateTime ExpiryDate,
    int DaysUntilExpiry,  // BFF hesaplar
    int EfficiencyPercent, // CargoDry modülünden gelir
    string Status  // "active"|"expiring"|"expired" — BFF hesaplar
);
```

### Aizen.Modules.ServiceRequest — Gereken Query

ServiceHistory endpoint: vessel'a bağlı service request geçmişi.
Eğer `ServiceRequest.VesselId` varsa direkt sorgu; yoksa `VesselId` eklenmeli.

```csharp
public record ServiceHistoryBffDto(
    string Id,
    DateTime Date,
    string ServiceType,
    string? Provider,
    string? Location,
    string? Notes,
    string Status  // "completed"|"pending"|"cancelled"
);
```

---

## 6. EF CORE MİGRATION GEREKSİNİMLERİ

Aşağıdaki migration'ların yazılması gerekir:

| Migration Adı | İçerik |
|---------------|--------|
| `AddVesselOperationalAndAssetType` | `Vessel.OperationalStatus`, `Vessel.AssetType` sütunları |
| `AddVesselSpecExtensions` | `BuilderName`, `BuildCountry`, `HullMaterial`, `SuperstructureMaterial` |
| `AddVesselOwnershipStatus` | `VesselOwnership.OwnershipStatus` |
| `AddVesselDocumentExtensions` | `Category`, `IssuingAuthority`, `ApprovedAt`, `ApprovedByUserId` |
| `AddVesselDocumentVersionExtensions` | `ThumbnailUrl`, `MimeType`, `FileSizeBytes`, `UploadedByUserId`, `Notes` |
| `AddVesselMediaExtensions` | `Title`, `Description`, `ThumbnailUrl`, `MimeType`, `FileSizeBytes`, `UploadedByUserId`, `SortOrder` |
| `AddVesselEngineTable` | Yeni `VesselEngine` tablosu |
| `AddCargoDryKitVesselId` | `KitActivation.VesselId` (eğer yoksa) |
| `AddServiceRequestVesselId` | `ServiceRequest.VesselId` (eğer yoksa) |

---

## 7. BFF CONTROLLER YAPISI

```
Aizen.AdminPanel.BFF/
└── Controllers/
    └── Vessel/
        └── VesselBffController.cs
            GET    /bff/vessels                                → GetVesselList
            GET    /bff/vessels/{id}                          → GetVesselDetail
            POST   /bff/vessels                               → RegisterVessel
            GET    /bff/vessels/export                        → ExportVesselRegistry
            GET    /bff/vessels/{id}/documents                → GetVesselDocuments
            POST   /bff/vessels/{id}/documents/{docId}/approve → ApproveDocument
            POST   /bff/vessels/{id}/documents/{docId}/replace → ReplaceDocument
            GET    /bff/vessels/{id}/media                    → GetVesselMedia
```

---

## 8. ÖNCELİK SIRASI (MVP → Post-MVP)

### MVP (Frontend'in çalışması için zorunlu)
1. `GET /bff/vessels` — list (en az mevcut alanlarla, yeni alanlar null gelebilir)
2. `GET /bff/vessels/{id}` — detail (engine/cargoDry olmadan da açılabilir, null-safe)
3. `GET /bff/vessels/{id}/documents` — documents list
4. `GET /bff/vessels/{id}/media` — media list

### Post-MVP
5. `POST /bff/vessels/{id}/documents/{docId}/approve`
6. `POST /bff/vessels/{id}/documents/{docId}/replace`
7. `POST /bff/vessels` — vessel registration form
8. `GET /bff/vessels/export` — Excel export
9. Entity migrations + new fields
10. CargoDry & ServiceRequest cross-module joins
