# 03 — Review Manual IAizenDistributedCache Usage

Review whether ReferenceData has manual cache logic and whether it should be replaced by `IAizenQueryHandlerCacheable`.

## Rule

Manual `IAizenDistributedCache` usage is acceptable for explicit custom cache keys, token/session validation, key existence checks, command-side invalidation and special explicit lookup scenarios.

Manual cache usage is not preferred for ordinary query result caching if `IAizenQueryHandlerCacheable` handles it.

## Inspect services

```text
ReferenceDataCacheKeyService
ReferenceDataCacheInvalidationService
CurrencyReferenceService
ExchangeRateReferenceService
LookupReferenceService
LookupTreeService
LocationReferenceService
SystemParameterReferenceService
```

## Required actions

1. Refactor query handlers that manually cache ordinary query responses to `IAizenQueryHandlerCacheable`.
2. Keep manual cache only inside cache invalidation, cache key, security/token-bound logic or special explicit key lookup scenarios.
3. Ensure cache keys are centralized in `IReferenceDataCacheKeyService`.
4. Ensure invalidation is centralized in `IReferenceDataCacheInvalidationService`.

## Output

Report manual cache usage found, kept, refactored, and cache key/invalidation service status.
