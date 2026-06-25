# Mock Data Startup Import Report

**Date**: 2026-06-14  
**Scope**: Startup wiring, environment guards, configuration for Identity, Vessel, ServiceRequest mock data seeders

---

## 1. Overview

Each module's mock data seeder is invoked during application startup via a DI-registered extension method. A layered configuration system with environment guards ensures seeders never run in production.

---

## 2. Startup Wiring per Module

### 2.1 Identity

**DependencyInjection.cs additions:**
```csharp
public static IServiceCollection AddIdentityMockData(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<MockDataSeedOptions>(configuration.GetSection("MockData"));
    services.AddScoped<IdentityMockDataSeeder>();
    return services;
}
```

**SeedIdentityAsync additions (called at startup):**
```csharp
// After existing seed logic
using var mockScope = app.Services.CreateScope();
var mockSeeder = mockScope.ServiceProvider.GetRequiredService<IdentityMockDataSeeder>();
await mockSeeder.RunAsync(cancellationToken);
```

**Program.cs:**
```csharp
builder.Services.AddIdentityMockData(builder.Configuration);
```

---

### 2.2 Vessel

**DependencyInjection.cs additions:**
```csharp
public static IServiceCollection AddVesselMockData(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<MockDataSeedOptions>(configuration.GetSection("MockData"));
    services.AddScoped<VesselMockDataSeeder>();
    return services;
}
```

**SeedVesselAsync additions:**
```csharp
using var mockScope = app.Services.CreateScope();
var mockSeeder = mockScope.ServiceProvider.GetRequiredService<VesselMockDataSeeder>();
await mockSeeder.RunAsync(cancellationToken);
```

**Program.cs:**
```csharp
builder.Services.AddVesselMockData(builder.Configuration);
```

---

### 2.3 ServiceRequest

Same pattern as Vessel, substituting `ServiceRequestMockDataSeeder` and `AddServiceRequestMockData`.

---

## 3. Configuration Schema

### MockDataSeedOptions class (same in all 3 modules)
```csharp
public sealed class MockDataSeedOptions
{
    public bool Enabled { get; set; }
    public bool RunOnStartup { get; set; }
    public string DataSet { get; set; } = "admin-demo";
    public string[] EnvironmentGuard { get; set; } = ["Local", "Development"];
}
```

### appsettings.json (production-safe default)
```json
{
  "MockData": {
    "Enabled": false,
    "RunOnStartup": false,
    "DataSet": "admin-demo",
    "EnvironmentGuard": ["Local", "Development"]
  }
}
```

### appsettings.Local.json (local dev override)
```json
{
  "MockData": {
    "Enabled": true,
    "RunOnStartup": true,
    "DataSet": "admin-demo",
    "EnvironmentGuard": ["Local", "Development"]
  }
}
```

---

## 4. Environment Guard Logic

Each seeder's `RunAsync()` first checks:

```csharp
if (!options.Enabled || !options.RunOnStartup)
    return;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (!options.EnvironmentGuard.Contains(env, StringComparer.OrdinalIgnoreCase))
{
    logger.LogWarning("MockData seeding skipped. Environment '{Env}' not in guard list.", env);
    return;
}

logger.LogInformation("Starting mock data seeding for dataset '{DataSet}'...", options.DataSet);
```

This three-layer guard ensures seeders cannot run unless:
1. `Enabled` is explicitly set to `true`
2. `RunOnStartup` is explicitly set to `true`
3. `ASPNETCORE_ENVIRONMENT` matches one of the allowed values

---

## 5. Execution Order

The seeder is called within the existing `Seed[Module]Async()` method **after** EF Core migrations are applied:

```
App Start
  → EF Core Migration (CreateDatabaseIfNotExists / MigrateAsync)
  → Existing Reference Data / System Seed (SeedIdentityBase, etc.)
  → MockDataSeeder.RunAsync()
```

This ensures:
- Database schema is always up-to-date before mock data is inserted
- System seed (roles, agreements, lookup data) is available before user/profile creation
- No FK violations from missing prerequisite data

---

## 6. JSON File Discovery

JSON files are discovered at runtime:
```csharp
var basePath = Path.Combine(AppContext.BaseDirectory, "Seed", "Json", "MockData", options.DataSet);
var usersJson = File.ReadAllText(Path.Combine(basePath, "identity-users.json"));
var users = JsonSerializer.Deserialize<List<UserSeedModel>>(usersJson);
```

Files are copied to output directory via existing `.csproj` glob:
```xml
<Content Include="Seed\Json\**\*" CopyToOutputDirectory="PreserveNewest" />
```

This glob was already present in all three Repository `.csproj` files. No project file changes were needed.

---

## 7. Startup Duration Impact

Mock data seeding only runs on first startup (idempotency checks skip already-seeded data). Subsequent startups have negligible overhead from the `AnyAsync` checks (indexed ID lookups).

---

## 8. Production Safety Summary

| Guard | Effect |
|-------|--------|
| `appsettings.json` has `Enabled: false` | Seeder is no-op in all non-local envs |
| `EnvironmentGuard` check | Even if `Enabled` is accidentally set, wrong env blocks execution |
| Idempotency | Even if seeder runs twice, no duplicates are created |
| No DB-level permissions change | Seeder uses same app DbContext, no elevated DB user required |

---

## 9. Remaining Gaps / Follow-ups

- Currently, seeder startup errors are logged but do not fail the application startup. Consider making this configurable via `FailOnSeedError` option for stricter local dev workflows.
- There is no CLI-based seed command (e.g. `dotnet run --seed`). If on-demand seeding (without full service startup) is needed, a separate `IHostedService` or CLI command can be added.
- No seed rollback mechanism. If mock data needs to be cleared, it must be done manually or via a database reset.
