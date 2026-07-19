# 20f — Backend: display provider/customer-uploaded message images

Providers can now upload images in chat (frontend: signed-URL upload → send message with `AttachmentFileId`). But a
**newly uploaded** message image will not display, because the 14c read-url authorizes a `fileId` only when it is a
**`ServiceRequestAttachment`** on the request — a message's `AttachmentFileId` is **not** in that table. (The seeded
image works only because it reuses the 14c attachment file.) This closes that gap. Small, access-scoped.

## Verified in source
- 14c: `GetAttachmentAccessCheckQueryHandler` checks `sr.Attachments.FirstOrDefault(a => a.FileId == fileId)` — i.e.
  request attachments only. Message images are `ServiceRequestMessageEntity.AttachmentFileId`, a different set.
- Send: `SendServiceRequestMessageCommandHandler` creates a `MessageType.Text` message carrying `AttachmentFileId`
  (it does NOT set `MessageType.Image`).

## Work

### 1. A read-url that authorizes MESSAGE attachments
Add a message-image read-url the SPA can call for an image bubble. Mirror 14c's access pattern (provider must be a
party to the conversation) but check the fileId against **messages**, not request attachments:

- Module access check: the provider may see this request (same three-prong check as detail/14c) **AND** the `fileId`
  is the `AttachmentFileId` of some `ServiceRequestMessageEntity` on that request. Fail closed → "not found".
- Then mint the signed read URL via the existing FileStorage `CreateReadUrl` (5-min TTL), no object key leaked.
- Expose it. Prefer the clean route the SPA already expects:
  `GET /api/v1/provider/service-requests/{serviceRequestId:long}/messages/{messageId:long}/image-url`
  → resolve the message, confirm it belongs to the request and carries an `AttachmentFileId`, run the access check,
  mint the URL. (A per-message route is cleaner than a raw fileId and avoids widening 14c.)

> Alternative (also fine): widen the 14c access check to ALSO accept a fileId that is a message attachment on the
> request. If you do this, the existing `…/attachments/{fileId}/read-url` serves both and the SPA needs no new
> endpoint — but be careful the check still requires the fileId to belong to THIS request (attachment **or** message).

### 2. Set `MessageType.Image` on send when an attachment is present (no location)
In `SendServiceRequestMessageCommandHandler`: when `req.AttachmentFileId.HasValue` and no location, create the
message as **`MessageType.Image`** (not `Text`). Keeps `content` as an optional caption. This makes the inbox
"son mesaj" preview show "Görsel" and the thread render deterministic.

## Constraints
- Provider-scoped; access-checked **before** minting (never mint for a fileId not on a message of a request the
  provider may see). Short-lived per-view URL, no object key to the SPA. Gate unchanged (provider upload still
  requires `channelOpen`).
- No customer identity. Codes/enums unchanged elsewhere.

## Acceptance — observed
- Provider uploads an image in an open conversation → the image message renders its thumbnail (via the new
  message-image URL) and enlarges in the lightbox; a foreign fileId / message not on this request → "not found".
- The seeded image still displays. Inbox preview for an image message shows "Görsel".

## Frontend (I will switch after this lands)
The image bubble currently calls the 14c `attachments/{fileId}/read-url` (works only for the seed). Once 20f exists I
switch it to the message-image URL (per-message route) so **uploaded** images display too. If you widen 14c instead
(alternative above), no frontend change is needed.

## Report
Append to `REPORT_BACKEND.md` ("20f"): an uploaded image displaying via the new/ widened URL, the two rejections
(foreign file, message not on request), and the `MessageType.Image` on send. Unfinished is **not done**.
