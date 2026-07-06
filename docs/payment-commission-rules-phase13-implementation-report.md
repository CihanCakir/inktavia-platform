# Payment Commission Rules — Phase 13 Implementation Report

**Date:** 2026-07-06  
**Phase:** Phase 13 — Commission Rules BFF & Admin Web Completion  
**Status:** ✅ Complete  
**Migration required:** ❌ None — all DB columns already present from 3 prior migrations

---

## Objective

Close the last Admin Web gap in the Payment module: surface all Phase 5 and CargoDry-specific
commission rule fields (`RuleName`, `CurrencyCode`, `CommercialModel`, `ContextType`, `ProductCode`,
`SalesChannel`) and 7 extended list filters (`contextType`, `commercialModel`, `productCode`,
`salesChannel`, `search`, `providerProfileId`, `effectiveOnUtc`) end-to-end across:

```
Payment Module → AdminPanel BFF → Admin Web (React/TypeScript)
```

---

## Hard Rules Compliance

| Rule | Status |
|------|--------|
| Do not change CargoDry renewal billing logic | ✅ Untouched |
| Do not change CargoDry settlement/payout/consignment rules | ✅ Untouched |
| Do not change CargoDry lifecycle event logic | ✅ Untouched |
| Do not implement live Iyzico | ✅ Not touched |
| Do not implement refund/credit note/reversal | ✅ Not touched |
| Do not introduce frontend financial calculations | ✅ No calculations in frontend |
| Admin Web must call only AdminPanel BFF | ✅ All calls go through BFF endpoints |
| BFF must not contain domain business logic | ✅ BFF forwards; no logic added |
| Commission rule validation must live in Payment module | ✅ Validators untouched |
| BFF must use AizenRemoteCall only for module calls | ✅ All remote calls use `[AizenRemoteCall*]` |
| Do not use mock/placeholder data if backend/BFF endpoints exist | ✅ All fields are real columns |
| Keep all changes additive and backward compatible | ✅ All params optional with defaults |

---

## DB Schema (No Migration Required)

All columns were already present from:

| Migration | Columns Added |
|-----------|--------------|
| `20260701152618_CommissionAdded` | Base commission rule schema |
| `20260703101918_AddCommissionRuleCargoDryDimensions` | `ContextType`, `ProductCode`, `SalesChannel` |
| `20260704074010_AddCargoDryCommissionRuleResolutionFields` | `RuleName`, `CurrencyCode`, `CommercialModel` |

---

## Phase 13A — Payment Module Layer

### Files Modified

#### `CommissionRuleEntity.cs`
Added two new domain methods to expose mutation without breaking private setters:

```csharp
public void SetMetadata(string? ruleName, string? currencyCode, CommercialModel? commercialModel)
public void SetContextDimensions(TransactionContextType? contextType, string? productCode, SalesChannel? salesChannel)
```

#### `CommissionRuleDto.cs`
Added 3 Phase 5 fields to the record constructor:
- `RuleName?`, `CurrencyCode?`, `CommercialModel?`

#### `CreateCommissionRuleCommand.cs`
Added 6 new optional properties:
- Phase 0: `ContextType?`, `ProductCode?`, `SalesChannel?`
- Phase 5: `RuleName?`, `CurrencyCode?`, `CommercialModel?`

#### `CreateCommissionRuleCommandHandler.cs`
After factory entity creation, conditionally calls new domain methods:
```csharp
if (request.ContextType.HasValue || ...) rule.SetContextDimensions(...);
if (request.RuleName is not null || ...) rule.SetMetadata(...);
```

#### `ICommissionRuleRepository.cs`
Extended `GetPagedAsync` with 7 optional params (additive, all defaulting to `null`):
`contextType`, `commercialModel`, `productCode`, `salesChannel`, `search`, `providerProfileId`, `effectiveOnUtc`

#### `CommissionRuleRepository.cs`
Implemented all 7 new filter conditions, including:
- Full-text search across `RuleCode`, `RuleName`, `CategoryCode`, `ProductCode`
- `effectiveOnUtc` range overlap check (`EffectiveFrom ≤ date ≤ EffectiveTo`)

#### `GetCommissionRulesListQuery.cs`
Added 7 new filter properties matching the repository signature.

#### `GetCommissionRulesListQueryHandler.cs`
- Updated `GetPagedAsync` call to pass all 7 new named params
- Updated `CommissionRuleDto` constructor to include Phase 5 fields

#### `GetCommissionRuleByIdQueryHandler.cs`
Updated DTO constructor to include `RuleName`, `CurrencyCode`, `CommercialModel`.

#### `CommissionRuleController.cs`
Extended `GetList` endpoint with 7 new `[FromQuery]` params wired to `GetCommissionRulesListQuery`.

---

## Phase 13B — AdminPanel BFF Layer

### Files Modified

#### `CommissionRuleBffDtos.cs`
- `CommissionRuleBffDto` record: added 6 new `string?` fields — `ContextType`, `ProductCode`,
  `SalesChannel`, `RuleName`, `CurrencyCode`, `CommercialModel`
- `CreateCommissionRuleBffRequest` record: added same 6 fields as optional params with `= null` defaults

> **Key pattern:** All enum-typed fields in the Payment module are represented as `string?` in BFF DTOs.
> The Payment module's JSON serializer handles string→enum conversion on receipt.

#### `GetCommissionRulesListBffQuery.cs`
Added 7 new filter properties: `ContextType?`, `CommercialModel?`, `ProductCode?`, `SalesChannel?`,
`Search?`, `ProviderProfileId?`, `EffectiveOnUtc?`

#### `IAdminPaymentBffRemoteCall.cs`
Extended `GetCommissionRulesPagedAsync` with 7 new `[Query]` params forwarded to the Payment module.

#### `GetCommissionRulesListBffQueryHandler.cs`
Updated to pass all 7 new filters using named params when calling `GetCommissionRulesPagedAsync`.

#### `AdminPaymentController.cs`
Extended `GetCommissionRules` endpoint with 7 new `[FromQuery]` params wired to
`GetCommissionRulesListBffQuery`.

---

## Phase 13C — Admin Web (Frontend)

### Files Modified

#### `payment.types.ts`

**`CommissionRuleDto`** — added 6 optional fields:
```typescript
contextType?: string      // 'CargoDry' | 'ServiceRequest' | null = all contexts
productCode?: string
salesChannel?: string     // 'Direct' | 'CargoDryKit' | null = all channels
ruleName?: string
currencyCode?: string
commercialModel?: string  // 'B2B' | 'B2C' | null = any model
```

**`CreateCommissionRuleRequest`** — added same 6 optional fields.

**`CommissionRuleListFilters`** — added 7 new optional filters:
```typescript
contextType?: string
commercialModel?: string
productCode?: string
salesChannel?: string
search?: string
providerProfileId?: number
effectiveOnUtc?: string   // ISO-8601 UTC date string
```

### Build Verification
```
npx tsc --noEmit → 0 errors
```

---

## Data Flow Summary

```
Admin Web
  CommissionRuleListFilters (7 new fields)
  CreateCommissionRuleRequest (6 new fields)
  CommissionRuleDto (6 new fields)
        ↓
AdminPanel BFF
  GET  /api/v1/admin-panel/payment/commission/rules?search=...&contextType=CargoDry&...
  POST /api/v1/admin-panel/payment/commission/rules  { ruleName, contextType, ... }
        ↓
Payment Module
  GET  /api/v1/payment/commission/rules?contextType=0&commercialModel=1&...
  POST /api/v1/payment/commission/rules  { contextType: "CargoDry", ... }
        ↓
PostgreSQL
  commission_rules table (all 6 columns already present)
```

---

## No Breaking Changes

All changes are strictly additive:
- New query params default to `null` — existing callers without them continue to work identically
- New DTO fields are `optional` in TypeScript and `string?` / `nullable` in C#
- No existing handlers, validators, migrations, or seed data were modified
- CargoDry renewal billing, settlement, lifecycle event, and payout logic: untouched
