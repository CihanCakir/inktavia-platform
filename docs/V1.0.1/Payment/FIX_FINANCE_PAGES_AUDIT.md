# FIX — admin finance pages audit: broken icons + missing i18n (mock/UI/API review)

> **Audit result of `/app/finance` + sub-pages (admin-web).** On-screen + code review. **No mock data and no API-access
> defect** — every page calls the real `AdminFinanceController` BFF endpoints (all wired, return 200; empty tables are
> legitimate empty states, not errors). The real problems are on the **5 older finance pages (Jul-06)**: **(1) broken
> icons** — they pass lucide-react-style PascalCase names to the shared Material-Symbols `<Icon>`, so the names render as
> literal text; **(2) no i18n** — hardcoded English + `en-US` number formatting in an otherwise Turkish app. The newer
> `FinancialReportingDashboardPage` (P12) is already localized + correct — leave it.
>
> **Repo:** `inktavia-marine-admin-web` (FE only). Do NOT touch backend/BFF, provider-web, CargoDry logic, or the P12
> dashboard. Fix the 5 pages; verify on-screen.

## Findings (confirmed)
- **Shared `<Icon>`** (`src/shared/ui/icon/Icon.tsx`) renders `<span class="material-symbols-outlined">{name}</span>` —
  the `name` MUST be a **Material-symbol ligature** (lowercase snake_case). Passing PascalCase lucide names → the font
  can't resolve the ligature → the literal name shows (e.g. "CHEVRONLEFT", "BARCHART3", "ALERTTRIANGLE", "FILETEXT",
  "CW").
- **Affected pages + their wrong icon names (`useTranslation` count = 0 on all five → zero i18n):**
  - `CargoDrySettlementReconciliationPage.tsx` — `AlertCircle`, `ChevronLeft`, `Download`, `Loader2`
  - `CargoDryRenewalReconciliationPage.tsx` — `AlertCircle`, `ChevronLeft`, `Download`, `Loader2`
  - `CommissionRuleUsageReportPage.tsx` — `AlertCircle`, `ChevronLeft`, `Download`, `Loader2`
  - `FinanceInvoiceReportPage.tsx` — `AlertCircle`, `ChevronLeft`, `Download`, `Loader2`
  - `FinanceReconciliationOverviewPage.tsx` (the `/app/finance` landing) — `AlertTriangle`, `ChevronRight` (+ the tile
    icons `BarChart3`/`FileText`/etc.)
- **i18n:** all five use hardcoded English strings (titles, subtitles, KPI labels, filter tabs, "Export CSV", empty-state
  text, "N mismatch/mismatches") and `toLocaleString('en-US', …)` number/currency formatting. The app is Turkish.
- **API/data:** every page hits its real endpoint (200); tables are empty only because no CargoDry settlement/renewal/
  invoice data is seeded — a legitimate empty state, not a bug. (Optional: seed some to see populated tables, like the
  Wave-B seed — not required.)

## Change 1 — fix icon names (Material-symbol) on the 5 pages
Replace every PascalCase/lucide name with the correct Material-symbol ligature. Use these mappings (verify each against
the current icon set; pick the closest existing symbol):
`AlertCircle → error` · `AlertTriangle → warning` · `ChevronLeft → chevron_left` · `ChevronRight → chevron_right` ·
`Download → download` · `Loader2 → progress_activity` (spinner; add the existing spin animation class if one is used
elsewhere) · `BarChart3 → bar_chart` · `FileText → description` · (any others → the matching Material symbol). Grep each
page for `name="[A-Z]…"` and fix all. After this, no page should render a raw icon name.

## Change 2 — localize the 5 pages (i18n tr + en)
Wire `useTranslation` and move all hardcoded strings into i18n keys (mirror how `FinancialReportingDashboardPage` /
`financialReport.json` is done). Add a finance-reports namespace (or extend an existing one) with **tr + en full
parity**: page titles/subtitles, the read-only banner, KPI labels (Total/With Mismatches/Page/Page Size), filter tabs
(All / Mismatches Only / Clean Only), "Export CSV", empty states ("No … match the current filter"), the mismatch badge
("N uyuşmazlık"), back-nav ("Finance Overview" → "Finans Genel Bakış"). Turkish primary. Replace `toLocaleString('en-US',
…)` with `tr-TR` (or the app's shared currency/number formatter) so amounts read `1.234,56`.

## Change 3 (optional) — the `/app/finance` overview landing
`FinanceReconciliationOverviewPage` is a tile-nav page that now duplicates the sidebar **Finans & Raporlar** menu (same
as the Payment dashboard we slimmed). Optional: either localize + fix its icons and keep it as a compact landing, or
slim it to a short intro (the menu is the primary nav). At minimum fix its icons + i18n so it's not broken/English.

## Don't-break / QA
- The P12 `FinancialReportingDashboardPage` (already localized + correct icons) is **not** touched.
- No backend/BFF/API changes; endpoints already return 200. CSV export unchanged.
- `npm run typecheck` + lint clean; tr+en parity for any new keys; no other pages regressed.

## Verification (on-screen)
Fresh admin login → each finance sub-page (`/app/finance`, `…/cargodry/settlements`, `…/cargodry/renewals`,
`…/commission-rule-usage` (Komisyon Kuralı Kullanımı), `…/invoice-statement` (Fatura Ekstresi)) renders with **real
Material-symbol icons** (no literal "CHEVRONLEFT"/"BARCHART3"/… text) and **Turkish** copy; numbers format `tr-TR`;
empty states read cleanly in Turkish; Export CSV + filters still work. typecheck/lint clean.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_FINANCE_PAGES.md`: the icon-name mappings applied, the i18n keys added (tr+en), the
overview-landing decision, and the on-screen before/after. Note that the empty tables are data (seed) not defects.
