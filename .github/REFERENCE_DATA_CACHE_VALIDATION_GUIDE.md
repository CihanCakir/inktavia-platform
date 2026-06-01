# ReferenceData Cache and Validation Guide

## Objective

Review and update ReferenceData cache usage according to the existing Aizen cache architecture.

## Cache patterns to understand

### Pattern 1 — Manual distributed cache usage

Use `IAizenDistributedCache` for explicit cache keys, token/session validation, cache existence checks, custom data retrieval and command-side invalidation.

### Pattern 2 — Cacheable query handler

Use the existing cacheable query handler interface, for example:

```csharp
public class GetChannelQueryHandler : MetropolQueryHandler<GetChannelQuery, GetChannelDto>,
    IAizenQueryHandlerCacheable
{
    public MetropolCacheType CacheType => MetropolCacheType.Memory;

    public MetropolCacheOptions CacheOptions => new()
    {
        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30)
    };
}
```

If the real project uses different type names, follow the actual project types.

## ReferenceData cache decision

Use `IAizenQueryHandlerCacheable` for stable, read-heavy queries.

Recommended cacheable queries:

```text
GetCurrencyListQuery
GetCurrencyDetailQuery
GetBaseCurrencyQuery
GetExchangeRateQuery
GetLookupGroupListQuery
GetLookupGroupTreeQuery
GetLookupGroupChildrenQuery
GetLookupItemsByGroupQuery
GetMeasurementUnitListQuery
GetMeasurementUnitsByTypeQuery
GetCountryListQuery
GetCityListByCountryQuery
GetDistrictListByCityQuery
GetNeighborhoodListByDistrictQuery
GetSystemParameterByKeyQuery
```

Use shorter TTL for exchange rates and system parameters.

Do not cache commands, mutation handlers, seed operations or Mongo index operations.
