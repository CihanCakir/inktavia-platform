# Turkey Location Seed Data

## Status

⚠️ **Development Sample Only**

This directory contains a **minimal development sample** for Istanbul/Kadikoy district only.

The full official Turkey city/district/neighborhood/street dataset is **not included** in this repository.

The seed architecture and JSON schema are fully ready. Import the official dataset into the generated JSON structure before production use.

## Schema

### country.json
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

### cities.json
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

### Districts/<plate>-<city>.json
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

### Neighborhoods/<plate>-<city>/<district>.json
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

### Streets/<plate>-<city>/<district>.json
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

## How to Import the Official Dataset

1. Obtain the official Turkish administrative data from [ADNKS](https://adnks.tuik.gov.tr/) or a GeoNames export.
2. Transform into the JSON schemas above.
3. Place files following the naming convention:
   - `Districts/<plate>-<cityslug>.json`
   - `Neighborhoods/<plate>-<cityslug>/<districtslug>.json`
   - `Streets/<plate>-<cityslug>/<districtslug>.json`
4. The seed service discovers all files recursively — no code changes required.
