# REPORT — BE_NF1b owner notification recipient-key alignment

> Implements `docs/V1.0.1/Notification/BE_NF1b_OWNER_RECIPIENT_KEY.md` (Option A: file owner-facing notifications under
> the participant **profile** id). Additive. Solution builds 0 errors; Notification 21/21, ServiceRequest 180/180.
> **Live-verified end-to-end: the owner inbox now shows the offer + history, and web push reaches the owner.**
> **Nothing committed.**

## Root cause (confirmed live, and refined vs the NF1 report)
Two **confounded** issues broke the owner inbox; NF1b + one deploy step fix both:
1. **Stale BFF enum (deploy step).** After NF1 added `NotificationType.ServiceRequestPublished (104)` and `OfferReceived
   (113)`, the owner's rows used those values. `bff-marine-mobile` was not rebuilt, so its Refit client threw
   `JsonException: could not convert … NotificationType … $.body.items[0].type` and the whole inbox call 500'd → empty.
   Rebuilding `bff-marine-mobile` fixed this. **Any BFF that deserializes `NotificationType` (mobile + AdminPanel) must be
   rebuilt when the enum gains values** — the provider BFF is immune only because it passes the type through untyped.
2. **Recipient-key mismatch (this task).** Owner-facing notifications were filed under `SR.OwnerUserId` = the Identity
   **user id** (100029), but the owner mobile inbox resolves the recipient via
   `GetUserNotificationsQueryHandler.ResolveEffectiveRecipientId()` = `KeycloakTokenInfo.ProviderProfileId ??
   UserInfo.UserId`. For a mobile participant on the BffAssertion path, `ProviderProfileId` is the **participant profile
   id** (100030, asserted in `X-Aizen-Provider-Profile-Id`). Proven live: a probe row under 100030 surfaced in the owner
   inbox; a probe under 100029 did not. Device tokens register under the same 100030, but push looked them up by the
   notification's `RecipientUserId` (100029) → miss. So the one mismatch broke **both** inbox and push.

## What changed (code)

### Identity — scoped user-id → participant-profile-id resolver
- New `GetParticipantProfileIdByUserIdQuery` + handler (`Modules/Identity/.../Application/ParticipantLookup/...`) using
  the existing `IUserProfileRepository.GetActiveProfileIdAsync(userId, WorkshopRoleContext.Participant)` → returns the
  profile `Id` (0 if none). New DTO `ParticipantProfileIdDto`.
- Exposed on `QueryController` as `GET /api/v1/identity/participant/profile-id?userId=` with **`[AllowAnonymous]`**.
  - **Deviation from the doc (justified):** the doc said to mirror MO2c's `[Authorize(Policy=IdentityRead)]`
    `IdentityLookupController`. But **notification-api's S2S remote calls carry no bearer token** (verified: the
    remote-call infra attaches no `Authorization`; its existing Identity calls — `providers/for-area`, `admin/user-ids`
    — are `[AllowAnonymous]` on `QueryController`). An `IdentityRead`-protected endpoint would 401/403 from
    notification-api, silently forcing the fallback and defeating the fix. So the new endpoint follows the reachable
    internal-read pattern (anonymous behind the cluster NetworkPolicy, ids only, no PII), consistent with I2/N-C/N-D.

### Notification — file owner-facing notifications under the profile id
- New `INotificationIdentityRemoteCall.GetParticipantProfileIdByUserId(userId)` + mirror DTO `ParticipantProfileIdResult`.
- New `OwnerRecipientResolver.ResolveAsync(...)` helper: resolves `OwnerUserId → participant profile id`, **falling back
  to the user id with a warning** if the resolver returns nothing (never drops the notification).
- Applied in both owner-facing paths: `ServiceRequestPublishedConsumer` (owner "your request is live") and
  `ServiceRequestOfferCreatedConsumer` (owner `OfferReceived`). Provider-facing notifications unchanged (already
  `ProviderProfileId`). Lifecycle-owner notifications (`JOB_STARTED`/`JOB_COMPLETED`) are N-F3.

### Backfill — one-time re-key of existing owner rows
- Migration `20260812120000_NF1bBackfillOwnerRecipientKey` (data-only; auto-applied via `MigrateAsync`, tracked in
  `__EFMigrationsHistory`): re-keys notifications whose `RecipientUserId` matches a `UserProfiles` row with
  `RoleContext = Participant(1)` to that profile's `Id`. **Deliberately an EF migration, not a re-runnable seeder:** a
  profile id can itself be another participant's user id (dense id space — e.g. 100030 is the owner's profile id *and*
  another participant's user id → profile 100031), so a re-run would cascade. A tracked migration runs exactly once.

## Live verification (running stack)
Deployed by rebuilding + recreating `identity-api` and `notification-api` (and `bff-marine-mobile` for issue #1).

1. **Resolver endpoint** (`identity-api:7101`): `userId=100029 → profileId=100030`; `userId=100011` (provider, no
   participant profile) `→ 0`; `userId=0 → 0`. ✔
2. **Backfill:** owner rows moved 100029 → **100030** (13 rows); other participants re-keyed too (10003→11003, 10005→
   11005, 10008→11008). Migration recorded in `__EFMigrationsHistory`. No re-run (tracked). ✔
3. **New owner notifications file under the profile id directly:** published SR 61/62 → owner `ServiceRequestPublished`
   rows 208/210 under **100030** (provider `AreaOpportunity` under 100011, unchanged). No fallback warning logged. ✔
4. **Owner inbox now works:** `GET /api/v1/mobile/notifications` returns **13 items** — including row **201 `OfferReceived`
   (SR 60)**, the NF1 D2 offer, now visible — plus `ServiceRequestPublished`, `OfferAccepted`, `DisputeOpened`,
   `NewMessageReceived` history. (Was 0 before.) ✔
5. **Web push now reaches the owner:** with a WebPush subscription registered under 100030, publishing an SR logged
   `InApp notification dispatched: Id=210 UserId=100030` then `Web push delivery failed for UserId=100030` — i.e. the push
   consumer **found the token under 100030 and attempted delivery** (it "failed" only because the test subscription
   endpoint was synthetic `example.invalid`; a real browser endpoint lands). Before NF1b it looked under 100029 and found
   nothing. ✔ (synthetic token removed afterward)
6. **Provider notifications unaffected:** provider2's rows remain under `ProviderProfileId` 100011. ✔

## Notes / follow-ups
- **Deploy dependency:** rebuilding `bff-marine-mobile` was required for the owner inbox (stale `NotificationType` enum).
  `bff-adminpanel` also deserializes `NotificationType` and should be rebuilt before it can encounter 104/113 rows.
- **0 real WebPush subscriptions:** the owner (and providers) still need to register a real browser/device push
  subscription for push to land in production; the keying is now correct so it will work once they do. FCM dispatch is
  **N-F2**.
- Test artifacts on the dev stack: SR 60/61/62 + their notifications.

## Next
**N-F2** (Email sends + templates; FCM/APNs dispatch on the InApp push path) → **N-F3** (lifecycle parity + deeplinks,
incl. owner `JOB_STARTED`/`JOB_COMPLETED` — file those under the profile id via the same resolver).
