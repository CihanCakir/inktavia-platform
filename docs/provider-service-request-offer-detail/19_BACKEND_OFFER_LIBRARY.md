# 19 — Backend: provider offer library (catalog items + templates)

Makes the offer drawer's **"Katalogdan Seç"** and **"Şablondan Başla"** real. A provider builds a private,
reusable library:
- **Catalog item** — one reusable line preset (a service/product with a default unit/price/tax). "Katalogdan Seç"
  appends selected items into the offer.
- **Template** — a named, multi-line bundle. "Şablondan Başla" loads a whole template's lines into the offer.

Everything is **provider-owned** (scoped to the calling provider) and **net-new** — there is no Commerce/product
module and no existing template structure. It lives in the **ServiceRequest module** (offer domain) and reuses the
existing offer-item shape and the server-authoritative totals path. Run in two phases: **19a catalog**, then **19b
templates**.

## The rules that must not be got wrong

1. **Ownership.** Every catalog item / template row is scoped to `ProviderProfileId`, resolved server-side from the
   BFF assertion: `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0` (same as the offer
   handlers). A provider may read/edit/delete **only their own** library. Never trust a client-sent providerProfileId.
   List/get/update/delete all filter by the resolved id; a cross-provider id is a "not found".
2. **Server still owns totals.** The library only supplies **seed values** (default price/qty/tax). Applying a
   catalog item or template does **not** introduce a new totals path — the picked lines are written through the
   **existing `SaveOfferDraft`**, and `OfferCalculationService` recomputes every total. The catalog's default price
   is a starting number the provider can edit; it is never authoritative. **No new "apply" endpoint** — the SPA
   reads the library, seeds the builder inputs, and saves the draft as it already does.
3. **Reuse the offer-item shape exactly.** Library lines mirror `ServiceRequestOfferItemEntity`:
   `ItemType, Title, Description, Quantity(decimal), UnitCode, UnitPrice(decimal), CurrencyCode, TaxRate,
   DiscountType, DiscountValue`. Money is `decimal`, never float.
4. **Validate codes.** `UnitCode` on a library line is validated against ReferenceData exactly like the offer
   path (14b `UnitCodeValidator`): null/empty allowed, a non-empty unknown code rejected `SR_OFFER_UNKNOWN_UNIT`.
5. **Migrations MUST ship a `.Designer.cs`** (the recurring lesson — a migration without it is silently not
   applied; verify the columns exist in the DB, not just in C#).

---

## Phase 19a — Catalog items

### Entity (ServiceRequest module, provider-scoped)
`ProviderCatalogItemEntity : AizenEntityWithAudit`
```
long   ProviderProfileId
ServiceRequestOfferItemType ItemType
string Title
string? Description
decimal DefaultQuantity        // e.g. 1
string? UnitCode               // ReferenceData code, validated
decimal DefaultUnitPrice
string  CurrencyCode           // default "TRY"
decimal DefaultTaxRate         // e.g. 0.20
bool    IsActive
```
EF config (table `service_request.provider_catalog_items`), index on `ProviderProfileId`. Migration **with Designer**.

### Commands / queries (all provider-scoped by the resolved profile id)
- `CreateCatalogItem`, `UpdateCatalogItem`, `DeleteCatalogItem` (soft-delete/`IsActive=false` preferred over hard
  delete), `ListCatalogItems` (active, the caller's own only).
- Validate `UnitCode` (14b). Validate `Title` non-empty, `DefaultUnitPrice >= 0`, `DefaultTaxRate` 0–1.

### BFF (provider-scoped, `[Authorize ProviderActive]`)
`GET/POST/PUT/DELETE /api/v1/provider/offer-catalog` (+ `/{id}`) → the drawer's "Katalogdan Seç" list + manage.
Refit passthrough; identity via the assertion (no id in the body).

### Seed
3–4 catalog items for **provider2** (city 35), e.g. "Gövde Basınçlı Yıkama" (Service, unit ADET, 1250, KDV 20%),
"Antifouling Boya — Jotun" (Product, unit LITER, 480, 20%), "Pasta Cila" (Service, 900, 20%). Idempotent (fixed ids).

### Acceptance — 19a
- Provider2 lists their catalog → 3–4 items; provider1 sees none of provider2's (ownership).
- Create with `unitCode:"ZZZZ"` → rejected `SR_OFFER_UNKNOWN_UNIT`; valid unit → accepted.
- Update/delete another provider's item id → "not found".
- Picking items in the SPA then saving the draft yields server-recomputed totals (no client totals trusted).

---

## Phase 19b — Templates (named multi-line bundles)

### Entities
`ProviderOfferTemplateEntity : AizenEntityWithAudit`
```
long ProviderProfileId
string Name
string? Description
bool IsActive
// children:
IReadOnlyCollection<ProviderOfferTemplateItemEntity> Items
```
`ProviderOfferTemplateItemEntity : AizenEntityWithAudit`
```
long ProviderOfferTemplateId
ServiceRequestOfferItemType ItemType
string Title
string? Description
decimal Quantity
string? UnitCode
decimal UnitPrice
string CurrencyCode
decimal TaxRate
OfferDiscountType DiscountType
decimal? DiscountValue
int SortOrder
```
Template items are **embedded snapshots** (self-contained) — a template does not break if a catalog item is later
edited/deleted. (A template MAY be built from catalog items in the UI, but it stores its own copy.) EF configs,
tables `service_request.provider_offer_templates` / `..._template_items`, FK + cascade, index on
`ProviderProfileId`. Migration **with Designer**.

### Commands / queries
- `CreateTemplate` (name + items), `UpdateTemplate` (rename + replace items), `DeleteTemplate`, `ListTemplates`
  (own, active), `GetTemplate` (own; returns items).
- Nice-to-have: `SaveDraftAsTemplate(serviceRequestId, name)` — snapshots the current offer draft's items into a new
  template. Optional; if included, it reads the draft's items server-side (do not trust client-sent totals).
- Validate each item's `UnitCode` (14b), `Name` non-empty, at least one item, prices/tax in range.

### BFF (provider-scoped)
`GET/POST/PUT/DELETE /api/v1/provider/offer-templates` (+ `/{id}`) → "Şablondan Başla" list + load + manage.

### Seed
1–2 templates for provider2, e.g. "Standart Karina Bakımı" = [Gövde Basınçlı Yıkama, Antifouling Boya, Pasta Cila].
Idempotent.

### Acceptance — 19b
- Provider2 lists templates → sees "Standart Karina Bakımı" with its items; provider1 sees none.
- Loading a template in the SPA seeds the builder with its lines; saving the draft → server-recomputed totals.
- Cross-provider template id → "not found". Template with 0 items → rejected. Unknown unit on a line → rejected.

---

## Applying to an offer (no new endpoint — important)
The SPA flow: read `offer-catalog` / `offer-templates` → the drawer picker appends/loads lines into the builder's
inputs → the **existing** `SaveOfferDraft` persists them → `OfferCalculationService` returns the authoritative
totals. The library never writes offer totals directly. Confirm no code path lets a library default price become an
offer total without passing through the calculation service.

## Constraints
- Provider-scoped everywhere; ownership enforced in the module, not just the BFF. No client-sent profile id.
- Reuse `ServiceRequestOfferItemType`, `OfferDiscountType`, decimal money, the 14b unit validation.
- Migrations with `.Designer.cs`; verify columns in the DB. Seed idempotent. No change to the totals algorithm.
- No new module. No BFF-side business logic beyond passthrough + the standard access policy.

## Frontend (I will do after this lands)
Drawer: "Katalogdan Seç" opens a panel of catalog items (multi-select → append to the builder); "Şablondan Başla"
lists templates (load → seed the builder, replacing empty draft); plus a "Taslağı şablon olarak kaydet" action.
Totals always come back from the server on draft save.

## Report
Append to `REPORT_BACKEND.md` ("19a", "19b"): list/create/reject results, the ownership rejection (cross-provider →
not found), the seed ids, confirmation that applying a template routes through SaveOfferDraft + OfferCalculationService
(server totals), and the migration columns verified in the DB. Unfinished is **not done**.
