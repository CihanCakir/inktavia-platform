# Admin QA — Live Walkthrough (Aug 7, 2026)

Logged-in pass at `http://localhost:3000` as `admin.user@inktavia.com` (OTP login, DEV code from `identity-api` log), console
attached. Confirms the static audit's headline findings and catches runtime issues. Full per-module checklists live in each
`docs/V1.0.1/Admin/<Module>/QA_FINDINGS.md`.

## Login (X0)
- BFF `POST /auth/otp-login/verify` returns `nextAction: "redirect_to_keycloak_handoff"` + a valid `loginTicket`
  (curl-confirmed → backend handoff is live).
- A UI verify attempt showed the blocked banner *"Kod doğrulandı ancak tek kullanımlık kod ile giriş henüz
  yapılandırılmadı"* and did not redirect — the admin-web FE handoff execution is not reliably completing. Login DID
  eventually succeed, so intermittent. **Confirm the FE redirect path for a fresh admin — go-live blocker.**
- Root `/` renders a bare **404** ("The page you are looking for does not exist") instead of redirecting.

## Screens walked
- **Dashboard** (`/app/dashboard`) — 🔴 confirms X2/X3. Real: Active Vessels **14**, Open Service Requests **0**. Hardcoded:
  **"Pending Payouts $124,500"** (USD), "Critical Inventory 7", all 4 charts, Recent-SR / Pending-Approvals / Recent-Messages
  lists. Sidebar TR, cards English. "Vessel Fleet Status" panel renders empty.
- **CargoDry → Products** (`/app/cargodry/products`) — all-English labels (no i18n). Seeded products show **TRY** (TRY
  250/400/300/150) → the USD issue is the create-form **default** (latent), not on current data. "SYSTEM STATUS" strip
  (QR Signing Service / Key Vault / IoT Telemetry Bridge / Serial Ledger) is a UI-only placeholder.
- **Payments → Gateway Logs** (`/app/payments/gateway-logs`) — 🔴 **100% mock** (14,282 / 142 / 28; EVT_9921…; fake
  **"Stripe Marine / Marine Pay / Global Swift"** gateways; dates 2026-06-30). TR-localized but fabricated. Marketplace uses
  iyzico — fake Stripe branding must not ship.
- **Finance → Financial Reporting** (`/app/finance/financial-reporting`) — ✅ **gold standard**: fully TR-localized, currency
  **TRY**, read-only ledger note (KDV/VAT + provider-funded discount excluded from net — matches §15/P12), real ledger
  drill-down. The reference pattern for the rest of the app.
- **Providers** (`/app/providers`) — real data (15 providers; charts TR: Onay/Durum Dağılımı). 🔴 table status badges render
  **raw Pascal English**: ONAY "Approved", DURUM "Active", ORGANIZASYON "SelfEmployed" — untranslated (X4). KONUM empty.

## Runtime
- Console error on load (dashboard): `SignalR: Failed to start the connection: The connection was stopped during
  negotiation.` (X7) — realtime hub not negotiating.

## Not walked (login intermittency + scope) — checklists to fill
Payments rule-CRUD detail pages, entitlements/commission-calc mock screens, all CargoDry sub-routes, SR detail/offers/
work-logs/dispute-review, messages moderation/reports/support realtime, notifications, identity lists, users detail tabs,
vessels documents, performance risk-watchlist/participant (broken icons), reference-data lookup/currencies, settings. Each has
a live checklist in its module `QA_FINDINGS.md`.
