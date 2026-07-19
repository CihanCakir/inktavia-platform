# CI-3a — provider-scoped renewal candidates endpoint (module + BFF)

Expose the provider's own upcoming renewal candidates (Active kits expiring soon, no open preparation). The
`GetCargoDryRenewalCandidatesQuery` already exists (admin/global) — add a provider filter and a provider surface.
**Read-only** for the provider (preparing/invoicing/dispatching a renewal stays admin-side; the provider just sees
what's coming due). No new domain model.

## 1. Query — add the provider filter
`GetCargoDryRenewalCandidatesQuery`: add
```csharp
/// <summary>When set, scopes candidates to this provider's kits. Null = global (admin).</summary>
public long? ProviderProfileId { get; init; }
```

## 2. Handler — thread it into the kit load
`GetCargoDryRenewalCandidatesQueryHandler.Handle`: the kit load is
`await _kits.GetExpiringAsync(request.WithinDays, ct: ct)`. `GetExpiringAsync` gained a
`long? providerProfileId = null` param in CI-1b, so pass it:
```csharp
var expiringKits = await _kits.GetExpiringAsync(request.WithinDays, request.ProviderProfileId, ct);
```
Everything else (open-preparation filtering, product enrichment, DTO mapping) is unchanged. Default `null` ⇒
existing admin behavior.

## 3. Module — provider controller action
`CargoDryProviderController` (`api/v1/cargodry/provider`, `[Authorize]`): add
```csharp
[HttpGet("renewals")]
public async Task<AizenApiResponse<List<CargoDryRenewalCandidateDto>?>> GetRenewalCandidates(
    [FromQuery] int withinDays = 90, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
    CancellationToken ct = default)
{
    var pid = ResolveProviderProfileId();          // assertion — never client-supplied
    var result = await _cqrs.ProcessAsync<List<CargoDryRenewalCandidateDto>>(
        new GetCargoDryRenewalCandidatesQuery {
            ProviderProfileId = pid, WithinDays = withinDays, Page = page, PageSize = pageSize }, ct);
    return SetResponse(result);
}
```
(Match the exact CQRS/response style the sibling `overview`/`alerts`/`inventory` actions use.)

## 4. BFF — `/api/v1/provider/cargodry/renewals`
- `IProviderCargoDryRemoteCall` (Refit): add
  ```csharp
  [Get("/api/v1/cargodry/provider/renewals")]
  Task<AizenApiResponse<List<CargoDryRenewalCandidateDto>>> GetRenewals(
      [Query] int withinDays, [Query] int page, [Query] int pageSize, CancellationToken ct = default);
  ```
  (Mirror the envelope/`[Query]` style of the existing `GetInventory`/`GetOverview` methods. DI registration for
  `IProviderCargoDryRemoteCall` already exists.)
- BFF `ProviderCargoDryController` (`/api/v1/provider/cargodry`): add `GET renewals` passing `withinDays`
  (default 90), `page`, `pageSize` straight through and returning the envelope. No provider id is sent — the module
  derives it from the assertion. Compose base URL already set.

## Acceptance (after rebuild cargodry-api + bff-marineprovider)
- `GET /provider/cargodry/renewals?withinDays=90` (provider2) → 200 with **2 candidates**: `CDK-PRV2-0006`
  (~5 days) and `CDK-PRV2-0005` (~20 days), ordered by expiry. Each item's `providerProfileId == 100011`.
- `CDK-PRV2-0003`/`0004` (120-day validity) are **not** returned at withinDays=90.
- A provider cannot widen scope — no `providerProfileId` param is accepted; results are always the caller's own.
- Admin renewal candidates endpoint (global) unchanged.

## Report
Append to `REPORT_BACKEND.md` ("CI-3a"): renewal candidates query gained an optional `ProviderProfileId`; provider
controller + BFF expose a read-only, assertion-scoped renewals endpoint; reuses the existing handler.
