# BE_S2 — pricing attribute definitions + values (motor/tekne/boya variables) + immutable snapshot

> **Repo:** `addesso-project` — **ServiceRequest module** (+ extend the existing ReferenceData remote call). SR
> second-wave phase S2 (§20.6). Captures the **pricing-relevant variables** (engine install type, engine type/class,
> paint type, work difficulty…) that inform an offer, validated against the **R4 marine lookups** (just seeded), scoped
> by service category, and **snapshotted immutably at acceptance** (fills the slot S8 reserved). Descriptive metadata —
> it doesn't change the line math in S2; it records the variables + becomes part of the immutable offer snapshot (§20.15).
> Backend first; FE (admin definition CRUD + provider offer-line attribute picker) follow as `FE_ADMIN_*` / `FE_PROVIDER_*`.

## Current state (investigated)
- **No attribute entity yet** (S8 only "reserved" the concept). Build the model.
- **`IServiceRequestReferenceDataRemoteCall` exists** (we added FX resolve in R1) → extend it to validate/resolve R4
  marine lookups. Service category = `ServiceRequestEntity.ServiceCategoryCode`; `ProviderOfferTemplate(Item)` exists for
  template scoping.

## S2a — `PricingAttributeDefinition` (admin-owned, category-scoped)
- Fields: `Code` (stable, unique), `Name` (tr/en), `DataType` { **Lookup**, Number, Text, Boolean }, `LookupGroupCode`
  (required when DataType=Lookup — the R4 group, e.g. `ENGINE_INSTALLATION_TYPE`, `PAINT_TYPE`, `WORK_DIFFICULTY`),
  `IsRequired`, `SortOrder`, `IsActive`; **applicability scope** = the service category code(s) it applies to (a
  `PricingAttributeDefinitionCategory` join, or a category list) — optionally template-level too.
- Admin CRUD + validation: unique code; when Lookup, the `LookupGroupCode` must resolve to an existing R4 group (via the
  remote call — fail-loud if unknown); number attributes may carry min/max.
- Migration + a small seed of the marine ones (EngineInstallationType, PaintType, WorkDifficulty) mapped to the R4 groups,
  scoped to the relevant categories. Idempotent (dedupe by code).

## S2b — `PricingAttributeValue` on the offer line (capture)
- Per **offer line** (line-level, consistent with S1/S8): `PricingAttributeValueEntity(offerItemId, definitionCode,
  valueLookupItemCode? / valueNumber? / valueText? / valueBool?)`. When the provider builds an offer for an SR of
  category X, the **applicable definitions** are those scoped to X; the provider sets values.
- **Validation:** each value validated against its definition — DataType match; for Lookup, the chosen item must belong
  to the definition's `LookupGroupCode` (validate via the remote call `GetLookupItemsByGroup(groupCode)`); required
  attributes present; number within min/max. Fail-loud on invalid (SR error code range).

## S2c — extend the ReferenceData remote call
- Add `GetLookupItemsByGroupAsync(groupCode)` (and/or a `ValidateLookupItem(groupCode, itemCode)`) to
  `IServiceRequestReferenceDataRemoteCall` (internal, typed, envelope-correct) to drive S2b validation + FE pickers.
  Reuse the R4 lookup query.

## S2d — immutable attribute snapshot at acceptance (fills the S8 reservation)
- Add `OfferLineAttributeSnapshotEntity` (immutable child of the line economics snapshot, FK → snapshot Restrict,
  validating factory, no mutators) capturing, at acceptance, each line's attribute definition code + resolved value +
  (for Lookup) the item code **and its display label** (denormalized so the snapshot is self-contained — no re-lookup
  after acceptance, §20.15). Populate it in the **P8 acceptance path** (`CreateFromLines`) alongside the other line
  snapshots. No re-valuation after acceptance.

## Don't-break / QA
- Additive: new definition/value/snapshot entities + remote-call method + admin CRUD + the acceptance-snapshot write.
  Existing offer/line economics (S1/S6/S7/S8), P8 acceptance, and the 8-equality invariants **unchanged** — attributes
  are descriptive, they don't enter the money math (the snapshot is metadata alongside the economics snapshot). Migration
  applies cleanly; seed idempotent; duplicate-seed protection; cacheable ReferenceData reads per convention. Builds clean;
  unit tests for validation (valid/invalid lookup, required-missing, wrong-type) + the immutable snapshot (tamper →
  throw).

## Verify
1. Admin defines a `PAINT_TYPE` Lookup attribute scoped to a hull category → CRUD works; an unknown `LookupGroupCode` is
   rejected.
2. A provider offer line for that category sets PaintType = antifouling (a real R4 item) → accepted; a value not in the
   group → rejected; a required attribute missing → rejected.
3. On acceptance, the line's attribute snapshot is written immutably with the item code + display label; tampering throws;
   no re-lookup needed to read it.
4. Existing offer economics + 8-equality unchanged (attributes don't move the math).

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S2.md`: the definition/value/snapshot model, the category scoping, the remote-call
lookup validation, the acceptance snapshot (S8 slot filled), the seeded marine attribute definitions, and the test
results. Then FE (admin definition CRUD + provider offer-line attribute picker) + next SR phase (S3 price-book+FX / S4
travel).
