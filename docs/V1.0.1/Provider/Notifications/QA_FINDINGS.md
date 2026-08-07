# Provider QA — Notifications (P-QA6)

## Static findings
**Implemented** (`notifications` api + `useNotifications`/`useNotificationPreferences`/`usePushSubscription`) but **nav marks
it `planned`** → false "soon" badge (fix in Navigation). The page uses EmptyState as its empty branch (not a stub).

## Live walkthrough checklist (localhost:3002/app/notifications)
- [ ] List loads (loading/empty/error); unread badge in the shell bell is live.
- [ ] Mark read / mark-all-read work; deep-links from a notification land on the right screen (offer/job/dispute/message).
- [ ] Preferences screen: per-channel/per-type toggles persist (N-B).
- [ ] Web push: subscribe prompt + a real push arrives (permission-gated).
- [ ] New notification appears live (realtime bell) — offers/payment/completion/dispute/maintenance/price-change events.

## Fix candidates
Nav flag (→ Navigation). Any runtime/visual issue → `FIX_*` here.
