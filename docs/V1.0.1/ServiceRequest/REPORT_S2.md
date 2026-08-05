# REPORT — BE_S2 (pricing attribute definitions + values + immutable acceptance snapshot)

> Scope: additive. New definition/value entities + admin CRUD (ServiceRequest), a value validator, one new
> ReferenceData remote-call method, and an immutable per-attribute snapshot captured at acceptance in the **Payment**
> economics snapshot. **Descriptive metadata only** — nothing here enters the line money math or the S1/S6/S7/S8
> 8-equality; the S8 line-snapshot economics and P8 acceptance economics are unchanged.
> Source spec: `docs/V1.0.1/ServiceRequest/BE_S2_PRICING_ATTRIBUTES.md`.

---

## Architecture decision (where S2d lives)

The spec's S2d says the attribute snapshot is an *immutable child of the line economics snapshot, populated in the P8
`CreateFromLines` path*. In this codebase that line snapshot (`OfferLineEconomicsSnapshotEntity`) and `CreateFromLines`
live in the **Payment** module, reached from ServiceRequest via the `CalculateServiceRequestEconomics` remote call — not
in ServiceRequest. Per the chosen option ("follow the doc"), the attribute snapshot is a real **FK-Restrict child of the
Payment line snapshot**, and the attribute data is threaded from SR through the acceptance-economics remote-call contract
(resolved SR-side, including the denormalized label) and materialised inside `CreateFromLines`. It is attached as a
descriptive child and never participates in any sum or invariant.

---

## S2a — `PricingAttributeDefinition` (admin-owned, category-scoped)

New entities (ServiceRequest `Domain/Entities/Pricing/`):
- **`PricingAttributeDefinitionEntity`** — `Code` (stable, unique, upper-invariant), `NameTr`/`NameEn`, `DataType`
  (`Lookup`/`Number`/`Text`/`Boolean` — new enum `Abstraction/Enum/PricingAttributeDataType.cs`), `LookupGroupCode`
  (kept only when `DataType=Lookup`), `IsRequired`, `SortOrder`, `MinValue`/`MaxValue` (kept only when `Number`),
  `IsActive`. Validating `Create`/`Update` factories; owned `Categories` join collection; `Deactivate()`.
- **`PricingAttributeDefinitionCategoryEntity`** — applicability scope join (`PricingAttributeDefinitionId` ×
  `ServiceCategoryCode`); replace-on-update, deduped, upper-invariant.

**Admin CRUD** (`Application/Command/Pricing/PricingAttributeDefinition{Commands,Handlers}.cs` +
`Controller/V1/Admin/AdminPricingAttributeController.cs`, `[Authorize(Roles="Admin")]`,
`api/v1/admin/service-requests/pricing-attributes`): List (optional `?serviceCategoryCode`), Create, Update (Code
immutable), Delete (= `Deactivate`, keeps the stable Code so existing snapshots stay resolvable and it can be
reactivated).

**Validation** (`Services/Pricing/PricingAttributeDefinitionValidator.cs`): names + ≥1 category required; when `Lookup`,
`LookupGroupCode` **must resolve** to an existing R4 group — fail-loud via the remote call (an unknown group resolves to
an empty item set → rejected, `SR_PRICING_ATTR_UNKNOWN_GROUP`); `Number` `min ≤ max`. Uniqueness of `Code` is enforced in
the Create handler (`SR_PRICING_ATTR_CODE_EXISTS`).

**Migration** `20260805192934_AddPricingAttributes` — tables `pricing_attribute_definitions` (unique `Code`),
`pricing_attribute_definition_categories` (FK→definition **Cascade**, unique `(DefinitionId, ServiceCategoryCode)`,
index `ServiceCategoryCode`), `pricing_attribute_values`.

**Idempotent seed** (`Repository/Seed/PricingAttributeDefinitionSeeder.cs`, wired into `SeedServiceRequestAsync` at boot,
runs regardless of the mock toggle): the three marine definitions, dedupe-by-code (never clobbers admin edits):

| Definition `Code` | DataType | R4 group | Scoped categories |
|---|---|---|---|
| `ENGINE_INSTALLATION_TYPE` | Lookup | `ENGINE_INSTALLATION_TYPE` | MAINTENANCE, REPAIR, INSPECTION |
| `PAINT_TYPE` | Lookup | `PAINT_TYPE` | PAINTING, MAINTENANCE |
| `WORK_DIFFICULTY` | Lookup | `WORK_DIFFICULTY` | PAINTING, MAINTENANCE, REPAIR, INSPECTION, CLEANING, TREATMENT |

(Categories are the real seeded SR `ServiceCategoryCode`s: CLEANING/INSPECTION/MAINTENANCE/PAINTING/REPAIR/TREATMENT.)

---

## S2b — `PricingAttributeValue` on the offer line + validation

- **`PricingAttributeValueEntity`** (`Domain/Entities/Pricing/`): `OfferItemId`, `DefinitionCode`, and exactly one typed
  slot (`ValueLookupItemCode` / `ValueNumber` / `ValueText` / `ValueBool`). Unique `(OfferItemId, DefinitionCode)`.
- **Provider write/read** (`Application/Command/Pricing/OfferLineAttribute{Commands,Handlers}.cs` +
  `Controller/V1/Pricing/ProviderPricingAttributeController.cs`, `api/v1/service-requests/provider`):
  - `PUT offers/{offerId}/items/{itemId}/attributes` — full-replace the line's values (provider-owned offer only;
    rejected once the offer is Accepted/Rejected/Withdrawn/Expired).
  - `GET offers/{offerId}/items/{itemId}/attributes` — current values.
  - `GET service-requests/{srId}/applicable-pricing-attributes` — the definitions applicable to the SR's category **with
    resolved Lookup options** (tr/en), for the FE picker.
- **Validation** (`Services/Pricing/PricingAttributeValidator.cs` → pure `PricingAttributeValueValidation`): applicable
  definitions = those scoped to the SR's `ServiceCategoryCode`; each value validated fail-loud —
  type-match (exactly the right slot for the DataType), Lookup membership in the definition's R4 group, required present,
  number in `[min,max]`, plus unknown/duplicate guards. SR error codes: `SR_PRICING_ATTR_UNKNOWN_DEFINITION`,
  `_DUPLICATE_VALUE`, `_WRONG_TYPE`, `_INVALID_LOOKUP_ITEM`, `_REQUIRED_MISSING`, `_NUMBER_OUT_OF_RANGE`.

The validation core is split into a **pure** function (`PricingAttributeValueValidation.Validate(defs, lookupMembers,
values)`) fed by an infra wrapper that loads defs + resolves group members — so it is unit-tested with no DB/remote.

---

## S2c — ReferenceData remote-call extension

`IServiceRequestReferenceDataRemoteCall` (SR `.Abstraction/RemoteCall/`) gains:
```csharp
[AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}?onlyActive={onlyActive}")]
Task<AizenApiResponse<List<SrLookupItemDto>>> GetLookupItemsByGroup(string groupCode, bool onlyActive);
```
reusing the existing R4 `GetLookupItemsByGroup` query (envelope-correct `AizenApiResponse<T>`; Refit auto-registered;
base URL already wired `service-request-api → reference-data-api:8080` / `localhost:7104`). `SrLookupItemDto` carries the
language-neutral `Code` + `Name`(tr)/`Description`(en). Consumed through `Services/Pricing/ReferenceDataLookupClient.cs`:
Redis-cached (1 h, positive results only so a fresh seed is visible before TTL) and **fail-loud** (a remote failure
throws rather than silently skipping — an offer can never capture an unvalidated lookup). An unknown group resolves to an
empty item set (the ReferenceData query returns empty, never throws) → treated as "unknown group".

---

## S2d — immutable attribute snapshot at acceptance (fills the S8 reservation)

- **`OfferLineAttributeSnapshotEntity`** (Payment `Domain/Entities/Economics/`) — immutable, insert-only child of
  `OfferLineEconomicsSnapshotEntity` (FK `OfferLineEconomicsSnapshotId`, **OnDelete Restrict**). Private setters, private
  ctor, no mutators; the only construction path is the validating `Create` factory (throws
  `PaymentEconomicsInvariantException` on an empty `DefinitionCode`). Stores `DefinitionCode`, `DataType` (raw SR int,
  held opaquely), the typed value, and for Lookup **both** `ValueLookupItemCode` **and** the denormalized
  `ValueLookupItemLabel` — self-contained, **no re-lookup after acceptance** (§20.15). Table
  `offer_line_attribute_snapshots` (Payment migration `20260805192943_AddOfferLineAttributeSnapshots`).
- **Population path:** at acceptance `AcceptServiceRequestOfferCommandHandler` calls the new
  `PricingAttributeSnapshotResolver` — it loads each offer line's `PricingAttributeValue` rows, resolves the Lookup
  display label via the R4 remote call, and emits per-line
  `CalculateServiceRequestEconomicsAttributeDto`s. These ride the existing `CalculateServiceRequestEconomics` remote-call
  request (new additive `Attributes` list on the line DTO) → `ServiceRequestEconomicsLine.Attributes` →
  `LineEconomicsInput.Attributes` → built into the immutable children **inside `CreateFromLines`**, attached to each
  line snapshot. **No re-valuation after acceptance** (values were validated at S2b write-time).
- **Money math untouched:** the attribute list is carried through the pure combiner and `CreateFromLines` purely as
  descriptive metadata; it enters no sum, no funding split, and none of the 8 equalities. `CreateFromLines` stays
  synchronous/pure (label resolution happens upstream, SR-side, as the module's async reference reads already do).

---

## Tests

**Payment `Domain.UnitTests` — 272 passed / 0 failed** (all pre-existing S8/8-equality tests unchanged):
- `OfferLineAttributeSnapshotTests`: Lookup capture normalises + denormalises the label; empty `DefinitionCode` →
  `PaymentEconomicsInvariantException` (tamper/guard); `CreateFromLines` attaches the attribute child to the correct line
  (and a line with none has none); **attributes do not change the aggregate economics** (byte-identical to the
  no-attribute run; canonical smoke values 5974 / 5200 still hold).
- `PaymentEconomicsSnapshotFromLinesTests.LineChildren_Expose_No_Public_Setters_Or_Mutators` extended to include
  `OfferLineAttributeSnapshotEntity` (immutability reflection theory — no public setters, no mutator methods).

**ServiceRequest `Application.UnitTests` — 68 passed / 0 failed** (pre-existing `AcceptOfferEconomicsRequestMappingTests`
still green → the `BuildEconomicsRequest` optional-parameter change is non-breaking):
- `PricingAttributeValueValidationTests`: valid lookup (+ case-insensitive), invalid lookup non-member, required missing,
  required present, wrong-type both directions (number-for-lookup, lookup-for-number), number out-of-range / in-range,
  unknown definition, duplicate value.

**Build:** ServiceRequest host + Payment host + both Repository projects build with **0 errors** (pre-existing warnings
only).

**Migrations apply cleanly:** both migrations were applied from scratch into throwaway databases (`sr_migtest`,
`pay_migtest`, dropped afterward — the real `inktavia_store` was untouched). Verified: the 3 SR pricing tables +
category-join FK (`Cascade`); the Payment `offer_line_attribute_snapshots` table + FK →
`offer_line_economics_snapshots` with **`ON DELETE RESTRICT`** and all columns incl. `ValueLookupItemLabel`.

---

## Verify checklist (spec §Verify)

1. **Admin defines a `PAINT_TYPE` Lookup attribute scoped to a hull category; unknown group rejected** — CRUD +
   `PricingAttributeDefinitionValidator` (unknown group → empty item set → `SR_PRICING_ATTR_UNKNOWN_GROUP`). ✅ (code +
   fail-loud path; live admin exercise pending a running stack — see below.)
2. **Provider line sets PaintType = ANTIFOULING (real R4 item) accepted; non-member rejected; required-missing
   rejected** — `PricingAttributeValueValidation` unit tests cover accepted / invalid-lookup / required-missing /
   wrong-type. ✅
3. **On acceptance the snapshot is written immutably with item code + display label; tampering throws; no re-lookup** —
   `OfferLineAttributeSnapshotTests` (capture + tamper→throw + immutability theory) + the SR→Payment population path. ✅
4. **Existing economics + 8-equality unchanged** — full S8 suite passes untouched + explicit
   `Attributes_DoNotChange_TheAggregateEconomics`. ✅

**Not live-exercised:** the boot seed and the admin/provider HTTP round-trip were not run against a live stack in this
pass (verification used throwaway-DB migration apply + unit tests). The seeder is wired into `SeedServiceRequestAsync`
and is idempotent-by-code; the local `inktavia_store` is generally behind on migrations (it is also missing the
unrelated `AddStructuredRejectCancelReasons`), so applying S2 there is left to the normal service-boot auto-migrate path.
**Not committed.**

---

## Next

FE: admin definition CRUD (`FE_ADMIN_*`) + provider offer-line attribute picker (`FE_PROVIDER_*`, driven by
`GET …/applicable-pricing-attributes`). Next SR phase: **S3** (price-book + FX, consumes the R1 resolve remote call) /
**S4** (travel).
