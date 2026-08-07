# Admin QA — Profile Performance (A-QA8)

Routes: `/app/performance` (dashboard), `/risk-watchlist`, `/providers/:profileId`, `/tier/:tier`, `/priority-preview`,
`/participants/:profileId`. Sidebar: Performans. Backend: Phase 20–22 (provider performance, risk watchlist, tiers, priority
preview, participant performance).

## Static findings
All pages **REAL and wired** (`useProfilePerformanceSnapshotsByTier`, history/logs/signals + recalc/raise/resolve mutations).
Tier/severity/confidence rendered via string `Record` maps (no numeric enums assumed — verify BFF doesn't serialize
tiers/severities as ints; if it does, `TIER_COLORS`/`SEVERITY_COLORS[...]` silently miss). Issues:
- 🔴 **Broken icons** on `RiskWatchlistPage` and `ParticipantPerformanceDetailPage` — they pass **Lucide-style** PascalCase
  names (`AlertTriangle`, `ChevronLeft`, `Loader2`, `Snowflake`, `RefreshCw`, `AlertCircle`, `ShieldCheck`) to
  `shared/ui/icon/Icon.tsx`, which renders the name as a **Material Symbols ligature** → blank/garbled icons. Correct example:
  `ProviderPerformanceDetailPage` uses `warning`, `chevron_left`, `progress_activity`.
- **No i18n** on `RiskWatchlistPage` + `ParticipantPerformanceDetailPage` (hardcoded English); other perf pages use
  `t('performance')`.
- Date-locale inconsistency: performance pages `en-US` while providers/approvals use `tr-TR`.
- `PerformanceDashboardPage.tsx:54` notes BFF `totalCount=0` fallback in mock env — verify real counts live.

## Live walkthrough checklist
- [ ] Risk-watchlist + participant-detail icons render (not blank squares).
- [ ] Tier/severity badges show labels (verify not raw ints from BFF).
- [ ] Both flagged pages localized tr/en; dates consistent (tr-TR).
- [ ] Dashboard tier counts are real (not 0).

## Fix candidates
`FIX_A_QA8_PERF_ICONS` (Material Symbols ligature names), `FIX_A_QA8_PERF_I18N`.
