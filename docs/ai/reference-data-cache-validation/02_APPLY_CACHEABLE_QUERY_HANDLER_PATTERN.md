# 02 — Apply Cacheable Query Handler Pattern

Apply `IAizenQueryHandlerCacheable` to ReferenceData query handlers that are safely cacheable.

## Rule

Only use the exact interface and cache option types found in step 01. If the project uses `IAizenQueryHandlerCacheable`, `MetropolCacheType` and `MetropolCacheOptions`, follow that pattern exactly. If it uses different Aizen Core names, use the actual names.

## Recommended cacheable handlers

```text
GetCurrencyListQueryHandler
GetCurrencyDetailQueryHandler
GetBaseCurrencyQueryHandler
GetExchangeRateQueryHandler
GetLookupGroupListQueryHandler
GetLookupGroupTreeQueryHandler
GetLookupGroupChildrenQueryHandler
GetLookupItemsByGroupQueryHandler
GetMeasurementUnitListQueryHandler
GetMeasurementUnitsByTypeQueryHandler
GetMeasurementUnitDetailQueryHandler
GetCountryListQueryHandler
GetCityListByCountryQueryHandler
GetDistrictListByCityQueryHandler
GetNeighborhoodListByDistrictQueryHandler
GetSystemParameterByKeyQueryHandler
GetSystemParametersByPrefixQueryHandler
```

## Do not cache

```text
Command handlers
UpdateExchangeRateCommandHandler
SyncExchangeRatesCommandHandler
MoveLookupGroupCommandHandler
Create/Update/Activate/Deactivate handlers
Seed service
Mongo index initializer
```

## Recommended TTL

```text
Currency list/detail/base: 24 hours
Lookup groups/tree/items: 12 hours
Measurement units: 24 hours
Location country/city/district/neighborhood: 24 hours
ExchangeRate current: 5 to 15 minutes
SystemParameter by key/prefix: 5 to 60 minutes according to existing standard
```

## DocumentationInfo

Every modified query handler must have DocumentationInfo explaining why the query is cacheable, its cache duration and invalidation source.

## Output

Report updated handlers, cache type, cache options, intentionally non-cached queries and missing query handlers.
