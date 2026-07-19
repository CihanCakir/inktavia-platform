# 20 — Backend: Messages page (inbox list + rich content + lifecycle system messages)

The Messages full page needs three things the current messaging (#18) does not provide. Run in phases; do **not**
one-shot. Everything is provider-scoped and keeps the #18 anti-harassment gate (`channelOpen`).

Recap of what #18 already gives (reuse, don't rebuild): per-request `GET/POST /provider/service-requests/{id}/messages`
with `channelOpen`; `ServiceRequestMessageEntity {SenderType, MessageType(Text/SystemNotification/StatusChange/Offer),
Content, IsRead, ReadAt, AttachmentFileId, CreatedAt}`; realtime `MessageAdded`; offer-message auto-created on submit;
`IServiceRequestMessageRepository.GetUnreadCountAsync`.

---

## Phase 20a — Conversation list (inbox / left pane)

There is **no** "my conversations" endpoint for the provider. Add one.

### Module query `GetProviderConversationList` (provider-scoped)
Returns, for each service request the calling provider has a **relationship** with (has an offer OR has ≥1 message),
one row:
```
long ServiceRequestId
string RequestCode
string Title
string? LastMessagePreview      // last Text content trimmed; for an Offer message → null (SPA shows "Teklif gönderildi")
int    LastMessageType           // so the SPA can render "Teklif gönderildi" / image / location previews
DateTime? LastMessageAt
int    UnreadCount               // GetUnreadCountAsync for this provider
bool   ChannelOpen               // has ≥1 Owner message
string? LifecycleStatus          // latest lifecycle code (20c): Offered | Accepted | Started | Completed | Closed | null
```
Ordered by `LastMessageAt` desc (nulls last). Provider id from the assertion
(`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`). **Only the caller's own** conversations —
never another provider's. Efficient: one grouped query over messages joined to the provider's requests; avoid N+1
(compute unread + last message in the projection, not per-row round trips).

### BFF `GET /api/v1/provider/conversations`
Provider-scoped (`ProviderActive` policy), assertion identity, passthrough to the module. Response = list above.

### Acceptance
- Provider2 → the requests they've bid on / messaged, each with correct unread + last preview + channelOpen + lifecycle.
- Provider1 sees none of provider2's. No customer name anywhere (privacy) — only request title/code.

---

## Phase 20b — Rich message content: image (display) + location (send, MVP)

Per the recorded decision: **location is MVP** (client-side maps, JSON coords); **provider image UPLOAD is
post-MVP**, but **image DISPLAY** (customer-sent / seeded) must work now.

### Enum
Add `MessageType.Image = 5`, `MessageType.Location = 6`.

### Image (display now)
- An image message carries `AttachmentFileId` (already on the entity) + `MessageType.Image`.
- Add BFF `GET /provider/service-requests/{id}/messages/{messageId}/image-url` → short-lived signed read URL,
  **access-scoped exactly like 14c**: the provider must be a party to the conversation AND the messageId must belong
  to that request AND carry an attachment. Mint per click, 5-min TTL, no object key leaked. (Reuse the 14c
  FileStorage `CreateReadUrl` + the conversation access check.)
- Provider image **upload** is out of scope here (post-MVP) — the composer's attach affordance can exist but the
  send path for images is a later phase. Do not implement provider image upload now.

### Location (send + display, MVP)
- Add nullable `decimal? LocationLat`, `decimal? LocationLng`, `string? LocationLabel` to `ServiceRequestMessageEntity`
  (+ EF config + migration **with Designer**).
- Sending a location: extend the existing `POST …/messages` request with optional `LocationLat/Lng/Label`; when
  present, create a `MessageType.Location` message (content may hold the label). **Gate still applies** — provider can
  only send when `channelOpen`; customer never gated.
- No server geocoding. The SPA renders a pin + "Konumu Aç" that opens the coords in the device's maps app
  (client-side). Coordinates travel as decimals, never in a URL query the server builds.

### Acceptance
- A seeded image message returns a working signed URL via the new endpoint; wrong message/id or foreign conversation
  → "not found" (same as 14c). No object key leaks.
- Provider sends a location while `channelOpen` → `MessageType.Location` row with lat/lng; while locked → rejected
  `SR_MSG_CHANNEL_LOCKED`.

---

## Phase 20c — Lifecycle system messages (shown provider-side in the thread)

Auto-create a **system** message in the conversation on each lifecycle event, so the thread shows the timeline the
provider expects. Use `MessageType.StatusChange`, `SenderType.System`, and put a **machine code** in `Content`
(the SPA maps it to Turkish; codes stay codes):

| Event | Trigger handler (already exists) | Content code | Provider-side label (SPA) |
|-------|----------------------------------|--------------|---------------------------|
| Teklif gönderildi | SubmitOffer (done in #18 as `MessageType.Offer`) | — | (already) |
| Teklif kabul edildi | `AcceptServiceRequestOfferCommandHandler` | `OFFER_ACCEPTED` | "Teklif kabul edildi" |
| Anlaşılan iş başlatıldı | `StartServiceRequestAssignmentCommandHandler` | `JOB_STARTED` | "Anlaşılan iş başlatıldı" |
| Anlaşılan iş tamamlandı | `ApproveServiceRequestCompletionCommandHandler` (owner approves) | `JOB_COMPLETED` | "Anlaşılan iş tamamlandı" |
| Konuşma kapandı | request → Closed/Cancelled/Expired terminal | `CONVERSATION_CLOSED` | "Konuşma kapandı" |

Rules:
- **Idempotent** per (serviceRequestId, code) — a re-run or retry must not duplicate the row.
- These are `SenderType.System` — they are **not** free text and are **exempt from the gate** (they don't open the
  channel for the provider; only an Owner message does).
- They feed 20a's `LifecycleStatus` (latest code wins) and render as **centered pills** in the thread.
- Emit the realtime `MessageAdded` for them too (so the provider's open thread/list refreshes), content-free frame.
- `CONVERSATION_CLOSED` also flips the conversation to a **read-only** state on the provider side (composer hidden) —
  expose this via 20a (`LifecycleStatus == "Closed"`) and the per-thread response so the SPA can lock the composer.

### Acceptance
- Accept an offer → one `OFFER_ACCEPTED` system message appears in that conversation (idempotent). Start job →
  `JOB_STARTED`. Approve completion → `JOB_COMPLETED`. Close/cancel the request → `CONVERSATION_CLOSED` and the
  conversation reads closed. Each emits a content-free `MessageAdded`.

---

## Phase 20d — Seed (so the page has something to show)

On a biddable request provider2 can see, seed a realistic conversation (idempotent, fixed ids):
- Owner text message (opens the channel), a provider text reply, the offer message, an owner **image** message
  (reuse the 14c seeded file or a new FileStorage seed), an owner **location** message (Çeşme-ish coords), and one
  lifecycle system message (`OFFER_ACCEPTED`). Set unread so the inbox shows a badge.

### Acceptance
- `GET /provider/conversations` shows this conversation with the right preview/unread/lifecycle; the thread renders
  text + offer card + image (via image-url) + location + the system pill.

---

## Constraints (all phases)
- Provider-scoped everywhere; ownership enforced in the module, not just the BFF. No client-sent profile id.
- Keep the #18 gate: provider free-text/location only when `channelOpen`; System/Offer messages exempt.
- No customer identity exposed — conversations labeled by request title/code only.
- Signed image URLs: access-checked before minting, short-lived, per click, no object key to the SPA (14c pattern).
- Money decimal; migrations ship `.Designer.cs`; codes stay codes (SPA translates); seeds idempotent.

## Report
Append to `REPORT_BACKEND.md` ("20a/20b/20c/20d"): the conversations response, an image-url success + a rejection,
a location send (open + locked), the four lifecycle rows appearing idempotently, and the seed ids. Unfinished is
**not done**.

## Frontend (I will build after this lands)
Two-pane Messages page: left inbox (search + rows with icon, title, last preview / "Teklif gönderildi", time,
unread badge, AKTİF/KİLİTLİ chip) from 20a; right thread from #18 + 20b/20c — provider/customer bubbles, offer card
("Teklifi Gör"), image thumbnails (image-url → lightbox), location pins ("Konumu Aç"), centered lifecycle pills;
composer enabled only when `channelOpen`, hidden when `Closed`, with attach (post-MVP) + location affordances.
