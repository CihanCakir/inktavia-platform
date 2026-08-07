# Admin QA — Identity / Users / Providers / Approvals (A-QA5)

Routes: `/app/users` (+`:id`,`/new`,`/:id/edit`), `/app/users/approvals` (+ organizer/venue review), `/app/identity/
{organizers,venues,participants}`, `/app/providers` (+`:profileId`).

## Static + live findings
**Providers** `ProvidersPage` / `ProviderDetailPage` — REAL (`useProvidersListQuery`, AdminPanel BFF; live: 15 providers,
charts TR). 🔴 **X4 raw-enum badges (live-confirmed):** table ONAY shows raw Pascal **"Approved"**, DURUM **"Active"**,
ORGANIZASYON **"SelfEmployed"** — untranslated. `StatusBadge` uses lowercase-keyed `APPROVAL_VARIANT[row.approvalStatus]`
with Pascal input → variant miss + raw label (`ProvidersPage.tsx:711-722`, `ProviderDetailPage.tsx:230-237`). KPIs/charts
normalize via `.toLowerCase()` so they're fine; only the badges leak. Finance panel correctly ₺.

**Identity lists** `OrganizersListPage` / `VenuesListPage` / `ParticipantsListPage` — REAL data but:
- **No i18n at all** (fully hardcoded English; no `identity`/`organizers`/`venues` namespace exists).
- Render `label={row.status}` **raw** (lowercase enum), untranslated. `OrganizersListPage` uses a stray `text-marine-navy-900`
  token.
- **Orphaned** — not in the sidebar (URL-only).

**Users** `UsersListPage` / `UserFullDetailPage` — REAL (`shared/api/hooks/useUsers.ts`). Known **Admin Users BFF TODOs**
surface at the UI:
- `row.email` (list L115) + `row.vesselCount` (L149) render blank/`0` (BFF fields still TODO).
- **Performance tab dead** (`UserFullDetailPage.tsx:498`): guard `userRole !== 'provider'` but `UserRole` has no `'provider'`
  → always the "not a provider" placeholder; `profileId={Number(id)}` (L200) is `NaN` for GUID ids.
- **Transactions tab always empty** (`useUserTransactions` `.catch(()=>[])` until BFF); Transactions `amount` printed raw (no
  ₺). `useUserServiceRequests` dead (SR tab uses `useServiceRequestsByUserQuery`).

**Approvals** `ApprovalsQueuePage` — REAL queries but **fabricates data**: `toRiskLevel()` invents risk from `profileId % 10`
when BFF null (L34-41); `submittedAt ?? new Date()` fakes dates (L55,72). Review detail pages are real
(`profileApprovalsApi`). ⚠ `approvalsMockInterceptor` is installed in `main.tsx` (DEV-only, gated by
`localStorage 'inktavia_mock_approvals'`) — ensure unset in test/staging.

## Live walkthrough checklist
- [ ] Providers: ONAY/DURUM/ORGANIZASYON render **localized labels**, not raw Pascal.
- [ ] Users list: email + vessel count populated (needs BFF); KPI cards real.
- [ ] User detail: Performance tab reachable for providers; Transactions tab shows real data or is hidden; amounts in ₺.
- [ ] Approvals: risk level + submitted date are **real** (not `id%10` / now()); mock interceptor off.
- [ ] Identity lists localized (tr/en) + reachable from nav; status labels mapped.

## Fix candidates
`FIX_A_QA5_PROVIDER_ENUM_BADGES`, `FIX_A_QA5_IDENTITY_NAV_I18N`, `FIX_A_QA5_USERS_BFF_FIELDS` (+ perf-tab guard), stop
fabricating approvals data. (email/vesselCount gated on Admin Users BFF work.)
