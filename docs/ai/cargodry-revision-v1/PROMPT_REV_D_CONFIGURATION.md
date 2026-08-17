# PROMPT REV-D — CargoDry Configuration Files
# local.json / development.json / production.json + Program.cs MongoDB Wiring

## Context

The CargoDry module requires a `Configuration/` directory in the host project (`Aizen.Modules.CargoDry`).
This follows the same pattern used by other modules (Identity, ServiceRequest, Messaging).

Each file is loaded by the `AizenApplicationBuilder` based on the current environment:
- `local.json` → `ASPNETCORE_ENVIRONMENT=local` (developer machine, Docker Compose)
- `development.json` → `ASPNETCORE_ENVIRONMENT=Development` (CI / shared dev server)
- `production.json` → `ASPNETCORE_ENVIRONMENT=Production` (deployment — secrets are Azure DevOps token replacements)

**Directory:** `Aizen.Modules.CargoDry/Configuration/`

All three files must be included as `Content` → `Copy if newer` in the `.csproj`.

---

## STEP 1 — local.json

**File:** `Configuration/local.json`

```json
{
  "ConnectionStrings": {
    "CargoDry": "Host=localhost;Port=5432;Database=aizen_cargodry;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=1;MaxPoolSize=10",
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=3000",
    "CargoDryMongo": "mongodb://localhost:27017"
  },
  "CargoDry": {
    "ActivationTokenSecret": "local-dev-secret-must-be-at-least-32-chars!!",
    "ActivationTokenTtlMinutes": 5,
    "BatchKeys": {
      "__comment": "Add dev batch keys here in format: BatchCode: Base64Key",
      "202506-STAN-DEV1": "ZGV2LWJhdGNoLWtleS1mb3ItbG9jYWwtZGV2ZWxvcG1lbnQ="
    },
    "RateLimiting": {
      "ValidateEndpoint": {
        "PermitLimit": 100,
        "WindowSeconds": 60
      }
    }
  },
  "RabbitMq": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "Port": 5672
  },
  "MongoDb": {
    "DatabaseName": "aizen_cargodry"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "MongoDB.Driver": "Warning"
      }
    }
  }
}
```

---

## STEP 2 — development.json

**File:** `Configuration/development.json`

```json
{
  "ConnectionStrings": {
    "CargoDry": "Host=dev-pg.inktavia.internal;Port=5432;Database=aizen_cargodry;Username=cargodry_svc;Password=#{CargoDry_PgPassword}#;Pooling=true;MinPoolSize=2;MaxPoolSize=20;SslMode=Require",
    "Redis": "dev-redis.inktavia.internal:6379,password=#{Redis_Password}#,abortConnect=false,ssl=false",
    "CargoDryMongo": "mongodb://cargodry_svc:#{CargoDry_MongoPassword}#@dev-mongo.inktavia.internal:27017/aizen_cargodry?authSource=aizen_cargodry"
  },
  "CargoDry": {
    "ActivationTokenSecret": "#{CargoDry_ActivationTokenSecret}#",
    "ActivationTokenTtlMinutes": 5,
    "BatchKeys": {},
    "RateLimiting": {
      "ValidateEndpoint": {
        "PermitLimit": 20,
        "WindowSeconds": 60
      }
    }
  },
  "RabbitMq": {
    "Host": "dev-rabbitmq.inktavia.internal",
    "VirtualHost": "inktavia-dev",
    "Username": "cargodry_svc",
    "Password": "#{RabbitMq_CargoDry_Password}#",
    "Port": 5672
  },
  "MongoDb": {
    "DatabaseName": "aizen_cargodry"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## STEP 3 — production.json

**File:** `Configuration/production.json`

```json
{
  "ConnectionStrings": {
    "CargoDry": "#{ConnectionStrings_CargoDry}#",
    "Redis": "#{ConnectionStrings_Redis}#",
    "CargoDryMongo": "#{ConnectionStrings_CargoDryMongo}#"
  },
  "CargoDry": {
    "ActivationTokenSecret": "#{CargoDry_ActivationTokenSecret}#",
    "ActivationTokenTtlMinutes": 5,
    "BatchKeys": {},
    "RateLimiting": {
      "ValidateEndpoint": {
        "PermitLimit": 10,
        "WindowSeconds": 60
      }
    }
  },
  "RabbitMq": {
    "Host": "#{RabbitMq_Host}#",
    "VirtualHost": "#{RabbitMq_VirtualHost}#",
    "Username": "#{RabbitMq_Username}#",
    "Password": "#{RabbitMq_Password}#",
    "Port": 5672
  },
  "MongoDb": {
    "DatabaseName": "#{MongoDb_DatabaseName}#"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Error",
        "System": "Error"
      }
    }
  }
}
```

---

## STEP 4 — .csproj Update

**File:** `Aizen.Modules.CargoDry.csproj`

Add the configuration files as content items so they are copied on build:

```xml
<ItemGroup>
  <Content Include="Configuration\local.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Configuration\development.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Configuration\production.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

## STEP 5 — Program.cs Full Replacement

The existing `Program.cs` (from PROMPT_C_CARGODRY_API.md) must be updated to:
1. Load MongoDB connection and register `IMongoDatabase`
2. Load `AddAizenCache` for `IAizenCache` (required by cacheable handlers from PROMPT_REV_C)
3. Register `DailySnapshotJob` as hosted service

**File:** `Aizen.Modules.CargoDry/Program.cs`

```csharp
using Aizen.Core.Application;
using Aizen.Core.Application.Enums;
using Aizen.Modules.CargoDry.Application;
using Aizen.Modules.CargoDry.Application.Consumers;
using Aizen.Modules.CargoDry.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Driver;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "CargoDry",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler },
}, args);

// ── Configuration ─────────────────────────────────────────────────────────────
// AizenApplicationBuilder automatically loads Configuration/{env}.json
// No explicit AddJsonFile needed — handled by the builder convention.

// ── PostgreSQL ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<CargoDryDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("CargoDry")));

// ── MongoDB ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("CargoDryMongo")));

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client   = sp.GetRequiredService<IMongoClient>();
    var dbName   = builder.Configuration["MongoDb:DatabaseName"] ?? "aizen_cargodry";
    return client.GetDatabase(dbName);
});

// ── Repository (PostgreSQL + MongoDB) ─────────────────────────────────────────
builder.Services.AddCargoDryRepository(builder.Configuration);

// ── Application (CQRS handlers + jobs + services) ─────────────────────────────
builder.Services.AddCargoDryApplication();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Aizen.Modules.CargoDry.Application.DependencyInjection).Assembly));

// ── Redis Cache (IAizenCache — required by cacheable handlers) ────────────────
builder.Services.AddAizenCache(builder.Configuration);

// ── Redis Connection (for ActivationTokenService JTI store + Rate Limiter) ───
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

// ── IP Rate Limiting ──────────────────────────────────────────────────────────
var rateLimitConfig = builder.Configuration.GetSection("CargoDry:RateLimiting:ValidateEndpoint");
builder.Services.AddRateLimiter(opts =>
{
    opts.AddSlidingWindowLimiter("validate-ip", limiter =>
    {
        limiter.PermitLimit         = rateLimitConfig.GetValue<int>("PermitLimit", 10);
        limiter.Window              = TimeSpan.FromSeconds(
            rateLimitConfig.GetValue<int>("WindowSeconds", 60));
        limiter.SegmentsPerWindow   = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit          = 0;
    });
    opts.RejectionStatusCode = 429;
});

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CommerceOrderCompletedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"],
            builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"]!);
                h.Password(builder.Configuration["RabbitMq:Password"]!);
            });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed + MongoDB Index Bootstrap ────────────────────────────────────────────
await app.SeedCargoDryAsync();

app.Run();
```

---

## STEP 6 — Configuration Loading Verification

Confirm `AizenApplicationBuilder.CreateBuilder` already loads `Configuration/{env}.json`.
If it does NOT, add explicit loading in `Program.cs` before `builder.Build()`:

```csharp
// Add ONLY if AizenApplicationBuilder does not auto-load Configuration/*.json
var env = builder.Environment.EnvironmentName.ToLower(); // "local" | "development" | "production"
builder.Configuration.AddJsonFile($"Configuration/{env}.json", optional: true, reloadOnChange: false);
```

To verify: grep for `AddJsonFile` or `Configuration/` in the other module `Program.cs` files:

```bash
grep -r "AddJsonFile\|Configuration/" \
  Modules/*/src/*/Program.cs \
  --include="*.cs" | head -10
```

---

## Verification Checklist

- [ ] `Configuration/local.json` created with PostgreSQL, Redis, MongoDB connection strings
- [ ] `Configuration/development.json` created with `#{...}#` token placeholders for all secrets
- [ ] `Configuration/production.json` created with `#{...}#` token placeholders for all values
- [ ] All 3 JSON files listed as `<Content CopyToOutputDirectory="PreserveNewest">` in `.csproj`
- [ ] `Program.cs` registers `IMongoClient` as Singleton and `IMongoDatabase` as Scoped
- [ ] `AddAizenCache(builder.Configuration)` called in `Program.cs`
- [ ] Rate limiting values read from `CargoDry:RateLimiting:ValidateEndpoint` config section
- [ ] RabbitMq `VirtualHost` read from config (not hardcoded `/`)
- [ ] `SeedCargoDryAsync()` extension also calls `MongoIndexBootstrap.EnsureIndexesAsync()`
- [ ] App starts successfully in `local` environment with Docker Compose (PG + Redis + Mongo)
- [ ] `dotnet build` compiles with 0 errors in all configurations
