# MongoDB Architecture Revision Prompt
## CargoDry + Messaging — Canonical AizenMongo Pattern Compliance

---

## Context & Architecture Baseline

### The Canonical Pattern (established in ReferenceData, Vessel, FileStorage)

The project has a core MongoDB abstraction layer in `Aizen.Core.Data.Mongo`. Every module that uses MongoDB **must** follow this exact pattern:

**1. `XxxMongoDbContext : AizenMongoContext`**
```csharp
// In: Modules/Xxx/src/Aizen.Modules.Xxx.Repository/Persistence/XxxMongoDbContext.cs
public sealed class XxxMongoDbContext : AizenMongoContext
{
    public XxxMongoDbContext(IOptions<DatabaseSettings> option) : base(option) { }
    protected override string ConfigurationKey => "XxxMongo";
    // ConfigurationKey maps to: DatabaseSettings:XxxMongo:ConnectionString in appsettings
}
```

**2. Document classes: extend `AizenDocumentBase`**
```csharp
// In: Modules/Xxx/src/Aizen.Modules.Xxx.Domain/Documents/
[AizenCollectionInfo(CollectionName = "xxx_collection_name")]
public sealed class XxxDocument : AizenDocumentBase
{
    // AizenDocumentBase provides:
    //   [BsonId] [BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; }
    //   public bool IsDeleted { get; set; }  ← global filter: IsDeleted == false
    // No manual [BsonId] or [BsonRepresentation] needed.
}
```

**3. Repositories: inject `IAizenMongoRepositoryFactory<TContext>`**
```csharp
public sealed class XxxRepository : IXxxRepository
{
    private readonly IAizenMongoRepository<XxxDocument> _repo;

    public XxxRepository(IAizenMongoRepositoryFactory<XxxMongoDbContext> factory)
    {
        _repo = factory.GetRepository<XxxDocument>();
        // OR: factory.GetRepository<XxxDocument>("custom_collection_name")
    }

    // Use _repo.FindAsync(), _repo.FindManyAsync(), _repo.AddAsync(), _repo.ReplaceAsync(), etc.
}
```

**4. DI registration: `AddAizenMongo()` in Program.cs — auto-discovers contexts**
```csharp
builder.Services.AddAizenMongo(builder.Configuration);
// Scans repository assemblies for AizenMongoContext subclasses — no manual IMongoClient/IMongoDatabase needed
```

**5. Config key: `DatabaseSettings` section, NOT `ConnectionStrings`**
```json
{
  "DatabaseSettings": {
    "XxxMongo": {
      "Type": "Mongo",
      "ConnectionString": "mongodb://..."
    }
  }
}
```

**6. Index initializer: inject `XxxMongoDbContext` for raw index access**
```csharp
public sealed class XxxMongoIndexInitializer
{
    private readonly IMongoDatabase _db;
    public XxxMongoIndexInitializer(XxxMongoDbContext ctx) => _db = ctx.Database;
    public async Task InitializeAsync(CancellationToken ct) { ... }
}
```

---

## Analysis: Current State vs. Required

### ❌ CargoDry — Fully Non-Compliant (7 violations)

| # | Issue | Current | Required |
|---|---|---|---|
| 1 | No `AizenMongoContext` subclass | Raw `IMongoClient` + `IMongoDatabase` as singletons | `CargoDryMongoDbContext : AizenMongoContext` |
| 2 | Documents don't extend `AizenDocumentBase` | Raw `[BsonId]`, `[BsonRepresentation]` | `AizenDocumentBase` (with `[AizenCollectionInfo]`) |
| 3 | Repository injects `IMongoDatabase` directly | `CargoDryActivationLogRepository(IMongoDatabase db)` | `IAizenMongoRepositoryFactory<CargoDryMongoDbContext>` |
| 4 | `AddAizenMongo()` not called | Manual `AddSingleton<IMongoClient>` + `AddScoped<IMongoDatabase>` | `AddAizenMongo(builder.Configuration)` |
| 5 | Config key mismatch | `ConnectionStrings:CargoDryMongo` | `DatabaseSettings:CargoDryMongo:ConnectionString` |
| 6 | `MongoIndexBootstrap` is static | `static class MongoIndexBootstrap` accepts `IMongoDatabase` param | Instance class, inject `CargoDryMongoDbContext` |
| 7 | `MongoDb:DatabaseName` config key | Non-standard key for database name | Context reads connection string from `DatabaseSettings` (DB name embedded in connection string) |

### ⚠️ Messaging — `AddAizenMongo` called but no context defined

| # | Issue | Current | Required |
|---|---|---|---|
| 1 | No `MessagingMongoDbContext` | `AddAizenMongo` is called but scans find nothing | `MessagingMongoDbContext : AizenMongoContext` |
| 2 | Wrong config key | `Mongo:ConnectionString` (non-standard) | `DatabaseSettings:MessagingMongo:ConnectionString` |
| 3 | `AddAizenMongo` discovery is a no-op | No context registered → factory never created | After creating context, `AddAizenMongo` will auto-register it |

### ✅ Notification — Correct (PostgreSQL only, no Mongo)

Notification module has no MongoDB usage — `NotificationTemplateEntity` is pure EF Core. No changes required.

---

## TASK 1: CargoDry — Create `CargoDryMongoDbContext`

**File to create:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Persistence/CargoDryMongoDbContext.cs`

```csharp
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

[DocumentationInfo("CargoDry MongoDB context",
    "MongoDB context for CargoDry activation logs and analytics snapshots. " +
    "ConfigurationKey maps to DatabaseSettings:CargoDryMongo:ConnectionString.")]
public sealed class CargoDryMongoDbContext : AizenMongoContext
{
    public CargoDryMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option) { }

    protected override string ConfigurationKey => "CargoDryMongo";
}
```

---

## TASK 2: CargoDry — Update `CargoDryActivationLogDocument`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/MongoDocuments/CargoDryActivationLogDocument.cs`

Replace the current implementation entirely:

```csharp
using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Append-only audit record for every kit lifecycle event (activation, revocation, expiry, renewal).
/// Stored in MongoDB collection: cargodry_activation_logs.
/// Id is a standard ObjectId (from AizenDocumentBase).
/// </summary>
[AizenCollectionInfo(CollectionName = "cargodry_activation_logs")]
public sealed class CargoDryActivationLogDocument : AizenDocumentBase
{
    // AizenDocumentBase provides:
    //   - string Id (ObjectId)
    //   - bool IsDeleted (global filter)
    // Do NOT add [BsonId] or [BsonRepresentation] — they are inherited.

    public long   KitId        { get; set; }
    public string SerialNumber { get; set; } = default!;
    public string KitCode      { get; set; } = default!;
    public string ProductCode  { get; set; } = default!;
    public string BatchCode    { get; set; } = default!;

    /// <summary>Type of event: "Activated" | "Revoked" | "Expired" | "Renewed" | "Extended"</summary>
    public string EventType    { get; set; } = default!;

    public long?  OwnerUserId        { get; set; }
    public long?  VesselId           { get; set; }
    public string? ActivationMethod  { get; set; }
    public string? ActivationSource  { get; set; }
    public string? DeviceInfo        { get; set; }
    public string? IpAddress         { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public string? RevokeReason { get; set; }

    public int?    AddedDays   { get; set; }
    public string? RenewalType { get; set; }
    public string? PaymentRef  { get; set; }

    public Dictionary<string, object>? TelemetryPayload { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Calendar date string (YYYY-MM-DD) for efficient date-range queries.</summary>
    public string DateKey { get; set; } = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
}
```

**Key change:** Remove the `[BsonId]` and `[BsonRepresentation(BsonType.ObjectId)]` manual declarations — these are now inherited from `AizenDocumentBase`. Remove the `using MongoDB.Bson;` and `using MongoDB.Bson.Serialization.Attributes;` imports.

---

## TASK 3: CargoDry — Update `CargoDryKitUsageSnapshotDocument`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/MongoDocuments/CargoDryKitUsageSnapshotDocument.cs`

This document has a special requirement: upsert-by-date semantics where one document exists per calendar date. The architecture handles this by using the standard ObjectId `Id` from `AizenDocumentBase`, with `DateKey` as the unique business key (separate field with a unique index).

```csharp
using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Daily aggregated analytics snapshot computed by DailySnapshotJob.
/// One document per calendar date. Upserted daily at 00:05 UTC.
/// DateKey is the business unique key (unique index enforced).
/// Id is a standard ObjectId (from AizenDocumentBase).
/// </summary>
[AizenCollectionInfo(CollectionName = "cargodry_usage_snapshots")]
public sealed class CargoDryKitUsageSnapshotDocument : AizenDocumentBase
{
    // AizenDocumentBase provides Id (ObjectId) and IsDeleted.
    // DateKey is the business key — unique index defined in CargoDryMongoIndexInitializer.

    public string DateKey        { get; set; } = default!;
    public DateTimeOffset ComputedAt { get; set; }

    public int Activations  { get; set; }
    public int Renewals     { get; set; }
    public int Expirations  { get; set; }
    public int Revocations  { get; set; }
    public int Extensions   { get; set; }

    public List<EfficiencyBucketSnapshot> EfficiencyBuckets { get; set; } = [];
    public List<ProductDaySnapshot>       ByProduct         { get; set; } = [];
}

public sealed class EfficiencyBucketSnapshot
{
    public string Bucket { get; set; } = default!;
    public int    Count  { get; set; }
}

public sealed class ProductDaySnapshot
{
    public string ProductCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}
```

**Important:** Remove `using MongoDB.Bson.Serialization.Attributes;`. The snapshot no longer uses `Id` as the date key — `DateKey` is a separate field with a unique index.

---

## TASK 4: CargoDry — Update `CargoDryActivationLogRepository`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Repositories/CargoDryActivationLogRepository.cs`

Use `IAizenMongoRepositoryFactory<CargoDryMongoDbContext>`:

```csharp
using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

[DocumentationInfo("CargoDry activation log repository",
    "MongoDB repository for kit lifecycle event audit logs.")]
public sealed class CargoDryActivationLogRepository : ICargoDryActivationLogRepository
{
    private readonly IAizenMongoRepository<CargoDryActivationLogDocument> _repo;

    public CargoDryActivationLogRepository(
        IAizenMongoRepositoryFactory<CargoDryMongoDbContext> factory)
    {
        _repo = factory.GetRepository<CargoDryActivationLogDocument>();
    }

    public Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct)
        => _repo.AddAsync(document, ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct)
    {
        var results = await _repo.FindManyAsync(
            predicate: x => x.KitId == kitId,
            orderBy: q => (IOrderedMongoQueryable<CargoDryActivationLogDocument>)q.OrderByDescending(x => x.OccurredAt),
            topCount: -1,
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType, CancellationToken ct)
    {
        var results = await _repo.FindManyAsync(
            predicate: eventType != null
                ? x => x.DateKey.CompareTo(fromDateKey) >= 0 &&
                       x.DateKey.CompareTo(toDateKey)   <= 0 &&
                       x.EventType == eventType
                : x => x.DateKey.CompareTo(fromDateKey) >= 0 &&
                       x.DateKey.CompareTo(toDateKey)   <= 0,
            orderBy: q => (IOrderedMongoQueryable<CargoDryActivationLogDocument>)q.OrderByDescending(x => x.OccurredAt),
            topCount: -1,
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct)
    {
        var results = await _repo.FindManyAsync(
            predicate: x => x.DateKey == dateKey,
            topCount: -1,
            cancellationToken: ct);
        return results
            .GroupBy(x => x.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct)
    {
        var results = await _repo.FindManyAsync(
            predicate: x => x.DateKey.CompareTo(fromDateKey) >= 0 &&
                            x.DateKey.CompareTo(toDateKey)   <= 0 &&
                            x.EventType == "Activated",
            topCount: -1,
            cancellationToken: ct);
        return results
            .GroupBy(x => x.ProductCode)
            .Select(g => (ProductCode: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
```

**Note on LINQ:** `IAizenMongoRepository<T>` uses `FindManyAsync` with `Expression<Func<T, bool>>`. String `.CompareTo()` may not translate to MongoDB LINQ. If compilation fails, fall back to building `FilterDefinition` via `Builders<T>.Filter` and using `_repo.WhereAsyncByFilterDefinition(filter)`.

---

## TASK 5: CargoDry — Update `CargoDrySnapshotRepository`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Repositories/CargoDrySnapshotRepository.cs`

The snapshot repository needs **upsert-by-DateKey** semantics. Since `IAizenMongoRepository<T>` does not expose a native `UpsertAsync`, inject `CargoDryMongoDbContext` directly for raw `IMongoDatabase` access — exactly the same pattern used by index initializers in ReferenceData.

```csharp
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

[DocumentationInfo("CargoDry snapshot repository",
    "MongoDB repository for daily analytics snapshots. Injects CargoDryMongoDbContext " +
    "directly for upsert-by-DateKey semantics not available in IAizenMongoRepository.")]
public sealed class CargoDrySnapshotRepository : ICargoDrySnapshotRepository
{
    private readonly IMongoCollection<CargoDryKitUsageSnapshotDocument> _collection;

    public CargoDrySnapshotRepository(CargoDryMongoDbContext context)
    {
        _collection = context.Database
            .GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
    }

    public Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct)
    {
        // Upsert by DateKey (the business unique key, NOT _id)
        var filter  = Builders<CargoDryKitUsageSnapshotDocument>.Filter.Eq(x => x.DateKey, document.DateKey);
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
        => _collection.Find(x => x.DateKey == dateKey).FirstOrDefaultAsync(ct)!;
}
```

---

## TASK 6: CargoDry — Rewrite `MongoIndexBootstrap`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Persistence/MongoIndexBootstrap.cs`

Convert from static class to instance class injecting `CargoDryMongoDbContext`:

```csharp
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

[DocumentationInfo("CargoDry MongoDB index initializer",
    "Ensures required indexes on cargodry_activation_logs and cargodry_usage_snapshots.")]
public sealed class CargoDryMongoIndexInitializer
{
    private readonly IMongoDatabase _db;

    public CargoDryMongoIndexInitializer(CargoDryMongoDbContext context)
    {
        _db = context.Database;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await CreateActivationLogIndexesAsync(ct);
        await CreateSnapshotIndexesAsync(ct);
    }

    private Task CreateActivationLogIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");
        return col.Indexes.CreateManyAsync([
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.KitId),
                new CreateIndexOptions { Name = "ix_kit_id" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.DateKey),
                new CreateIndexOptions { Name = "ix_date_key" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Ascending(x => x.EventType)
                    .Ascending(x => x.DateKey),
                new CreateIndexOptions { Name = "ix_event_type_date" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Descending(x => x.OccurredAt),
                new CreateIndexOptions { Name = "ix_occurred_at_desc" }),
        ], ct);
    }

    private Task CreateSnapshotIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
        return col.Indexes.CreateOneAsync(
            new CreateIndexModel<CargoDryKitUsageSnapshotDocument>(
                Builders<CargoDryKitUsageSnapshotDocument>.IndexKeys.Descending(x => x.DateKey),
                new CreateIndexOptions { Unique = true, Name = "ux_date_key" }),
            cancellationToken: ct);
    }
}
```

**Note:** Rename the file from `MongoIndexBootstrap.cs` to `CargoDryMongoIndexInitializer.cs` to match the naming convention (`ReferenceDataMongoIndexInitializer`).

---

## TASK 7: CargoDry — Update `DependencyInjection.cs` (Repository)

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/DependencyInjection.cs`

Remove the manual `IMongoClient`/`IMongoDatabase` registrations. Add `CargoDryMongoIndexInitializer`:

```csharp
// REMOVE these lines from AddCargoDryRepository:
services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(configuration.GetConnectionString("CargoDryMongo")));
services.AddScoped<IMongoDatabase>(sp => { ... });

// ADD these lines:
services.AddScoped<CargoDryMongoIndexInitializer>();
// (IAdminCargoDryBffRemoteCall repositories remain unchanged)

// In SeedCargoDryAsync, REPLACE:
var mongoDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
await MongoIndexBootstrap.EnsureIndexesAsync(mongoDb, ct);

// WITH:
var mongoIndexer = scope.ServiceProvider.GetRequiredService<CargoDryMongoIndexInitializer>();
await mongoIndexer.InitializeAsync(ct);
```

The complete `DependencyInjection.cs` for the Repository layer should look like:

```csharp
public static IServiceCollection AddCargoDryRepository(
    this IServiceCollection services, IConfiguration configuration)
{
    // ── PostgreSQL ─────────────────────────────────────────────────────────
    services.AddScoped<ICargoDryProductRepository, CargoDryProductRepository>();
    services.AddScoped<ICargoDryKitRepository,     CargoDryKitRepository>();
    services.AddScoped<ICargoDryBatchRepository,   CargoDryBatchRepository>();
    services.AddScoped<CargoDryProductSeed>();
    services.AddScoped<CargoDryBatchMockSeed>();

    // ── MongoDB ────────────────────────────────────────────────────────────
    // IMongoClient and IMongoDatabase are NO LONGER registered here.
    // AddAizenMongo() in Program.cs auto-discovers CargoDryMongoDbContext.
    services.AddScoped<ICargoDryActivationLogRepository, CargoDryActivationLogRepository>();
    services.AddScoped<ICargoDrySnapshotRepository,      CargoDrySnapshotRepository>();
    services.AddScoped<CargoDryMongoIndexInitializer>();

    return services;
}
```

---

## TASK 8: CargoDry — Update `Program.cs`

**File to modify:**
`Modules/CargoDry/src/Aizen.Modules.CargoDry/Program.cs`

Replace the manual MongoDB setup block with `AddAizenMongo`:

```csharp
// REMOVE this block:
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("CargoDryMongo")));
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = builder.Configuration["MongoDb:DatabaseName"] ?? "aizen_cargodry";
    return client.GetDatabase(dbName);
});

// ADD after AddAizenUnitOfWork:
builder.Services.AddAizenMongo(builder.Configuration);
```

Also remove `using MongoDB.Driver;` from Program.cs if it's only used for the removed block.

---

## TASK 9: CargoDry — Update Configuration Files

The config key changes from `ConnectionStrings:CargoDryMongo` to `DatabaseSettings:CargoDryMongo:ConnectionString`.

**File: `Modules/CargoDry/src/Aizen.Modules.CargoDry/configuration/appsettings.Local.json`**

Replace:
```json
"ConnectionStrings": {
    "CargoDryMongo": "mongodb://aizen:aizenpw@localhost:27017"
},
"MongoDb": {
    "DatabaseName": "aizen_cargodry"
}
```

With:
```json
"DatabaseSettings": {
    "CargoDry": {
        "Type": "PostgreSQL",
        "ConnectionString": "Host=localhost;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
    },
    "CargoDryMongo": {
        "Type": "Mongo",
        "ConnectionString": "mongodb://aizen:aizenpw@localhost:27017/aizen_cargodry?authSource=admin"
    }
}
```

**File: `Modules/CargoDry/src/Aizen.Modules.CargoDry/configuration/appsettings.Development.json`**

Same replacement — change `localhost` to `mongo` (Docker container name):
```json
"DatabaseSettings": {
    "CargoDry": {
        "Type": "PostgreSQL",
        "ConnectionString": "Host=postgres;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
    },
    "CargoDryMongo": {
        "Type": "Mongo",
        "ConnectionString": "mongodb://aizen:aizenpw@mongo:27017/aizen_cargodry?authSource=admin"
    }
}
```

Remove `"ConnectionStrings": { "CargoDryMongo": "..." }` and `"MongoDb": { "DatabaseName": "..." }` from both files.

Keep `"ConnectionStrings": { "CargoDry": "..." }` — it's used by `AddAizenUnitOfWork` for PostgreSQL.

---

## TASK 10: Messaging — Create `MessagingMongoDbContext`

**File to create:**
`Modules/Messaging/src/Aizen.Modules.Messaging.Repository/Persistence/MessagingMongoDbContext.cs`

```csharp
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Messaging.Repository.Persistence;

[DocumentationInfo("Messaging MongoDB context",
    "MongoDB context for Messaging module read-side documents (message audit, conversation snapshots). " +
    "ConfigurationKey maps to DatabaseSettings:MessagingMongo:ConnectionString.")]
public sealed class MessagingMongoDbContext : AizenMongoContext
{
    public MessagingMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option) { }

    protected override string ConfigurationKey => "MessagingMongo";
}
```

This class must exist in the Repository assembly for `AddAizenMongo()` to discover it during startup scanning.

---

## TASK 11: Messaging — Fix Configuration Keys

**File: `Modules/Messaging/src/Aizen.Modules.Messaging/configuration/appsettings.Development.json`**

Replace:
```json
"Mongo": {
    "ConnectionString": "mongodb://aizen:aizenpw@mongo:27017"
}
```

With:
```json
"DatabaseSettings": {
    "Messaging": {
        "Type": "PostgreSQL",
        "ConnectionString": "Host=postgres;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
    },
    "MessagingMongo": {
        "Type": "Mongo",
        "ConnectionString": "mongodb://aizen:aizenpw@mongo:27017/aizen_messaging?authSource=admin"
    }
}
```

**File: `Modules/Messaging/src/Aizen.Modules.Messaging/configuration/appsettings.Local.json`** (create if it doesn't exist, following the pattern from other modules):

```json
{
  "DatabaseSettings": {
    "Messaging": {
      "Type": "PostgreSQL",
      "ConnectionString": "Host=localhost;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
    },
    "MessagingMongo": {
      "Type": "Mongo",
      "ConnectionString": "mongodb://aizen:aizenpw@localhost:27017/aizen_messaging?authSource=admin"
    }
  },
  "ConnectionStrings": {
    "Messaging": "Host=localhost;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
  },
  "DistributedCache": {
    "InstanceName": "Messaging:",
    "Configuration": "localhost:6379,abortConnect=False,defaultDatabase=13"
  },
  "MessageBroker": {
    "QueueSettings": {
      "HostName": "localhost",
      "UserName": "aizen",
      "Password": "aizenpw",
      "Port": 5672,
      "VirtualHost": "/",
      "Debug": false
    }
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/inktavia-realm",
    "MetadataAddress": "http://localhost:8080/realms/inktavia-realm/.well-known/openid-configuration",
    "Audience": "messaging-api",
    "RequireHttpsMetadata": "false"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## TASK 12: Messaging — Verify `appsettings.Development.json` keeps `ConnectionStrings`

`AddAizenUnitOfWork` for PostgreSQL reads `ConnectionStrings:Messaging`. Keep that section alongside the new `DatabaseSettings`:

```json
{
  "ConnectionStrings": {
    "Messaging": "Host=postgres;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
  },
  "DatabaseSettings": {
    "Messaging": {
      "Type": "PostgreSQL",
      "ConnectionString": "Host=postgres;Port=5432;Database=aizen;Username=aizen;Password=aizenpw"
    },
    "MessagingMongo": {
      "Type": "Mongo",
      "ConnectionString": "mongodb://aizen:aizenpw@mongo:27017/aizen_messaging?authSource=admin"
    }
  }
}
```

---

## Verification Checklist

After applying all tasks:

**CargoDry:**
- [ ] `dotnet build` on CargoDry project — zero errors
- [ ] No references to `IMongoClient` or `IMongoDatabase` in `Program.cs` or `DependencyInjection.cs`
- [ ] `CargoDryActivationLogDocument` and `CargoDryKitUsageSnapshotDocument` both reference `Aizen.Core.Data.Mongo.Document` namespace, not `MongoDB.Bson`
- [ ] `CargoDryActivationLogRepository` constructor accepts `IAizenMongoRepositoryFactory<CargoDryMongoDbContext>`, not `IMongoDatabase`
- [ ] `CargoDrySnapshotRepository` constructor accepts `CargoDryMongoDbContext`, not `IMongoDatabase`
- [ ] `appsettings.Local.json` and `appsettings.Development.json` have `DatabaseSettings:CargoDryMongo:ConnectionString` — no `ConnectionStrings:CargoDryMongo`, no `MongoDb:DatabaseName`
- [ ] On startup, `SeedCargoDryAsync` calls `CargoDryMongoIndexInitializer.InitializeAsync()` — no static call

**Messaging:**
- [ ] `MessagingMongoDbContext.cs` exists in `Aizen.Modules.Messaging.Repository/Persistence/`
- [ ] `appsettings.Development.json` has `DatabaseSettings:MessagingMongo:ConnectionString` — no `Mongo:ConnectionString`
- [ ] `appsettings.Local.json` created with correct localhost addresses
- [ ] `dotnet build` on Messaging project — zero errors

**Notification:**
- [ ] No changes required — Notification is PostgreSQL-only ✅

---

## File Summary

| Action | File |
|---|---|
| CREATE | `Modules/CargoDry/.../Persistence/CargoDryMongoDbContext.cs` |
| MODIFY | `Modules/CargoDry/.../MongoDocuments/CargoDryActivationLogDocument.cs` |
| MODIFY | `Modules/CargoDry/.../MongoDocuments/CargoDryKitUsageSnapshotDocument.cs` |
| MODIFY | `Modules/CargoDry/.../Repositories/CargoDryActivationLogRepository.cs` |
| MODIFY | `Modules/CargoDry/.../Repositories/CargoDrySnapshotRepository.cs` |
| RENAME+MODIFY | `Persistence/MongoIndexBootstrap.cs` → `CargoDryMongoIndexInitializer.cs` |
| MODIFY | `Modules/CargoDry/.../Repository/DependencyInjection.cs` |
| MODIFY | `Modules/CargoDry/.../CargoDry/Program.cs` |
| MODIFY | `configuration/appsettings.Local.json` (CargoDry) |
| MODIFY | `configuration/appsettings.Development.json` (CargoDry) |
| CREATE | `Modules/Messaging/.../Persistence/MessagingMongoDbContext.cs` |
| MODIFY | `configuration/appsettings.Development.json` (Messaging) |
| CREATE | `configuration/appsettings.Local.json` (Messaging) |
