# PROMPT A — CargoDry Module: Foundation (Abstraction + Domain + Repository)

## Context

CargoDry is Inktavia Marine OS'un **primary user acquisition channel**'ıdır.
Fiziksel kit ürünleri QR kod ile platformun onboarding flow'unu tetikler.
Bu nedenle güvenlik katmanı (HMAC imzalama, batch key rotation, activation token) domain'e entegre edilmiştir.

Module path: `Modules/CargoDry/src/`
Projects: `Aizen.Modules.CargoDry.Abstraction`, `Aizen.Modules.CargoDry.Domain`, `Aizen.Modules.CargoDry.Repository`

**İlk iş:** Her projedeki `Class1.cs` silinecek.

Architecture rules: Identity/ServiceRequest/Messaging modülleriyle aynı pattern. Namespace root: `Aizen.Modules.CargoDry`.

---

## STEP 1 — Abstraction Project

### 1.1 Enums

**`Enum/CargoDryKitStatus.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum CargoDryKitStatus
{
    Available   = 1,  // Üretildi, henüz aktive edilmedi
    Activated   = 2,  // Aktif kullanımda
    Expired     = 3,  // Süresi dolmuş (scheduler tarafından set edilir)
    Renewed     = 4,  // Yenilendi (eski kit closed)
    Revoked     = 5,  // Admin tarafından iptal edildi (fraud/hata)
    Lost        = 6,  // Kullanıcı kayıp bildirdi
    Transferred = 7,  // Yeni sahibine devredildi
}
```

**`Enum/ActivationMethod.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum ActivationMethod
{
    QrScan      = 1,  // Mobil QR tarama
    SerialEntry = 2,  // Manuel seri no girişi
    AdminForced = 3,  // Admin panelden force activation
}
```

**`Enum/ActivationSource.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum ActivationSource
{
    MobileApp  = 1,
    WebApp     = 2,
    AdminPanel = 3,
}
```

**`Enum/RenewalType.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum RenewalType
{
    OnlinePurchase = 1,  // Commerce modülü üzerinden
    PhysicalKit    = 2,  // Yeni fiziksel kit aktivasyonu ile
    AdminExtension = 3,  // Admin elle süre uzattı
}
```

---

### 1.2 DTOs

**`Dto/CargoDryProductDto.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProductDto
{
    public long    Id            { get; init; }
    public string  ProductCode   { get; init; } = default!;
    public string  Name          { get; init; } = default!;
    public string  Description   { get; init; } = default!;
    public int     ValidityDays  { get; init; }
    public bool    HasSmartDevice{ get; init; }
    public decimal RetailPrice   { get; init; }
    public string  CurrencyCode  { get; init; } = default!;
    public bool    IsActive      { get; init; }
}
```

**`Dto/CargoDryKitDto.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryKitDto
{
    public long              Id                { get; init; }
    public string            SerialNumber      { get; init; } = default!;
    public string            KitCode           { get; init; } = default!;  // Display-friendly code
    public string            ProductCode       { get; init; } = default!;
    public string            ProductName       { get; init; } = default!;
    public string            BatchCode         { get; init; } = default!;
    public CargoDryKitStatus Status            { get; init; }
    public long?             OwnerUserId       { get; init; }
    public string?           OwnerDisplayName  { get; init; }
    public long?             VesselId          { get; init; }
    public string?           VesselName        { get; init; }
    public DateTimeOffset?   ActivatedAt       { get; init; }
    public DateTimeOffset?   ExpiresAt         { get; init; }
    public double            EfficiencyPercent { get; init; }
    public int               DaysUntilExpiry   { get; init; }
    public int               RenewalCount      { get; init; }
    public DateTimeOffset    ManufacturedAt    { get; init; }
}
```

**`Dto/CargoDryKitValidationDto.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>QR/serial doğrulama sonucu. Public endpoint'ten döner (auth gerektirmez).</summary>
public sealed class CargoDryKitValidationDto
{
    public bool    IsValid          { get; init; }
    public string? InvalidReason    { get; init; }  // "BatchRevoked" | "InvalidSignature" | "KitUnavailable" | "AlreadyActivated"
    public string? ProductName      { get; init; }
    public string? ProductCode      { get; init; }
    public int     ValidityDays     { get; init; }
    public bool    HasSmartDevice   { get; init; }
    /// <summary>5 dakika TTL JWT. Sadece IsValid=true olduğunda dolu.</summary>
    public string? ActivationToken  { get; init; }
    public DateTimeOffset? TokenExpiresAt { get; init; }
}
```

**`Dto/CargoDryStatsDto.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryStatsDto
{
    public int TotalKits           { get; init; }
    public int AvailableKits       { get; init; }
    public int ActiveKits          { get; init; }
    public int ExpiringKits        { get; init; }  // 30 gün içinde sona erecek
    public int ExpiredKits         { get; init; }
    public int RevokedKits         { get; init; }
    public int TodayActivations    { get; init; }
    public int TotalBatches        { get; init; }
    public double RenewalRatePercent { get; init; }
}
```

**`Dto/GenerateBatchResultDto.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class GenerateBatchResultDto
{
    public string BatchCode       { get; init; } = default!;
    public int    GeneratedCount  { get; init; }
    public string QrZipFileUrl    { get; init; } = default!;   // FileStorage URL
    public string ExcelFileUrl    { get; init; } = default!;   // FileStorage URL
}
```

---

### 1.3 Integration Message Contracts

**`Message/CargoDryKitActivatedMessage.cs`**
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Consumed by: Notification, Vessel</summary>
public sealed class CargoDryKitActivatedMessage : AizenBaseMessage
{
    public long           KitId         { get; set; }
    public string         KitCode       { get; set; } = default!;
    public string         SerialNumber  { get; set; } = default!;
    public string         ProductName   { get; set; } = default!;
    public long           OwnerUserId   { get; set; }
    public long           VesselId      { get; set; }
    public DateTimeOffset ActivatedAt   { get; set; }
    public DateTimeOffset ExpiresAt     { get; set; }
    public int            ValidityDays  { get; set; }
}
```

**`Message/CargoDryKitExpiringMessage.cs`**
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Published by scheduler job. DaysLeft: 30 | 7 | 1. Consumed by: Notification</summary>
public sealed class CargoDryKitExpiringMessage : AizenBaseMessage
{
    public long           KitId         { get; set; }
    public string         KitCode       { get; set; } = default!;
    public string         ProductName   { get; set; } = default!;
    public long           OwnerUserId   { get; set; }
    public long           VesselId      { get; set; }
    public DateTimeOffset ExpiresAt     { get; set; }
    public int            DaysLeft      { get; set; }  // 30 | 7 | 1
}
```

**`Message/CargoDryKitExpiredMessage.cs`**
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Consumed by: Notification, Vessel (badge güncelleme)</summary>
public sealed class CargoDryKitExpiredMessage : AizenBaseMessage
{
    public long   KitId       { get; set; }
    public string KitCode     { get; set; } = default!;
    public long   OwnerUserId { get; set; }
    public long   VesselId    { get; set; }
}
```

**`Message/CargoDryKitRenewedMessage.cs`**
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Consumed by: Notification</summary>
public sealed class CargoDryKitRenewedMessage : AizenBaseMessage
{
    public long           KitId         { get; set; }
    public string         KitCode       { get; set; } = default!;
    public long           OwnerUserId   { get; set; }
    public DateTimeOffset NewExpiresAt  { get; set; }
    public string         RenewalType   { get; set; } = default!;
}
```

**`Message/CargoDryKitRevokedMessage.cs`**
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Consumed by: Notification, Vessel</summary>
public sealed class CargoDryKitRevokedMessage : AizenBaseMessage
{
    public long   KitId       { get; set; }
    public string KitCode     { get; set; } = default!;
    public long?  OwnerUserId { get; set; }
    public long?  VesselId    { get; set; }
    public string Reason      { get; set; } = default!;
}
```

---

### 1.4 Repository Interfaces

**`Interface/Repository/ICargoDryProductRepository.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

public interface ICargoDryProductRepository
{
    Task<CargoDryProductEntity?> GetByCodeAsync(string productCode, CancellationToken ct = default);
    Task<List<CargoDryProductEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(CargoDryProductEntity entity, CancellationToken ct = default);
}
```

**`Interface/Repository/ICargoDryKitRepository.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

public interface ICargoDryKitRepository
{
    Task<CargoDryKitEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CargoDryKitEntity?> GetBySerialAsync(string serialNumber, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetByOwnerAsync(long userId, CancellationToken ct = default);
    Task<CargoDryKitEntity?> GetActiveByVesselAsync(long vesselId, string productCode, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetExpiringAsync(int withinDays, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetExpiredUnmarkedAsync(CancellationToken ct = default);
    Task<(List<CargoDryKitEntity> Items, int Total)> GetPagedAsync(
        CargoDryKitStatus? status, string? search, int skip, int take, CancellationToken ct = default);
    Task<CargoDryStatsProjection> GetStatsAsync(CancellationToken ct = default);
    Task AddAsync(CargoDryKitEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<CargoDryKitEntity> entities, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class CargoDryStatsProjection
{
    public int Total { get; init; }
    public int Available { get; init; }
    public int Active { get; init; }
    public int Expiring { get; init; }
    public int Expired { get; init; }
    public int Revoked { get; init; }
    public int TodayActivations { get; init; }
}
```

**`Interface/Repository/ICargoDryBatchRepository.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

public interface ICargoDryBatchRepository
{
    Task<CargoDryBatchEntity?> GetByCodeAsync(string batchCode, CancellationToken ct = default);
    Task<List<CargoDryBatchEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(CargoDryBatchEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### 1.5 Service Interfaces

**`Interface/Service/ICargoDryQrService.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// QR payload imzalama ve doğrulama.
/// HMAC-SHA256 kullanır. Batch-specific secret key Azure Key Vault'ta saklanır.
/// </summary>
public interface ICargoDryQrService
{
    /// <summary>Seri no + batch'e ait HMAC imzasını üretir (üretim zamanında kullanılır).</summary>
    Task<string> SignAsync(string serialNumber, string batchCode, CancellationToken ct = default);

    /// <summary>QR payload'dan gelen imzayı doğrular. FixedTimeEquals kullanılır.</summary>
    Task<bool> VerifyAsync(string serialNumber, string batchCode, string signature, CancellationToken ct = default);

    /// <summary>Yüksek-entropy seri no üretir. Format: XXXX-XXXX-XXXX-XXXX (Base32)</summary>
    string GenerateSerialNumber();

    /// <summary>QR PNG üretir (URL içeren).</summary>
    byte[] GenerateQrCode(string payload);
}
```

**`Interface/Service/IActivationTokenService.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Validate→Activate adımları arasındaki kısa ömürlü JWT.
/// 5 dakika TTL. JTI ile tek kullanımlık (Redis blacklist).
/// Race condition ve QR-fotoğraf saldırılarına karşı koruma sağlar.
/// </summary>
public interface IActivationTokenService
{
    string Generate(string serialNumber);
    ActivationTokenClaims? Verify(string token);
}

public sealed class ActivationTokenClaims
{
    public string SerialNumber { get; init; } = default!;
    public string Jti          { get; init; } = default!;
    public DateTimeOffset ExpiresAt { get; init; }
}
```

**`Interface/Service/IBatchKeyVaultService.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Her production batch'i için ayrı HMAC secret key saklar.
/// Production: Azure Key Vault / AWS KMS.
/// Development: appsettings.json fallback.
/// </summary>
public interface IBatchKeyVaultService
{
    Task<string> GetKeyAsync(string batchCode, CancellationToken ct = default);
    Task<string> CreateKeyAsync(string batchCode, CancellationToken ct = default);
}
```

---

## STEP 2 — Domain Project

### 2.1 CargoDryProductEntity

**`Domain/Entities/CargoDryProductEntity.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryProductEntity : AizenEntity<long>
{
    public string  ProductCode   { get; private set; } = default!;
    public string  Name          { get; private set; } = default!;
    public string  Description   { get; private set; } = default!;
    public int     ValidityDays  { get; private set; }
    public bool    HasSmartDevice{ get; private set; }
    public decimal RetailPrice   { get; private set; }
    public string  CurrencyCode  { get; private set; } = default!;
    public bool    IsActive      { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CargoDryProductEntity() { }

    public static CargoDryProductEntity Create(
        string productCode, string name, string description,
        int validityDays, decimal retailPrice, string currencyCode,
        bool hasSmartDevice = false)
        => new()
        {
            ProductCode    = productCode.ToUpperInvariant(),
            Name           = name,
            Description    = description,
            ValidityDays   = validityDays,
            RetailPrice    = retailPrice,
            CurrencyCode   = currencyCode.ToUpperInvariant(),
            HasSmartDevice = hasSmartDevice,
            IsActive       = true,
            CreatedAt      = DateTimeOffset.UtcNow,
        };

    public void SetActive(bool active) => IsActive = active;
    public void UpdatePrice(decimal price) => RetailPrice = price;
}
```

### 2.2 CargoDryBatchEntity

**`Domain/Entities/CargoDryBatchEntity.cs`**
```csharp
namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>
/// Üretim partisi. Her batch için ayrı HMAC secret key (Key Vault'ta).
/// Batch revoke edilirse tüm aktive edilmemiş kitleri geçersiz olur.
/// </summary>
public sealed class CargoDryBatchEntity : AizenEntity<long>
{
    public string  BatchCode       { get; private set; } = default!;
    public string  ProductCode     { get; private set; } = default!;
    public int     KitCount        { get; private set; }
    public bool    IsRevoked       { get; private set; }
    public string? RevokeReason    { get; private set; }
    public string? QrZipFileRef    { get; private set; }  // FileStorage ref
    public string? ExcelFileRef    { get; private set; }  // FileStorage ref
    public DateTimeOffset CreatedAt  { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public long CreatedByAdminId   { get; private set; }

    private CargoDryBatchEntity() { }

    public static CargoDryBatchEntity Create(
        string batchCode, string productCode, int kitCount, long adminId)
        => new()
        {
            BatchCode         = batchCode,
            ProductCode       = productCode,
            KitCount          = kitCount,
            IsRevoked         = false,
            CreatedAt         = DateTimeOffset.UtcNow,
            CreatedByAdminId  = adminId,
        };

    public void Revoke(string reason)
    {
        IsRevoked    = true;
        RevokeReason = reason;
        RevokedAt    = DateTimeOffset.UtcNow;
    }

    public void SetFileRefs(string qrZipRef, string excelRef)
    {
        QrZipFileRef = qrZipRef;
        ExcelFileRef = excelRef;
    }
}
```

### 2.3 CargoDryKitEntity

**`Domain/Entities/CargoDryKitEntity.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>
/// Fiziksel kit biriminin dijital karşılığı.
/// Her kutu bir SerialNumber ve QrPayload ile eşleşir.
/// Domain invariant: Status geçişleri sadece domain method'lar üzerinden.
/// </summary>
public sealed class CargoDryKitEntity : AizenEntity<long>
{
    public string            SerialNumber      { get; private set; } = default!;
    public string            KitCode           { get; private set; } = default!;
    public string            QrPayload         { get; private set; } = default!;
    public string            ProductCode       { get; private set; } = default!;
    public string            BatchCode         { get; private set; } = default!;
    public CargoDryKitStatus Status            { get; private set; }
    public long?             OwnerUserId       { get; private set; }
    public long?             VesselId          { get; private set; }
    public DateTimeOffset    ManufacturedAt    { get; private set; }
    public DateTimeOffset?   ActivatedAt       { get; private set; }
    public DateTimeOffset?   ExpiresAt         { get; private set; }
    public int               RenewalCount      { get; private set; }
    public string?           RevokeReason      { get; private set; }
    public DateTimeOffset?   RevokedAt         { get; private set; }

    private CargoDryKitEntity() { }

    public static CargoDryKitEntity Create(
        string serialNumber, string kitCode, string qrPayload,
        string productCode, string batchCode)
        => new()
        {
            SerialNumber   = serialNumber,
            KitCode        = kitCode,
            QrPayload      = qrPayload,
            ProductCode    = productCode,
            BatchCode      = batchCode,
            Status         = CargoDryKitStatus.Available,
            ManufacturedAt = DateTimeOffset.UtcNow,
        };

    // ── Domain Methods ────────────────────────────────────────────────────────

    public void Activate(long userId, long vesselId, int validityDays)
    {
        if (Status != CargoDryKitStatus.Available)
            throw new InvalidOperationException(
                $"Kit {SerialNumber} cannot be activated — current status: {Status}");

        Status      = CargoDryKitStatus.Activated;
        OwnerUserId = userId;
        VesselId    = vesselId;
        ActivatedAt = DateTimeOffset.UtcNow;
        ExpiresAt   = DateTimeOffset.UtcNow.AddDays(validityDays);
    }

    /// <summary>
    /// Mevcut kit süresi uzatılır (same kit, extended expiry).
    /// Commerce renewal tamamlandığında çağrılır.
    /// </summary>
    public void Renew(int additionalDays, string paymentRef)
    {
        if (Status != CargoDryKitStatus.Activated && Status != CargoDryKitStatus.Expired)
            throw new InvalidOperationException($"Kit {SerialNumber} cannot be renewed — status: {Status}");

        var baseDate  = ExpiresAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow
            ? ExpiresAt.Value
            : DateTimeOffset.UtcNow;
        ExpiresAt     = baseDate.AddDays(additionalDays);
        Status        = CargoDryKitStatus.Activated;
        RenewalCount += 1;
        _ = paymentRef; // PaymentRef is stored in CargoDryRenewalEntity
    }

    public void MarkExpired()
    {
        if (Status == CargoDryKitStatus.Activated)
            Status = CargoDryKitStatus.Expired;
    }

    public void Revoke(string reason)
    {
        Status       = CargoDryKitStatus.Revoked;
        RevokeReason = reason;
        RevokedAt    = DateTimeOffset.UtcNow;
    }

    public void Transfer(long newUserId, long newVesselId)
    {
        if (Status != CargoDryKitStatus.Activated)
            throw new InvalidOperationException($"Only active kits can be transferred.");

        Status      = CargoDryKitStatus.Transferred;
        OwnerUserId = newUserId;
        VesselId    = newVesselId;
        Status      = CargoDryKitStatus.Activated; // Transfer sonrası aktif kalır
    }

    // ── Calculated Properties ─────────────────────────────────────────────────

    public double EfficiencyPercent
    {
        get
        {
            if (!ExpiresAt.HasValue || !ActivatedAt.HasValue) return 0;
            var total     = (ExpiresAt.Value - ActivatedAt.Value).TotalDays;
            var remaining = (ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
            return total <= 0 ? 0 : Math.Max(0, Math.Min(100, remaining / total * 100));
        }
    }

    public int DaysUntilExpiry
        => ExpiresAt.HasValue
            ? (int)Math.Max(0, Math.Ceiling((ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays))
            : 0;

    public bool IsExpiringSoon(int withinDays = 30)
        => Status == CargoDryKitStatus.Activated && DaysUntilExpiry <= withinDays;
}
```

### 2.4 CargoDryActivationLogEntity

**`Domain/Entities/CargoDryActivationLogEntity.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>Fraud detection ve audit için her aktivasyon olayı kaydedilir.</summary>
public sealed class CargoDryActivationLogEntity : AizenEntity<long>
{
    public long             KitId            { get; private set; }
    public long             UserId           { get; private set; }
    public long             VesselId         { get; private set; }
    public DateTimeOffset   ActivatedAt      { get; private set; }
    public ActivationMethod Method           { get; private set; }
    public ActivationSource Source           { get; private set; }
    public string?          DeviceInfo       { get; private set; }
    public string?          IpAddress        { get; private set; }

    private CargoDryActivationLogEntity() { }

    public static CargoDryActivationLogEntity Create(
        long kitId, long userId, long vesselId,
        ActivationMethod method, ActivationSource source,
        string? deviceInfo = null, string? ipAddress = null)
        => new()
        {
            KitId       = kitId,
            UserId      = userId,
            VesselId    = vesselId,
            ActivatedAt = DateTimeOffset.UtcNow,
            Method      = method,
            Source      = source,
            DeviceInfo  = deviceInfo,
            IpAddress   = ipAddress,
        };
}
```

### 2.5 CargoDryRenewalEntity

**`Domain/Entities/CargoDryRenewalEntity.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryRenewalEntity : AizenEntity<long>
{
    public long           KitId          { get; private set; }
    public long           OwnerUserId    { get; private set; }
    public DateTimeOffset RenewedAt      { get; private set; }
    public DateTimeOffset NewExpiresAt   { get; private set; }
    public int            AddedDays      { get; private set; }
    public RenewalType    RenewalType    { get; private set; }
    public string?        PaymentRef     { get; private set; }  // Commerce order ID
    public long?          AdminUserId    { get; private set; }  // AdminExtension için

    private CargoDryRenewalEntity() { }

    public static CargoDryRenewalEntity Create(
        long kitId, long ownerUserId, DateTimeOffset newExpiresAt,
        int addedDays, RenewalType type,
        string? paymentRef = null, long? adminUserId = null)
        => new()
        {
            KitId       = kitId,
            OwnerUserId = ownerUserId,
            RenewedAt   = DateTimeOffset.UtcNow,
            NewExpiresAt = newExpiresAt,
            AddedDays   = addedDays,
            RenewalType = type,
            PaymentRef  = paymentRef,
            AdminUserId = adminUserId,
        };
}
```

---

## STEP 3 — Repository Project

### 3.1 DbContext

**`Repository/Persistence/CargoDryDbContext.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

public sealed class CargoDryDbContext : DbContext
{
    public CargoDryDbContext(DbContextOptions<CargoDryDbContext> options) : base(options) { }

    public DbSet<CargoDryProductEntity>      Products       => Set<CargoDryProductEntity>();
    public DbSet<CargoDryBatchEntity>        Batches        => Set<CargoDryBatchEntity>();
    public DbSet<CargoDryKitEntity>          Kits           => Set<CargoDryKitEntity>();
    public DbSet<CargoDryActivationLogEntity> ActivationLogs => Set<CargoDryActivationLogEntity>();
    public DbSet<CargoDryRenewalEntity>      Renewals       => Set<CargoDryRenewalEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cargodry");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CargoDryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 3.2 Entity Configurations

**`Configurations/CargoDryKitEntityConfiguration.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryKitEntityConfiguration : IEntityTypeConfiguration<CargoDryKitEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryKitEntity> builder)
    {
        builder.ToTable("kits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.SerialNumber).IsUnique();

        builder.Property(x => x.KitCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.KitCode).IsUnique();

        builder.Property(x => x.QrPayload).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.OwnerUserId);
        builder.Property(x => x.VesselId);
        builder.Property(x => x.ManufacturedAt).IsRequired();
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RenewalCount).HasDefaultValue(0);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.RevokedAt);

        // EfficiencyPercent ve DaysUntilExpiry hesaplanan property'ler — NOT MAPPED
        builder.Ignore(x => x.EfficiencyPercent);
        builder.Ignore(x => x.DaysUntilExpiry);

        builder.HasIndex(x => new { x.OwnerUserId, x.Status });
        builder.HasIndex(x => new { x.VesselId, x.Status });
        builder.HasIndex(x => new { x.BatchCode, x.Status });
        builder.HasIndex(x => x.ExpiresAt);  // Expiry scheduler için
    }
}
```

**`Configurations/CargoDryBatchEntityConfiguration.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryBatchEntityConfiguration : IEntityTypeConfiguration<CargoDryBatchEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryBatchEntity> builder)
    {
        builder.ToTable("batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.BatchCode).IsUnique();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.KitCount).IsRequired();
        builder.Property(x => x.IsRevoked).IsRequired();
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.QrZipFileRef).HasMaxLength(500);
        builder.Property(x => x.ExcelFileRef).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.CreatedByAdminId).IsRequired();
    }
}
```

> Implement `CargoDryProductEntityConfiguration`, `CargoDryActivationLogEntityConfiguration`, `CargoDryRenewalEntityConfiguration` following the same pattern. Table names: `products`, `activation_logs`, `renewals`.

### 3.3 Repositories

**`Repository/Repositories/CargoDryKitRepository.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryKitRepository : ICargoDryKitRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryKitRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryKitEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDryKitEntity?> GetBySerialAsync(string serialNumber, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x => x.SerialNumber == serialNumber, ct);

    public Task<List<CargoDryKitEntity>> GetByOwnerAsync(long userId, CancellationToken ct)
        => _db.Kits
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.ActivatedAt)
            .ToListAsync(ct);

    public Task<CargoDryKitEntity?> GetActiveByVesselAsync(
        long vesselId, string productCode, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x =>
            x.VesselId == vesselId &&
            x.ProductCode == productCode &&
            x.Status == CargoDryKitStatus.Activated, ct);

    public Task<List<CargoDryKitEntity>> GetExpiringAsync(int withinDays, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(withinDays);
        return _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value <= cutoff
                     && x.ExpiresAt.Value > DateTimeOffset.UtcNow)
            .ToListAsync(ct);
    }

    public Task<List<CargoDryKitEntity>> GetExpiredUnmarkedAsync(CancellationToken ct)
        => _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value < DateTimeOffset.UtcNow)
            .ToListAsync(ct);

    public async Task<(List<CargoDryKitEntity> Items, int Total)> GetPagedAsync(
        CargoDryKitStatus? status, string? search, int skip, int take, CancellationToken ct)
    {
        var q = _db.Kits.AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SerialNumber.Contains(search) || x.KitCode.Contains(search));
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.ManufacturedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<CargoDryStatsProjection> GetStatsAsync(CancellationToken ct)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var in30Days = DateTimeOffset.UtcNow.AddDays(30);

        return new CargoDryStatsProjection
        {
            Total     = await _db.Kits.CountAsync(ct),
            Available = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Available, ct),
            Active    = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Activated, ct),
            Expiring  = await _db.Kits.CountAsync(x =>
                x.Status == CargoDryKitStatus.Activated &&
                x.ExpiresAt.HasValue && x.ExpiresAt.Value <= in30Days, ct),
            Expired   = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Expired, ct),
            Revoked   = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Revoked, ct),
            TodayActivations = await _db.Kits.CountAsync(x =>
                x.ActivatedAt.HasValue &&
                x.ActivatedAt.Value.Date == today, ct),
        };
    }

    public async Task AddAsync(CargoDryKitEntity entity, CancellationToken ct)
    {
        await _db.Kits.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<CargoDryKitEntity> entities, CancellationToken ct)
    {
        await _db.Kits.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
```

> `CargoDryProductRepository`, `CargoDryBatchRepository` aynı pattern'da implement edilecek.

### 3.4 Seed Data

**`Repository/Seed/CargoDryProductSeed.cs`**
```csharp
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Repository.Seed;

public sealed class CargoDryProductSeed
{
    private readonly CargoDryDbContext _db;
    private readonly ILogger<CargoDryProductSeed> _logger;

    public CargoDryProductSeed(CargoDryDbContext db, ILogger<CargoDryProductSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var products = new[]
        {
            CargoDryProductEntity.Create("STANDARD-90",  "CargoDry Standard",
                "90-day moisture protection kit for standard marine storage.",
                90, 149.99m, "USD"),
            CargoDryProductEntity.Create("PREMIUM-180", "CargoDry Premium",
                "180-day heavy-duty moisture control for yacht bilges and cabins.",
                180, 249.99m, "USD"),
            CargoDryProductEntity.Create("PREMIUM-365", "CargoDry Premium Annual",
                "365-day comprehensive moisture management solution.",
                365, 399.99m, "USD"),
            CargoDryProductEntity.Create("SMART-90", "CargoDry Smart",
                "90-day smart kit with IoT humidity sensor and real-time monitoring.",
                90, 299.99m, "USD", hasSmartDevice: true),
        };

        foreach (var product in products)
        {
            var exists = await _db.Products.AnyAsync(
                x => x.ProductCode == product.ProductCode, ct);
            if (!exists)
            {
                await _db.Products.AddAsync(product, ct);
                _logger.LogInformation("Seeding CargoDry product: {Code}", product.ProductCode);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

### 3.5 DI Registration

**`Repository/DependencyInjection.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using Aizen.Modules.CargoDry.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.CargoDry.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryRepository(this IServiceCollection services)
    {
        services.AddScoped<ICargoDryProductRepository, CargoDryProductRepository>();
        services.AddScoped<ICargoDryKitRepository,     CargoDryKitRepository>();
        services.AddScoped<ICargoDryBatchRepository,   CargoDryBatchRepository>();
        services.AddScoped<CargoDryProductSeed>();
        return services;
    }

    public static async Task SeedCargoDryAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoDryDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);
        var seeder = scope.ServiceProvider.GetRequiredService<CargoDryProductSeed>();
        await seeder.SeedAsync(ct);
    }
}
```

---

## STEP 4 — csproj References

**`Aizen.Modules.CargoDry.Abstraction.csproj`** — ekle:
```xml
<PackageReference Include="Aizen.Core.Messagebus.Abstraction" Version="*" />
```

**`Aizen.Modules.CargoDry.Domain.csproj`** — ekle:
```xml
<ProjectReference Include="..\Aizen.Modules.CargoDry.Abstraction\..." />
```

**`Aizen.Modules.CargoDry.Repository.csproj`** — ekle:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.*" />
<ProjectReference Include="..\Aizen.Modules.CargoDry.Domain\..." />
<ProjectReference Include="..\Aizen.Modules.CargoDry.Abstraction\..." />
```

## Migration

```bash
dotnet ef migrations add InitialCreate \
  --project Aizen.Modules.CargoDry.Repository \
  --startup-project Aizen.Modules.CargoDry \
  --context CargoDryDbContext \
  --output-dir Persistence/Migrations
```

## Verification Checklist

- [ ] 5 entity derlenir, AizenEntity'den extends eder
- [ ] `CargoDryKitEntity.Activate()` Available→Activated geçişini uygular, diğer statüslerde throw
- [ ] `CargoDryKitEntity.EfficiencyPercent` hesaplanan property — EF tarafından NOT MAPPED
- [ ] `CargoDryBatchEntity.Revoke()` IsRevoked=true set eder
- [ ] Schema `cargodry`, table'lar: `products`, `batches`, `kits`, `activation_logs`, `renewals`
- [ ] `CargoDryKitRepository.GetExpiringAsync(30)` yaklaşan 30 gün içindeki aktif kitleri döner
- [ ] Seed: 4 ürün (Standard-90, Premium-180, Premium-365, Smart-90) — idempotent
