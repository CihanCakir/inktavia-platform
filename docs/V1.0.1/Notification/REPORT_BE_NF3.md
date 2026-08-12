# REPORT — BE_NF3 lifecycle notification parity

> Implements `docs/V1.0.1/Notification/BE_NF3_LIFECYCLE_PARITY.md`. Requires NF1/NF1b/NF2. Additive, preference-gated,
> per-party multi-channel (InApp + web/FCM push + email), owner-facing keyed by the participant profile id (NF1b).
> Solution builds 0 errors; Notification 21/21, ServiceRequest 180/180. **JOB_STARTED + JOB_COMPLETED live-verified;
> OFFER_ACCEPTED code-verified (live accept blocked by a pre-existing economics gate — see below). Nothing committed.**

## What changed (code)

### 1. OFFER_ACCEPTED → provider (party + key + channels fixed)
`ServiceRequestOfferAcceptedConsumer` previously notified the **owner** by the raw `OwnerUserId`, InApp only — but the
event ("your offer was accepted") is **provider-facing**. Now notifies the **provider** by `ProviderProfileId` (on the
message), multi-channel (`SendInAppAndEmailAsync` + the push path). Seeded `SR_OFFER_ACCEPTED_EMAIL` (the InApp template
already existed). The optional owner "you accepted an offer" confirmation is deferred (the meaningful one is the
provider's).

### 2. JOB_STARTED → owner (new consumer)
- Added `OwnerUserId` to `ServiceRequestAssignmentStartedMessage` (additive; the WC1 event had no owner id), populated at
  `StartServiceRequestAssignmentCommandHandler` (`sr.OwnerUserId`).
- New `NotificationType.JobStarted = 123` + `SR_JOB_STARTED_INAPP` / `SR_JOB_STARTED_EMAIL` templates (tr).
- New `ServiceRequestAssignmentStartedConsumer` (auto-registered by the bus scan) → notifies the **owner**
  (NF1b-resolved participant profile id), multi-channel, `ReferenceType/Id` → the SR (N-F3b deeplink).

### 3. JOB_COMPLETED → provider (verified)
`ServiceRequestCompletionApprovedConsumer` already keys by the D5-fixed `ProviderProfileId` and (NF2) goes multi-channel
via `SendInAppAndEmailAsync` with the `SR_COMPLETION_APPROVED_EMAIL` template. Confirmed — no code change needed.

### 4. Aligned the 3 remaining raw-user-id owner consumers
`ServiceRequestCompletionSubmittedConsumer`, `ServiceRequestCompletionAutoApproveApproachingConsumer`,
`MaintenanceReminderDueConsumer`: switched `RecipientUserId = OwnerUserId` → the NF1b `OwnerRecipientResolver`
(→ participant profile id, fallback to user id + warn) and routed through `SendInAppAndEmailAsync` (multi-channel).
Seeded the missing Email templates (`SR_COMPLETION_SUBMITTED_EMAIL`, `SR_COMPLETION_AUTOAPPROVE_APPROACHING_EMAIL`,
`SR_MAINTENANCE_REMINDER_DUE_EMAIL`). These were invisible to the owner inbox before (filed under the raw user id, per
NF1b); now they reach the owner's inbox + push + email.

## Live verification (running stack)
Deployed service-request-api + notification-api + **bff-marine-mobile** (rebuilt for the new `JobStarted=123` enum — the
NF1b lesson: BFFs that deserialize `NotificationType` must be rebuilt on an enum add). Senders are the dev-safe stubs
(FCM + email); VAPID real. Test setup on SR 9011 (owner 100029/profile 100030, provider2 profile 100011, assignment
91001): Email opt-in enabled + owner Fcm token + provider WebPush token (synthetic, removed after). New templates seeded;
the `ServiceRequestAssignmentStarted` consumer registered on the bus.

- **JOB_STARTED — PASS.** provider2 started assignment 91001 → owner rows `123` InApp(223)+Email(224); logs
  `[FCM STUB] … → Mobile push (Fcm) sent to UserId=100030`, `[EMAIL STUB] Email to qa.owner.aug5@…`,
  `JobStarted SR=9011 Assignment=91001 → notified owner 100030`. Owner gets InApp + FCM + email, resolved to the profile
  id. ✔
- **JOB_COMPLETED — PASS.** owner approved a completion on SR 9011 → provider rows `131` InApp(225)+Email(226); logs
  `CompletionApproved … → notified provider 100011`, `[EMAIL STUB] Email to provider2@…`, `Web push … UserId=100011`.
  Provider gets InApp + email + web push, keyed by `ProviderProfileId`. ✔ *(the completion was seeded directly as a DB
  fixture because the real submit requires an evidence file upload — the approve→CompletionApproved→consumer path is the
  real, deployed code.)*
- **One row per event, gated:** owner/provider inboxes show only the InApp rows; the Email rows are delivery-only (D4).
  (NF2 also live-verified the mute→skip gate.)

### OFFER_ACCEPTED — code-verified; live accept blocked by a pre-existing economics gate
The consumer fix is deployed, builds clean, the `(OfferAccepted, InApp/Email)` templates are seeded, and the message
carries `ProviderProfileId` — and the consumer is **structurally identical** to the JOB_COMPLETED consumer that fired
correctly to the provider (same `SendInAppAndEmailAsync`, same `ProviderProfileId` routing). **I could not live-trigger a
real accept:** every offer acceptance is currently **rejected by §19.2 profit-protection** — *"No safe combination of
advantages meets the minimum contributions: provider −2.14/0.00"* — a **structural** block (the provider's contribution
is negative for the current commission/VAT/cost-share config, independent of price; tried 1 200, 10 000, and a
commission-exempt pass-through item — all rejected identically). This is a **pre-existing economics-config issue**
(documented in `FIX_PRODUCTION_ECONOMICS_VALUES`), not NF3. Relaxing the profit-protection policy to force the accept was
**declined** (it mutates shared financial config to bypass a business gate — out of this task's scope). **To live-prove
OFFER_ACCEPTED, the economics config needs a provider-margin fix first** (owner decision), or a temporary policy relax in
a throwaway env.

### The 3 aligned owner consumers (CompletionSubmitted / AutoApprove / Maintenance)
Not separately live-triggered (evidence-upload / scheduled-job gated), but each uses the **exact** pattern as the
live-verified JOB_STARTED consumer (`OwnerRecipientResolver` + `SendInAppAndEmailAsync`) — build- and template-verified.

## Notes / follow-ups (carried)
- **Real** FCM/email need the Firebase/SMTP secrets; real device push needs genuine tokens. Contact-email endpoint PII
  hardening + `bff-adminpanel` rebuild (from NF1b/NF2) still open.
- Test artifacts on the dev stack: SR 63/64/65/66; SR 9011 driven start→complete (now Completed) as the JOB probe; notif
  rows 223–234.

## Next
**N-F3b (FE deeplinks):** provider-web push click → offers/offer detail; owner mobile push tap → SR/offer detail — using
the `ReferenceType/ReferenceId` the push payload already carries. This closes the N-F multi-channel notification track.
