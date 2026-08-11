# BE_WC3a — chat image read + write cutover (Messaging store) · closes task #81

> **Repo:** `addesso-project` (Messaging + BFFs + a small SR access-check touch). Phase-4 **WC3a** per
> `WC3_PLAN_READER_MIGRATION.md`: move chat **image** read + write to the Messaging store, so images join text+location
> (WC2) on Messaging and the read-url stops depending on `sr.Messages` (finally closing task #81 for chat). Rides the
> WC2 `Messaging:WriteCutover:ChatMessages` flag. Requires WC0–WC2. **Do not commit.**

## Investigated baseline
- **Image parity (confirmed):** `MapMessage` stores a synced image as `MessageAttachmentEntity.FileStorageId =
  fileId.ToString()`; a native Messaging image send stores the same → a Messaging attachment check finds **both** old
  (synced) and new (native) images.
- **The read-url access-check serves ALL attachment types:** `GetOwnerAttachmentAccessCheck` (owner) +
  `GetAttachmentAccessCheck` (provider) verify `fileId` against `sr.Attachments` (request), `sr.Messages` (chat),
  work-log evidence, completion evidence. **Only the chat branch moves to Messaging**; request/evidence stay in SR.
- **WC2 left images on the SR path** (the BFF `image → SR send` branch) precisely because the read-url still read
  `sr.Messages`. WC3a removes that branch and migrates the chat read-url.

## BE — WC3a

### 1. New Messaging chat-attachment access-check (participant-scoped)
Add a Messaging query/endpoint: given `(ContextType=ServiceRequest, ContextId=srId, fileId)` and the caller (resolved
from the token / BffAssertion), return **access-ok** iff the caller is a **participant** of the conversation for that
context **and** `fileId.ToString()` matches a `MessageAttachmentEntity.FileStorageId` on one of its messages. Pure
read; no url minting here (the BFF mints via FileStorage, as today). Participant-scoping means a caller can only get a
url for an image in their own conversation.

### 2. Repoint the owner + provider CHAT-image read-url to a two-store check (BFF-orchestrated)
The owner (MO10b `…/attachments/{fileId}/read-url`) + provider (`ServiceRequestsController …/attachments/{fileId}/read-url`)
read-url handlers:
- **Try the Messaging chat-attachment check first** (new, §1). On access-ok → mint the read-url via FileStorage
  (unchanged) and return.
- **On miss, fall back to the existing SR check** (`GetOwnerAttachmentAccessCheck` / `GetAttachmentAccessCheck`) for
  **request / work-log / completion evidence** attachments → mint as today.
- Each module checks its own store; no cross-module DB access. Request/evidence read-urls are **unchanged**; chat
  images now resolve from Messaging (works for old synced + new native images).

### 3. Flip owner + provider IMAGE writes to Messaging native (behind the WC2 flag)
- With `Messaging:WriteCutover:ChatMessages` **ON**, image sends go to the **Messaging native** path (resolve the
  conversation as in WC2 → `attachment-upload-url` → upload → `SendMessage` `MediaAttachment` with the completed
  FileStorageId), joining text+location. **Remove the WC2 `image → SR path` branch.**
- With the flag **OFF**, images (and text/location) revert to the SR path (reversible, as WC2).
- The owner uses the same FileStorage upload it already has (M4f/MO10b); the Messaging `attachment-upload-url` +
  `CompleteUploadSession` complete the attachment on the Messaging message.

## Don't-break / QA
- **Request / work-log / completion-evidence read-urls unchanged** (still SR) — verify a request attachment + a
  completion-evidence read-url still resolve. Only the **chat** image path moved.
- **Reversible:** flag OFF → images on the SR path + the SR read-url branch (which still finds them); flag ON → Messaging.
- **Transition-safe:** old chat images (pre-cutover) are in **both** stores → the Messaging check finds them; new ones
  are Messaging-only → the Messaging check finds them; the SR fallback covers any request/evidence.
- Tests: (1) Messaging + BFFs + SR + solution build 0 errors; (2) the Messaging chat-attachment check returns ok for a
  participant + a real chat fileId, **denies** a non-participant or a fileId not on the conversation; (3) flag ON →
  owner + provider image send lands as a Messaging `MediaAttachment` (FileStorageId set), **no new `sr.Messages` image
  row**; (4) the two-store read-url resolves a **chat** image via Messaging and a **request/evidence** attachment via
  SR; (5) an **old** synced image still displays (Messaging check); (6) flag OFF → SR path + SR read-url, reversible.
  Live smoke (owner+provider send an image, flag ON → displays from Messaging; a request/evidence attachment still
  displays) needs the stack — document it.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC3a_IMAGE_CUTOVER.md`: the Messaging chat-attachment access-check, the two-store
read-url repoint (Messaging chat + SR request/evidence fallback), the image write flip (Messaging native, WC2 flag,
SR-branch removed), and the tests + live-smoke. Note task #81 closed for chat. Then **WC3b** (dispute composer
transcript → Messaging — decide the remote-call vs FE-loads fork) and **WC3c** (retire the legacy SR chat read-endpoints).
