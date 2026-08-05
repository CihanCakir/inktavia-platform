# REPORT — N-B notification preferences (user × category × channel)

> Phase N-B of `Notification/ROADMAP_DELIVERY_SUPPORT_REASONS.md`. Lets each user choose which notification **categories**
> reach them on which **channel** (primarily web push from N-A; Email ready for later). **Additive** — the two-phase
> bus/WS2, the N-A push consumer transport, and the in-app badge are untouched; only a preference check was added inside
> the existing push/email dispatch. Depends on N-A (push works end-to-end).

---

## B1 — categories + Type→Category map
- `NotificationCategory { Messages, ServiceRequests, Disputes, Payments, CargoDry, Account, Broadcast }`
  (`Modules/Notification/.../Abstraction/Enum/NotificationCategory.cs`).
- Single source of truth `NotificationCategoryMap.Resolve(NotificationType)` (`.../Abstraction/NotificationCategoryMap.cs`)
  by the enum's numeric bands: **Messages** 200–201, **ServiceRequests** 100–132, **Disputes** 140–141, **Payments**
  150–156, **CargoDry** 300–309, **Account** 400–411 (profile + auth/OTP), **Broadcast** 900. `IsAlwaysDeliver(Account)`
  marks the security category; `ToggleableCategories` is the user-facing list (Account excluded).

## B2 — preference model + defaults
- `NotificationPreferenceEntity` (UserId × Category × Channel → Enabled + UpdatedAt), table
  `notification.notification_preferences` with a **unique index (UserId, Category, Channel)**; migration
  `20260805130421_AddNotificationPreferences` (auto-applied at boot via `SeedNotificationAsync`). Repo
  `INotificationPreferenceRepository` (`GetByUserAsync` / `UpsertAsync`).
- **Resolution is centralized in `NotificationPreferencePolicy`** (`.../Abstraction/NotificationPreferencePolicy.cs`):
  - `IsLocked` = InApp (any category) **or** Account (security) → the user can't change it; always delivers.
  - `DefaultEnabled` when a row is absent: **InApp = on**, **Push = on** (N-A behaviour until muted), **Email = off**
    (opt-in), Sms = off. Account push/email always on.
  - `Resolve(category, channel, stored)` = locked → true; else stored ?? default.
- `GetPreferences` returns a per-cell `{ enabled, locked }` matrix over the toggleable categories only (Account/security
  excluded); `UpdatePreference(category, channel, enabled)` upserts and rejects locked cells (InApp / Account).

## B3 — dispatch gates (the enforcement)
- **Push:** inside `NotificationSentPushConsumer.ExecuteCommitMessage` — resolve `message.Type → Category`, read the
  recipient's stored Push pref, `Resolve(...)`; **skip the push if disabled** and return. The in-app inbox row + live
  badge already persisted (this consumer only sends push), so muting push never hides the notification. The two-phase-bus
  wiring and the N-A consumer structure are unchanged — only the check was added.
- **Email:** inside `SendNotificationCommandHandler.Handle`, **for `Channel == Email` only** — same Type→Category +
  policy check; skip the email send if disabled. InApp is never gated (always-on baseline); Push is not gated here (it's
  the consumer's job). Security/Account emails always deliver.
- **Broadcast:** `AdminBroadcast (900) → Broadcast` category; push/email gated by the same shared policy — no special
  code path.

## B4 — API + BFF (envelope-correct)
- Module: `GET /api/v1/notification/notifications/preferences` + `PUT .../preferences`
  (`GetNotificationPreferencesQuery` / `UpdateNotificationPreferenceCommand`, identity resolved from the forwarded
  assertion — ProviderProfileId → UserId).
- **AdminPanel BFF** (`api/v1/admin-panel/notifications/preferences`) — added GET/PUT to `INotificationBffRemoteCall` +
  `NotificationsController`, `Task<AizenApiResponse<NotificationPreferencesResponse>>`, pass-through `.Body`.
- **MarineProvider BFF** (`api/v1/provider/notifications/preferences`) — CQRS handlers
  (`GetNotificationPreferencesBffQuery` / `UpdateNotificationPreferenceBffCommand`) mirroring the provider push pattern
  (resolve provider identity → remote call → `.Body`).

## B5 — FE (admin + provider preferences matrix)
- **admin-web:** `NotificationPreferencesCard` on the existing `PreferencesPage` (`/app/settings/preferences`) — a
  Category × Channel table (In-app | Push | Email | SMS). In-app cells render locked (🔒 always-on); Push/Email are
  checkboxes bound to `enabled`; SMS is greyed/disabled; the N-A `usePushSubscription` toggle is reused at the top as the
  master browser-push on/off. `preferencesApi` + `useNotificationPreferences`/`useUpdateNotificationPreference`
  (react-query; the PUT returns the refreshed matrix and seeds the cache). Strings in the `settings` namespace
  (`notificationPreferences.*`), **tr + en**.
- **provider-web:** the same matrix as a `SectionCard` on the provider `SettingsPage` (`/app/settings`), provider design
  tokens, `notificationsApi.getPreferences/updatePreference` + a react-query hook, `settings` namespace tr + en.

## Deploy / quality
- Rebuilt + redeployed (same-image, force-recreate) `notification-api`, `bff-adminpanel`, `bff-marineprovider` **and**
  `bff-marineprovider-2` (both replicas). Migration auto-applied at boot (table + unique index confirmed). Backend builds
  0 errors; admin-web + provider-web `tsc --noEmit` clean; ESLint clean on the changed files.

## Verify (on-screen — admin, live)
1. **Defaults (new/row-less user):** the admin matrix renders with **In-app locked-on**, **Push on** (all categories),
   **Email off**, **SMS greyed** — matching the documented defaults. tr labels + master push toggle present.
2. **Read/write:** toggling **Messages → Push** off wrote `(100012, Messages, Push, Enabled=false)` to the DB and the UI
   reflected it (only that cell unchecked); the PUT returned the refreshed matrix.
3. **Gate — Messages → Push OFF (verify #1):** sent a message to the (subscribed) recipient → **no browser push**
   (`registration.getNotifications()` = 0) while the **in-app row still persisted** (`notification.notifications` row,
   Type 200 InApp). Re-enabled → the next message **delivered a real FCM push** (`Web push sent to UserId=… NotificationId=119`)
   received by the SW with deep-link `/app/messages?c=9900000001`. Same conversation/recipient/sender — the only
   difference was the preference. (This maps to verify #2: with Payments→Push at its default-on, a non-muted category
   pushes while a muted one doesn't.)
4. **Security lock (verify #3):** `PUT preferences` for **Account/Push** and for **Messages/InApp** are **rejected**
   (module 400 "locked"), while **Payments/Push** is accepted — so OTP/security push is not user-disableable and In-app
   can't be turned off.

## Verify — provider (on-screen, live)
The provider `SettingsPage` (`/app/settings`, PROVIDER 2 AS) renders the same matrix with correct defaults (In-app
locked-on, Push on, Email off, SMS greyed, master push toggle, tr labels). **Read** verified (GET via the provider BFF
resolves the provider identity). **Write** verified: toggling **Messages → Email** on wrote
`(100011, Messages, Email, Enabled=true)` and the UI reflected the checked cell. (Verification rows were cleaned up
afterward; the table is back to empty → all users at documented defaults.)

## Next
**N-C** — event coverage + region targeting (map each domain event → type → default channels; region job-requests via
the provider city group). The dispatch path now consults preferences before each channel, so N-C events inherit the
per-user gating automatically.
