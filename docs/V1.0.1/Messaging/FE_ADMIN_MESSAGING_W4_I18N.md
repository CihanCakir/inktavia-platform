# FE_ADMIN — Admin messaging Wave 4: localize the whole surface (i18n tr + en)

> **Repo:** `inktavia-marine-admin-web` (FE only). The admin messaging surface is feature-complete (W1 socket, W2
> intervention, W3 panels) but **hardcoded English**. Wave 4 wires `useTranslation('messages')` across all five files and
> fills the `messages` namespace with **tr + en full parity**. Same mechanical pass as the finance-pages i18n fix — no
> behavior/logic change, no backend.

## Current state
- Namespace `messages` is **registered** (`i18n.ts` + `namespaces.ts`) but holds only 6 keys
  (`title, subtitle, all, pending, flagged, searchPlaceholder`). **Keep** these; extend the file.
- **Zero** `useTranslation` usage in the surface. Files to localize:
  - `pages/app/MessagesPage.tsx` — conversations two-pane **+ the W2 intervention UI** (flag popover, moderate kebab,
    internal-note).
  - `pages/app/MessagesModerationPage.tsx` — W3 moderation queue.
  - `pages/app/MessagesReportsPage.tsx` — W3 reports.
  - `features/messages/components/MessagesSegmentedNav.tsx` — the three tab labels.
  - `features/messages/components/MessageBubble.tsx` — bubble/moderation states.

## What to do
Wire `const { t } = useTranslation('messages')` in each file and replace **every** hardcoded user-facing string with a
key. Turkish primary; add the English mirror in `en/messages.json` with **exact key parity**. Structure the namespace
into blocks (nest sensibly), covering at least:

- **conversations** (MessagesPage): title/subtitle (reuse existing `title`/`subtitle`), tab labels (reuse `all`/
  `pending`/`flagged`), `searchPlaceholder` (reuse), sidebar "Conversations" header + count, empty states ("No
  conversations", "Select a conversation" / description, "No messages yet" / description), the **hub status** labels
  (Live / Connecting… / Offline / Error), unread/"Flagged" chips, context labels.
- **compose** (MessagesPage reply box): "Type a message…" / "Write an internal admin note…", SEND / SENDING…, "Press
  Enter to send · Audit Mode Enabled", "Failed to send. Try again.", INTERNAL NOTE toggle + "Internal note — not visible
  to participants", attachment states (uploading %, done, error/dismiss, "Attach file").
- **flag** (W2): FLAG button, the reason popover (label/placeholder/required-validation, confirm/cancel), success/error.
- **moderate** (W2 + MessageBubble): the kebab actions (Allow / Flag / Block), reason prompt, the moderated/redacted
  bubble text ("Removed by moderator" + reason), the status pills (Flagged / Blocked / PendingReview / Allowed).
- **nav** (MessagesSegmentedNav): Konuşmalar / Moderasyon Kuyruğu / Raporlar.
- **moderation** (MessagesModerationPage): page title/subtitle, table column headers (message/conversation/sender/
  status/reason/time), row actions, KPI labels (Total / Blocked / Flagged / Pending), pagination (Prev/Next/Page),
  empty/loading/error states.
- **reports** (MessagesReportsPage): page title/subtitle, date-range labels (From/To/last 30 days), provider table
  headers (Provider / Avg / Median / Messages), chart titles (Slowest responders / Channel share / Daily volume / Peak
  hours), KPI labels (Total messages / Busiest channel / Peak hour), empty/loading/error states.

## Formatting
- Numbers/dates → `tr-TR` (reports already use it — keep; apply the same to any remaining `en-US`/`en-GB` in the
  conversation timestamps / moderation timestamps). Use the app's shared formatter if one exists.
- Enum-ish labels (ConversationStatus, MessageModerationStatus, ContextType) → localized via keyed maps (mirror how the
  finance ledger enum labels are localized), not raw enum strings.

## Don't-break / QA
- No logic/behavior/route/endpoint change — pure string extraction + `t()`. The live socket, W2 mutations, W3 queries all
  unchanged.
- **tr + en exact key parity** (no missing keys in either file); no orphan/unused keys left hardcoded. Grep the five
  files for user-facing string literals after the pass → none remain (except non-UI constants).
- `npm run typecheck` + lint clean; language switch tr↔en flips every label on all three views.

## Verification (on-screen)
Fresh admin login, with the app in **Turkish** then **English**:
1. `/app/messages` — header, tabs, search, hub-status dot, empty states, reply box, FLAG popover, moderate kebab, and
   MessageBubble moderated states all render in the active language (no leftover English in tr mode).
2. `/app/messages/moderation` — title, columns, status badges, actions, KPIs, pagination localized.
3. `/app/messages/reports` — title, date range, table headers, chart titles, KPIs localized; numbers/dates `tr-TR`.
4. Toggling language flips everything; typecheck/lint clean; no regression to W1/W2/W3.

## Report
`docs/V1.0.1/Messaging/REPORT_FE_ADMIN_MESSAGING_W4_I18N.md`: the keys added per block (tr+en parity count), the files
wired, the enum-label maps, formatting fixes, and the on-screen tr/en transcript. Admin messaging is then fully
localized; remaining backlog = test-data cleanup · live-moderation events · timestamptz sweep.
