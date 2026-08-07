# FIX P-QA0 — reconcile nav `status` flags (remove false "soon" badges)

> **Repo:** `inktavia-marine-provider-web` — FE only, additive, no backend. Fixes the misleading sidebar state where
> **implemented** features wear a "coming soon" badge. Small, high-visibility. tr+en unaffected (labels already localized).
> Report per the QA cadence.

## Findings (static)
`src/app/router/navigation.ts` tags each item `status: 'implemented' | 'planned'`; `src/app/shell/desktop/DesktopShell.tsx`
(~line 53) renders a **"soon" badge** on `planned` items. Stale flags:
- `offers` → `OffersPage` has 4 query hooks (S2–S5 economics). **Should be `implemented`.**
- `notifications` → full `useNotifications`/`useNotificationPreferences`/`usePushSubscription` + api. **Should be `implemented`.**
Genuinely `planned` (correct, they render the `PlannedPage` stub): `performance`, `documents`.
Ambiguous — verify live: `service-requests` (DiscoveryPage, 0 query hooks), `cargodry` (products page 0 query hooks).

## Change
1. In `navigation.ts`, set `offers` → `implemented` and `notifications` → `implemented`.
2. **Verify `service-requests` and `cargodry` on screen** (localhost:3002): if the page renders real data → `implemented`;
   if it's a bare EmptyState/no live query → leave `planned` (honest) and note it for its module phase.
3. Leave `performance` + `documents` as `planned` for now — they ARE stubs; the badge is honest until P-QA7 builds or hides
   them. (Decision to hide-vs-build is P-QA7, not here.)
4. Do not change the badge mechanism itself — just make the flags truthful.

## Verify (on screen)
- [ ] Sidebar shows **no** "soon" badge on Offers or Notifications; both open working pages.
- [ ] `performance`/`documents` still badged (until P-QA7).
- [ ] service-requests/cargodry flags match what their pages actually render.
- [ ] Group labels localized (tr+en), active state correct.

## Report
`docs/V1.0.1/Provider/Navigation/REPORT_P_QA0.md`: the flag changes + the live-verified state of each item.
