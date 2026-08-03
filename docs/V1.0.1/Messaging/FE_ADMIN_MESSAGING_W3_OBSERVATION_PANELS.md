# FE_ADMIN — Admin messaging Wave 3: observation panels (moderation queue + reports) + type the report remote calls

> **Repo:** `inktavia-marine-admin-web` (FE-primary) + a small **BFF typing fix** in `addesso-project` (AdminPanel BFF).
> Wave 3 surfaces the two admin observation views whose hooks already exist but render nowhere: the **moderation queue**
> and the **messaging reports**. It also pays down the last messaging BFF envelope debt (the report remote calls return
> `object`).
>
> Backend module endpoints already exist: `ConversationModerationController.GetQueue`
> (`GetModerationQueueResponse { Items: ModerationQueueItemDto[], Total }`) and `MessagingReportingController`
> (`provider-response-time` → `GetProviderResponseTimeReportResponse { Items }`, `channel-usage` →
> `GetChannelUsageReportResponse { ByChannel, DailyTrend, PeakHours }`). Moderation-queue is already wired end-to-end
> (BFF remote call returns `AizenApiResponse<GetModerationQueueResponse>` ✅, FE `useModerationQueueQuery` exists) — it
> just needs a UI. Reports need the BFF typing fix first.

## PART A — BFF: type the report remote calls (envelope lesson)
`IAdminMessagingBffRemoteCall`: the two report calls currently return **`Task<object>`** while the module returns
`AizenApiResponse<T?>` — the exact wrapped-vs-bare mismatch that silently yields junk (see the earlier list-empty fix).
- Change `GetProviderResponseTimeAsync` → `Task<AizenApiResponse<GetProviderResponseTimeReportResponse>>` and
  `GetChannelUsageAsync` → `Task<AizenApiResponse<GetChannelUsageReportResponse>>` (import the module response types).
- `AdminMessagingController.GetReports` (+ its handler): unwrap each envelope (`.Result` / body) and merge into a
  **typed** BFF response record, e.g.
  `MessagingReportsBffResponse(List<ProviderResponseTimeDto> ProviderResponseTime, ChannelUsageBff ChannelUsage)` where
  `ChannelUsageBff` carries `ByChannel`, `DailyTrend`, `PeakHours`. Return `AizenApiResponse<MessagingReportsBffResponse>`
  (no more anonymous `object`).
- Verify on the wire the FE now receives clean typed JSON (not `{ providerResponseTime: { header, body… } }`).

## PART B — FE types (`features/messages/types/messaging.types.ts`)
Align `MessagingReportResponse` to the real shape: `{ providerResponseTime: ProviderResponseTimeItem[]; channelUsage: {
byChannel: ChannelVolumeItem[]; dailyTrend: DailyMessageVolumeItem[]; peakHours: PeakHourItem[] } }`. Add the missing
`DailyMessageVolumeItem` / `PeakHourItem` interfaces (mirror the module DTOs). `ModerationQueueItem` /
`ModerationQueueResponse` already exist — keep.

## PART C — navigation: three views under the audit surface
Keep `/app/messages` as the cohesive "Communication Audit" surface with a small **segmented header nav** (routes for
deep-linking), not new sidebar items:
- `Konuşmalar` → `/app/messages` (the existing two-pane).
- `Moderasyon Kuyruğu` → `/app/messages/moderation`.
- `Raporlar` → `/app/messages/reports`.
Add the two routes to `routeObjects.tsx`; render the segmented control on all three so the admin can move between them.
(Do not fragment the sidebar — one "Mesajlar" entry stays; the sub-views live in the page header.)

## PART D — Moderation Queue panel (`/app/messages/moderation`)
Render `useModerationQueueQuery` (already returns the envelope-correct data, `refetchInterval` 30s):
- A table/list of `ModerationQueueItem` (message excerpt, conversation title/id, sender, status badge —
  Flagged/Blocked/PendingReview, moderation reason, timestamp). Reuse `StatusBadge`.
- Per-row **act**: reuse the W2 `useModerateMessageMutation` to change status (Block / Allow / Flag) + reason → row
  updates on refetch. Optionally a link that opens the parent conversation in the Conversations view.
- KPI strip (total in queue, # blocked, # flagged, # pending) from the response. Empty state when the queue is clean.
- Pagination via `skip/take` (the query already supports it).

## PART E — Reports panel (`/app/messages/reports`)
Render `useMessagingReportsQuery(from, to)` with a date-range filter (default last 30 days). Reuse the shared
`@shared/ui/charts/Charts` primitives (as the finance dashboard does — no new chart components):
- **Provider response time** — `providerResponseTime[]` (provider, avg/median response minutes, message count): a
  sortable table + a `BarChartCard` of avg response time per provider (top N).
- **Channel usage** — `channelUsage.byChannel`: a `DonutChartCard` (share by channel); `channelUsage.dailyTrend`: a
  `LineChartCard` (messages/day over the range); `channelUsage.peakHours`: a `BarChartCard` (message count by hour 0–23).
- KPI strip (total messages, busiest channel, peak hour). Clean empty states; `tr-TR` number/date formatting.

## Realtime / i18n
- No realtime needed: the moderation queue polls (30s) and reports are periodic (5-min stale). (Live moderation/flag
  events remain the deferred W1-boundary enhancement.)
- The page is currently hardcoded English (full i18n is Wave 4). Add new strings consistent with the current copy; put
  them in `messages.json` (tr+en) if easy, else inline and note for W4. Don't block W3 on full i18n.

## Don't-break / QA
- Only the two report remote calls + `GetReports` merge change on the backend (typing); no module/other-endpoint change.
  Moderation-queue backend untouched (already correct). Conversations view + live socket (W1/W2) unregressed.
- Envelope-correct throughout (wrapped module → `AizenApiResponse<T>` on the BFF). `npm run typecheck` + lint clean;
  backend builds clean.

## Verification (on-screen)
Fresh admin login:
1. `/app/messages` header shows Konuşmalar · Moderasyon Kuyruğu · Raporlar; each routes correctly and the Conversations
   view still works (list + detail + live).
2. **Moderation Queue** lists flagged/blocked messages (seed a couple via the W2 flag/moderate actions to populate it);
   acting on a row changes status and the row refreshes; KPIs + empty state correct.
3. **Reports** renders the provider-response-time table/bar + channel-usage donut/line/peak-hours charts from **typed**
   data (no raw envelope leakage); date-range filter works; `tr-TR` formatting.
4. typecheck/lint clean; backend builds clean; no regression to W1/W2.

## Report
`docs/V1.0.1/Messaging/REPORT_FE_ADMIN_MESSAGING_W3.md`: the BFF report-typing fix (object → typed envelope), the FE type
alignment, the segmented nav + two new routes, the moderation-queue + reports panels, i18n handling, and the on-screen
transcript. Note the deferred live-moderation-events enhancement and W4 i18n.
