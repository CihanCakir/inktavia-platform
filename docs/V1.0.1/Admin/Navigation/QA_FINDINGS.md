# Admin QA — Navigation & Login (A-QA0)

## Static findings
- **X0 — OTP→Keycloak login handoff (🔴 blocker).** BFF `POST /api/v1/admin-panel/auth/otp-login/verify` returns
  `nextAction: "redirect_to_keycloak_handoff"` + a valid `loginTicket` (curl-confirmed, backend live). A live UI attempt
  showed the blocked banner *"Kod doğrulandı ancak tek kullanımlık kod ile giriş henüz yapılandırılmadı"* (the FE
  `keycloak_handoff_required` copy) and did not redirect. FE handling of the verify result lives around
  `src/features/auth/api/otpLoginApi.ts` + `src/pages/public/LoginPage.tsx` — the `redirect_to_keycloak_handoff` branch
  (build the Keycloak authorization URL from `loginTicket` → redirect → `/auth/callback`) needs verification. Intermittent:
  login DID eventually succeed for this audit.
- **X1 — two nav sources.** `src/app/router/navigation.ts` (`navigationItems`, has `requiredPermission`) is imported nowhere
  → **dead code**. The real sidebar is `src/app/layouts/DashboardLayout.tsx` `NAV_GROUPS` (Turkish labels), rendered by
  `src/shared/ui/sidebar/Sidebar.tsx` with **no permission filtering** → every item shows for every authed user. Routes use
  `ProtectedRoute` only; `PermissionRoute.tsx` exists but is unused.
- **Orphaned routes** (page exists, no sidebar entry, URL-only): `/app/file-storage`, `/app/identity/{organizers,venues,
  participants}`, `/app/commerce/orders`, `/app/commissions`, `/app/reference-data/currencies`. `/app/notifications/inbox`
  reached only via the Topbar bell.
- No item is badged "coming soon" → no false "soon" badges (unlike provider). Mismatch is structural, not badge-level.

## Live walkthrough checklist
- [ ] Fresh admin (clear session) → `/login` → email → OTP (DEV code in `identity-api` log) → **completes to `/app/dashboard`**
      without the "handoff not configured" banner. Capture the `verify` response `nextAction` + whether the Keycloak redirect
      fires.
- [ ] Every sidebar item routes to a real screen; no 404. Root `/` currently renders a bare **404** (should redirect to
      dashboard/login) — confirm + fix.
- [ ] Log in as a non-admin role (if available) → confirm restricted items are hidden (currently they won't be).
- [ ] Deep-link each orphaned route → decide: add to nav or hide.

## Fix candidates
`FIX_A_QA0_*`: (1) FE OTP handoff redirect; (2) collapse to one nav source + wire `requiredPermission` filtering in Sidebar
+ `PermissionRoute` on routes; (3) root `/` redirect; (4) surface-or-hide orphaned routes.
