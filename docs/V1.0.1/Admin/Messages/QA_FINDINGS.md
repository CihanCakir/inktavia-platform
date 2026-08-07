# Admin QA — Messages + Notifications (A-QA9)

Routes: `/app/messages` (+ `/moderation`, `/reports`, `/support`), `/app/notifications/{inbox,templates}`.

## Static + live findings
- **Messages cluster = REAL, exemplary.** `MessagesPage` (list + SignalR hub), `MessagesModerationPage` (queue + moderate +
  hub), `MessagesReportsPage`, `MessagesSupportPage` — all wired, enum-mapped via `src/features/messages/messagingEnumMaps.ts`,
  full `messages` i18n (tr+en). (Backend: Messaging W2–W5 moderation/reports/support "Destek" + realtime.)
- 🔴 **X7 — SignalR negotiation fails (live).** Console on load: `Failed to start the connection: The connection was stopped
  during negotiation.` The realtime hub doesn't negotiate → moderation/messages realtime may not update live. Verify the hub
  URL/auth (token on the negotiate call) against `bff-adminpanel`.
- **Notifications Inbox** `NotificationsInboxPage` — REAL (list + mark-read + push N-A) but **hardcoded English, no
  `notifications` i18n namespace exists** at all under `locales/{en,tr}/`. In-app `type` is a BFF **string** (icon map fine).
  Reached only via the Topbar bell (not in sidebar).
- **Notification Templates** `NotificationTemplatesPage` — **404/500 risk**: entity `notificationApi.ts` has
  `TODO: /notification-templates endpoint NOT confirmed`, gated by `NOTIFICATION_TEMPLATES_MODULE` (default **false**), yet
  the route is mounted **unconditionally** and the page calls a real `GET /notification-templates`. Partial i18n.

## Live walkthrough checklist
- [ ] Messages: thread loads, send works, **realtime delivers without the negotiation error** (X7), unread counts update.
- [ ] Moderation queue: flag/moderate + realtime; enum labels correct.
- [ ] Reports + Support ("Destek") queues load.
- [ ] Notifications inbox: list + mark-read + push toggle; add `notifications` i18n namespace (tr+en).
- [ ] `/app/notifications/templates`: confirm the BFF endpoint exists (no 404) or gate the route behind the flag.

## Fix candidates
`FIX_A_QA9_SIGNALR_NEGOTIATION`, `FIX_NOTIFICATIONS_I18N_NS`, `FIX_NOTIFICATION_TEMPLATES_GATE`.
