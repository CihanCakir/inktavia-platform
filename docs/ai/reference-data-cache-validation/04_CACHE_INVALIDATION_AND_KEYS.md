# 04 — Cache Invalidation and Keys

Ensure write commands trigger correct cache invalidation.

## Required invalidation behavior

### Currency

```text
CreateCurrencyCommand -> currency list
UpdateCurrencyCommand -> currency list and currency detail
ActivateCurrencyCommand -> currency list and currency detail
DeactivateCurrencyCommand -> currency list and currency detail
SetBaseCurrencyCommand -> currency list and base currency
```

### ExchangeRate

```text
UpdateExchangeRateCommand -> exchange-rate pair
SyncExchangeRatesCommand -> all affected exchange-rate pair keys
CreateExchangeRateHistoryCommand -> exchange-rate history if cached
```

### Lookup

```text
CreateLookupGroupCommand -> lookup group list and lookup tree
UpdateLookupGroupCommand -> lookup group list and lookup tree
MoveLookupGroupCommand -> lookup group list, lookup tree, affected children and breadcrumb
ActivateLookupGroupCommand -> lookup group list and lookup tree
DeactivateLookupGroupCommand -> lookup group list and lookup tree
CreateLookupItemCommand -> lookup items by group and lookup tree
UpdateLookupItemCommand -> lookup items by group and lookup tree
ActivateLookupItemCommand -> lookup items by group and lookup tree
DeactivateLookupItemCommand -> lookup items by group and lookup tree
SetDefaultLookupItemCommand -> lookup items by group
ChangeLookupItemSortOrderCommand -> lookup items by group
```

### Measurement

```text
CreateMeasurementUnitCommand -> measurement list and unit type
UpdateMeasurementUnitCommand -> measurement list, unit detail and unit type
Activate/Deactivate -> measurement list and unit type
```

### Location

```text
Country mutations -> countries and country detail
City mutations -> cities by country
District mutations -> districts by city
Neighborhood mutations -> neighborhoods by district
Street mutations -> streets by neighborhood
```

### SystemParameter

```text
Create/Update/Activate/Deactivate -> key and prefix caches
```

## Cache key service

Ensure `ReferenceDataCacheKeyService` contains deterministic methods:

```text
Countries(languageCode)
Country(countryCode, languageCode)
Cities(countryCode, languageCode)
City(countryCode, cityCode, languageCode)
Districts(countryCode, cityCode, languageCode)
Neighborhoods(countryCode, cityCode, districtCode, languageCode)
LookupGroupList()
LookupTree()
LookupItems(groupCode)
CurrencyList()
Currency(currencyCode)
BaseCurrency()
ExchangeRate(fromCurrencyCode, toCurrencyCode)
MeasurementUnits(unitType)
MeasurementUnit(unitCode)
SystemParameter(key)
SystemParametersByPrefix(prefix)
```

If wildcard removal is unavailable, invalidate only deterministic keys available from command context and add TODO comments for broader invalidation.

## Output

Report updated command handlers, invalidation calls, key methods added, limitations and TODO items.
