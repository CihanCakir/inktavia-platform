# Notification Delivery + Live Support + Reject/Cancel Reasons — Research & Roadmap

> Research-first roadmap for: **web push (permissioned)**, **per-user notification preferences**, **event→channel
> coverage** (region job-requests, user message, payment received/in-provision…), **live support channel** (provider &
> participant → admin, by topic/reason), and a **structured reject/cancel reason taxonomy**. Plus one immediate bug
> (admin attachment 500). Underpins the existing `Notification/ROADMAP.md` N1–N4 (those are specific notification *types*;
> this is the *delivery platform* + support + reasons). Phases become individual kickoffs after sign-off.

## Current state (investigated)
- **Web push — ~70% scaffolded, not proven end-to-end.** Backend has `NotificationChannel {InApp,Push,Email,Sms}`,
  `PushPlatform.WebPush`, `UserDeviceTokenEntity`+repo, `PushSubscription` request/response/keys,
  `VapidPublicKeyResponse`, a **concrete `WebPushSender.cs`**, `NotificationsController` push endpoints, migration
  `AddWebPushSubscriptionFields`. **Gaps:** VAPID keys not in config (needs env/appsettings); unclear that
  notification-create actually invokes `WebPushSender` per channel; **admin-web has no push-subscription** (provider has
  `usePushSubscription.ts`); and the `NotificationsController` was mis-routed (`api/v1/notifications` vs admin-web
  `/api/v1/admin-panel`) — **just fixed** in messaging-completion, so push endpoints were 404 before.
- **Notification preferences — DO NOT EXIST.** No per-user × type × channel preference entity. "Provider tanımlamaları"
  (choose which events → which channel) must be built.
- **Event coverage — the `NotificationType` enum is rich:** SR 100–101, Offer 110–112, Assignment 120–122, Completion
  130–132, Dispute 140–141, **Payment 150–156 (Released/Captured/Cancelled/Refunded/ReminderDue/PayoutCompleted/Failed)**,
  `NewMessageReceived 200`, CargoDry 300s, Profile 400s, `AdminBroadcast 900`. Most requested events exist; **"Ödeme
  Provizyonda" (PreAuth/authorization-hold)** has no type yet (P9 auth-mode); **"bölgede açılan iş talebi"** =
  `ServiceRequestCreated` targeted to providers in the region (reuse the provider realtime **city group**).
- **Attachment 500 (admin) — root cause found:** BFF `IAdminMessagingBffRemoteCall.GetAttachmentUploadUrlAsync` returns
  **`Task<object>`** while the module returns `AizenApiResponse<RequestAttachmentUploadUrlResponse?>` — the **same
  wrapped-vs-bare envelope mismatch** that 500'd the reports. FileStorage/upload plumbing is fine; the BFF just can't
  deserialize the envelope.
- **Reject/cancel reasons — SR is FREE TEXT, Payment is structured.** `ServiceRequestOfferEntity.RejectionReason`,
  `ServiceRequestAssignmentEntity.RejectionReason`/`CancellationReason` = free-text strings (maxlen 1000). Payment
  `RefundReason` enum is structured (UserCancel/ServiceNotDelivered/MutualAgreement/OrganizerCancel/ProviderFailedToDeliver/
  SystemError/ServiceRequestCancelled/DuplicateCharge/DisputeResolvedForPayer) and P10 already maps `RefundCauseMap ↔
  RefundReason`. So a structured SR reason taxonomy is missing and should **auto-map to the Payment refund cause**.
- **Live support — no dedicated concept yet, but the rails exist.** Messaging `MessagingContextType` already has
  `CargoDrySupport`, `VenueInquiry`, `DirectMessage`. A support request = a Messaging conversation of a Support context +
  a subject/reason; the admin Communication Audit already observes+intervenes. Need a support ContextType + reason
  category + an admin **support queue** surface (grouped by topic/reason) + a provider/participant "connect to live
  support" entry point.

## N0 — immediate fix: admin attachment 500 (do first, tiny)
Type the BFF attachment-upload-url remote call: `Task<object>` → `Task<AizenApiResponse<RequestAttachmentUploadUrlResponse>>`
(+ unwrap in the BFF handler), mirroring the reports envelope fix. Verify admin image/document upload → 200 → renders.

## N-A — web push, permissioned, end-to-end
1. **VAPID config** (public/private keys via env/secret; expose public key via the existing `VapidPublicKeyResponse`
   endpoint — served through the BFF).
2. **Verify/wire delivery:** on notification-create, if the recipient's channel includes Push and they have a device
   token → `WebPushSender` sends (VAPID). Confirm the concrete sender actually runs (not stubbed).
3. **Admin subscribe UI** (permission prompt → subscribe → store subscription) — provider already has
   `usePushSubscription`; verify it end-to-end and add the admin equivalent. Service worker for receiving pushes.
4. **Endpoints via BFF** (subscribe/unsubscribe/vapid-key/register-token) — reachable at the admin-panel base (the route
   fix landed; verify).
Depends on: the routing fix (done). Gate delivery by N-B preferences.

## N-B — notification preferences ("provider/participant tanımlamaları")
New per-user preference model: **user × NotificationType (or category) × channel {InApp, Push, Email, Sms}**, with
sensible defaults (InApp on; Push opt-in). Provider + admin UI to toggle. The dispatch path consults preferences before
sending each channel (in-app always persists; push only if opted-in + subscribed). Depends on N-A (push) for the push
toggle to mean something.

## N-C — event coverage + region targeting (the requested triggers)
Map each domain event → `NotificationType` → default channels, and wire the specific asks:
- **"Bölgesinde açılan iş talepleri"** → `ServiceRequestCreated` targeted to providers whose service-area/city matches
  (reuse the provider realtime **city group** + Identity I2 service-area when available; MVP = city).
- **"Kullanıcı Mesajı"** → `NewMessageReceived` (already fires; ensure push channel honored).
- **"Ödeme Alındı"** → `PaymentCaptured (151)`; **"Ödeme Provizyonda"** → **add** a `PaymentAuthorized/PreAuthHeld` type
  (P9 auth-mode) if PreAuth is used; PayoutCompleted/Refunded/Failed already exist.
Admin-sent notifications (`AdminBroadcast 900`) also respect channel/preferences. Depends on N-A + N-B.

## N-D — live support channel (provider & participant → admin, by topic/reason)
- **Support ContextType + reason/subject:** a support request = a Messaging conversation with a Support context and a
  **reason category** (e.g. Payment, ServiceRequest, Account, Technical, Other) + free subject. Provider & participant
  get a **"Canlı desteğe bağlan"** entry that opens/opens-to such a conversation.
- **Admin support queue surface:** in the admin panel, a view (sibling to Communication Audit) listing **open support
  requests grouped by topic/reason**, with unread/priority, so admin doesn't miss customers — reuse the messaging
  observe/intervene + unread/notify machinery just completed.
- Reuses: Messaging conversations, admin live socket, unread counters, notifications (N-A/B/C). Depends on the messaging
  infra (done) + notifications.

## N-E — structured reject/cancel reason taxonomy (SR + Payment, auto-linking)
- Replace SR free-text `RejectionReason`/`CancellationReason` with a **structured reason** (enum or ReferenceData
  lookup) for: offer reject, assignment reject, service-request cancel — each with an optional free-text note.
- **Auto-map** the chosen SR reason → Payment `RefundReason`/`RefundCause` (extend the P10 `RefundCauseMap`) so
  cancel/reject deterministically drives the correct refund allocation **and** the right notification (N-C) + dispute
  context (S13).
- UI: reason picker on the reject/cancel actions (provider + admin + customer surface where applicable). Depends on
  Payment P10 (done) + SR.

## Suggested sequencing
**N0 (bug) → N-A (web push) → N-B (preferences) → N-C (event coverage) → N-D (support channel) → N-E (reasons).**
N0 is immediate. N-A/N-B are the delivery platform (do together). N-C wires the specific triggers. N-D and N-E are
larger features that build on the platform; N-D reuses the just-finished messaging infra, N-E touches SR+Payment.
Production note: push needs VAPID secrets; "Ödeme Provizyonda" depends on P9 PreAuth mode; region targeting is richer
once Identity I2 (service-area) lands (city-level MVP meanwhile).

## Next
After sign-off, each phase → its own `*_KICKOFF.md`. Recommend starting with **N0** (unblocks your attachment 500 today)
then **N-A + N-B** (the web-push platform you asked for).
