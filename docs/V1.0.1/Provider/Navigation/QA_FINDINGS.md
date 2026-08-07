# Provider QA — Navigation (P-QA0, cross-cutting)

## Static findings
- `src/app/router/navigation.ts` tags each item `status: 'implemented' | 'planned'`; `src/app/shell/desktop/DesktopShell.tsx`
  (~line 53) renders a **"soon" badge** on `planned` items.
- **Stale flags (implemented but marked `planned` → false "soon" badge):**
  - `offers` → `OffersPage` has 4 query hooks (built with S2–S5 economics). **Implemented.**
  - `notifications` → full `useNotifications` / `useNotificationPreferences` / `usePushSubscription` + api. **Implemented.**
- **Genuinely `planned` (correct):** `performance`, `documents` (both render the `PlannedPage` "Coming Soon" stub).
- **Ambiguous — verify:** `service-requests` (DiscoveryPage, 0 query hooks), `cargodry` (products page 0 query hooks).

## Fix
1. Set `offers` and `notifications` → `implemented` (remove the false badge).
2. For truly-planned items (`performance`, `documents`): either build them (P-QA7) or **remove them from the sidebar until
   built** — don't ship a nav entry that dead-ends on "Coming Soon".
3. Confirm every remaining item's `status` matches its live state (use the master table).

## Live walkthrough checklist (localhost:3002)
- [ ] Every sidebar item leads to a working screen (no dead "soon" on a working page; no nav entry → PlannedPage).
- [ ] Group headings (work/commerce/finance/account) render localized labels (tr+en), not raw keys.
- [ ] Active/selected nav state highlights correctly on each route + nested routes (`jobs/:id`, `messages/:threadId`).
- [ ] Mobile shell tabs (`mobileTab`) show the right set.
