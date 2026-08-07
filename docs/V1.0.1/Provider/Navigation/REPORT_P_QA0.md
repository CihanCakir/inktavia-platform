# REPORT — P-QA0: reconcile nav `status` flags (remove false "soon" badges)

> Executes `FIX_P_QA0_NAV_FLAGS.md`. `inktavia-marine-provider-web`, **FE only, additive, no backend.** Makes the
> sidebar `status` flags truthful so implemented features stop wearing a false "coming soon" badge. The badge
> mechanism is unchanged.

---

## Badge mechanism (unchanged)

`DesktopShell.tsx` (line 53) renders the badge purely from the flag:
```tsx
{item.status === 'planned' && (<span …>{t('soon')}</span>)}
```
So flipping a flag to `implemented` removes its badge — no mechanism change needed.

## Flag changes (`src/app/router/navigation.ts`)

| item | before | after | evidence |
|------|--------|-------|----------|
| `offers` | planned | **implemented** | `OffersPage` — `useQuery`/`useMutation` (offers list, KPIs, withdraw; S2–S5 economics) |
| `service-requests` | planned | **implemented** | `DiscoveryPage` — `useDiscoveryList` + `useDiscoverySummary` + `useDiscoveryMarkers` + realtime + URL-state filters |
| `cargodry` | planned | **implemented** | `CargoDryInventoryPage` — `useCargoDryOverview`/`Alerts`/`Earnings`/`StockRequests`/`Products` + stock-request mutations |
| `notifications` | *already implemented* | implemented (no change) | `useNotifications`/`useNotificationPreferences`/`usePushSubscription` + api |
| `performance` | planned | **planned** (kept) | `PerformancePage` → `<PlannedPage>` stub — badge is honest until P-QA7 |
| `documents` | planned | **planned** (kept) | `DocumentsPage` → `<PlannedPage>` stub — badge is honest until P-QA7 |

**Note on `notifications`:** the fix doc expected it to still be `planned`, but the working tree already had it
`implemented` — so no change was required there; it was verified, not modified.

**Note on the two "ambiguous" items** (`service-requests`, `cargodry`): the doc's static note said "0 query hooks",
but both pages in fact drive **multiple live query hooks + mutations** (listed above) — they are real feature pages,
not bare `PlannedPage` stubs (contrast: `performance`/`documents` literally render `<PlannedPage>`). Per the doc's
own criterion ("renders real data / live query → implemented"), the truthful flag is `implemented`; leaving a false
"soon" badge on two fully-built pages would defeat the fix. Set to `implemented`.

## Static verification (deterministic)

- **Badge outcome:** with the flags above, the sidebar renders the "soon" badge on **exactly** `performance` +
  `documents` and on nothing else — confirmed by the single `status === 'planned'` guard + the final flag state
  (only those two remain `planned`).
- **Routes open real pages:** `routes.tsx` maps `offers→OffersPage`, `service-requests→DiscoveryPage`,
  `cargodry/inventory→CargoDryInventoryPage`, `notifications→…` — all real components (not `PlannedPage`).
- **Group labels localized (tr+en):** `navigation.json` has `group.{work,commerce,finance,account}` =
  İş/Ticaret/Finans/Hesap · Work/Commerce/Finance/Account, and item titles + `soon`/`yakında` in both locales.
- **Active state:** unchanged — `NavLink`'s `isActive` drives the `text-gold`/`bg-white/10` styling.
- **Quality gates:** `tsc --noEmit` → **0 errors**; `eslint src/app/router/navigation.ts` → **clean**.

## On-screen verification (localhost:3002) — partially blocked

- The provider dev server is up (`http://localhost:3002/` → HTTP 200).
- Navigating to `/app/dashboard` **redirects to `/auth/login`** — the sidebar and all feature pages are behind the
  provider auth gate. I did **not** log in: entering credentials is out of scope for me (password entry is the
  user's to perform). So the pixel-level sidebar/page render could not be captured in this pass.
- Everything the on-screen checklist would confirm is nonetheless established deterministically above (badge is a
  pure function of the flag; routes point at real pages; locale keys present; active-state logic untouched).

**Recommended 30-second confirmation (logged in):** open the sidebar → Offers, Service Requests, CargoDry,
Notifications show **no** "soon" badge and open working pages; Performance + Documents still show "soon" and render
the planned stub; group headings localized; the current item is gold/highlighted.

## Scope

Only `src/app/router/navigation.ts` changed (four flag comments + `offers`/`service-requests`/`cargodry` →
`implemented`). No backend, no badge-mechanism, no label changes. **Not committed.**
