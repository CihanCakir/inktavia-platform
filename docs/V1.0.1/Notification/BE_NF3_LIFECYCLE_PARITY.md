# BE_NF3 — lifecycle notification parity (OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED) + owner keying alignment

> **Repo:** `addesso-project` (Notification module). Complete the user's requirement: the lifecycle events
> `OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED` notify the **right party** on **all channels** (in-app + web/FCM push +
> email), **preference-gated**, with owner-facing ones keyed by the participant profile id (NF1b). Also align the
> remaining owner-facing lifecycle consumers that still notify by the raw user id, single-channel. Requires
> NF1/NF1b/NF2. FE deeplink routing is **N-F3b** (the push payload already carries the ref). **Do not commit.**

## Findings (investigated)
| Event | Consumer | Today | Correct |
|---|---|---|---|
| **OFFER_ACCEPTED** | `ServiceRequestOfferAcceptedConsumer` | notifies **owner** by **raw `OwnerUserId`**, **InApp only** | should notify the **provider** ("your offer was accepted") — `ProviderProfileId` (on `ServiceRequestOfferAcceptedMessage`), **multi-channel**; optional owner confirmation (resolved) |
| **JOB_STARTED** | **none** — no consumer for `ServiceRequestAssignmentStartedMessage` (WC1 added the event) | **owner never notified** | **add** a consumer → notify the **owner** (NF1b-resolved profile id), **multi-channel** |
| **JOB_COMPLETED** | `ServiceRequestCompletionApprovedConsumer` | **provider** (`recipientId`, D5-fixed) | ✅ party + key; **verify multi-channel** (SendInAppAndEmail) |
| CompletionSubmitted | `ServiceRequestCompletionSubmittedConsumer` | **owner** by **raw `OwnerUserId`**, InApp only | owner (resolved) + multi-channel |
| AutoApproveApproaching | `...CompletionAutoApproveApproachingConsumer` | **owner** raw id, InApp | owner (resolved) + multi-channel |
| MaintenanceReminderDue | `MaintenanceReminderDueConsumer` | **owner** raw id, InApp | owner (resolved) + multi-channel |

## BE — N-F3

### 1. OFFER_ACCEPTED → provider (fix party + key + channels)
`ServiceRequestOfferAcceptedConsumer`: notify the **provider** — `RecipientUserId = ProviderProfileId` (on the message)
— with a **provider-facing** OfferAccepted ("your offer was accepted"), **multi-channel** (`SendInAppAndEmailAsync` +
the push path). If a distinct **owner** confirmation is wanted ("you accepted an offer"), send it too, **NF1b-resolved**
+ multi-channel — but the primary, meaningful notification is the provider's. Seed the provider-facing (Type, InApp)
+ (Type, Email) templates (silent-no-op guard).

### 2. JOB_STARTED → owner (new consumer)
**Add a Notification consumer for `ServiceRequestAssignmentStartedMessage`** (WC1's event, currently un-consumed by
Notification) → notify the **owner** ("the provider started your job"), **NF1b-resolved** (participant profile id),
**multi-channel**. Add the owner-facing notification type + (InApp, Email) templates (tr). ReferenceType/Id → the SR/
job detail (for N-F3b deeplink).

### 3. JOB_COMPLETED → provider (verify)
`ServiceRequestCompletionApprovedConsumer` already notifies the provider by the D5-fixed `ProviderProfileId`. **Confirm
it is multi-channel** (goes through `SendInAppAndEmailAsync` + push) and Email-templated; add the Email template if
missing.

### 4. Align the remaining owner-facing lifecycle consumers (the NF2-flagged "raw user id" events)
`ServiceRequestCompletionSubmittedConsumer`, `ServiceRequestCompletionAutoApproveApproachingConsumer`,
`MaintenanceReminderDueConsumer`: change `RecipientUserId = message.OwnerUserId` → the **NF1b `OwnerRecipientResolver`**
(→ participant profile id, fallback to user id + warn) and route through **`SendInAppAndEmailAsync`** (multi-channel).
Seed the missing (Type, Email) templates. Now these reach the owner's inbox + push + email (they were invisible under
the raw user id, per NF1b).

## Cross-cutting (unchanged invariants)
- **Preference-gated** (N-B): InApp baseline; Push (web + FCM) via the Push gate; Email via the Email gate.
- **One logical notification** per event (D4 inbox = InApp row; push + email are deliveries).
- **Owner-facing → participant profile id** (NF1b resolver) so inbox + push + email all reach the owner; provider-facing
  → `ProviderProfileId`.
- **Silent-no-op guard:** every `(Type, Channel)` a consumer sends needs a seeded template.

## Don't-break / QA
- Additive: fix the OFFER_ACCEPTED party, add the JOB_STARTED consumer, align the 3 raw-user-id owner consumers, verify
  JOB_COMPLETED, seed the missing templates. Provider-keyed consumers + the NF1b/NF2 machinery unchanged.
- Tests: (1) Notification + solution build 0 errors; (2) **accept an offer → the provider** gets in-app + push + email
  (not just the owner); (3) **start a job → the owner** gets in-app + push + email (new consumer fires, resolved key);
  (4) **approve completion → the provider** gets multi-channel; (5) CompletionSubmitted / AutoApprove / Maintenance now
  reach the **owner's inbox** (resolved key) + email; (6) every event = one inbox row, preference-gated (mute → skip
  that channel); (7) each new (Type, Email/InApp) template resolves. Live smoke: run accept → start → complete and
  confirm the right party gets each on the right channels.

## Report
`docs/V1.0.1/Notification/REPORT_BE_NF3.md`: the OFFER_ACCEPTED party/key/channel fix, the new JOB_STARTED owner
consumer, the JOB_COMPLETED verification, the 3 owner-consumer keying alignments, the seeded templates, and the live
proof of per-party multi-channel lifecycle notifications. Then **N-F3b (FE deeplinks):** provider-web push click →
offers page → offer detail; owner mobile push tap → offer/SR detail — using the `ReferenceType/ReferenceId` the push
payload already carries. **This closes the N-F multi-channel notification track.** Remaining ops (from NF1b/NF2):
contact-email PII endpoint hardening, bff-adminpanel rebuild, real WebPush/FCM subscriptions (client).
