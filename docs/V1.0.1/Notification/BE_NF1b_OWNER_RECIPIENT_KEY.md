# BE_NF1b — owner notification recipient-key alignment (participant profile id)

> **Repo:** `addesso-project` (Notification + a scoped Identity read endpoint). Fix the **pre-existing keying mismatch**
> found while verifying NF1's offer→owner: the owner's D2 row is created, but the **owner mobile inbox shows 0 items**
> (all 13 of the owner's InApp notifications), and **push misses the owner** too. **Decision (Option A):** file
> owner-facing notifications under the owner's **participant profile id** — matching where the owner's inbox + device
> tokens already resolve, and symmetric with the provider (ProviderProfileId). This also makes the owner's **web push +
> FCM** work (the user's original concern) with **no token re-registration**. Additive. **Do not commit.**

## Root cause (confirmed)
- Owner-facing notifications are filed under **`SR.OwnerUserId` = the Identity user id** (e.g. `100029`).
- The owner mobile inbox resolves the recipient via **`ResolveEffectiveRecipientId()` = `ProviderProfileId ??
  UserInfo.UserId`** (`GetUserNotificationsQueryHandler` L104) → for a participant on the BffAssertion path,
  `ProviderProfileId` = the **participant profile id** (e.g. `100030`). → inbox queries `100030`, rows are under
  `100029` → **0**.
- **Device tokens** are registered under the same `ResolveEffectiveRecipientId()` = `100030` (RegisterDeviceToken). But
  `NotificationSentPushConsumer` looks up tokens by the notification's `RecipientUserId` = `100029` → `GetActiveByUserAsync(100029)`
  → no token → **no web push / FCM**. So the **same mismatch breaks both the inbox and push**.
- The **provider** side works because provider notifications + inbox + tokens **all** use `ProviderProfileId`. The owner
  notification-filing is the lone outlier (uses the user id).

## Fix — file owner-facing notifications under the participant profile id
### 1. Identity — resolve participant profile id by user id (scoped read)
Add a **scoped, non-admin Identity read endpoint** (mirror the MO2c `IdentityLookupController` least-privilege
pattern): given a **user id**, return the **participant profile id** (`participant_profiles WHERE UserId = X →
ProfileId`). Add `GetParticipantProfileIdByUserId` (or a small batch variant) to `INotificationIdentityRemoteCall`.
`[Authorize(Policy=IdentityRead)]` — callable by the notification-api service token. Do **not** touch existing admin
endpoints.

### 2. Notification — file owner notifications under the resolved profile id
In every **owner-facing** notification path, resolve `OwnerUserId → participant profile id` before `SendNotification`,
and set `RecipientUserId = participantProfileId`:
- **Offer → owner** (NF1 D2, `ServiceRequestOfferCreatedConsumer`).
- **ServiceRequestPublished → owner** "your request is live" (NF1 D1).
- **Lifecycle owner** notifications (N-F3: `JOB_STARTED`, `JOB_COMPLETED` → owner).
Cache/batch the resolution where a fan-out notifies many; **fallback to `OwnerUserId` + a warning log** if the resolver
returns nothing (don't drop the notification). Provider-facing notifications are unchanged (already ProviderProfileId).

### 3. Backfill the existing owner notifications
Re-key the **13 existing owner InApp notifications** from the user id to the participant profile id (a one-time,
idempotent backfill: for each owner-facing row under a known user id, look up the participant profile id and update
`RecipientUserId`) so the owner's **history** appears in the inbox. Guard against re-runs (skip already-migrated rows).

## Why this fixes push too
Once owner notifications carry `RecipientUserId = 100030` (the profile id) and the owner's device tokens are already
registered under `100030`, `NotificationSentPushConsumer.GetActiveByUserAsync(100030)` finds the WebPush subscription
(and, after N-F2, the FCM token) → the owner's **web push + Firebase push land** — no token re-registration.

## Don't-break / QA
- Additive: a scoped Identity resolver + the notification-filing switch (owner paths only) + a one-time backfill.
  Provider notifications, `SR.OwnerUserId` (load-bearing across the SR module), and the inbox/token resolution are
  **unchanged**. Least-privilege (no admin grant/relaxation).
- Tests: (1) Notification + Identity + solution build 0 errors; (2) the resolver returns the participant profile id for
  an owner user id (and empty → fallback); (3) an offer → the owner-facing row is filed under the **participant profile
  id**, and the owner mobile inbox **now returns it**; (4) the backfill re-keys the 13 rows (idempotent re-run = no-op)
  and the owner sees history; (5) with a WebPush subscription under the profile id, the owner gets **web push** for the
  offer (and FCM once N-F2 lands); (6) provider notifications unaffected.
  Live smoke: make an offer on the owner's SR → owner inbox shows it + web push fires (subscription present).

## Report
`docs/V1.0.1/Notification/REPORT_BE_NF1b.md`: the scoped Identity user-id→profile-id resolver, the owner-notification
filing switch (offer + published + lifecycle-owner), the 13-row backfill, and the live proof that the owner inbox +
push now reach the owner. Note this unblocks the owner half of the whole N-F track. Then **N-F2** (Email + FCM
dispatch) → **N-F3** (lifecycle parity + deeplinks).
