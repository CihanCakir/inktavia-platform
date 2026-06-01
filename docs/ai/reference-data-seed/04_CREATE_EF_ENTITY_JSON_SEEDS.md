# 04 — Create EF Entity JSON Seed Files

Create JSON seed files for EF Core entities.

## Location

```text
Aizen.Modules.ReferenceData.Repository/Seed/Json/
```

## Currency

File:

```text
Currency/currencies.json
```

Seed values:

```json
[
  { "code": "TRY", "numericCode": "949", "name": "Turkish Lira", "symbol": "₺", "decimalPlaces": 2, "isBaseCurrency": true, "isActive": true },
  { "code": "USD", "numericCode": "840", "name": "US Dollar", "symbol": "$", "decimalPlaces": 2, "isBaseCurrency": false, "isActive": true },
  { "code": "EUR", "numericCode": "978", "name": "Euro", "symbol": "€", "decimalPlaces": 2, "isBaseCurrency": false, "isActive": true },
  { "code": "GBP", "numericCode": "826", "name": "Pound Sterling", "symbol": "£", "decimalPlaces": 2, "isBaseCurrency": false, "isActive": true }
]
```

## ExchangeRate

Create template only:

```text
ExchangeRate/exchange-rates.json
ExchangeRate/exchange-rate-histories.json
```

Do not invent current market rates. Use empty array or sample disabled data if the project standard supports it.

## Measurement

File:

```text
Measurement/measurement-units.json
```

Include:

```text
METER, CENTIMETER, FEET, INCH
KILOGRAM, GRAM
LITER
CELSIUS, FAHRENHEIT
PERCENTAGE
BAR
KNOT
HOUR, DAY
PIECE
```

## System

Files:

```text
System/languages.json
System/time-zones.json
System/country-phone-codes.json
System/system-parameters.json
```

Seed:
- languages: tr, en, de
- time zones: Europe/Istanbul, UTC, Europe/Berlin
- phone codes: TR +90, DE +49, IR +98
- system parameters: ReferenceData.Seed.Enabled, ReferenceData.Cache.Enabled, DefaultCurrencyCode, DefaultLanguageCode

## Idempotency key

Use:
- Currency.Code
- MeasurementUnit.Code
- Language.Code
- TimeZone.Code
- CountryPhoneCode.CountryCode
- SystemParameter.Key

## Output

Report created JSON files.
