# 03 — Create Seed Models and JSON Reader

Create seed model classes under:

```text
Aizen.Modules.ReferenceData.Repository/Seed/Models/
```

Every seed model class must include DocumentationInfo.

## Required seed models

### Currency

```text
CurrencySeedModel
ExchangeRateSeedModel
ExchangeRateHistorySeedModel
```

### Measurement

```text
MeasurementUnitSeedModel
```

### Lookup

```text
LookupGroupSeedModel
LookupItemSeedModel
LookupTreeSeedModel
```

LookupGroupSeedModel fields:

```text
Code
ParentCode
Name
Description
GroupType
SortOrder
IsSystemGroup
IsActive
```

LookupItemSeedModel fields:

```text
GroupCode
Code
Name
Description
IconKey
ColorCode
SortOrder
IsDefault
IsActive
```

### System

```text
SystemParameterSeedModel
LanguageSeedModel
TimeZoneSeedModel
CountryPhoneCodeSeedModel
```

### Location

```text
LocationCountrySeedModel
LocationCitySeedModel
LocationDistrictSeedModel
LocationNeighborhoodSeedModel
LocationStreetSeedModel
```

Location name fields must support multilingual data:

```csharp
public Dictionary<string, string> Name { get; set; } = new();
```

## JSON reader

Create:

```text
Aizen.Modules.ReferenceData.Repository/Seed/Readers/IReferenceDataJsonSeedReader.cs
Aizen.Modules.ReferenceData.Repository/Seed/Readers/ReferenceDataJsonSeedReader.cs
```

Required methods:

```csharp
Task<IReadOnlyList<T>> ReadListAsync<T>(string relativePath, CancellationToken cancellationToken = default);
Task<T?> ReadSingleAsync<T>(string relativePath, CancellationToken cancellationToken = default);
```

Rules:
- Use `System.Text.Json`.
- Use case-insensitive property names.
- Throw meaningful exception if JSON is invalid.
- Return empty list if optional seed file does not exist only when explicitly configured.
- Do not silently ignore invalid required files.

## Output

Report seed models and reader files.
