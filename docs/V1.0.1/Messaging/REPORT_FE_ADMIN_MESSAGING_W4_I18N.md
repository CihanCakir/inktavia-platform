# REPORT — FE_ADMIN Messaging Wave 4: full i18n (tr + en)

**Repo:** `inktavia-marine-admin-web` (FE only). Pure string extraction + `t()` — no logic/behavior/route/endpoint
change. The admin messaging surface (W1 socket, W2 intervention, W3 panels) was hardcoded English; it is now fully
localized against the `messages` namespace with **tr + en exact key parity**.

## Namespace: `messages` (tr + en, 138 leaf keys each — exact parity)

Kept the 6 pre-existing top-level scalars (`title, subtitle, all, pending, flagged, searchPlaceholder`) and reused
them (page title + the sidebar filter tabs). Added nested blocks:

| block | keys | covers |
|---|---|---|
| `conversations` | 12 | sidebar heading, empty/select/no-messages states, unread + flagged chip, `hub.*` (Live/Connecting…/Offline/Error) |
| `compose` | 11 | message + internal-note placeholders, SEND/SENDING…, audit hint, send-error, INTERNAL NOTE toggle + banner, attachment (uploading/dismiss/attach) |
| `flag` | 11 | FLAG button + FLAGGED badge, popover title/placeholder/required-validation, confirm/confirming/cancel, success title+body, error title |
| `moderate` | 14 | kebab menu title + Block/Flag/Allow actions, required/optional reason placeholders + validation, apply/cancel, moderated success title+body, "Removed by moderator", "Under Review" |
| `bubble` | 4 | Internal Admin Note heading, Tap to open map, Google/Yandex Maps |
| `nav` | 3 | Conversations / Moderation Queue / Reports (were TR-only hardcoded) |
| `moderation` | 30 | title/subtitle, KPI strip, queue title/subtitle, 7 column headers, loading/error/retry/empty, row reason placeholder, allow/flag/block, open-conversation, pagination (showing/prev/next/page), applied/failed toasts |
| `reports` | 33 | title/subtitle, period + generated, 3 KPIs, empty/error/retry/loading/no-activity, `provider.*` (title/subtitle/empty/4 cols/bar title+subtitle+series), `channel.*` (donut/line/peak titles+subtitles+series+empties) |
| `enums` | 14 | `moderationStatus` (5), `conversationStatus` (4), `contextType` (5) |

Parity verified programmatically: **en 138 / tr 138, zero keys missing on either side.**

Interpolations used (same keys, locale-appropriate templates): `compose.attachUploading` (`{{fileName}}`,
`{{progress}}`), `moderate.successBody` (`{{status}}` — the localized status), `moderation.showing`
(`{{from}}/{{to}}/{{total}}`), `moderation.page` (`{{page}}`), `reports.generated` (`{{ts}}`),
`reports.provider.barSubtitle` (`{{count}}`).

## Files wired (5) + 1 new enum-map module

- `features/messages/messagingEnumMaps.ts` **(new)** — the keyed enum→leaf-key maps (mirrors the finance ledger enum
  maps): `MODERATION_STATUS_KEY`, `CONVERSATION_STATUS_KEY`, `CONTEXT_TYPE_KEY`, plus `MODERATION_STATUS_VARIANT`
  (StatusBadge variant, moved here from the moderation page so the views share one source of truth). Enums are
  string-valued, so each map turns the enum string into a stable leaf key resolved against `messages.enums.*`.
- `pages/app/MessagesPage.tsx` — `useTranslation('messages')` (+ in `HubStatusDot`); every literal → key; context &
  conversation-status labels localized via the enum maps; the filter tabs reuse `all/pending/flagged`.
- `pages/app/MessagesModerationPage.tsx` — page/KPI/columns/actions/pagination/states + status badge label via the
  enum map; dropped the local `STATUS_VARIANT` in favour of the shared one.
- `pages/app/MessagesReportsPage.tsx` — page/filter/KPIs/table/chart titles/states; `contextType` localized in the
  busiest-channel KPI and the donut slice names (`labelContext` helper, raw-value fallback).
- `features/messages/components/MessagesSegmentedNav.tsx` — the three tab labels via `nav.*`.
- `features/messages/components/MessageBubble.tsx` — `useTranslation('messages')` in each sub-component
  (LocationBubble/ModerationBadge/InternalNoteBubble/ModerateMenu/main); moderation-action labels keyed;
  moderated/redacted text, badges, map links localized.

## Formatting fixes

- Replaced the two remaining `toLocaleTimeString('en-US', …)` calls with `'tr-TR'` (MessagesPage sidebar timestamp;
  MessageBubble `formatTime`). Reports/moderation already used `tr-TR`. Dates/numbers are **always** `tr-TR`
  regardless of UI language (per spec), verified on-screen (moderation "03 Ağu 2026", reports "Generated
  03.08.2026 …" stayed tr-TR in English mode).

## Left raw (intentionally, not UI literals)

Dynamic backend data — message bodies, stored moderation-reason text (`item.moderationReason` / the "Reason" column
value), attachment file names/types, and `senderRole` (a `ParticipantRole`, outside the spec's enum-map set: status /
conversation-status / context-type). These are data, not copy.

## QA

- `npm run typecheck` — clean.
- `eslint` on the 5 files + the enum map — clean (fixed one unused-import along the way). The repo-wide 42 eslint
  errors are pre-existing in unrelated files (LoginPage, test helpers).
- Grep of the five files: no user-facing string literals remain (no JSX text nodes, no literal
  placeholder/title/label/subtitle props, no `en-US`/`en-GB`).

## On-screen transcript (tr ↔ en, full local stack, fresh admin session)

**Turkish** (`i18nextLng=tr`):
- `/app/messages` — "İletişim Denetimi", hub "Canlı", nav "Konuşmalar/Moderasyon Kuyruğu/Raporlar", search "Konuşma
  ara...", sidebar "Konuşmalar", tabs "Tümü/Bekleyen/İşaretli", context "DOĞRUDAN MESAJ/SERVİS TALEBİ", "⚑ İşaretli",
  empty "Bir konuşma seçin". Chat header "Doğrudan Mesaj · AKTİF"; buttons "İŞARETLE/DAHİLİ NOT"; bubble "DAHİLİ
  YÖNETİCİ NOTU"; badges "İŞARETLI"; compose "Bir mesaj yazın… / GÖNDER"; hint "Göndermek için Enter'a basın · Denetim
  Modu Etkin". FLAG popover: "Konuşmayı işaretle / Neden (zorunlu)… / İşaretle / İptal".
- `/app/messages/moderation` — "Moderasyon Kuyruğu" + subtitle; KPIs "KUYRUKTA/ENGELLENEN/İŞARETLİ/BEKLEYEN"; columns
  "Mesaj/Konuşma/Gönderen/Durum/Neden/Gönderildi/İşlemler"; badge "İşaretli"; actions "İzin ver/İşaretle/Engelle";
  placeholder "Neden (isteğe bağlı)".
- `/app/messages/reports` — "Mesajlaşma Raporları"; "DÖNEM" + "…tarihinde oluşturuldu"; KPIs "TOPLAM MESAJ / EN YOĞUN
  KANAL: Doğrudan Mesaj / ZİRVE SAATİ"; "Sağlayıcı yanıt süresi", "En yavaş yanıtlayanlar" (…ilk 8 sağlayıcı), "Kanala
  göre mesajlar", "Günlük mesaj hacmi"; empty "Sağlayıcı verisi yok."

**English** (`i18nextLng=en`, same views): every label flipped — "Communication Audit / Live /
Conversations·Moderation Queue·Reports / All·Pending·Flagged / DIRECT MESSAGE / Select a conversation";
"Moderation Queue" KPIs+columns+Allow/Flag/Block; "Messaging Reports", "BUSIEST CHANNEL: Direct Message" (the
ContextType enum flipped tr→en), "Slowest responders / Top 8 providers…", "Messages by channel", "Daily message
volume". Dates remained `tr-TR` in both modes. Language restored to `tr` after verification.

Admin messaging is now fully localized. Remaining backlog: test-data cleanup · live-moderation events ·
timestamptz sweep.
