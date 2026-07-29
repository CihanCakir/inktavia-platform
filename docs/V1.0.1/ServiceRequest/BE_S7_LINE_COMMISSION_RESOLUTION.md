# BE-S7 — Line-level commission resolution (Payment `CommissionRule` dimensions) + remote-call contract + SR preview — Backend Prompt

> **Modules:** `Aizen.Modules.Payment` (resolver + contract — primary) consumed by `Aizen.Modules.ServiceRequest` (preview).
> **Phase:** ServiceRequest S7 (roadmap `docs/V1.0.1/ServiceRequest/ROADMAP.md`), narrow P8 core (S1 ✅ → **S7** → S8 → P8).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §20.11 (per-line commission), §6/§13.7 (CommissionRule
> dimensions), §20.15 (Σ line commission = transaction commission — the authoritative per-line SNAPSHOT is S8).
> **Rule:** EXTEND BE-P2's `CommissionRuleResolver` to a **line-set** resolution; do NOT rewrite it, do NOT re-resolve
> base rates differently. Mirror the established cross-module pattern `IPaymentModuleRemoteCall`
> (`Payment.Abstraction/RemoteCall`, internal `[Authorize]` service-to-service endpoint) already used by SR for escrow.
> **Non-goal:** no line snapshot rows / 8-invariant persistence (S8), no acceptance wiring / immutable snapshot (P8), no
> `MarkAppliedAsync` (applied-count is P8 only), no funding/discount (S6), no FX/TL fixing (S3). S7 is **pure resolution +
> a preview surface**; it persists nothing authoritative. Inspect first.

## 0. Verified current state
- BE-P2 `CommissionRuleResolver.ResolveAsync(ctx) → CommissionResolution(RuleId, RuleCode, RuleType, Rate,
  SpecificityRank, Priority, Source)` resolves **one** rate; candidacy already honors `LineType` + `CommissionEligibility`
  + `ProductCode` dimensions (a rule whose declared dim the context doesn't supply is excluded). Fail-loud
  `CommissionRuleConflict` on ties. This is the single-line primitive S7 calls per line.
- BE-S1 (done) added per line: `LineCommissionEligibility {Eligible/Exempt/InheritFromCategory}` + computed
  `CommissionBaseAmount` (Eligible = pre-tax post-line-discount `Max(LineSubtotal−proRataDiscount,0)`, Exempt = 0) + offer
  `CommissionBaseTotal` (Σ, ≤ Subtotal). These are the **inputs** to S7's resolution.
- Cross-module contract: `IPaymentModuleRemoteCall : IAizenRemoteCall` (Payment.Abstraction) exposes internal endpoints
  (`/api/v1/payment/internal/...`, `[Authorize]` service-to-service, idempotent) — SR already calls `CreateEscrowAsync`/
  `ReleaseEscrowAsync`. S7 adds one resolution method here.
- SR references `Payment.Abstraction` only (not Payment.Domain) — keep it that way (module boundary).

## 1. Payment-side line-set commission resolver (§20.11) — pure
`Payment.Domain` (extend, or a thin service over `CommissionRuleResolver`):
`ResolveLineCommissionsAsync(providerContext, IReadOnlyList<LineCommissionInput> lines, atUtc)
 → LineCommissionResolutionResult`.
- `providerContext`: `ProviderProfileId, ProviderPlanId?, CurrencyCode` (shared across lines).
- `LineCommissionInput` (per line): `LineRef` (offer-item id / correlation), `LineType` (from `ServiceRequestOfferItemType`
  mapped to the Payment `LineType` dim), `ProductCode?`, `CommissionEligibility` (Eligible/Exempt/InheritFromCategory),
  `CategoryCode?`, `CommissionBaseAmount` (from S1), `LineProviderRevenue` (the provider-receivable for the line =
  `Max(LineSubtotal − proRataDiscount, 0)` — equals base for eligible; for exempt/pass-through the provider still receives
  it though commission = 0).
- Per line:
  - `Exempt` → `Commissionable=false, ResolvedRate=0, CommissionAmount=0, ProviderNet=LineProviderRevenue`.
  - `Eligible` / `InheritFromCategory` → call BE-P2 `ResolveAsync` with that line's dims (Eligible passes
    `CommissionEligibility=Eligible`; `InheritFromCategory` resolves by category/plan/global without a line-eligibility
    override) → `ResolvedRate`; `CommissionAmount = MoneyMath.Round(CommissionBaseAmount × ResolvedRate)`;
    `ProviderNet = LineProviderRevenue − CommissionAmount`; `Commissionable=true`.
- **Aggregation guarantee (§20.15):** `TransactionCommission = Σ CommissionAmount`,
  `TransactionProviderNet = Σ ProviderNet`, `TransactionCommissionBase = Σ CommissionBaseAmount`. Commission rounded
  **per line** then summed (matches §13.6 "commission rounded separately"); the transaction total is the sum of rounded
  line amounts (document this so P8/S8 reconcile to the same number — no separate transaction-level rounding of a blended
  rate). Fail-loud: any line conflict → `CommissionRuleConflict` (propagate; do not swallow). Pure — **no writes, no
  MarkApplied** (applied-count is P8).
- Result: `{ Lines: [{LineRef, Commissionable, CommissionBaseAmount, ResolvedRate, RuleCode, CommissionAmount,
  ProviderNet}], TransactionCommission, TransactionProviderNet, TransactionCommissionBase, CurrencyCode }`.

> **Tax note (open, YMM):** `LineProviderRevenue`/`CommissionBaseAmount` are **pre-tax** (consistent with S1). Whether
> provider net is reported gross/net of KDV is the standing YMM open decision — S7 stays pre-tax and records the basis; do
> not bake a VAT treatment.

## 2. Remote-call contract (mirror `IPaymentModuleRemoteCall`)
Add to `IPaymentModuleRemoteCall` (or a sibling internal contract) in `Payment.Abstraction/RemoteCall`:
`[AizenRemoteCallPost("/api/v1/payment/internal/commission/resolve-lines")]
 Task<ResolveLineCommissionsRemoteCallResponse> ResolveLineCommissionsAsync(
   [AizenRemoteCallBody] ResolveLineCommissionsRemoteCallRequest request,
   [AizenRemoteCallHeader("Authorization")] string authorization, CancellationToken ct = default);`
- Request/Response DTOs in `Payment.Abstraction.RemoteCall.Requests/Responses` (concrete typed DTOs — NOT `object`):
  request carries providerContext + line inputs; response carries the per-line result + transaction totals above.
- Payment-side internal controller endpoint (`[Authorize]` service-to-service, same auth style as the escrow endpoints)
  → MediatR query `ResolveLineCommissionsQuery` → the §1 resolver. **Idempotent/read-only** (pure resolve; no state).

## 3. ServiceRequest-side preview (consumer) — no authoritative persistence
- A SR query `GetOfferCommissionPreviewQuery(offerId)` (or extend the offer detail/preview) that maps the offer's priced
  lines → `LineCommissionInput[]` (LineType from `ServiceRequestOfferItemType`, eligibility + base from S1, provider
  context from the offer) and calls `ResolveLineCommissionsAsync` via the contract → returns per-line
  `{resolvedRate, commissionAmount, providerNet, commissionable}` + transaction totals **for display** (offer builder
  transparency: "line 5000 → 12% → 600, your net 4400").
- **Do NOT persist** the resolved commission on the offer/line in S7 (rates change; the authoritative per-line record is
  written at acceptance in S8/P8). If a cached "last quoted" is ever wanted it must be explicitly non-authoritative +
  timestamped — default S7 = compute-on-demand, no new SR columns, no SR migration.
- SR keeps referencing `Payment.Abstraction` only.

## 4. Persistence / migration
- **Payment:** no new tables (resolver is pure; endpoint is stateless). Only new: contract method + DTOs + internal
  controller + query/handler + DI.
- **ServiceRequest:** no migration (preview is compute-on-demand). Mapping + query/handler + DTO + controller/BFF exposure
  as the offer builder needs.

## 5. Tests
- **Per-line resolution:** eligible line → rate from CommissionRule (e.g. STANDARD 0.12) → `CommissionAmount =
  Round(base×0.12)`, `ProviderNet = revenue − commission`; exempt line (Travel) → rate 0, commission 0, providerNet =
  full line; `InheritFromCategory` → resolves by category/plan/global.
- **Aggregation (§20.15):** `Σ line commission = TransactionCommission`; `Σ providerNet = TransactionProviderNet`; mixed
  eligible+exempt offer sums correctly; per-line rounding then sum is the reported total (no blended-rate re-rounding).
- **Conflict propagation:** a line whose dims hit a CommissionRule tie → `CommissionRuleConflict` surfaced (not swallowed).
- **Purity:** resolver performs no writes, no MarkApplied; same inputs → same result.
- **Contract/remote-call:** request→response DTO round-trips typed (no `object`); internal endpoint `[Authorize]` (401
  without service token, proven by test); SR preview maps offer lines → inputs → per-line display correctly.
- **Rounding:** `MoneyMath.Round` 2dp AwayFromZero; `RoundRate` 4dp.

## 6. Acceptance criteria
- Per-line commission resolved via BE-P2 CommissionRule dimensions (LineType/ProductCode/CommissionEligibility);
  eligible/exempt/inherit handled; `ProviderNet` per line correct; **Σ line commission = transaction commission** (§20.15),
  per-line rounding preserved for P8/S8 reconciliation.
- Exposed via a `Payment.Abstraction` internal remote-call (mirrors `IPaymentModuleRemoteCall`, typed DTOs, service-to-
  service `[Authorize]`); SR consumes it as a **compute-on-demand preview** with no authoritative persistence and no SR
  migration; SR still references only `Payment.Abstraction`.
- Pure/no writes/no MarkApplied (P8); no snapshot (S8); no funding/discount (S6); no FX/TL (S3); no acceptance change.
  Build clean; existing escrow remote-calls + BE-P2 resolver untouched.

## 7. Verify — run and PASTE output
1. `dotnet build` Payment + ServiceRequest: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api service-request-api`; clean boot (no migration expected —
   confirm none generated / none needed).
3. Tests green (paste): per-line eligible/exempt/inherit, aggregation Σ=transaction, conflict propagation, purity,
   typed remote-call round-trip + `[Authorize]` 401, SR preview mapping, rounding.
4. Smoke: resolve an offer {Service 5000 eligible @STANDARD 0.12, Travel 800 exempt} → Service commission 600 net 4400,
   Travel commission 0 net 800; TransactionCommission 600, TransactionProviderNet 5200, TransactionCommissionBase 5000.

## 8. Report
`REPORT_BACKEND.md` ("BE-S7"): line-set commission resolver (extends BE-P2, per-line commissionable/base/rate/amount/
providerNet, Σ=transaction, per-line rounding) + `Payment.Abstraction` internal remote-call `ResolveLineCommissions` +
SR compute-on-demand preview (no authoritative persistence, no SR migration). Note: authoritative per-line SNAPSHOT +
8-invariant = S8; acceptance orchestration + immutable snapshot + MarkApplied = P8; line discount funding = S6; FX/TL = S3;
VAT treatment = YMM open. Next: **BE-S8 (line snapshots → aggregate derivation + 8 equalities, 0 tolerance)**. Do not touch
CargoDry.
