# REPORT — FE_MO9d owner notification center + push + live bell + preferences (Expo)

> MO9 **phase d** — the owner track's **close-out**: the FE consumes MO9c (inbox / mark-read / preferences /
> device-token) + MO9b (`/hubs/notification` live bell) + MO9a (real FCM push). Wired into the existing
> `features/notifications/`. `inktavia-marine-mobile`. Additive; tr+en; mock parity; cost-free. **NOT committed.**
> **This closes the MO9 phase and the owner economics track (MO1–MO9).**

## Outcome
- **`npx tsc --noEmit` = 0 errors.** i18n `notifications` namespace **28/28 en=tr parity**. Cost-free (the UI shows
  `title`/`body` + a deep-link ref only — no amount/commission/net field anywhere). All packages already installed
  (`@microsoft/signalr@8`, `expo-notifications@0.30`, `expo-secure-store`, `zustand`, React Query) — no `npm install`.

## What was scaffolded-but-wrong (fixed) vs built
The `features/notifications/` folder existed but was static/mock and the realtime + push clients pointed at the wrong
targets. MO9d fixed those and wired everything to the real backends.

### Fixed
- **`services/realtimeClient.ts`** → the hub URL was `/hubs/notifications` on method `ReceiveNotification`; corrected to
  **`/hubs/notification`** + **`ReceiveEvent`**, filtering `frame.type === "mobileNotification"` (dual PascalCase/
  camelCase). `accessTokenFactory` reads the auth store; auto-reconnect kept.
- **`services/notificationService.ts`** → push used `getExpoPushTokenAsync` (an Expo token, not FCM); corrected to
  **`Notifications.getDevicePushTokenAsync()`** (the raw FCM/APNs device token MO9a's FirebaseAdmin sender needs),
  registered via the BFF `device-token` endpoint, with a rotation listener + clear, all try/catch-guarded.
- **`core/api/endpoints.ts`** → the `NOTIFICATIONS` group used legacy `/notifications` paths; corrected to the real
  `/api/v1/mobile/notifications*` BFF routes.

### Built
- **API + hooks** (new `api/notificationsApi.ts` + `api/useNotifications.ts`) — `useNotifications({skip,take})`,
  `useUnreadCount`, `useMarkRead`, `useMarkAllRead`, `useNotificationPreferences`, `useUpdatePreference` (optimistic +
  rollback), `useRegisterDeviceToken`; writes invalidate `queryKeys.notifications.all`.
- **Notification center** (`NotificationCenterScreen`) — the real paged inbox (load-more over skip/take), unread
  styling, the badge from `unreadCount`, relative timestamps, loading/empty/error; **tap → mark-read + deep-link**; a
  **mark-all-read** action. `NotificationDetailScreen` reads the inbox cache + a deep-link button.
- **Push (expo-notifications)** — notification handler + permission request on sign-in → device (FCM) token →
  register; a rotation listener re-registers; sign-out clears. Register hooked into `authService.establishSession`,
  clear into `logout`. Foreground (in-app toast + inbox invalidate), background, and tap (route via the nav ref) all
  handled. **Expo Go guard** everywhere — no dev build → a warning + null token, and the in-app bell/inbox/preferences
  still work.
- **Live bell (SignalR)** — a no-UI `<NotificationsRuntime/>` (under QueryProvider + ToastProvider) connects on
  `isAuthenticated`, disconnects on sign-out; on a `mobileNotification` `ReceiveEvent` frame it invalidates the inbox
  (frame = hint, API = source of truth). Auto-reconnect with backoff.
- **Preferences screen** (`NotificationPreferencesScreen`) — the real matrix; Push/Email toggles (PUT on change,
  optimistic + rollback via toast); locked cells (InApp, Account) rendered on + disabled with a hint.
- **Navigation ref** — new `navigationRef` (`createNavigationContainerRef`) passed to the single `<NavigationContainer>`
  in `RootNavigator`, so a background/quit notification-tap can route.
- **Live unread badge** — replaced the hardcoded `NotificationBell unreadCount={2}` in the header screens with the
  live `useUnreadCount()`.
- **i18n** — extended the `notifications` namespace (center + preferences + push) in en + tr (28/28).
- **Mock parity** — `notifications.handlers.ts` aligned to the real `/api/v1/mobile/notifications*` paths (inbox +
  read + mark-all + preferences matrix with locked InApp + device-token), 6 owner-shaped seeds (some unread), behind
  `EXPO_PUBLIC_MOCK_MODE`.

### Deep-link map (via the nav ref; works from a background tap)
`Maintenance*` → ProfileTab/MaintenanceSchedules · `Subscription*`/`Premium*`/`BenefitBudget*`/`*Price*` →
ProfileTab/Membership · `referenceType === "ServiceRequest"` (offers/completion/dispute/payment) →
ServicesTab/ServiceRequestDetail `{requestId}` · `Message*` → ServiceRequestDetail · else → open the notification
center. (Dispute/offer notifications route to the SR detail — the notification ref carries only the SR id, not a
disputeId/offerId; the SR detail hosts those sections, so it's safe.)

## Files
- **Created (5):** `api/notificationsApi.ts`, `api/useNotifications.ts`, `deepLink.ts`, `NotificationsRuntime.tsx`,
  `app/navigation/navigationRef.ts`.
- **Modified (17):** `endpoints.ts`, `queryKeys.ts`, `services/realtimeClient.ts`, `services/notificationService.ts`,
  `NotificationCenterScreen`, `NotificationDetailScreen`, `NotificationPreferencesScreen`, `RootNavigator.tsx`,
  `AppProviders.tsx`, `authService.ts`, 5 header screens (live badge), `en.json`, `tr.json`,
  `mock/handlers/notifications.handlers.ts`.

## Don't-break / QA + deviations
- Additive; mock parity behind `EXPO_PUBLIC_MOCK_MODE`; cost-free.
- **Needs a dev build / physical device:** real device push (Expo Go has no FCM — guarded, never crashes); live push
  is also env-gated on the Firebase secret (MO9a).
- Filters simplified to **All / Unread** (the old type-specific chips keyed on legacy type values that no longer
  exist). Preferences use RN `Switch` directly (the shared `SwitchInput` has no `disabled` prop for locked cells).
- `core/mock/data/notifications.data.ts` is now unused (the handler is self-contained) — left in place.
- **Live socket + push smoke** (connect `/hubs/notification`, publish a `mobileNotification` frame → badge bumps; tap
  a real push → deep-link) needs the running stack + Redis + a dev build — manual.

## Project-level gates that remain (unchanged, out of scope)
iyzico P9 live keys (paid checkout / change-order live capture — MO3/MO6/MO7), a Firebase service-account secret (live
device push — MO9a), and the R2 KDV values.

## MO1–MO9 owner economics track — CLOSED
This report closes the MO9 phase and the owner economics track: MO1 (service requests) · MO2 (offers) · MO3 (accept→pay)
· MO4 (completion review) · MO5 (disputes) · MO6 (change orders) · MO7 (membership + discount) · MO8 (maintenance
self-service) · MO9a (Firebase push sender) · MO9b (realtime edge) · MO9c (notification surface) · MO9d (notification
center + push + live bell + preferences).
