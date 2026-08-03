# REPORT — FE_ADMIN merge "Paketler" + "Abonelik Planları" into one canonical page + collapsible Paketler menu

> **Repo:** `inktavia-marine-admin-web` (FE only). **Spec:** `FE_ADMIN_MERGE_PACKAGES_SUBSCRIPTION_PLANS.md`.
> **Scope kept:** no backend/BFF/Payment-logic changes; the subscription-plan **editor** pages and `UserSubscriptionsPage`
> were not touched. Provider-web not touched.

## What the diff was

```
 src/app/layouts/DashboardLayout.tsx              |  14 +-   (Ticaret menu restructure)
 src/app/router/routeObjects.tsx                  |   3 +-   (redirect + drop import)
 src/pages/app/PackagesPage.tsx                   | 239 +++   (canonical merged page)
 src/pages/app/payments/SubscriptionPlansPage.tsx | 232 ---   (DELETED)
 src/shared/i18n/locales/en/navigation.json       |   5 +    (packagesChildren)
 src/shared/i18n/locales/tr/navigation.json       |   5 +    (packagesChildren)
 src/shared/i18n/locales/en/payments.json         |  48 +    (packages block)
 src/shared/i18n/locales/tr/payments.json         |  48 +    (packages block)
 src/shared/ui/sidebar/Sidebar.tsx                |  30 +-   (query-aware active matching)
 9 files changed, 302 insertions(+), 322 deletions(-)
```

## 1. Canonical page — `src/pages/app/PackagesPage.tsx`

`PackagesPage` is now the single source of truth. It keeps its own catalog pricing-card look + KPI strip and folds in the
management capability that used to live in `SubscriptionPlansPage`:

- **Audience via query param.** `useSearchParams` reads `?audience=provider|participant` (default `provider`). A bare
  `/app/packages` is normalized to `?audience=provider` (via `setSearchParams({audience:'provider'}, {replace:true})` in a
  guarded effect) so the URL and the sidebar active-child stay in sync. Switching the in-page tab calls
  `setSearchParams({audience: next}, {replace:true})`.
- **Merged plan card = catalog look + management actions.** Provider/participant cards keep the pricing-card design
  (plan-code chip, FREE/PAID tier badge, "Most Popular" for rank-1 paid, ₺/ay price, `FeatureCheck` checklist, taglines).
  Added on top:
  - an **ACTIVE/INACTIVE** status badge in the card header, driven by `plan.isActive` (`ActiveBadge`).
  - a two-button footer (`PlanCardFooter`): **Düzenle** → `ROUTES.SUBSCRIPTION_PLAN_EDIT(id)`, and
    **Aktifleştir/Pasifleştir** wired to the real mutations
    `useActivate/DeactivateProviderPlanMutation` (provider) and `useActivate/DeactivateParticipantPlanMutation`
    (participant) — exactly as the old `SubscriptionPlansPage` did (disabled while `isPending`; active → red
    "Devre Dışı Bırak", inactive → gold "Planı Etkinleştir").
  - the KPI strip (provider count / participant count / total / "Abonelik Yönetimi → USER_SUBSCRIPTIONS") and the info
    footer were retained.
- **Disabled "Yakında" tabs.** CargoDry / Commerce (Ticaret Satıcı) tabs remain visible but are rendered as inert `<div>`s
  (`opacity-60`, `cursor-not-allowed`, `aria-disabled`, no `onClick`) with a **"Yakında"** pill. No mock content renders.
- **Full i18n.** Every string is a key. Reuses `payments.json` `subscriptionPlans.tabs.*` (tab labels) and
  `subscriptionPlans.card.activatePlan` / `.deactivate` (toggle button); everything else comes from the new `packages`
  block. Numbers keep `tr-TR` formatting.

### Deleted
- **File:** `src/pages/app/payments/SubscriptionPlansPage.tsx` (232 lines).
- **Dead mock data removed with it:** `MOCK_CARGODRY_PLANS`, `MOCK_COMMERCE_PLANS`, and their only consumer
  `GenericPlanCard`. Repo-wide grep for `SubscriptionPlansPage | MOCK_CARGODRY_PLANS | MOCK_COMMERCE_PLANS |
  GenericPlanCard` → **zero references**.

## 2. Routes — `src/app/router/routeObjects.tsx`
- Removed the `SubscriptionPlansPage` import.
- Base list route is now a redirect:
  `{ path: 'payments/subscription-plans', element: <Navigate to={ROUTES.PACKAGES} replace /> }` (`Navigate` was already
  imported from `react-router`).
- Editor routes **unchanged**: `payments/subscription-plans/new` and `payments/subscription-plans/:planId/edit` still map to
  `SubscriptionPlanEditorPage`. `SUBSCRIPTION_PLANS/_NEW/_EDIT` + `USER_SUBSCRIPTIONS` constants left in place.

## 3. Sidebar menu — `src/app/layouts/DashboardLayout.tsx` (Ticaret group)
- The single `packages` leaf is now a **collapsible `packages` parent** with children:
  - `packages-provider` → `${ROUTES.PACKAGES}?audience=provider` (icon `store`)
  - `packages-participant` → `${ROUTES.PACKAGES}?audience=participant` (icon `directions_boat`)
  - `packages-subscriptions` → `ROUTES.USER_SUBSCRIPTIONS` (icon `card_membership`)
- Removed `payments-subscription-plans` **and** `payments-user-subscriptions` from the **Ödemeler** group (no more
  duplication; Abonelikler now lives under Paketler).

### Sidebar active-state (`src/shared/ui/sidebar/Sidebar.tsx`)
The shared Sidebar matched on `pathname` only, so two children sharing `/app/packages` but differing by `?audience=` would
neither highlight nor auto-open the parent. Made `matchesPath` **query-aware** with a minimal, backwards-compatible change:
it now compares the pathname parts and, **only when the nav path pins a query** (`?audience=…`), also requires an exact
query match. Query-less nav paths (every other menu item) ignore the current query and behave exactly as before. Callers
pass `pathname + search`. Result: the parent auto-opens on `/app/packages`, and the correct audience child highlights.

## 4. i18n

`navigation.json` (tr + en) — added `packagesChildren`:
| key | tr | en |
|---|---|---|
| `providerPlans` | Sağlayıcı Paketleri | Provider Plans |
| `participantPlans` | Katılımcı Paketleri | Participant Plans |
| `subscriptions` | Abonelikler | Subscriptions |

`payments.json` (tr + en) — added the `packages` block: `title, subtitle, newPackage, edit, subscriptions, comingSoon`;
`kpi.{providerPackages,participantPackages,totalPackages,subscriptionMgmt}` each with a `…Sub` caption;
`status.{active,inactive}`; `features.{maxOffers,unlimitedOffers,fullAnalytics,priorityBoost,enhancedVisibility,
inkCoinMultiplier,serviceDiscount,cargoDryDiscount,prioritySupport,exclusiveEvents}`; `providerTagline,
participantTagline`; `empty.{provider,participant,createFirst}`; `info.{prefix,subscriptionMgmt,and,payments,suffix}`.
tr+en parity verified.

## Quality gates
- `npm run typecheck` → **clean** (`tsc --noEmit`, no output).
- `npx eslint` on the 4 touched `.tsx` files → **exit 0, no problems**. (The repo-wide `npm run lint` reports pre-existing
  errors in untouched files — `LoginPage.tsx`, test helpers, etc. — none introduced here.)
- Dead-reference grep → zero for `SubscriptionPlansPage`, `MOCK_CARGODRY_PLANS`, `MOCK_COMMERCE_PLANS`, `GenericPlanCard`.

## On-screen verification (fresh admin session, localhost:3000)

1. **Menu + deep-links.** Ticaret → **Paketler** expands to 3 children. "Sağlayıcı Paketleri" opens
   `/app/packages?audience=provider` with the **provider** tab active and that child highlighted; "Katılımcı Paketleri"
   opens `?audience=participant`, switches the in-page tab to **participant**, and highlights that child. URL and tab stay
   in sync. ✅
2. **Merged card.** Provider cards show the catalog look **and** the ACTIVE/INACTIVE badge (seeded plans render **PASİF** →
   gold "Planı Etkinleştir"); participant cards render **AKTİF** → red "Devre Dışı Bırak". "Most Popular" shows on the
   rank-1 paid plan. **Düzenle** navigates to `/app/payments/subscription-plans/1/edit` and the editor renders
   ("Planı Düzenle", live preview). ✅
3. **Activate/Pasifleştir wiring.** Clicking the toggle fires the correct real endpoint
   (`POST …/payment/provider-plans/1/activate`, `POST …/participant-plans/1/deactivate`), the button disables while pending,
   and the list query refetches (`GET …/provider-plans` 200) afterward. ⚠️ **Note:** the backend currently returns **400**
   for these seeded plans, so `isActive` did not visibly flip during the test. This is a **backend business-rule rejection**,
   not a frontend regression — the mutations are byte-for-byte the same ones `SubscriptionPlansPage` used and were not
   modified (backend/BFF were out of scope). No frontend console error; the UI degrades gracefully (button re-enables).
4. **Coming-soon tabs.** CargoDry Planları / Ticaret Satıcı Planları are visibly disabled with a **YAKINDA** pill and render
   no cards. ✅
5. **Redirect + menu cleanup.** Visiting `/app/payments/subscription-plans` redirects to `/app/packages?audience=provider`.
   The **Ödemeler** menu no longer lists "Abonelik Planları" or "Kullanıcı Abonelikleri". ✅
6. **Copy.** All on-screen strings render in Turkish. ✅

## Follow-up (out of scope, backend)
The provider/participant plan **activate/deactivate** endpoints return HTTP 400 for the current seed data. Since this is a
FE-only merge that reuses the existing mutations unchanged, the round-trip state flip could not be observed on screen. If a
successful toggle needs to be demonstrated, the Payment module's activate/deactivate rule (or the seed plan state) should be
investigated separately — no frontend change is required.
