# 05 — Create Turkey Location JSON Seeds

Create JSON seed files and import structure for Turkey location documents.

## Important production data rule

Do not invent incomplete production administrative data.

If the repository already contains official Turkey location data, transform it into the schema below.

If the repository does not contain official data, create:
1. schema-compatible JSON templates,
2. a clear README explaining how to import the full official dataset,
3. a minimal development sample for Istanbul/Kadikoy only marked as non-production sample.

## Target folder

```text
Aizen.Modules.ReferenceData.Repository/Seed/Json/Location/TR/
```

## Files

```text
country.json
cities.json
Districts/34-istanbul.json
Neighborhoods/34-istanbul/kadikoy.json
Streets/34-istanbul/kadikoy.json
README.md
```

## country.json schema

```json
{
  "countryCode": "TR",
  "numericCode": "792",
  "name": { "tr": "Türkiye", "en": "Turkey", "de": "Türkei" },
  "defaultCurrencyCode": "TRY",
  "phoneCode": "+90",
  "isActive": true
}
```

## cities.json schema

```json
[
  {
    "countryCode": "TR",
    "cityCode": "34",
    "name": { "tr": "İstanbul", "en": "Istanbul" },
    "latitude": 41.0082,
    "longitude": 28.9784,
    "isCoastalCity": true,
    "isActive": true
  }
]
```

## district file schema

```json
[
  {
    "countryCode": "TR",
    "cityCode": "34",
    "districtCode": "KADIKOY",
    "name": { "tr": "Kadıköy", "en": "Kadikoy" },
    "latitude": 40.9911,
    "longitude": 29.0270,
    "isCoastalDistrict": true,
    "isActive": true
  }
]
```

## neighborhood file schema

```json
[
  {
    "countryCode": "TR",
    "cityCode": "34",
    "districtCode": "KADIKOY",
    "neighborhoodCode": "CAFERAGA",
    "name": { "tr": "Caferağa", "en": "Caferaga" },
    "postalCode": "34710",
    "isActive": true
  }
]
```

## street file schema

```json
[
  {
    "countryCode": "TR",
    "cityCode": "34",
    "districtCode": "KADIKOY",
    "neighborhoodCode": "CAFERAGA",
    "streetCode": "MODA_CADDESI",
    "name": { "tr": "Moda Caddesi", "en": "Moda Avenue" },
    "postalCode": "34710",
    "isActive": true
  }
]
```

## Seed behavior

Create service methods that can:
- seed country
- seed cities
- seed all district files
- seed all neighborhood files recursively
- seed all street files recursively

The service must discover JSON files recursively under `Location/TR`.

## Output

Report:
```text
1. Created Turkey seed files
2. Whether full official dataset exists
3. Whether only development sample was created
4. Import instructions for full production dataset
```
