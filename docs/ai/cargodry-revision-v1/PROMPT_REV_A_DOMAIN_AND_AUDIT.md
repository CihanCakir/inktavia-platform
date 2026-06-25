# PROMPT REV-A — CargoDry Domain Audit Fixes
# AizenEntityWithAudit + DocumentationInfo + EF Configuration Updates

## Context & Goal

The existing CargoDry domain (PROMPT_A_CARGODRY_DOMAIN.md) uses `AizenEntity<long>` as base class for all entities.
This must be replaced with `AizenEntityWithAudit` throughout.

`AizenEntityWithAudit` provides: `Id (long)`, `IsActive (bool)`, `IsDeleted (bool)`, `CreatedAt (DateTimeOffset)`, `UpdatedAt (DateTimeOffset)`, `CreatedBy (long?)`, `UpdatedBy (long?)`.

Because of this, any manually declared `CreatedAt` field in entities must be **removed**.
All entities must also receive `[DocumentationInfo]` attributes (pattern confirmed from Messaging module).

Module path: `Modules/CargoDry/src/`
Projects affected: `Aizen.Modules.CargoDry.Domain`, `Aizen.Modules.CargoDry.Repository`

---

## STEP 1 — Domain Entity Revisions

### 1.1 CargoDryProductEntity

**File:** `Domain/Entities/CargoDryProductEntity.cs`

**Changes:**
- `AizenEntity<long>` → `AizenEntityWithAudit`
- Remove `public DateTimeOffset CreatedAt { get; private set; }` — provided by base
- Remove `CreatedAt = DateTimeOffset.UtcNow` from `Create()` factory
- Add `[DocumentationInfo]` attribute
- Add `using Aizen.Core.Domain;`

```csharp
using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Product entity",
    "Defines a moisture protection kit product variant (e.g., Standard-90, Premium-180). " +
    "Product catalog is managed by admin. ValidityDays drives kit expiry calculation on activation.")]
public sealed class CargoDryProductEntity : AizenEntityWithAudit
{
    public string  ProductCode    { get; private set; } = default!;
    public string  Name           { get; private set; } = default!;
    public string  Description    { get; private set; } = default!;
    public int     ValidityDays   { get; private set; }
    public bool    HasSmartDevice { get; private set; }
    public decimal RetailPrice    { get; private set; }
    public string  CurrencyCode   { get; private set; } = default!;

    // IsActive inherited from AizenEntityWithAudit — do NOT redeclare
    // CreatedAt inherited from AizenEntityWithAudit — do NOT redeclare

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
            // CreatedAt set automatically by AizenEntityWithAudit / DbContext interceptor
        };

    public void SetActive(bool active) => IsActive = active;
    public void UpdatePrice(decimal price) => RetailPrice = price;
}
```

---

### 1.2 CargoDryBatchEntity

**File:** `Domain/Entities/CargoDryBatchEntity.cs`

**Changes:**
- `AizenEntity<long>` → `AizenEntityWithAudit`
- Remove `public DateTimeOffset CreatedAt { get; private set; }` — provided by base
- Remove `CreatedAt = DateTimeOffset.UtcNow` from `Create()`
- `CreatedByAdminId` stays — it is a domain-specific field (not the same as `CreatedBy` audit field)
- Add `[DocumentationInfo]` attribute

```csharp
using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Batch entity",
    "A production batch groups a set of generated kit serial numbers. Each batch has its own " +
    "HMAC secret key stored in Key Vault. Revoking a batch invalidates all un-activated kits in it.")]
public sealed class CargoDryBatchEntity : AizenEntityWithAudit
{
    public string  BatchCode        { get; private set; } = default!;
    public string  ProductCode      { get; private set; } = default!;
    public int     KitCount         { get; private set; }
    public bool    IsRevoked        { get; private set; }
    public string? RevokeReason     { get; private set; }
    public string? QrZipFileRef     { get; private set; }
    public string? ExcelFileRef     { get; private set; }
    // CreatedAt from AizenEntityWithAudit — DO NOT re-declare
    public DateTimeOffset? RevokedAt         { get; private set; }
    public long            CreatedByAdminId  { get; private set; }  // Domain-specific admin reference

    private CargoDryBatchEntity() { }

    public static CargoDryBatchEntity Create(
        string batchCode, string productCode, int kitCount, long adminId)
        => new()
        {
            BatchCode        = batchCode,
            ProductCode      = productCode,
            KitCount         = kitCount,
            IsRevoked        = false,
            CreatedByAdminId = adminId,
            IsActive         = true,
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

---

### 1.3 CargoDryKitEntity

**File:** `Domain/Entities/CargoDryKitEntity.cs`

**Changes:**
- `AizenEntity<long>` → `AizenEntityWithAudit`
- `ManufacturedAt` stays — it is a domain-specific timestamp (when the physical kit was manufactured), distinct from `CreatedAt` (when the DB record was created)
- Add `[DocumentationInfo]` attribute

```csharp
using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Kit entity",
    "The digital twin of a physical moisture protection kit. " +
    "Status transitions are enforced through domain methods only. " +
    "EfficiencyPercent and DaysUntilExpiry are computed properties — NOT mapped to the database.")]
public sealed class CargoDryKitEntity : AizenEntityWithAudit
{
    public string            SerialNumber   { get; private set; } = default!;
    public string            KitCode        { get; private set; } = default!;
    public string            QrPayload      { get; private set; } = default!;
    public string            ProductCode    { get; private set; } = default!;
    public string            BatchCode      { get; private set; } = default!;
    public CargoDryKitStatus Status         { get; private set; }
    public long?             OwnerUserId    { get; private set; }
    public long?             VesselId       { get; private set; }
    public DateTimeOffset    ManufacturedAt { get; private set; }   // Physical production date
    public DateTimeOffset?   ActivatedAt    { get; private set; }
    public DateTimeOffset?   ExpiresAt      { get; private set; }
    public int               RenewalCount   { get; private set; }
    public string?           RevokeReason   { get; private set; }
    public DateTimeOffset?   RevokedAt      { get; private set; }

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
            IsActive       = true,
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
        _ = paymentRef;
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
            throw new InvalidOperationException("Only active kits can be transferred.");
        OwnerUserId = newUserId;
        VesselId    = newVesselId;
        // Status remains Activated after transfer
    }

    // ── Computed Properties (NOT MAPPED) ──────────────────────────────────────

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

---

### 1.4 CargoDryRenewalEntity

**File:** `Domain/Entities/CargoDryRenewalEntity.cs`

**Changes:**
- `AizenEntity<long>` → `AizenEntityWithAudit`
- `RenewedAt` stays — domain timestamp of when the renewal action occurred
- Add `[DocumentationInfo]`

```csharp
using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Renewal entity",
    "Audit record for each kit renewal event. Stores the payment reference, added days, " +
    "and renewal type (OnlinePurchase / PhysicalKit / AdminExtension).")]
public sealed class CargoDryRenewalEntity : AizenEntityWithAudit
{
    public long           KitId        { get; private set; }
    public long           OwnerUserId  { get; private set; }
    public DateTimeOffset RenewedAt    { get; private set; }
    public DateTimeOffset NewExpiresAt { get; private set; }
    public int            AddedDays    { get; private set; }
    public RenewalType    RenewalType  { get; private set; }
    public string?        PaymentRef   { get; private set; }
    public long?          AdminUserId  { get; private set; }

    private CargoDryRenewalEntity() { }

    public static CargoDryRenewalEntity Create(
        long kitId, long ownerUserId, DateTimeOffset newExpiresAt,
        int addedDays, RenewalType type,
        string? paymentRef = null, long? adminUserId = null)
        => new()
        {
            KitId        = kitId,
            OwnerUserId  = ownerUserId,
            RenewedAt    = DateTimeOffset.UtcNow,
            NewExpiresAt = newExpiresAt,
            AddedDays    = addedDays,
            RenewalType  = type,
            PaymentRef   = paymentRef,
            AdminUserId  = adminUserId,
            IsActive     = true,
        };
}
```

---

### 1.5 Remove CargoDryActivationLogEntity from Domain

`CargoDryActivationLogEntity` is being migrated to MongoDB (see PROMPT_REV_B).

**Action:** Delete `Domain/Entities/CargoDryActivationLogEntity.cs` entirely after completing PROMPT_REV_B.

---

## STEP 2 — EF Core Configuration Updates

### 2.1 CargoDryProductEntityConfiguration

Remove the explicit `CreatedAt` column mapping — it is now handled by `AizenEntityWithAudit` base through EF interceptors.

**File:** `Repository/Persistence/Configurations/CargoDryProductEntityConfiguration.cs`

```csharp
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryProductEntityConfiguration : IEntityTypeConfiguration<CargoDryProductEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryProductEntity> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ProductCode).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ValidityDays).IsRequired();
        builder.Property(x => x.HasSmartDevice).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RetailPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        // IsActive: mapped by AizenEntityWithAudit base configuration
        // CreatedAt / UpdatedAt: mapped by AizenEntityWithAudit base configuration — DO NOT re-map
    }
}
```

### 2.2 CargoDryBatchEntityConfiguration

Remove `CreatedAt` from explicit mappings.

**File:** `Repository/Persistence/Configurations/CargoDryBatchEntityConfiguration.cs`

```csharp
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
        builder.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.QrZipFileRef).HasMaxLength(500);
        builder.Property(x => x.ExcelFileRef).HasMaxLength(500);
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.CreatedByAdminId).IsRequired();
        // CreatedAt / UpdatedAt: from AizenEntityWithAudit — DO NOT re-map
    }
}
```

### 2.3 CargoDryKitEntityConfiguration

No `CreatedAt` was manually mapped here in the original — verify `ManufacturedAt` stays and computed properties are still ignored.

```csharp
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
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ManufacturedAt).IsRequired();
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RenewalCount).HasDefaultValue(0);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.RevokedAt);

        // CRITICAL: Computed properties — NOT mapped to database
        builder.Ignore(x => x.EfficiencyPercent);
        builder.Ignore(x => x.DaysUntilExpiry);

        builder.HasIndex(x => new { x.OwnerUserId, x.Status });
        builder.HasIndex(x => new { x.VesselId, x.Status });
        builder.HasIndex(x => new { x.BatchCode, x.Status });
        builder.HasIndex(x => x.ExpiresAt);
        // CreatedAt / UpdatedAt: from AizenEntityWithAudit
    }
}
```

### 2.4 CargoDryRenewalEntityConfiguration

```csharp
public sealed class CargoDryRenewalEntityConfiguration : IEntityTypeConfiguration<CargoDryRenewalEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryRenewalEntity> builder)
    {
        builder.ToTable("renewals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.RenewedAt).IsRequired();
        builder.Property(x => x.NewExpiresAt).IsRequired();
        builder.Property(x => x.AddedDays).IsRequired();
        builder.Property(x => x.RenewalType).HasConversion<int>().IsRequired();
        builder.Property(x => x.PaymentRef).HasMaxLength(200);
        builder.HasIndex(x => x.KitId);
        builder.HasIndex(x => x.OwnerUserId);
    }
}
```

### 2.5 Remove CargoDryActivationLogEntityConfiguration

Delete `Repository/Persistence/Configurations/CargoDryActivationLogEntityConfiguration.cs` — activation logs are now in MongoDB (see PROMPT_REV_B).

### 2.6 Update CargoDryDbContext

Remove `ActivationLogs` DbSet — that collection moves to MongoDB.

```csharp
public sealed class CargoDryDbContext : AizenDbContext
{
    public CargoDryDbContext(DbContextOptions<CargoDryDbContext> options) : base(options) { }

    public DbSet<CargoDryProductEntity>  Products  => Set<CargoDryProductEntity>();
    public DbSet<CargoDryBatchEntity>    Batches   => Set<CargoDryBatchEntity>();
    public DbSet<CargoDryKitEntity>      Kits      => Set<CargoDryKitEntity>();
    public DbSet<CargoDryRenewalEntity>  Renewals  => Set<CargoDryRenewalEntity>();
    // ActivationLogs removed — moved to MongoDB

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("cargodry");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CargoDryDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}
```

---

## STEP 3 — Migration

After applying all entity changes, create a new migration:

```bash
dotnet ef migrations add AuditBaseRefactor \
  --project Aizen.Modules.CargoDry.Repository \
  --startup-project Aizen.Modules.CargoDry \
  --context CargoDryDbContext \
  --output-dir Persistence/Migrations
```

**Expected schema changes:**
- `cargodry.products`: `created_at`, `updated_at`, `created_by`, `updated_by` columns added (from base)
- `cargodry.batches`: same audit columns; `created_at` column previously defined manually is now from base
- `cargodry.kits`: audit columns added
- `cargodry.renewals`: audit columns added
- `cargodry.activation_logs` table: DROPPED (data moved to MongoDB)

---

## Verification Checklist

- [ ] All 4 remaining PostgreSQL entities extend `AizenEntityWithAudit` (not `AizenEntity<long>`)
- [ ] No entity has a manually declared `CreatedAt` property
- [ ] All entities have `[DocumentationInfo("...", "...")]`
- [ ] All EF configurations do NOT re-map `CreatedAt` / `UpdatedAt` / `CreatedBy` / `UpdatedBy`
- [ ] `CargoDryDbContext` has NO `ActivationLogs` DbSet
- [ ] `CargoDryActivationLogEntityConfiguration.cs` is deleted
- [ ] `CargoDryActivationLogEntity.cs` is deleted
- [ ] Migration `AuditBaseRefactor` applies without error
- [ ] `dotnet build` compiles with 0 errors
