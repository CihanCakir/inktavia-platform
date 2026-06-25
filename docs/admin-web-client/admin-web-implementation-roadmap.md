# Admin Web Client — Implementation Roadmap

## Phase 0: Project Setup

- [ ] Initialize Vite + React + TypeScript project
- [ ] Install dependencies: `axios`, `@tanstack/react-query`, `keycloak-js`, `@react-keycloak/web`, `zustand`, `react-router-dom`, `react-hook-form`, `zod`
- [ ] Configure environment variables (`VITE_BFF_BASE_URL`, `VITE_KEYCLOAK_*`)
- [ ] Set up `tsconfig.json` with strict mode and path aliases (`@/api`, `@/types`, `@/hooks`)
- [ ] Set up Prettier + ESLint

---

## Phase 1: Auth Foundation

- [ ] Implement `authStore.ts` (Zustand): store Keycloak token + Identity tokens
- [ ] Implement `keycloak.ts`: initialize `keycloak-js` with PKCE, configure `onTokenExpired`
- [ ] Implement `bffClient.ts`: Axios instance with request interceptor (attach both headers) and 401 retry interceptor
- [ ] Implement `authApi.ts`: login endpoints, refresh, change password
- [ ] Implement `LoginPage.tsx`: Identity login form (username or phone+OTP)
- [ ] Implement `ProtectedRoute.tsx`: redirect to login if either token missing
- [ ] Implement `AppRouter.tsx`: public `/login` route, all other routes wrapped in `ProtectedRoute`
- [ ] Test: login → both tokens set → protected route accessible → 401 triggers refresh → re-login on failure

---

## Phase 2: Dashboard

- [ ] Implement `dashboardApi.ts` + `getDashboardOverview` query function
- [ ] Implement `useDashboardOverview` hook
- [ ] Implement `DashboardPage.tsx`: show KPI cards (vessels, service requests, disputes, pending approvals)
- [ ] Handle `warnings[]` display (toast or inline warning banner)

---

## Phase 3: Identity Management

- [ ] Implement `identityApi.ts` (all profile endpoints)
- [ ] Implement `useIdentity.ts` hooks
- [ ] Implement `IdentityPage.tsx`: tabbed view (Organizers / Venues / Participants)
- [ ] Implement paginated table for each profile type
- [ ] Implement profile detail drawer/modal
- [ ] Implement approve/reject actions with confirmation dialog (reason field for reject)
- [ ] Implement `ProfileWithRolesPage.tsx` or modal

---

## Phase 4: Vessels

- [ ] Implement `vesselsApi.ts` (all vessel endpoints)
- [ ] Implement `useVessels.ts` hooks
- [ ] Implement `VesselsPage.tsx`: searchable + filterable vessel list
- [ ] Implement `VesselDetailPage.tsx`: vessel info + documents + owners tabs
- [ ] Implement vessel edit form (using `getFormOptions` for type/status dropdowns)
- [ ] Implement archive / restore / status-change actions
- [ ] Implement vessel document removal with confirmation

---

## Phase 5: Service Requests

- [ ] Implement `serviceRequestsApi.ts` (all service request endpoints)
- [ ] Implement `useServiceRequests.ts` hooks
- [ ] Implement `ServiceRequestsPage.tsx`: paged, filterable list with status + vessel filters (use `getFilterOptions`)
- [ ] Implement `ServiceRequestDetailPage.tsx`: operation detail + timeline view
- [ ] Implement cancel / approve completion / reject completion actions
- [ ] Implement `DisputesPage.tsx`: paged dispute list
- [ ] Implement dispute status change + resolve actions

---

## Phase 6: File Management

- [ ] Implement `filesApi.ts` (all file endpoints)
- [ ] Implement `useFiles.ts` hooks
- [ ] Implement file review overlay (accessible from Vessel detail or Service Request detail)
- [ ] Implement read URL generation (single + bulk) with expiry display
- [ ] Implement file delete with confirmation
- [ ] Implement visibility toggle (Public / Private)

---

## Phase 7: Reference Data

- [ ] Implement `referenceDataApi.ts`
- [ ] Implement `useReferenceData.ts` hooks
- [ ] Populate dropdowns in vessel edit form (countries, cities, measurement units, lookup items)
- [ ] (Optional) Implement read-only Reference Data admin page for system parameters inspection

---

## Phase 8: Cross-Cutting Concerns

- [ ] Global error boundary (catch unhandled React errors)
- [ ] Global 401/403 handler: unified redirect behaviour
- [ ] `AdminBffWarning[]` component: displayed as collapsible alert when downstream module calls partially fail
- [ ] Pagination component (shared, reusable)
- [ ] Confirmation dialog component (shared)
- [ ] Loading skeleton components
- [ ] Toast notification system

---

## Phase 9: Polish & Hardening

- [ ] Token refresh scheduling (Keycloak `updateToken` + Identity proactive refresh)
- [ ] Retry strategy on transient errors (429, 503)
- [ ] Session expiry: clear both tokens, redirect to login with "session expired" message
- [ ] Accessibility audit (keyboard navigation, ARIA labels on modals/tables)
- [ ] Environment-specific config validation on startup

---

## Phase 10: Testing

- [ ] Unit tests for `authStore`, `bffClient` interceptors, `unwrap` helper
- [ ] Integration tests for each API module (mock server / MSW)
- [ ] E2E tests for login flow, dashboard load, approve/reject profile, vessel archive

---

## Dependency Map

```
Phase 0 (Setup)
  └── Phase 1 (Auth)
        ├── Phase 2 (Dashboard)
        ├── Phase 3 (Identity)
        ├── Phase 4 (Vessels)
        │     └── Phase 6 (Files) — file review used in vessel detail
        ├── Phase 5 (Service Requests)
        │     └── Phase 6 (Files) — file review used in SR detail
        ├── Phase 7 (Reference Data) — feeds vessel form options
        └── Phase 8 (Cross-Cutting)
              └── Phase 9 (Polish)
                    └── Phase 10 (Testing)
```
