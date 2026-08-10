# REPORT — BE_MO10b owner chat image + location (send + display)

> MO10 **phase b**: add **image** and **location** to the owner chat built in MO10a — mirroring the provider (upload →
> send with `AttachmentFileId`; display via a signed read-url; location via `CreateLocation`). Mobile BFF + one
> owner-scoped read-url query in the SR module. Additive; identity from token; cost-free. **NOT committed.**
>
> Repo: `addesso-project` (`Bff/src/Marine.Participant.Mobile` + SR module).

## Outcome
- **Builds clean** — SR module + mobile BFF host: 0 errors.
- **Tests: 178/178 (SR) + 25/25 (mobile BFF) green** — 4 new SR owner-attachment-access cases + 9 new BFF
  send-validation cases.
- **The provider handler + provider read-url are untouched**; the SR send entity branches are unchanged; the owner
  write stays on the SR path (so the read-url check finds the fileId — task #81).

## Architecture (as the provider does it)
The **SR module returns an access-OK boolean only; the BFF mints the signed read-url**. So: a new owner access-check
query in the SR module, and the mobile BFF mints the presigned GET via the `IFileStorageRemoteCall.CreateReadUrl` it
already uses for MO4/MO5 evidence.

## BE — SR module (one new owner-scoped query)
- **`GetOwnerAttachmentAccessCheckQuery` + handler** — a copy of the provider `GetAttachmentAccessCheck` with the
  three-prong provider block replaced by a single **ownership gate** (`sr.OwnerUserId == the asserted UserInfo.UserId`)
  and the **identical fileId-belongs-to-SR verification** (request attachment | message attachment | work-log evidence
  | completion evidence). Every failure is a vague not-found (no info leak). Returns the reused
  `GetAttachmentAccessCheckResponse { FileId, Authorized }`. The security-load-bearing core is a pure static
  `Evaluate(sr, ownerUserId, fileId)` (no I/O) — unit-testable in isolation; the handler is a thin loader around it.
- **Route:** `GET api/v1/service-requests/{srId}/attachments/{fileId}/access-check` on the owner `ServiceRequestController`
  (`[Authorize]`, resolves the owner from the token via `CurrentUserId`). The provider access-check
  (`api/v1/service-requests/provider/...`) is untouched.
- **Send / location — no module change:** `SendServiceRequestMessage` already branches `CreateLocation` (coords) vs
  `Create(MessageType.Image)` (attachment) vs Text; the request already carries `AttachmentFileId` +
  `LocationLat/Lng/Label`. The owner write rides the MO10a SR send (`SenderType=Owner`).

## BE — mobile BFF
- **Remote call:** `IServiceRequestRemoteCall.CheckAttachmentAccess(srId, fileId)` → the new owner access-check route.
  (`SendMessage` from MO10a already carries the attachment/location fields — no change.)
- **Send extended:** `MobileSendChatMessageRequest` + `SendMobileChatMessageCommand` now carry `AttachmentFileId` +
  `LocationLat/Lng/Label`. A pure static **`MobileChatSend.Validate`** enforces **exactly one of { text, image,
  location }** + coord range (lat ∈ [-90,90], lng ∈ [-180,180]) + length caps (content ≤ 4000, label ≤ 200) — the SR
  module validates none of this, so the BFF is the gate. The handler forwards the one chosen kind to the SR send.
- **Read-url:** `GetMobileAttachmentReadUrlQuery` + handler — mirrors the provider BFF read-url: owner access-check
  (module) → mint the short-TTL (5 min) presigned GET via `IFileStorageRemoteCall.CreateReadUrl`. A failed access-check
  is a clean not-found; a resolved-but-missing object degrades to an **empty url** (FE placeholder — the access-check
  already proved ownership, so a storage miss is not a security event). New `MobileAttachmentsController` at
  `GET api/v1/mobile/service-requests/{srId}/attachments/{fileId}/read-url`.
- **Image upload — reused, nothing new:** the owner uploads a chat image through the existing
  `api/v1/mobile/uploads` client-side-presigned session (the M4f/avatar flow) → a committed `FileId` → send it as the
  `AttachmentFileId`. Bytes never traverse the BFF.
- **Thread DTO — no change:** MO10a's `MobileChatMessageDto` already carries `AttachmentFileId` + `Location*`; the FE
  (MO10c) calls the read-url per image message and renders the location card.

## Config
No compose change for MO10b: the owner access-check is on `service-request-api` (already whitelists
`marine-mobile-bff` from BE_MO1), and the FileStorage + service-request base URLs are already configured on the mobile
BFF. (MO10a already added the messaging-api allowlist + base URL for the chat reads.)

## Tests
**SR module (`OwnerAttachmentAccessMo10bTests`, 4):** owner + fileId-on-SR (a chat message image) → authorized; a
fileId not on the SR → rejected; an SR the caller doesn't own → rejected; unresolved owner / missing SR → rejected.
**Mobile BFF (`MobileChatSendMo10bTests`, 9):** text/image/location each classify to the right kind; empty →
rejected; more-than-one → rejected; out-of-range coords → rejected; overlong text → rejected; the send request is
cost-free.

### How the remaining acceptance criteria are enforced
- **(2) image send → SR `MessageType.Image` with the fileId** — the BFF forwards `AttachmentFileId` to the SR send,
  which sets `MessageType.Image` and lands the fileId in `sr.Messages` (build-verified; the access-check test seeds
  exactly that shape).
- **(4) location send → `CreateLocation`** — the BFF forwards `LocationLat/Lng/Label`; the SR entity branch takes over
  (unchanged).
- **(5) provider read-url + handler unchanged** — the owner query/route are additive; the provider ones are untouched.
- **Live smoke** (upload → send image → provider sees it → owner opens read-url → image renders) needs the running
  stack + storage — documented.

### Live smoke (manual, env-gated)
`POST /api/v1/mobile/uploads/session` → PUT the image bytes to the presigned url → `POST .../complete` → a `FileId` →
`POST /api/v1/mobile/service-requests/{srId}/messages { attachmentFileId }` (image message, lands in `sr.Messages`) →
the provider sees it live → the owner (or provider) opens
`GET /api/v1/mobile/service-requests/{srId}/attachments/{fileId}/read-url` → a short-TTL signed url → the image renders.
A location message: `POST .../messages { locationLat, locationLng, locationLabel }`.

## Don't-break / QA
Additive: one owner-scoped SR read-url query + a route; the extended MO10a send (optional attachment/location) + a BFF
validator; a read-url BFF endpoint; the reused FileStorage upload. The provider handler + provider read-url are
untouched; the SR send entity branches are unchanged; the owner write stays on the SR path. Cost-free (file id +
coords + label only). Security: the read-url is owner-gated (`sr.OwnerUserId == token`) AND fileId-scoped to that SR
(no reading another SR's / another owner's files); short-TTL signed url.

## Next
**MO10c** — FE Expo owner chat: full text/image/location send + render, realtime, unread/read, System pills, and
**remove the MO1 chat coming-soon stub**.
