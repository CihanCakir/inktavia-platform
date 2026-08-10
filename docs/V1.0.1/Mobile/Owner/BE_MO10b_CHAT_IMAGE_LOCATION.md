# BE_MO10b — owner chat image + location (send + display)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile` + a small owner-scoped read-url query in the SR
> module). MO10 **phase b** per `MO10_PLAN.md`: add **image** and **location** to the owner chat built in MO10a —
> mirroring the provider (upload → send with `AttachmentFileId`; display via a signed read-url; location via
> `CreateLocation`). Additive; identity from token; cost-free. **Do not commit.**

## Baseline (investigated — the provider pattern to mirror)
- **Provider image send:** `POST /api/v1/provider/service-requests/{srId}/messages` with a bare `AttachmentFileId`
  (Guid). The fileId comes from a **FileStorage upload** (the same upload-session the app already uses for avatar /
  vessel documents — the mobile BFF has `IFileStorageRemoteCall` + the M4e/avatar upload command). `SendServiceRequest
  Message` sets `MessageType.Image` when `AttachmentFileId` is present.
- **Provider image display:** `GET /api/v1/provider/service-requests/{srId}/attachments/{fileId}/read-url` →
  `GetAttachmentAccessCheck` (provider **three-prong**: biddable | has offer | assigned) **+** verifies the fileId
  belongs to the SR (`sr.Attachments` | `sr.Messages` | work-log evidence | completion evidence) → mints a short-lived
  signed read-url via FileStorage.
- **Location:** `SendServiceRequestMessage` already branches to `CreateLocation(lat, lng, label)` when
  `LocationLat/Lng` are present; `ServiceRequestMessageDto` carries `LocationLat/Lng/Label`. **No backend needed** — it
  rides the MO10a send.
- **Key consistency (task #81):** the SR access check reads from **`sr.Messages`** (the SR store). Because the owner
  **writes through the SR module** (MO10a — `SendServiceRequestMessage`, `SenderType=Owner`), the `AttachmentFileId`
  lands in `sr.Messages`, so the SR read-url check **finds it**. Keep the owner write on the SR path (MO10a) — do not
  route the image send through the Messaging-native `attachment-upload-url` (that path stores under a ConversationId,
  not `sr.Messages`, and the SR read-url check would miss it).

## BE — owner image + location (mobile BFF + one SR module query)
1. **Image upload (reuse):** the owner uploads the image through the **existing mobile BFF FileStorage upload** (the
   avatar / vessel-document upload-session pattern — `IFileStorageRemoteCall`) → returns a **FileId**. No new upload
   endpoint if the existing one is reusable for a chat image; otherwise add a thin `chat-image` upload command that
   reuses the same FileStorage session (validate content-type = image/*, size cap).
2. **Image + location send (extend MO10a):** add optional `AttachmentFileId` and `LocationLat`/`LocationLng`/
   `LocationLabel` to the MO10a mobile send request → forward to SR `SendServiceRequestMessage`
   (`SenderType = Owner`). SR sets `MessageType.Image` (attachment) or `Location` (coords) accordingly. Validate:
   exactly one of {text, image, location} per message (mirror the entity's branches); coords in range; label length.
3. **Owner-scoped read-url (new SR module query + mobile BFF endpoint):**
   - **SR module:** a new **owner-scoped** attachment access-check query — mirror `GetAttachmentAccessCheckQueryHandler`
     but replace the provider three-prong with an **owner-ownership gate** (`sr.OwnerUserId == UserInfo.UserId`), keep
     the identical fileId-belongs-to-SR verification (`sr.Attachments` | `sr.Messages` | work-log evidence |
     completion evidence). Return the signed read-url (or the access-ok → BFF mints via FileStorage, matching the
     provider flow). Do **not** modify the provider handler.
   - **Mobile BFF:** `GET api/v1/mobile/service-requests/{srId}/attachments/{fileId}/read-url` → owner read-url
     (short-TTL presigned GET). Owner identity from the token.
4. **Thread DTO:** MO10a's `MobileChatMessageDto` already carries `AttachmentFileId` + `Location*`; the FE (MO10c)
   calls the read-url per image message and renders the location card. No further DTO change (confirm the fields flow).

## Don't-break / QA
- Additive: extend the MO10a send (optional attachment/location) + reuse FileStorage upload + one new **owner-scoped**
  SR read-url query + one mobile BFF read-url endpoint. **The provider handler + provider read-url are untouched**;
  the SR send entity branches are unchanged. Owner write stays on the SR path (so the read-url check finds the fileId).
- **Cost-free:** an image/location message carries no economics — file id + coords + label only.
- **Security:** the read-url is owner-gated (`sr.OwnerUserId == token`) **and** fileId-scoped to that SR (no reading
  another SR's / another owner's files); short-TTL signed url; owner can only attach to their own SR threads.
- Tests: (1) mobile BFF + SR module + solution build 0 errors; (2) owner image send → SR message `MessageType.Image`
  with the fileId (lands in `sr.Messages`); (3) owner-scoped read-url returns a url for a fileId on the owner's SR,
  and **rejects** a fileId not on the SR / an SR the owner doesn't own; (4) owner location send → `CreateLocation`
  (lat/lng/label on the DTO); (5) the provider read-url + handler are unchanged; (6) cost-free guard on the payloads.
  Live smoke (upload → send image → provider sees it → owner opens read-url → image renders) needs the stack +
  storage — document it.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO10b_CHAT_IMAGE_LOCATION.md`: the image upload (reused FileStorage) + send
(`AttachmentFileId` → SR `MessageType.Image`), the **owner-scoped** SR read-url query + mobile BFF endpoint (owner
ownership + fileId-on-SR, provider handler untouched, task #81 consistency), the location send (`CreateLocation`), and
the tests. Then **MO10c** (FE Expo owner chat: full text/image/location send + render, realtime, unread/read, system
pills, **remove the MO1 chat coming-soon stub**).
