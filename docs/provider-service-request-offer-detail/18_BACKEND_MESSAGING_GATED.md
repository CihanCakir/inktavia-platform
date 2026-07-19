# 18 — Backend: offer-gated messaging (anti-harassment) + provider realtime

Messaging must **not** be a cold-DM channel. The rule (from the product owner):

> A provider cannot free-text a customer out of nowhere. The **offer itself** is delivered to the customer as a
> message. Only **after the customer replies** does the free-text channel open on the provider side. The detail
> page shows the **past conversation (read-only)**; active back-and-forth happens in the **Messages menu**.

So the core is a **gate**, not a chat feature. Most of the plumbing already exists; the gate and the realtime path
do not. This prompt adds them.

## Verified in source (2026-07-16) — what exists, what's missing

Exists:
- `ServiceRequestMessageEntity` (SR-scoped): `SenderType {Owner,Provider,Admin,System}`,
  `MessageType {Text,SystemNotification,StatusChange}`, `IsRead/ReadAt`, `AttachmentFileId`.
- Commands/queries: `SendServiceRequestMessage`, `MarkServiceRequestMessagesRead`, `GetServiceRequestMessages`;
  `IServiceRequestMessageRepository`.
- Integration event contract `ServiceRequestMessageSentMessage { ServiceRequestId, MessageId, SenderUserId, SenderType }`.

Missing / wrong (this prompt fixes):
- **No gate.** `SendServiceRequestMessageCommandHandler` lets anyone with the command post free text — no
  anti-harassment rule at all.
- **`ServiceRequestMessageSentMessage` is never published.** The send handler only calls the module realtime
  publisher, and it passes `providerProfileId: null`, so **nothing reaches a provider**. There is no provider BFF
  consumer and no provider `providerEvent` for messages.
- No provider-scoped BFF endpoints for get/send.

## The gate (the one invariant that must not be got wrong)

Per service-request conversation, define **channelOpen** (derived, not stored):

> `channelOpen == the conversation contains at least one message with SenderType == Owner`

Rules:
- **Provider → free text (`MessageType.Text`)**: allowed **only if `channelOpen`**. Otherwise reject
  `SR_MSG_CHANNEL_LOCKED`. This is the anti-harassment rule — a provider can never initiate.
- **Owner (customer)**: never gated. The customer may always message about their own request/offer. The customer's
  first message is exactly what flips `channelOpen` to true.
- **Admin / System**: not gated.
- The **offer message** below is `SenderType=Provider, MessageType=Offer` but is **system-generated on submit**,
  not free text — it is exempt from the gate (it is the sanctioned first contact).

Enforce this in `SendServiceRequestMessageCommandHandler` (or a small domain guard), server-side. The BFF must not
be the only place it lives.

## Offer-as-message

Add `MessageType.Offer = 4`. When a provider **submits** an offer (the existing SubmitOffer path, 10d), create one
`ServiceRequestMessageEntity { SenderType=Provider, MessageType=Offer, Content = <offer ref/summary, NOT free
text>, }` in the conversation. This is "the offer drops to the customer as a message." It does **not** open the
channel for the provider (only an Owner message does).

> **DECISION — LOCKED: create the offer-message on SUBMIT.** (Owner confirmed 2026-07-16.) The offer is *delivered*
> the moment it is sent, so the offer-message is created in the SubmitOffer path; it sits in the customer's inbox
> whether or not they have opened it yet. The customer's later *view* is a separate read-receipt tracked by #15
> `OfferViewedByCustomer` — do **not** couple the offer-message to the view command.

Idempotent: exactly one offer-message per offer submission (guard on offer id / a dedup key) — a resubmit after a
revision (see #15 Option A) creates a new offer-message for the new version; earlier ones stay as history.

## Provider realtime (finally wire it)

- On **any** message send, publish the RabbitMQ `ServiceRequestMessageSentMessage` **and include the
  `ProviderProfileId`** of the conversation's provider so the BFF can address the group. (Add `ProviderProfileId`
  to the message contract; resolve it from the offer/assignment on the SR.)
- Add provider BFF consumer `MessageAddedRealtimeConsumer : AizenBaseMessageConsumer<ServiceRequestMessageSentMessage>`
  (copy of `OfferAcceptedRealtimeConsumer`) → `provider:{ProviderProfileId}` →
  `SendAsync("providerEvent", ProviderRealtimeEvent { EventType = "MessageAdded", ServiceRequestId })`.
  Add `MessageAdded` to `ProviderRealtimeEventTypes`.
- **Do not** put message content on the realtime frame (it is free text — same rule as #15). The SPA refetches the
  thread. Also: only notify the provider for **Owner→provider** messages (a provider's own send shouldn't toast
  themselves); the consumer/publisher should target the *counterparty*.

## Provider BFF endpoints (provider-scoped, access-checked like detail)

- `GET /provider/service-requests/{id}/messages` → `{ items: [...], channelOpen: bool }`. Access-check the
  provider's relationship to the SR (reuse the detail access check). Returns the conversation history + the gate
  state so the SPA knows whether to show a composer.
- `POST /provider/service-requests/{id}/messages` → send free text. BFF sets `SenderType=Provider`; the **module**
  enforces the gate and rejects `SR_MSG_CHANNEL_LOCKED` when the customer hasn't replied. `MarkRead` on open.

## Customer/mobile side (the ~2-week wiring — contract only here)

- Owner send + read via their own BFF → module `SendServiceRequestMessage (SenderType=Owner)` (ungated). The
  **first** owner message flips `channelOpen` and (via §realtime) toasts the provider "MessageAdded".
- Owner viewing an offer stays #15 `OfferViewedByCustomer` (read-receipt), separate from messaging.

## Provider frontend (I will do after this lands)
- **Detail page** "Mesajlar & Aktivite" panel: **read-only** recent conversation history. If `channelOpen` →
  "Mesajlara devam et" link to the Messages section. If locked → muted note "Müşteri yanıtladığında mesajlaşma
  açılır." **No composer on the detail page.**
- **Messages menu section** (`/app/messages`, today YAKINDA): full thread + composer, composer enabled only when
  `channelOpen`.

## Acceptance — observed
- Provider POST message while no Owner message exists → **rejected `SR_MSG_CHANNEL_LOCKED`**.
- Owner sends a message → provider gets a `MessageAdded` `providerEvent` (content NOT on the frame); `channelOpen`
  now true; provider POST now **succeeds**.
- Offer submit creates exactly one `MessageType=Offer` message (idempotent per submission); it does **not** open the
  channel for the provider.
- `GET …/messages` returns history + correct `channelOpen`, only for a provider related to the SR (others rejected).
- No message content on any realtime frame; provider isn't toasted for their own sends.

## Report
Append to `REPORT_BACKEND.md` ("18"): the locked→open transition (both POST results), the offer-message row, the
`MessageAdded` frame (content-free), and the access-scoping of GET. Note the submit-vs-view decision taken.
Unfinished is **not done**.
