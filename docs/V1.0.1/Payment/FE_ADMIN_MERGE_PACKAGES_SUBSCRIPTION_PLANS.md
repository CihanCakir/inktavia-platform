# FE_ADMIN — merge the twin "Paketler" + "Abonelik Planları" pages into one canonical page + a collapsible Paketler menu

> **Repo:** `inktavia-marine-admin-web` (FE only). `/app/packages` (`PackagesPage`) and
> `/app/payments/subscription-plans` (`SubscriptionPlansPage`) are **redundant twins**: both read the **same** real data
> (`useProviderPlansQuery` + `useParticipantPlansQuery`) and navigate to the **same** editor
> (`SUBSCRIPTION_PLAN_EDIT`). They differ only in presentation: Packages = catalog/pricing-card + KPI strip (hardcoded TR,
> read-only); Subscription-Plans = management rows (i18n, real activate/deactivate mutations, + 2 **mock** CargoDry/
> Commerce tabs). **Merge into one canonical page** at `/app/packages` and expose it through a **collapsible "Paketler"
> sidebar menu**.
>
> **Owner decisions (locked):**
> 1. **Menu:** top-level collapsible **Paketler** (Ticaret group) with sub-items **Sağlayıcı Paketleri**, **Katılımcı
>    Paketleri**, **Abonelikler**. Provider/participant deep-link via `?audience=`.
> 2. **Canonical style:** **blend of both** — keep the catalog pricing-card look **and** the KPI strip, **and** add the
>    real activate/deactivate management actions, **and** full i18n.
> 3. **Mock tabs:** keep CargoDry / Commerce as **disabled "Yakında"** tabs (no mock content rendered).
>
> **Do NOT touch** backend/BFF, provider-web, Payment module logic, the subscription-plan **editor** pages, or the
> `UserSubscriptionsPage`. Keep the Stitch theme. `npm run typecheck` + lint clean; tr+en i18n parity for all new keys.

---

## 1. Canonical page — merge into `PackagesPage` (`src/pages/app/PackagesPage.tsx`)
`PackagesPage` becomes the single source of truth. Fold in the management capability from `SubscriptionPlansPage`, keep
the catalog look + KPI strip, and localize everything.

### 1a. Audience via query param (drives the sub-menu deep-links)
- Read `?audience=provider|participant` (default `provider`) and initialize the active tab from it (use
  `useSearchParams`). When the user switches tab in-page, update the query param (`setSearchParams`, `replace: true`) so
  the URL and the sidebar active-child stay in sync.
- Keep the in-page tab switcher, but its tabs are now: **Sağlayıcı** / **Katılımcı** (real) + **CargoDry** / **Commerce**
  (disabled — see 1c).

### 1b. Merged plan card = catalog look + management actions
For the **provider** and **participant** cards, keep `PackagesPage`'s pricing-card design (plan code chip, FREE/PAID
badge, "Most Popular" for rank 0 paid, ₺/ay price, the feature-checklist `FeatureCheck` rows, taglines). **Add** the
management row from `SubscriptionPlansPage`:
- Add an **ACTIVE/INACTIVE** status badge to the card header (from `plan.isActive`).
- Replace the single "Paketi Düzenle" button with a two-button footer: **Düzenle** (→ `SUBSCRIPTION_PLAN_EDIT`) +
  **Aktifleştir/Pasifleştir** (calls the real mutations `useActivateProviderPlanMutation` /
  `useDeactivateProviderPlanMutation` for provider, `useActivate/DeactivateParticipantPlanMutation` for participant;
  disable while `isPending`; active→red "Pasifleştir", inactive→gold "Aktifleştir"). Mirror the exact mutation wiring in
  the current `SubscriptionPlansPage` cards.
- Keep the KPI strip (provider count / participant count / total / "Abonelik Yönetimi → USER_SUBSCRIPTIONS") and the info
  footer from `PackagesPage`.

### 1c. Disabled "Yakında" mock tabs
- Render **CargoDry** and **Commerce** as tabs that are **visually disabled** (reduced opacity, `cursor-not-allowed`,
  no `onClick`) with a small **"Yakında"** pill. Do **not** render `MOCK_CARGODRY_PLANS` / `MOCK_COMMERCE_PLANS` content;
  **delete those mock arrays and `GenericPlanCard`** (the only consumer) — a disabled tab shows nothing/coming-soon, so
  the mock data is dead. (This keeps the tab visible per the decision, without shipping fake plan cards.)

### 1d. Full i18n (tr + en)
The current `PackagesPage` is hardcoded Turkish. Wire `useTranslation` and move every string to keys. **Reuse** the
existing `payments.json` `subscriptionPlans` block where it fits (`tabs`, `card.*`, `empty.*`, `newPlan`, `mockNote`),
and add a **`packages` block** (tr+en parity) for the catalog-only strings: page title/subtitle, KPI labels
(`providerPackages`, `participantPackages`, `totalPackages`, `subscriptionMgmt` + their `sub` captions), catalog feature
labels (`maxOffers`/`unlimitedOffers`, `fullAnalytics`, `priorityBoost`, `enhancedVisibility`, `inkCoinMultiplier`,
`serviceDiscount`, `cargoDryDiscount`, `prioritySupport`, `exclusiveEvents`), taglines (`providerTagline`,
`participantTagline`), buttons (`edit`, `newPackage`, `subscriptions`), the `comingSoon` pill, empty-state copy, and the
info footer. Turkish primary. Numbers already use `tr-TR` — keep.

### 1e. Delete `SubscriptionPlansPage`
After its management logic + i18n are folded into `PackagesPage`, delete
`src/pages/app/payments/SubscriptionPlansPage.tsx`. Grep `SubscriptionPlansPage` repo-wide first; the only remaining
reference should be the route entry (handled in §2).

## 2. Routes (`src/app/router/routes.tsx` + `routeObjects.tsx`)
- **Keep** `PACKAGES: '/app/packages'` as the canonical list route.
- **`routeObjects.tsx`:** remove the `SubscriptionPlansPage` import; change the base list route
  `{ path: 'payments/subscription-plans', element: <SubscriptionPlansPage /> }` to a **redirect** to the canonical page:
  `{ path: 'payments/subscription-plans', element: <Navigate to={ROUTES.PACKAGES} replace /> }` (import `Navigate` from
  `react-router`) so old bookmarks/deep links still land. **Keep unchanged** the editor routes
  `payments/subscription-plans/new` and `payments/subscription-plans/:planId/edit` (they are separate entries; the merged
  page still navigates to them via the existing `ROUTES.SUBSCRIPTION_PLAN_NEW` / `SUBSCRIPTION_PLAN_EDIT` constants).
- Leave `SUBSCRIPTION_PLANS` / `SUBSCRIPTION_PLAN_NEW` / `SUBSCRIPTION_PLAN_EDIT` / `USER_SUBSCRIPTIONS` constants in
  place (still used by the redirect + editor + page).

## 3. Sidebar menu (`src/app/layouts/DashboardLayout.tsx` NAV_GROUPS, Ticaret group)
- Replace the single top-level `packages` item (`{ key: 'packages', icon: 'sell', labelKey: 'packages', path:
  ROUTES.PACKAGES }`) with a **collapsible `packages` parent** (reuse the existing `children` capability):
  ```
  {
    key: 'packages', icon: 'sell', labelKey: 'packages', path: ROUTES.PACKAGES,
    children: [
      { key: 'packages-provider',     icon: 'store',           labelKey: 'packagesChildren.providerPlans',    path: `${ROUTES.PACKAGES}?audience=provider` },
      { key: 'packages-participant',  icon: 'directions_boat', labelKey: 'packagesChildren.participantPlans', path: `${ROUTES.PACKAGES}?audience=participant` },
      { key: 'packages-subscriptions',icon: 'card_membership', labelKey: 'packagesChildren.subscriptions',    path: ROUTES.USER_SUBSCRIPTIONS },
    ],
  }
  ```
  (If the Sidebar's active-child matching keys off `pathname` only and ignores the query string, both audience children
  will highlight on `/app/packages`; that's acceptable. If easy, disambiguate the active child by the `?audience=` value —
  but do **not** over-engineer the shared Sidebar.)
- **Remove** the `payments-subscription-plans` child (line ~78) from the **Ödemeler** menu — it now lives under Paketler.
- **Move** `payments-user-subscriptions` out of Ödemeler: remove it there (line ~79) since "Abonelikler" is now a
  Paketler child pointing at `USER_SUBSCRIPTIONS`. (Avoid listing user-subscriptions in two menus.)

## 4. i18n menu labels (`navigation.json`, tr + en)
- Keep the `packages` parent label ("Paketler" / "Packages").
- Add `packagesChildren.{providerPlans, participantPlans, subscriptions}` — tr: "Sağlayıcı Paketleri", "Katılımcı
  Paketleri", "Abonelikler"; en: "Provider Plans", "Participant Plans", "Subscriptions". Full parity.
- Leave `paymentsChildren.subscriptionPlans` / `paymentsChildren.userSubscriptions` keys in the file (harmless) or remove
  them if you also removed the menu items — your call; just keep tr+en in sync.

## Don't-break / QA
- No backend/BFF/Payment-logic changes. Editor pages + `UserSubscriptionsPage` untouched. Activate/deactivate use the
  existing mutations exactly as `SubscriptionPlansPage` did.
- After deletion, grep `SubscriptionPlansPage`, `MOCK_CARGODRY_PLANS`, `MOCK_COMMERCE_PLANS`, `GenericPlanCard` → zero
  references. No dead imports.
- `/app/payments/subscription-plans` redirects to `/app/packages`; `…/new` and `…/:planId/edit` still open the editor.
- The merged page: provider/participant tabs show **real** plans with catalog cards **and** working Aktifleştir/
  Pasifleştir + Düzenle; KPI strip present; CargoDry/Commerce tabs disabled with "Yakında"; everything Turkish (i18n).
- Sidebar: Ticaret shows a collapsible **Paketler** (Sağlayıcı Paketleri / Katılımcı Paketleri / Abonelikler); the two
  audience children deep-link and select the right in-page tab; Ödemeler no longer lists subscription-plans/
  user-subscriptions. Collapsed-rail flyout + mobile drawer work for the new parent (existing Sidebar capability).
- `npm run typecheck` + lint clean; tr+en parity for every new key.

## Verification (on-screen)
Fresh admin login.
1. Ticaret → **Paketler** expands to 3 children. "Sağlayıcı Paketleri" opens `/app/packages?audience=provider` with the
   provider tab active; "Katılımcı Paketleri" → participant tab; "Abonelikler" → user-subscriptions page.
2. A provider plan card shows the catalog look **and** an Aktifleştir/Pasifleştir button that really toggles
   `isActive` (list refetches); Düzenle opens the editor.
3. CargoDry/Commerce tabs are visibly **disabled** with "Yakında"; no fake plan cards render.
4. Ödemeler menu no longer has "Abonelik Planları"/"Kullanıcı Abonelikleri"; visiting
   `/app/payments/subscription-plans` redirects to `/app/packages`; the editor deep links still work.
5. All copy Turkish; `npm run typecheck` + lint clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_MERGE_PACKAGES.md`: what the diff was, what merged into `PackagesPage` (management
actions + KPI + i18n), the deleted file + mock arrays, the redirect, the new Paketler menu (children + deep-link
behavior), i18n keys added (tr+en), and an on-screen transcript.
