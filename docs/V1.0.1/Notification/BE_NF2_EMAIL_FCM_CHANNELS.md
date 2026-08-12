# BE_NF2 — the missing delivery channels: FCM/APNs push + email

> **Repo:** `addesso-project` (Notification module). With the recipient keying fixed (NF1b), add the two channels the
> user asked for that don't fire today: **FCM/APNs push** (owner's Firebase mobile push) and **email** (provider + owner).
> Both **N-B preference-gated** and **delivery-only** — the inbox (filtered to InApp in NF1 D4) still shows **one**
> logical notification. Requires NF1 + NF1b. **Do not commit.**

## Baseline (investigated)
- Push today: a `Channel=InApp` notification → `NotificationSentMessage` → **`NotificationSentPushConsumer`**, which
  dispatches **WebPush ONLY** (`Platform == WebPush`), N-B Push-gated. FCM/APNs tokens are registered (MO9c) and the
  MO9a **`IFcmSender`** exists, but **nothing dispatches to them** on this path → owner Firebase push never fires.
- Email today: `SendNotificationCommandHandler` gates + dispatches `Channel=Email`, but **the SR/offer/lifecycle types
  have NO Email template** (only `CargoDryRenewalNotificationRequested` does) → a `Channel=Email` send is a **silent
  no-op**. And **no consumer sends `Channel=Email`** for these events anyway.

## Part A — FCM/APNs push dispatch (owner mobile Firebase push)
Extend the InApp→push path to dispatch **Fcm/Apns** tokens too, not just WebPush:
- **Broaden `NotificationSentPushConsumer`** (or add a sibling `NotificationSentFcmConsumer` bound to the same
  `NotificationSentMessage`) to also fetch the recipient's `Platform ∈ {Fcm, Apns}` device tokens and deliver via the
  MO9a **`IFcmSender`** (`SendAsync` per token, or `SendMulticastAsync` for a batch/region fan-out). Carry the
  notification `Title`/`Body` + **`ReferenceType`/`ReferenceId`** in the data payload (for the N-F3 deeplink).
- **N-B Push preference-gated** (reuse the existing Push gate — one gate covers web + mobile push). **Invalid-token
  cleanup** (deactivate on `Unregistered`/`InvalidArgument`, per MO9a). **Sequential / WS2 discipline.**
- **Dev-safe:** if the Firebase service-account secret isn't configured, the MO9a `FcmSenderStub` no-ops (builds/runs
  without creds); real delivery needs the secret (like the MO9a gate).
- Result: **one `Channel=InApp` notification → web push (browser) + FCM push (mobile)**. The owner's Firebase push now
  fires (once the owner's mobile app has registered an Fcm token — MO9c/MO9d).

## Part B — Email channel (provider + owner)
1. **Seed Email templates** (tr) for the types that currently have none — mirror the InApp templates' variables:
   - Provider-facing: `ServiceRequestAreaOpportunity` (new SR in your area), `OfferCreated` (offer submitted),
     `OfferAccepted` (your offer accepted), `AssignmentCreated`/`JobStarted`, `CompletionApproved`/`JobCompleted` as
     applicable to the provider.
   - Owner-facing: `ServiceRequestPublished` (your request is live), `OfferReceived` (you received an offer), and the
     owner lifecycle types (`JOB_STARTED`/`JOB_COMPLETED`) that N-F3 will notify the owner about.
   - **Silent-no-op guard:** every `(Type, Email)` pair that a consumer sends **must** have a seeded template, or the
     email is dropped. Duplicate-seed-safe (idempotent, by TemplateCode).
2. **Consumers also send `Channel=Email`** alongside the InApp send for the owner/provider-facing events — a small
   helper (e.g. `SendInAppAndEmail(...)`) so consumers don't hand-roll two calls, OR a second `SendNotificationCommand`
   with `Channel=Email` per event. The handler **already** N-B Email-gates (opt-in per category) — muted users get no
   email, the InApp row + push still land. Owner-facing emails file under the **participant profile id** (NF1b resolver)
   — same recipient as the InApp row.
3. Email rows are **delivery-only** — the inbox (D4: `Channel==InApp`) does not show them, so the list stays **one**
   logical notification.

## Cross-cutting
- **One logical notification, N channels:** InApp = the list row; web/FCM push + email = deliveries. (D4 already
  filters the inbox.)
- **Preference-gated everywhere:** InApp always-on baseline; Push (web + FCM) via the N-B Push gate; Email via the N-B
  Email gate. A user who muted a channel gets no delivery on it.
- **Region targeting** (SR-published → area providers) unchanged; each area provider now gets in-app + web push + email
  (+ FCM if they have a mobile token), all gated.

## Don't-break / QA
- Additive: extend the push consumer for FCM/APNs + seed Email templates + consumers send Email. In-app + web push +
  the keying (NF1b) unchanged. Provider notifications unchanged in recipient.
- Tests: (1) Notification + solution build 0 errors; (2) an InApp notification with an Fcm token → `IFcmSender` invoked
  (stub in dev), N-B Push-gated (muted → skip), invalid token deactivated; (3) each seeded `(Type, Email)` template
  resolves + interpolates; (4) a consumer event → an InApp row (list) + an Email row (not in list) + push attempted;
  Email muted → no Email row, InApp + push still fire; (5) the inbox shows one row per event (no email/push duplication);
  (6) owner-facing Email files under the participant profile id. Live smoke: offer → owner in-app + email + (FCM if
  token) + (web push if subscription); provider new-SR-in-area → in-app + email + web push.

## Report
`docs/V1.0.1/Notification/REPORT_BE_NF2.md`: the FCM/APNs dispatch extension (owner Firebase push), the seeded Email
templates + consumer email sends (provider + owner, preference-gated), the one-logical-notification confirmation, and
the live proof of multi-channel delivery. Note the Firebase-secret gate (real FCM) + the WebPush-subscription
prerequisite. Then **N-F3** (lifecycle parity: accept→provider, start→owner, complete→owner across all channels +
deeplinks: push payload `ReferenceType/Id` → FE routes to the offer/request detail).
