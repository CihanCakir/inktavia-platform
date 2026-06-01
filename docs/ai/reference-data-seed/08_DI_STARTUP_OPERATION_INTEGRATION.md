# 08 — DI, Startup and Operation Integration

Wire JSON seed infrastructure into the module.

## DependencyInjection

Update:

```text
Aizen.Modules.ReferenceData.Repository/DependencyInjection.cs
```

Register:

```text
IReferenceDataJsonSeedReader -> ReferenceDataJsonSeedReader
IReferenceDataJsonSeedService -> ReferenceDataJsonSeedService
CurrencyJsonSeedService
ExchangeRateJsonSeedService
MeasurementJsonSeedService
LookupJsonSeedService
SystemJsonSeedService
LocationJsonSeedService
```

Use scoped lifetime for seed services.

## Options

Create seed options if the project supports options pattern:

```text
ReferenceDataSeedOptions
```

Fields:

```csharp
public bool Enabled { get; set; }
public bool SeedEfEntities { get; set; }
public bool SeedLocationDocuments { get; set; }
public string JsonRootPath { get; set; } = "Seed/Json";
public bool FailOnMissingRequiredFile { get; set; } = true;
```

Add appsettings sample section:

```json
{
  "ReferenceDataSeed": {
    "Enabled": true,
    "SeedEfEntities": true,
    "SeedLocationDocuments": true,
    "JsonRootPath": "Seed/Json",
    "FailOnMissingRequiredFile": true
  }
}
```

## Startup / Operation

Inspect the existing starter operation pattern.

Recommended startup order:

```text
1. Apply EF migration if existing project does this automatically
2. Ensure Mongo indexes
3. Run ReferenceData JSON seed if enabled
4. Start API
```

## Output

Report DI registrations, options, appsettings sample and startup integration point.
