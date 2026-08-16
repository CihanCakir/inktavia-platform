# CANON-e — Frontend spec: canonicalize onboarding service categories

**For:** the `inktavia-marine-provider-web` team.
**Status:** SPEC ONLY — the backend (CANON-b/c/d) is done; the frontend change below is **not yet applied**.
**File to change:** `src/features/onboarding/model/onboardingFieldConfigs.ts` → `SERVICE_CATEGORIES`.

## Why
The provider-eligibility read-model (`provider_service_categories.ServiceCategoryCode`) used a **separate onboarding
vocabulary** (`engine-mechanical`, `hull-paint`, …) that did not match the ReferenceData `SERVICE_PROVIDER_CATEGORY`
catalogue the rest of the platform uses (ServiceRequest categories, the public service catalogue). That mismatch
silently broke category matching (region fan-out, service×location availability). The backend now stores the
**canonical** form; the onboarding UI must send canonical ids so new submissions stay aligned.

## Canonical stored form (the contract)
Eligibility stores **`lower(SERVICE_PROVIDER_CATEGORY.Code)`** — underscores preserved, e.g. `motor_maintenance`.
The onboarding UI should send the **`SERVICE_PROVIDER_CATEGORY.Code`** (e.g. `MOTOR_MAINTENANCE`); the backend
lower-cases it. Do **not** send the old hyphenated ids.

## The change

### Option A (preferred) — drive the list from the lookup group
The file already flags a "PLANNED BFF lookup" pattern (used for countries). Fetch the categories from the
`SERVICE_PROVIDER_CATEGORY` lookup group via the MarineProvider BFF (mirror the existing lookup fetch — same one the
owner mobile app uses for service requests), and submit each item's **`code`** as the id. Keep i18n labels keyed by
code.

### Option B (interim) — hardcode the canonical ids
Replace the `SERVICE_CATEGORIES` ids with the canonical codes, keeping the existing i18n label keys:

```ts
export const SERVICE_CATEGORIES = [
  { id: 'MOTOR_MAINTENANCE',  labelKey: 'onboarding:capabilities.category.engine' },
  { id: 'HULL_MAINTENANCE',   labelKey: 'onboarding:capabilities.category.hull' },
  { id: 'ELECTRICAL_SERVICE', labelKey: 'onboarding:capabilities.category.electrical' },
  { id: 'RIGGING_SAILS',      labelKey: 'onboarding:capabilities.category.rigging' },
  { id: 'BOAT_CLEANING',      labelKey: 'onboarding:capabilities.category.cleaning' },
  { id: 'UPHOLSTERY',         labelKey: 'onboarding:capabilities.category.upholstery' },
  { id: 'CONCIERGE_SUPPORT',  labelKey: 'onboarding:capabilities.category.concierge' },
] as const
```

## Old → new lock-step table (all 8 onboarding ids)

| Old onboarding id | New canonical id (`SERVICE_PROVIDER_CATEGORY.Code`) | Note |
|---|---|---|
| `engine-mechanical` | `MOTOR_MAINTENANCE` | 1:1 |
| `hull-paint` | `HULL_MAINTENANCE` | 1:1 (new catalogue item) |
| `electrical` | `ELECTRICAL_SERVICE` | **merged** with `electronics` |
| `electronics` | `ELECTRICAL_SERVICE` | **merged** with `electrical` |
| `rigging-sails` | `RIGGING_SAILS` | 1:1 (new catalogue item) |
| `cleaning-care` | `BOAT_CLEANING` | 1:1 |
| `upholstery` | `UPHOLSTERY` | 1:1 (new catalogue item) |
| `concierge-support` | `CONCIERGE_SUPPORT` | 1:1 (new catalogue item) |

### ⚠️ The `electrical` + `electronics` merge
Both old options now map to the single `ELECTRICAL_SERVICE`. The onboarding UI must therefore **drop one of the two
options** (present a single "Electrical" category) or relabel — showing two options that submit the identical id is
confusing and produces duplicate selections. Recommended: keep one "Electrical" option
(label `capabilities.category.electrical`) and remove the `electronics` option.

### i18n labels (unchanged; TR already present)
The canonical catalogue stores a single English name; localized labels stay in the frontend i18n, which already has
both languages:

| labelKey | EN | TR |
|---|---|---|
| `capabilities.category.engine` | Engine & Mechanical | Motor ve Mekanik |
| `capabilities.category.hull` | Hull & Paint | Tekne ve Boya |
| `capabilities.category.electrical` | Electrical | Elektrik |
| `capabilities.category.rigging` | Rigging & Sails | Arma ve Yelken |
| `capabilities.category.cleaning` | Cleaning & Care | Temizlik ve Bakım |
| `capabilities.category.upholstery` | Upholstery | Döşeme |
| `capabilities.category.concierge` | Concierge Support | Concierge Destek |

## Deploy ordering (safe)
1. **Backend first (already done):** CANON-b adds the 4 new catalogue items; CANON-c makes the write path emit
   canonical codes; CANON-d backfills existing rows. The write path accepts **both** the old ids and the canonical
   codes during the transition (`ProviderServiceCategoryCanonicalMap.ToCanonical` remaps legacy, passes canonical
   through), so deploying the backend before the frontend is safe.
2. **Frontend second:** switch `SERVICE_CATEGORIES` to canonical ids (Option A or B) and drop the duplicate
   electronics option. New onboarding submissions are only *trusted to be canonical* once this ships — but nothing
   breaks in the interim because the backend remaps the legacy ids.
