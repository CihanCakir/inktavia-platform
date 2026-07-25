# PAY-0 + PAY-1 — BFF↔Payment Bridge + Provider Payouts — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. The Payment backend is rich (transactions, payouts,
> invoices, subscriptions) but **every controller is Admin-only** and Payment is **not reachable from the MarineProvider
> BFF**. This prompt (a) builds the BFF↔Payment bridge so the provider portal can call provider-scoped Payment
> endpoints, and (b) ships the first provider surface: **Payouts** (the actual money disbursed to the provider),
> complementing the CargoDry Finance page (which shows settlement *accrual*).
>
> **Scope = read-only, provider-scoped.** No new tables. Provider sees ONLY their own data (resolved from the BFF
> assertion, never from route/body).

## Verified anchors
- `PayoutRecordEntity`: `ProviderProfileId`, `Amount`, `CurrencyCode` (default "TRY"), `Status` (`PayoutStatus`:
  Pending=1, Processing=2, Completed=3, Failed=4, Cancelled=5, OnHold=6, Approved=7), `GatewayProvider`,
  `GatewayPayoutId`, `RequestedAt`, `ProcessedAt`, `HeldAt`, `HoldReason`, `FailureReason`, `SourceType`, `SourceId`,
  `Description`.
- Repo `IPayoutRecordRepository.GetPagedAsync(PayoutStatus? status, long? providerProfileId, DateTime? fromDate,
  DateTime? toDate, int skip, int take, ct)` → `(Items, Total)` — **already provider-filterable.**
- Identity: no Payment controller reads `IAizenInfoAccessor` yet, but the module uses the shared Operation starter, so
  `IAizenInfoAccessor.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId` is available (same as CargoDry).
- Keycloak audience loop (`infrastructure/keycloak/provider-realm/setup-provider-realm.sh`, ~line 113) lists
  `identity/reference-data/vessel/file-storage/service-request/messaging/notification/cargodry` — **payment-api is
  missing.**
- `payment-api` docker-compose env has `KEYCLOAK_AUDIENCE: payment-api` but **no `BffAssertion`** config.
- Learned patterns (memories): a module behind the provider BFF needs (1) an `aud-<module>-api` mapper on
  `provider-portal-bff`, and (2) `BffAssertion__SharedSecret` env, or it returns **401** → BFF 911. Module controllers
  must return the **Aizen envelope** (`AizenWebApiController.SetResponse`) or the BFF Refit `.Body` is empty/500.

---

## PAY-0 — Bridge (do this first; nothing works without it)

1. **Keycloak audience mapper.** Add `payment-api` to the loop in `setup-provider-realm.sh`:
   ```bash
   for AUD in identity-api reference-data-api vessel-api file-storage-api \
              service-request-api messaging-api notification-api cargodry-api payment-api; do
   ```
   Then apply to the **running** Keycloak (the script may not re-run): add the `aud-payment-api` oidc-audience-mapper to
   the `provider-portal-bff` client via kcadm (mirror the notification fix), then restart `bff-marineprovider` (+ replica)
   to bust the cached service token.

2. **payment-api env** (`docker-compose.yaml`, payment-api → environment): mirror cargodry-api:
   ```yaml
   BffAssertion__SharedSecret: ${AIZEN_BFF_ASSERTION_SECRET:-}
   BffAssertion__AllowedClientIds__0: provider-portal-bff
   ```

3. **Module provider controller.** New `PaymentProviderController : AizenWebApiController`
   `[ApiController] [Route("api/v1/payment/provider")] [Authorize]`, ctor `(IHttpContextAccessor, IAizenCQRSProcessor,
   IAizenInfoAccessor)`. Add:
   ```csharp
   private long ResolveProviderProfileId()
   {
       var pid = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
       if (pid <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");
       return pid;
   }
   ```
   All provider endpoints resolve pid here and pass it into the query — **never** accept it from route/body.

4. **BFF.** `IPaymentRemoteCall` (new, under `Application/Common/RemoteClients/`) + register it; base URL from config
   `RemoteCalls:IPaymentRemoteCall:BaseUrl = http://payment-api:8080`. The service token now carries `payment-api` aud
   (step 1), so calls authenticate.

---

## PAY-1 — Provider Payouts (list + summary)

### DTOs (Payment.Abstraction/Dto)
```csharp
public sealed class ProviderPayoutDto
{
    public decimal Amount            { get; init; }
    public string  CurrencyCode      { get; init; } = "TRY";
    public int     Status            { get; init; }      // PayoutStatus (numeric)
    public string  GatewayProvider   { get; init; } = default!;
    public string? GatewayPayoutId   { get; init; }      // transfer reference (mask/short if sensitive)
    public string? SourceType        { get; init; }
    public long?   SourceId          { get; init; }
    public string? Description        { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public string? HoldReason        { get; init; }
    public string? FailureReason     { get; init; }
}
public sealed class ProviderPayoutPagedResultDto { public List<ProviderPayoutDto> Items {get;init;}=[]; public int Total{get;init;} public int Page{get;init;} public int PageSize{get;init;} }
public sealed class ProviderPayoutSummaryDto
{
    public decimal PendingAmount    { get; init; }   // Pending + Approved (awaiting disbursement)
    public decimal ProcessingAmount { get; init; }   // Processing
    public decimal CompletedAmount  { get; init; }   // Completed (paid)
    public decimal OnHoldAmount     { get; init; }   // OnHold
    public string  CurrencyCode     { get; init; } = "TRY";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
```

### Queries + handlers
- `GetProviderPayouts { long ProviderProfileId; int? Status; int Page=1; int PageSize=20 }` → paged DTO. Handler:
  `skip=(Page-1)*PageSize`; `GetPagedAsync((PayoutStatus?)Status, providerProfileId: pid, null, null, skip, PageSize, ct)`;
  map → DTO.
- `GetProviderPayoutSummary { long ProviderProfileId }` → summary. Add repo helper
  `Task<decimal> SumProviderAmountByStatusAsync(long pid, IEnumerable<PayoutStatus> statuses, ct)`
  (mirror CargoDry `SumProviderPayoutByStatusAsync`) and fill each bucket. Currency: first payout or "TRY".

### Controller endpoints (PaymentProviderController)
```
GET /api/v1/payment/provider/payouts?status=&page=1&pageSize=20   → AizenApiResponse<ProviderPayoutPagedResultDto>
GET /api/v1/payment/provider/payouts/summary                       → AizenApiResponse<ProviderPayoutSummaryDto>
```
Typed `AizenApiResponse<T>` via `SetResponse`; `[ProducesResponseType]`.

### BFF passthrough
- `IPaymentRemoteCall`: `GetPayouts([Query] int? status,[Query] int page,[Query] int pageSize)`,
  `GetPayoutSummary()` → `AizenApiResponse<...>`.
- BFF queries `GetProviderPayoutsBff` + `GetProviderPayoutSummaryBff` (resolver `_resolver.ResolveAsync` + `_h.ProfileId`
  guard → `.Body`, mirror `GetCargoDryTrendBff`); BFF controller (new `PaymentController` under
  `Controllers/V1/`, `[Route("api/v1/provider/payment")]`): `GET payouts` + `GET payouts/summary`, typed + PRT.
- Routes: `GET /api/v1/provider/payment/payouts`, `.../payouts/summary`.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Table: `payout_records`. DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `payment-api` + `bff-marineprovider`; clean boot.
2. **PAY-0 proof — bridge auth works (no 401):**
   ```
   docker compose exec payment-api printenv | grep -i BffAssertion      # SharedSecret + AllowedClientIds set
   GET /api/v1/provider/payment/payouts?page=1&pageSize=20               # provider2 token → 200 (NOT 401/911)
   ```
   If 401 → audience mapper (`aud-payment-api` on provider-portal-bff) or BffAssertion missing; fix, restart bff, retry.
3. **DB inputs** (no token):
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Status\", COUNT(*), COALESCE(SUM(\"Amount\"),0) FROM payout_records
      WHERE \"ProviderProfileId\"=100011 GROUP BY 1 ORDER BY 1;"
   ```
   If provider2 has **no** payout rows, seed 2 idempotent dev rows (dev-gated): one Pending (Amount 60) + one Completed
   (Amount 40) with `SourceType='CargoDrySettlement'`, `GatewayProvider='manual'` — so the summary/list are testable.
   (Guard: skip if provider2 already has payout rows.)
4. **HTTP smoke** (provider2 token):
   ```
   GET /api/v1/provider/payment/payouts                # 200, only provider2 rows
   GET /api/v1/provider/payment/payouts?status=1       # only Pending
   GET /api/v1/provider/payment/payouts/summary        # pending/processing/completed/onhold match DB sums
   ```
5. **Isolation:** a different provider's payout must NOT appear in provider2's list.

## Acceptance
- Provider BFF → Payment authenticates (200, not 401); `aud-payment-api` + BffAssertion configured.
- `PaymentProviderController` resolves `ProviderProfileId` from the assertion (never route/body); returns Aizen envelope.
- `GET /provider/payment/payouts` (+status/paging) and `/payouts/summary` return provider-scoped data; typed + PRT;
  isolation holds. Read-only; build clean; env + DB + HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("PAY-0 + PAY-1"): BFF↔Payment bridge (aud-payment-api mapper, payment-api BffAssertion, module
provider-auth resolver, BFF IPaymentRemoteCall) + provider payouts list/summary (reusing `GetPagedAsync`), DTOs in
Abstraction, typed + PRT. Verified: bridge auth 200, DB sums, isolation. Next: PAY-2 payment profile (manage).
