# 02 — Create Seed Folder and JSON Convention

Create the seed folder structure under:

```text
Aizen.Modules.ReferenceData.Repository/Seed/
```

## Required folders

```text
Seed/
  Json/
    Currency/
    ExchangeRate/
    Measurement/
    Lookup/
    System/
    Location/
      TR/
        Districts/
        Neighborhoods/
        Streets/
  Models/
    Currency/
    ExchangeRate/
    Measurement/
    Lookup/
    System/
    Location/
  Readers/
  Services/
```

## Required documentation

Create:

```text
Aizen.Modules.ReferenceData.Repository/Seed/Json/README.md
```

Explain:
- JSON format rules
- Turkish location seed folder convention
- idempotency rules
- production dataset import warning
- how to add new country/location seed files
- how to add new lookup group/item

## JSON file naming rule

Use kebab-case:

```text
currencies.json
exchange-rates.json
measurement-units.json
lookup-groups.json
lookup-items.json
lookup-tree-marine.json
system-parameters.json
languages.json
time-zones.json
country-phone-codes.json
country.json
cities.json
```

For Turkey districts:

```text
Districts/34-istanbul.json
Districts/35-izmir.json
```

For neighborhoods:

```text
Neighborhoods/34-istanbul/kadikoy.json
Neighborhoods/34-istanbul/besiktas.json
```

For streets:

```text
Streets/34-istanbul/kadikoy.json
```

## Output

Report created folders and files.
