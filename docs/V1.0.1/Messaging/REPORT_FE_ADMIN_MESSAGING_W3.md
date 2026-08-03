# REPORT — FE_ADMIN Messaging Wave 3: observation panels + report remote-call typing

**Scope delivered:** the two admin observation surfaces whose hooks already existed but rendered nowhere —
the **Moderation Queue** and the **Messaging Reports** — plus the last messaging BFF envelope-typing debt
(the report remote calls returned `object`). FE-primary; a small BFF typing change; and one **necessary
blocker fix** in the Messaging module (see §6).

Repos touched:
- `addesso-project` — AdminPanel BFF (typing) + Messaging module (blocker fix).
- `inktavia-marine-admin-web` — FE panels, types, routes, segmented nav.

---

## 1. PART A — BFF: type the report remote calls (envelope lesson)

The Messaging module's `MessagingReportingController` returns the `AizenApiResponse` envelope via `SetResponse`
(same as conversations/moderation-queue). The two report remote calls were still `Task<object>`, so the whole
`{ header, body }` envelope was surfaced to the FE (the wrapped-vs-bare mismatch from the earlier list-empty fix).

**Wrinkle discovered:** the module's report response records (`GetProviderResponseTimeReportResponse`,
`GetChannelUsageReportResponse`, and their item DTOs) live in `Aizen.Modules.Messaging.**Application**`, which the
BFF does **not** reference (it references only `…Messaging.Abstraction`, where `GetModerationQueueResponse` lives).
To honour the QA guardrail "no module change" for the *typing* work, I introduced BFF-local mirror records instead
of moving/module-referencing types. Refit/System.Text.Json bind by JSON property name, so the shapes line up on the
wire.

Files:
- `Bff/…/Aizen.Bff.AdminPanel.Application/Common/Dto/MessagingReportsContracts.cs` **(new)** — mirror bodies
  (`ProviderResponseTimeReportBody`, `ChannelUsageReportBody` + item records `ProviderResponseTimeItem`,
  `ChannelVolumeItem`, `DailyMessageVolumeItem`, `PeakHourItem`) and the merged BFF response
  `MessagingReportsBffResponse(List<ProviderResponseTimeItem> ProviderResponseTime, ChannelUsageBff ChannelUsage,
  DateTimeOffset GeneratedAt)` where `ChannelUsageBff` carries `ByChannel/DailyTrend/PeakHours`.
- `Bff/…/Common/RemoteClients/IAdminMessagingBffRemoteCall.cs` — `GetProviderResponseTimeAsync` →
  `Task<AizenApiResponse<ProviderResponseTimeReportBody>>`, `GetChannelUsageAsync` →
  `Task<AizenApiResponse<ChannelUsageReportBody>>` (+ explanatory comment).
- `Bff/…/Controllers/V1/AdminMessagingController.cs` — `GetReports` now unwraps each envelope's `.Body` and merges
  into a typed `AizenApiResponse<MessagingReportsBffResponse>` (no more anonymous `object` / raw-envelope leakage).

On the wire the FE now receives clean typed JSON:
`{ providerResponseTime: [...], channelUsage: { byChannel:[...], dailyTrend:[...], peakHours:[...] }, generatedAt }`.

## 2. PART B — FE types (`features/messages/types/messaging.types.ts`)

Aligned `MessagingReportResponse` to the real shape and added the missing item interfaces:
`channelUsage` changed from `ChannelVolumeItem[]` → `ChannelUsageReport { byChannel[]; dailyTrend[]; peakHours[] }`;
added `DailyMessageVolumeItem { date; messageCount; newConversations }` and `PeakHourItem { hour; messageCount }`
(mirroring the module DTOs; `Date` serializes as a `YYYY-MM-DD` string). `ModerationQueueItem` /
`ModerationQueueResponse` were already correct and kept as-is.

## 3. PART C — navigation: one surface, three views

Kept `/app/messages` as the cohesive "Communication Audit" surface with a **segmented header nav** (not new
sidebar items). New shared component `features/messages/components/MessagesSegmentedNav.tsx` renders on all three
views: `Konuşmalar → /app/messages`, `Moderasyon Kuyruğu → /app/messages/moderation`,
`Raporlar → /app/messages/reports` (NavLink active states; `end` on the base route). The single "Mesajlar"
sidebar entry is untouched. Added `MESSAGES_MODERATION` / `MESSAGES_REPORTS` to `routes.tsx` and the two child
routes + eager imports to `routeObjects.tsx`.

## 4. PART D — Moderation Queue panel (`/app/messages/moderation`)

`pages/app/MessagesModerationPage.tsx` (new): renders `useModerationQueueQuery` (30s poll). Table of
`ModerationQueueItem` (excerpt, conversation title/id, sender + role, `StatusBadge` for
Flagged/Blocked/PendingReview/Allowed, moderation reason, tr-TR timestamp). Per-row **act** reuses the W2
`useModerateMessageMutation` — an optional reason input + Allow/Flag/Block buttons; on success the toast fires and
the `modQueue` cache invalidates → row refreshes. KPI strip (in queue / blocked / flagged / pending) + clean empty
state + `skip/take` pagination.

**Hook fix (pagination):** `useModerationQueueQuery`'s query key didn't include `skip/take`, so paging never
refetched. Changed the key to `[...MESSAGING_KEYS.modQueue(), skip, take]` while keeping `modQueue()` a 2-element
prefix so the moderate/flag mutations' `invalidateQueries(modQueue())` still matches every paged variant.

## 5. PART E — Reports panel (`/app/messages/reports`)

`pages/app/MessagesReportsPage.tsx` (new): renders `useMessagingReportsQuery(from, to)` with a `DateRangePicker`
defaulting to the last 30 days (date-only range → inclusive UTC `[from, to]`). Reuses the shared
`@shared/ui/charts/Charts` primitives (no new chart components):
- **Provider response time** — sortable table (provider, avg first-response min, conversations, unanswered) +
  `BarChartCard` of the top-8 slowest responders.
- **Channel usage** — `DonutChartCard` (share by channel), `LineChartCard` (daily volume), `BarChartCard`
  (peak hours 0–23).
- KPI strip (total messages, busiest channel, peak hour); `tr-TR` number/date formatting; per-section empty states
  and a loading/error/`Retry` card.

## 6. Blocker fix in the Messaging module (necessary, pre-existing) ⚠️

On-screen verification revealed the two module reporting endpoints were returning **500 before any W3 change** —
the BFF faithfully propagated them (Refit → `ApiException 500`), so the Reports panel could only ever show its error
state. Root cause was a pair of pre-existing UTC read-path/date-translation bugs against Postgres `timestamptz`
columns (the same class as the P12 ledger UTC read-path fix). Because one failing endpoint 500s the whole merged
`GetReports`, the reports feature was fully non-functional without this. Minimal, contract-preserving fixes:

- `…/GetProviderResponseTimeReport/GetProviderResponseTimeReportQueryHandler.cs` — the `CreateDate` (timestamptz)
  filter bound `DateTimeOffset.DateTime` (Kind=Unspecified, rejected by Npgsql). Changed to `.UtcDateTime`.
- `…/GetChannelUsageReport/GetChannelUsageReportQueryHandler.cs` — same `CreateDate` fix on the `byChannel` filter;
  and the `dailyTrend`/`peakHours` aggregations used `GroupBy(m.SentAt.Date)` / `.Hour` / a `.Date` sub-count that
  Npgsql cannot translate over these columns. Reworked to pull the raw `SentAt` timestamps (and new-conversation
  `CreateDate`s) for the range and bucket them **client-side** by UTC day / UTC hour — consistent with how the
  provider handler already aggregates in memory. No contract/DTO/endpoint-shape change.

This is the only deviation from the stated "no module change" guardrail; it was required to satisfy the "reports
render typed charts" verification gate. Endpoint signatures and response shapes are unchanged.

## 7. Realtime / i18n

No realtime added (queue polls 30s; reports are periodic, 5-min stale). The messaging surface is still hardcoded
copy (the existing `MessagesPage` uses no `useTranslation`); to stay consistent the new panels use inline copy —
the segmented nav in Turkish (`Konuşmalar/Moderasyon Kuyruğu/Raporlar`) and panel bodies in English, matching the
current "Communication Audit" copy. `messages.json` (tr/en) was left as-is; full i18n is deferred to **W4**.

## 8. Build / typecheck / lint

- BFF `Aizen.Bff.AdminPanel` — `dotnet build` **0 errors**.
- Messaging module `…Application` — `dotnet build` **0 errors**; `messaging-api` container image rebuilt.
- admin-web — `npm run typecheck` **clean**; `eslint` on all changed files **0 errors** (the 42 repo-wide eslint
  errors are pre-existing in unrelated files: `LoginPage`, test helpers).

## 9. On-screen verification (fresh admin session, full local stack)

1. **Segmented nav** — `/app/messages` header shows `Konuşmalar · Moderasyon Kuyruğu · Raporlar`; clicking each tab
   routes (verified Konuşmalar↔Raporlar via the control, and all three via deep-link); the two-pane Conversations
   view still works and the live socket shows **"Live"** (W1/W2 unregressed); single "Mesajlar" sidebar entry.
2. **Moderation Queue** — listed 16 real items (15 Flagged + 1 PendingReview) with reasons/tr-TR timestamps; KPIs
   `IN QUEUE 16 / BLOCKED 0 / FLAGGED 15 / PENDING 1`. Clicking **Allow** on a row fired the "Moderation applied"
   toast, dropped the row, and updated KPIs to `15 / 0 / 14 / 1` on refetch.
3. **Reports** — after the module blocker fix, rendered from **typed** data (no raw-envelope leakage): KPIs
   `TOTAL MESSAGES 20 / BUSIEST CHANNEL DirectMessage / PEAK HOUR 08:00`; channel donut (DirectMessage), daily-volume
   line (07 Tem→03 Ağu, tr-TR months), peak-hours bar (tooltip "13:00 Messages: 11"); provider table + bar showed
   clean "No provider data" empty states (this dataset has only DirectMessage conversations). Narrowing the date
   range to `01.08–03.08.2026` refetched (Generated timestamp advanced) and recomputed **PEAK HOUR → 13:00** —
   date-range filter verified.

## 10. Deferred

- **Live moderation/flag events** on the queue (the W1-boundary realtime enhancement) — still deferred; the queue
  polls at 30s.
- **Full i18n** (tr/en) of the messaging surface — **W4**.
