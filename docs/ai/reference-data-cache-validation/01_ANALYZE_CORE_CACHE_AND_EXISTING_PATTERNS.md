# 01 — Analyze Core Cache and Existing Patterns

Do not write code in this step. Analyze first.

## Search targets

Search the entire repository for:

```text
IAizenQueryHandlerCacheable
IAizenDistributedCache
MetropolCacheType
MetropolCacheOptions
AizenQueryHandler
MetropolQueryHandler
CacheOptions
CacheType
ExistsAsync
GetAsync
SetAsync
RemoveAsync
ToAizenHashLine
```

## Inspect Core/cache service

Find the Core/cache service and inspect cache abstractions, cache options, cache type enum, memory/distributed implementations, query pipeline behavior, how `IAizenQueryHandlerCacheable` is detected, how cache keys are generated, and how expiration options are applied.

## Inspect implementations

Find handlers implementing `IAizenQueryHandlerCacheable` and report base handler type, response type, cache type, duration, automatic vs explicit key behavior, and invalidation behavior.

Find `IAizenDistributedCache` usages and classify them as token/session validation, data retrieval, custom key usage, manual invalidation or ordinary query cache.

## ReferenceData inspection

Inspect ReferenceData queries and commands. Report query handlers that are good candidates for `IAizenQueryHandlerCacheable`, handlers that should not be cached, commands that must invalidate cache, existing cache key/invalidation services, and missing files.

## Output

Produce an analysis report only. Do not modify files.
