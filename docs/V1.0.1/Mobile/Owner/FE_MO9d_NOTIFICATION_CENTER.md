# FE_MO9d — owner notification center + push + live bell + preferences (Expo)

> **Repo:** `inktavia-marine-mobile` (RN/Expo). MO9 **phase d** per `MO9_PLAN.md` — the owner track's **close-out**:
> the FE consumes MO9c (inbox / mark-read / preferences / device-token) + MO9b (`/hubs/notification` live bell) +
> MO9a (real push delivery). Wire into the existing `features/notifications/`. Additive; tr+en; mock parity;
> cost-free. **Do not commit.**

## Backend contracts (already built)
- **Inbox / actions (MO9c):** `GET api/v1/mobile/notifications?skip=&take=` → `{ Items: MobileNotificationDto[],
  Total, UnreadCount }` where `MobileNotificationDto = { Id, Type(string), Title, Body, ReferenceType, ReferenceId,
  IsRead, CreatedAt, ReadAt }` (no MetadataJson, no economics — money rides `Body` text). `PATCH .../{id}/read`,
  `POST .../mark-all-read`, `GET/PUT .../preferences` (matrix: per-category `InApp`/`Push`/`Email` `{Enabled,Locked}`;
  update `{Category, Channel, Enabled}` — only Push/Email changeable, InApp + Account locked), `POST .../device-token`
  (`{ DeviceToken }` — the BFF forces `Platform = Fcm`).
- **Live bell (MO9b):** SignalR hub at **`/hubs/notification`** on the mobile BFF; the framework broadcasts on the
  single method **`ReceiveEvent`** with an envelope whose `Type == "mobileNotification"` and `payload =
  MobileRealtimeNotification { NotificationId, Type, Title, ReferenceType, ReferenceId, SentAt }` — a thin, cost-free
  hint (refetch is the source of truth, the frame is only a nudge).

## FE — build on the existing `features/notifications/`
1. **API + hooks:** a `notificationsApi` (inbox skip/take, mark-read, mark-all, get/put preferences, register
   device-token) via the app's `normalizeEnvelope` + React Query, mirroring the MO1–MO8 slices. Remove any hardcoded
   demo notifications; keep the `EXPO_PUBLIC_MOCK_MODE` branch realistic (a few owner-shaped mock notifications).
2. **Notification center screen:** paged list (infinite scroll or load-more over `skip/take`), unread styling, the
   **unread badge** from `UnreadCount`, relative timestamps, loading/empty/error states. **Tap → deep-link** by
   `ReferenceType`/`ReferenceId` to the right screen (SR detail, offer detail, dispute detail, maintenance, membership)
   — and mark that item read. A **"mark all read"** action. tr+en.
3. **Push (expo-notifications):**
   - On sign-in / first entry, request OS notification permission (graceful if denied — the in-app bell still works).
   - Obtain the **FCM** device token and register it via `POST device-token`; **re-register on token rotation**;
     **clear/unregister on sign-out**. (Physical device / dev build needed for a real token — Expo Go has no FCM; guard
     so the app never crashes in Expo Go, matching the social-auth graceful pattern.)
   - Handle **foreground** (in-app toast + badge bump), **background**, and **notification-tap** → route via the same
     deep-link map as the list.
4. **Live bell (SignalR):** connect to `/hubs/notification` with the app's BFF auth token (`@microsoft/signalr`,
   `accessTokenFactory` from the auth store); on `ReceiveEvent` where `Type == "mobileNotification"` → **invalidate the
   inbox query** (refetch) and bump the badge / prepend optimistically; auto-reconnect with backoff; disconnect on
   sign-out / re-connect on token refresh. The frame is a hint — always reconcile against the API. Mirror the provider
   web's realtime client shape where practical.
5. **Preferences screen** (profile/settings area): render the category matrix; toggle **Push/Email** per category
   (PUT on change, optimistic + rollback on error); **locked** cells (InApp, Account) rendered on + disabled with a
   hint. tr+en.

## Don't-break / QA
- Additive: `features/notifications/` gains the real API + center + preferences + push + live bell; nothing else
  changes. Mock parity preserved behind `EXPO_PUBLIC_MOCK_MODE`. **Cost-free:** the UI shows `Title`/`Body` + a
  deep-link only — no amount/commission/net fields anywhere (backend already scrubbed).
- Expo Go guard: push token acquisition is wrapped so the app degrades gracefully without a dev build (like the
  Google/Apple social buttons); the in-app bell + inbox + preferences work in Expo Go.
- Verification: (1) `tsc --noEmit` 0 errors; (2) i18n tr/en parity for all new keys; (3) inbox lists + paginates,
  tap deep-links + marks read, mark-all clears the badge; (4) preferences round-trip (locked cells non-editable);
  (5) device-token registers on sign-in and clears on sign-out (mock/dev-build); (6) live bell: a published
  `mobileNotification` frame bumps the badge + refetches (manual smoke against the running stack + Redis — document
  the steps); (7) grep the notification UI — no economics fields.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_FE_MO9d_NOTIFICATION_CENTER.md`: the notification center (paged inbox + deep-links +
mark-read/all), the push wiring (permission → FCM token → device-token register, rotation + sign-out, foreground/
background/tap routing, Expo Go guard), the SignalR live bell on `/hubs/notification`, the preferences screen, the
tr+en + mock parity + cost-free confirmation, and the manual live-smoke steps. **This closes the MO9 phase and the
owner economics track (MO1–MO9).** Remaining project-level gates (unchanged): iyzico P9 live keys (paid checkout /
change-order live capture), a Firebase service-account secret (live device push), and the R2 KDV values.
