# PROMPT REV-B — CargoDry MongoDB Integration
# Activation Log Document + Kit Usage Snapshot Document

## Context & Rationale

The PostgreSQL `CargoDryActivationLogEntity` is being migrated to MongoDB for two reasons:

1. **Write pattern**: Activation logs are append-only. MongoDB is a better fit for high-volume, schema-flexible audit documents.
2. **Analytics source**: The daily snapshot job aggregates activation log documents by date/product/vessel — much cheaper than PostgreSQL GROUP BY on large tables.
3. **Future telemetry**: Smart device data (SMART-90 product line) will attach telemetry payloads to activation records. MongoDB's flexible schema avoids costly migrations.

**New MongoDB documents:**
- `CargoDryActivationLogDocument` — written on every kit activation, revocation, expiry
- `CargoDryKitUsageSnapshotDocument` — daily computed analytics snapshot consumed by the analytics endpoint

Module: `Aizen.Modules.CargoDry`
Reference package: `Aizen.Core.Data.Mongo` (already available in the platform)

---

## STEP 1 — MongoDB Document Classes

### 1.1 CargoDryActivationLogDocument

**File:** `Domain/MongoDocuments/CargoDryActivationLogDocument.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Append-only audit record for every kit lifecycle event (activation, revocation, expiry, renewal).
/// Stored in MongoDB collection: cargodry_activation_logs.
/// High-write, schema-flexible, never updated after insert.
/// </summary>
public sealed class CargoDryActivationLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>PostgreSQL Kit.Id — foreign reference, not a MongoDB ObjectId.</summary>
    public long KitId         { get; set; }
    public string SerialNumber { get; set; } = default!;
    public string KitCode      { get; set; } = default!;
    public string ProductCode  { get; set; } = default!;
    public string BatchCode    { get; set; } = default!;

    /// <summary>Type of event that generated this log entry.</summary>
    public string EventType    { get; set; } = default!;  // "Activated" | "Revoked" | "Expired" | "Renewed" | "Extended"

    // ── Activation-specific ────────────────────────────────────────────────────
    public long?  OwnerUserId   { get; set; }
    public long?  VesselId      { get; set; }
    public string? ActivationMethod { get; set; }   // "QrScan" | "SerialEntry" | "AdminForced"
    public string? ActivationSource { get; set; }   // "MobileApp" | "WebApp" | "AdminPanel"
    public string? DeviceInfo   { get; set; }
    public string? IpAddress    { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    // ── Revocation-specific ───────────────────────────────────────────────────
    public string? RevokeReason { get; set; }

    // ── Renewal-specific ──────────────────────────────────────────────────────
    public int?    AddedDays       { get; set; }
    public string? RenewalType     { get; set; }
    public string? PaymentRef      { get; set; }

    // ── Smart device (future SMART-90 telemetry) ──────────────────────────────
    public Dictionary<string, object>? TelemetryPayload { get; set; }

    // ── Common ────────────────────────────────────────────────────────────────
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Calendar date string (YYYY-MM-DD) for efficient date-range queries without conversion.</summary>
    public string DateKey { get; set; } = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
}
```

---

### 1.2 CargoDryKitUsageSnapshotDocument

**File:** `Domain/MongoDocuments/CargoDryKitUsageSnapshotDocument.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Daily aggregated analytics snapshot computed by DailySnapshotJob.
/// Stored in MongoDB collection: cargodry_usage_snapshots.
/// One document per calendar date. Upserted daily at 00:05 UTC.
/// Consumed by GetCargoDryAnalyticsQueryHandler for O(1) read.
/// </summary>
public sealed class CargoDryKitUsageSnapshotDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = default!;   // DateKey: "2026-06-25"

    public string DateKey         { get; set; } = default!;   // "2026-06-25"
    public DateTimeOffset ComputedAt { get; set; }

    // ── Daily counts ──────────────────────────────────────────────────────────
    public int Activations  { get; set; }
    public int Renewals     { get; set; }
    public int Expirations  { get; set; }
    public int Revocations  { get; set; }
    public int Extensions   { get; set; }

    // ── Efficiency distribution ───────────────────────────────────────────────
    public List<EfficiencyBucketSnapshot> EfficiencyBuckets { get; set; } = [];

    // ── By product breakdown ──────────────────────────────────────────────────
    public List<ProductDaySnapshot> ByProduct { get; set; } = [];
}

public sealed class EfficiencyBucketSnapshot
{
    public string Bucket { get; set; } = default!;   // "0-25" | "25-50" | "50-75" | "75-100"
    public int Count     { get; set; }
}

public sealed class ProductDaySnapshot
{
    public string ProductCode  { get; set; } = default!;
    public string ProductName  { get; set; } = default!;
    public int    Activations  { get; set; }
    public int    Renewals     { get; set; }
}
```

---

## STEP 2 — Repository Interfaces

### 2.1 ICargoDryActivationLogRepository

**File:** `Abstraction/Interface/Repository/ICargoDryActivationLogRepository.cs`

```csharp
using Aizen.Modules.CargoDry.Domain.MongoDocuments;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

public interface ICargoDryActivationLogRepository
{
    /// <summary>Insert a new log entry. Fire-and-forget safe (don't await on hot path).</summary>
    Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct = default);

    /// <summary>Get all log entries for a specific kit.</summary>
    Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct = default);

    /// <summary>Get all activations within a date range (inclusive). DateKey format: "YYYY-MM-DD".</summary>
    Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType = null, CancellationToken ct = default);

    /// <summary>Count events by type for a given date key.</summary>
    Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct = default);

    /// <summary>Activation counts grouped by product code for a date range.</summary>
    Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct = default);
}
```

---

### 2.2 ICargoDrySnapshotRepository

**File:** `Abstraction/Interface/Repository/ICargoDrySnapshotRepository.cs`

```csharp
using Aizen.Modules.CargoDry.Domain.MongoDocuments;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

public interface ICargoDrySnapshotRepository
{
    /// <summary>Upsert a daily snapshot by DateKey.</summary>
    Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct = default);

    /// <summary>Get the N most recent snapshots for trend charts.</summary>
    Task<List<CargoDryKitUsageSnapshotDocument>> GetRecentAsync(int days = 30, CancellationToken ct = default);

    /// <summary>Get a single snapshot by date key. Returns null if not yet computed.</summary>
    Task<CargoDryKitUsageSnapshotDocument?> GetByDateKeyAsync(string dateKey, CancellationToken ct = default);
}
```

---

## STEP 3 — MongoDB Repository Implementations

### 3.1 CargoDryActivationLogRepository

**File:** `Repository/Repositories/CargoDryActivationLogRepository.cs`

```csharp
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryActivationLogRepository : ICargoDryActivationLogRepository
{
    private readonly IMongoCollection<CargoDryActivationLogDocument> _collection;

    public CargoDryActivationLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");
    }

    public Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct)
        => _collection.InsertOneAsync(document, cancellationToken: ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct)
        => await _collection
            .Find(x => x.KitId == kitId)
            .SortByDescending(x => x.OccurredAt)
            .ToListAsync(ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey)
        );

        if (!string.IsNullOrEmpty(eventType))
            filter &= Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, eventType);

        return await _collection.Find(filter).SortByDescending(x => x.OccurredAt).ToListAsync(ct);
    }

    public async Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct)
    {
        var docs = await _collection
            .Find(x => x.DateKey == dateKey)
            .Project(x => x.EventType)
            .ToListAsync(ct);

        return docs.GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, "Activated")
        );

        var docs = await _collection
            .Find(filter)
            .Project(x => x.ProductCode)
            .ToListAsync(ct);

        return docs.GroupBy(p => p)
            .Select(g => (ProductCode: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
```

---

### 3.2 CargoDrySnapshotRepository

**File:** `Repository/Repositories/CargoDrySnapshotRepository.cs`

```csharp
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDrySnapshotRepository : ICargoDrySnapshotRepository
{
    private readonly IMongoCollection<CargoDryKitUsageSnapshotDocument> _collection;

    public CargoDrySnapshotRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
    }

    public Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct)
    {
        var filter  = Builders<CargoDryKitUsageSnapshotDocument>.Filter.Eq(x => x.Id, document.DateKey);
        var options = new ReplaceOptions { IsUpsert = true };
        return _collection.ReplaceOneAsync(filter, document, options, ct);
    }

    public async Task<List<CargoDryKitUsageSnapshotDocument>> GetRecentAsync(int days, CancellationToken ct)
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        return await _collection
            .Find(x => string.Compare(x.DateKey, fromDate, StringComparison.Ordinal) >= 0)
            .SortByDescending(x => x.DateKey)
            .ToListAsync(ct);
    }

    public Task<CargoDryKitUsageSnapshotDocument?> GetByDateKeyAsync(string dateKey, CancellationToken ct)
        => _collection.Find(x => x.Id == dateKey).FirstOrDefaultAsync(ct)!;
}
```

---

## STEP 4 — MongoDB Index Bootstrap

**File:** `Repository/Persistence/MongoIndexBootstrap.cs`

```csharp
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

public static class MongoIndexBootstrap
{
    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken ct = default)
    {
        // ── Activation logs ────────────────────────────────────────────────────
        var logs = database.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");

        await logs.Indexes.CreateManyAsync([
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.KitId)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.DateKey)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Ascending(x => x.EventType)
                    .Ascending(x => x.DateKey)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Descending(x => x.OccurredAt)),
        ], ct);

        // ── Snapshots ──────────────────────────────────────────────────────────
        var snapshots = database.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");

        await snapshots.Indexes.CreateOneAsync(
            new CreateIndexModel<CargoDryKitUsageSnapshotDocument>(
                Builders<CargoDryKitUsageSnapshotDocument>.IndexKeys.Descending(x => x.DateKey)),
            cancellationToken: ct);
    }
}
```

---

## STEP 5 — Daily Snapshot Job

**File:** `Application/Jobs/DailySnapshotJob.cs`

```csharp
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Runs daily at 00:05 UTC. Reads the previous day's activation log documents from MongoDB,
/// aggregates them into a CargoDryKitUsageSnapshotDocument, and upserts into the snapshots collection.
/// The analytics endpoint reads snapshots — this job keeps it up to date.
/// </summary>
public sealed class DailySnapshotJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySnapshotJob> _logger;

    public DailySnapshotJob(IServiceScopeFactory sf, ILogger<DailySnapshotJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now        = DateTimeOffset.UtcNow;
            var next0005   = now.Date.AddDays(now.Hour >= 0 && now.Minute >= 5 ? 1 : 0).AddMinutes(5);
            var delay      = next0005 - now;
            await Task.Delay(delay, stoppingToken);

            _logger.LogInformation("DailySnapshotJob starting at {Time}", DateTimeOffset.UtcNow);
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "DailySnapshotJob failed"); }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _scopeFactory.CreateScope();
        var logRepo      = scope.ServiceProvider.GetRequiredService<ICargoDryActivationLogRepository>();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<ICargoDrySnapshotRepository>();

        // Compute yesterday's date key
        var dateKey = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var logs    = await logRepo.GetByDateRangeAsync(dateKey, dateKey, null, ct);

        if (logs.Count == 0)
        {
            _logger.LogInformation("DailySnapshotJob: no logs for {DateKey}", dateKey);
            return;
        }

        var byProduct = logs
            .Where(l => l.EventType == "Activated")
            .GroupBy(l => l.ProductCode)
            .Select(g => new ProductDaySnapshot
            {
                ProductCode = g.Key,
                ProductName = g.Key,   // ProductName can be enriched later from product cache
                Activations = g.Count(),
                Renewals    = logs.Count(l => l.EventType == "Renewed" && l.ProductCode == g.Key),
            })
            .ToList();

        var snapshot = new CargoDryKitUsageSnapshotDocument
        {
            Id          = dateKey,
            DateKey     = dateKey,
            ComputedAt  = DateTimeOffset.UtcNow,
            Activations = logs.Count(l => l.EventType == "Activated"),
            Renewals    = logs.Count(l => l.EventType == "Renewed"),
            Expirations = logs.Count(l => l.EventType == "Expired"),
            Revocations = logs.Count(l => l.EventType == "Revoked"),
            Extensions  = logs.Count(l => l.EventType == "Extended"),
            ByProduct   = byProduct,
            EfficiencyBuckets =
            [
                new EfficiencyBucketSnapshot { Bucket = "0-25",   Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "25-50",  Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "50-75",  Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "75-100", Count = 0 },
                // Efficiency buckets require live kit data — populated from PG query (future enhancement)
            ],
        };

        await snapshotRepo.UpsertAsync(snapshot, ct);
        _logger.LogInformation("DailySnapshotJob: upserted snapshot for {DateKey} ({Count} log entries)", dateKey, logs.Count);
    }
}
```

---

## STEP 6 — ActivateKitCommandHandler Update

The handler must now write to MongoDB instead of PostgreSQL for the activation log.

**File:** `Application/Commands/ActivateKit/ActivateKitCommandHandler.cs`

Add `ICargoDryActivationLogRepository` injection. After `kit.Activate(...)` and `SaveChangesAsync`:

```csharp
// Replace ActivationLog PostgreSQL insert with MongoDB document insert
private readonly ICargoDryActivationLogRepository _activationLogs;

// In Handle():
// (Remove CargoDryActivationLogEntity.Create(...) and any db.ActivationLogs.Add())

// After kit.Activate() and SaveChangesAsync():
var logDoc = new CargoDryActivationLogDocument
{
    KitId            = kit.Id,
    SerialNumber     = kit.SerialNumber,
    KitCode          = kit.KitCode,
    ProductCode      = kit.ProductCode,
    BatchCode        = kit.BatchCode,
    EventType        = "Activated",
    OwnerUserId      = request.UserId,
    VesselId         = request.VesselId,
    ActivationMethod = request.Method.ToString(),
    ActivationSource = request.Source.ToString(),
    DeviceInfo       = request.DeviceInfo,
    IpAddress        = request.IpAddress,
    ExpiresAt        = kit.ExpiresAt,
    OccurredAt       = DateTimeOffset.UtcNow,
    DateKey          = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
};

// Fire-and-forget: don't let MongoDB failure block activation
_ = _activationLogs.InsertAsync(logDoc, ct)
    .ContinueWith(t => _logger.LogError(t.Exception, "Failed to write activation log for Kit {KitId}", kit.Id),
        TaskContinuationOptions.OnlyOnFaulted);
```

Similarly update `RevokeKitCommandHandler` and `KitExpiredMarkingJob` to insert log documents with `EventType = "Revoked"` / `"Expired"`.

---

## STEP 7 — DI Registration Update

**File:** `Repository/DependencyInjection.cs`

Add MongoDB registration:

```csharp
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

public static IServiceCollection AddCargoDryRepository(
    this IServiceCollection services, IConfiguration configuration)
{
    // ── PostgreSQL ────────────────────────────────────────────────────────────
    services.AddScoped<ICargoDryProductRepository, CargoDryProductRepository>();
    services.AddScoped<ICargoDryKitRepository,     CargoDryKitRepository>();
    services.AddScoped<ICargoDryBatchRepository,   CargoDryBatchRepository>();
    services.AddScoped<CargoDryProductSeed>();

    // ── MongoDB ───────────────────────────────────────────────────────────────
    services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(configuration.GetConnectionString("CargoDryMongo")));

    services.AddScoped<IMongoDatabase>(sp =>
        sp.GetRequiredService<IMongoClient>().GetDatabase("aizen_cargodry"));

    services.AddScoped<ICargoDryActivationLogRepository, CargoDryActivationLogRepository>();
    services.AddScoped<ICargoDrySnapshotRepository,      CargoDrySnapshotRepository>();

    return services;
}

public static async Task SeedCargoDryAsync(this IHost host, CancellationToken ct = default)
{
    using var scope = host.Services.CreateScope();

    // PostgreSQL migration + seed
    var db      = scope.ServiceProvider.GetRequiredService<CargoDryDbContext>();
    var pending = await db.Database.GetPendingMigrationsAsync(ct);
    if (pending.Any()) await db.Database.MigrateAsync(ct);
    var seeder = scope.ServiceProvider.GetRequiredService<CargoDryProductSeed>();
    await seeder.SeedAsync(ct);

    // MongoDB index bootstrap
    var mongoDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoIndexBootstrap.EnsureIndexesAsync(mongoDb, ct);
}
```

Also register `DailySnapshotJob` in `Application/DependencyInjection.cs`:

```csharp
services.AddHostedService<DailySnapshotJob>();
```

---

## STEP 8 — csproj Addition

**`Aizen.Modules.CargoDry.Repository.csproj`** — add:

```xml
<PackageReference Include="MongoDB.Driver" Version="2.*" />
```

---

## Verification Checklist

- [ ] `CargoDryActivationLogDocument` created with correct BsonId and all fields
- [ ] `CargoDryKitUsageSnapshotDocument` created — `Id` is the `DateKey` string
- [ ] `ICargoDryActivationLogRepository` and `ICargoDrySnapshotRepository` interfaces defined in Abstraction
- [ ] Both repository implementations compile with correct MongoDB collection names
- [ ] `MongoIndexBootstrap.EnsureIndexesAsync()` creates 4 indexes on activation_logs + 1 on snapshots
- [ ] `DailySnapshotJob` registered as `IHostedService`
- [ ] `ActivateKitCommandHandler` writes to MongoDB (fire-and-forget, non-blocking)
- [ ] `RevokeKitCommandHandler` writes `EventType = "Revoked"` log document
- [ ] `KitExpiredMarkingJob` writes `EventType = "Expired"` log documents
- [ ] DI registration passes MongoDB connection string from `Configuration["ConnectionStrings:CargoDryMongo"]`
- [ ] `MongoDB.Driver` package referenced in Repository project
- [ ] `dotnet build` compiles with 0 errors
