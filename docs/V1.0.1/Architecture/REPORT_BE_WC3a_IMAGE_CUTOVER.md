# REPORT — BE_WC3a chat image read + write cutover (Messaging store)

> Implements `BE_WC3a_IMAGE_CUTOVER.md`. Moves chat **image** read (read-url access-check) + write to the Messaging
> store, joining text+location (WC2) on Messaging, behind the WC2 `Messaging:WriteCutover:ChatMessages` flag. Closes
> task #81 for chat. **Builds 0 errors. Live smoke PASSED (see below). Not committed.**

## 1. Messaging chat-attachment access-check (participant-scoped) — WC3a.1
New pure-read query + endpoint on the Messaging module:
- `ChatAttachmentAccessResponse { bool Authorized }` (Abstraction).
- `GetChatAttachmentAccessQuery(ContextType, ContextId, FileId)` + handler: loads the conversation via
  `GetByContextWithMessagesAsync` (participants + messages + attachments), and returns `Authorized = true` **iff** the
  authenticated caller (`UserInfo.UserId`, never the client) is a **participant** AND `FileId.ToString()` matches a
  `MessageAttachmentEntity.FileStorageId` on one of its non-deleted messages. Returns `false` (never throws) on any miss
  — no conversation, non-participant, or unknown fileId — so the BFF can cleanly fall back to the SR check.
- Endpoint `GET /api/v1/conversations/by-context/mine/attachments/{fileId}/access-check?contextType=&contextId=`
  (`[Authorize]`, participant-scoped), on `ConversationsController`.
- **Transition parity:** both old synced images (the sync `MapMessage` sets `FileStorageId = fileId.ToString()`) and new
  native sends store the same reference, so the check matches both.

## 2. Two-store read-url repoint (owner + provider) — WC3a.2
Both chat-image read-url handlers now do a **two-store** access-check before minting via FileStorage (mint unchanged):
1. **Messaging first** (`IMessagingRemoteCall.CheckChatAttachmentAccess(fileId, ServiceRequest, srId)`) — authorizes the
   **chat** image (native or old synced).
2. **On a false/miss → SR fallback** (`IServiceRequestRemoteCall.CheckAttachmentAccess`) — authorizes **request /
   work-log / completion-evidence** attachments, **unchanged**.
3. Neither → vague not-found (no info leak). Each module checks its own store; no cross-module DB access.
- Owner: `GetMobileAttachmentReadUrlQueryHandler`; provider: `GetAttachmentReadUrlBffQueryHandler`. Added
  `CheckChatAttachmentAccess` to both BFFs' `IMessagingRemoteCall`. FileStorage `CreateReadUrl` (fileId-based) is untouched.

## 3. Image write flip to Messaging native (behind the WC2 flag) — WC3a.3
- With `Messaging:WriteCutover:ChatMessages` **ON**, owner + provider image sends now go to **Messaging native**:
  resolve the conversation (as WC2) → `SendMessage` with `Type = MediaAttachment`, `AttachmentFileStorageId =
  fileId.ToString()`, `AttachmentFileType = "image"`. The client already uploaded the image (mobile upload session /
  provider upload) and passes the fileId, so this uses the handler's **direct pre-uploaded-FileStorageId path** — no
  Messaging upload session — storing exactly what the sync mirror produces, so the WC3a read-url check finds it.
  **Removed the WC2 `image → SR path` branch** (the flag decision is now just `flag`, not `flag && kind != Image`).
- With the flag **OFF**, images (and text/location) revert to the SR path (reversible). The provider `MapToSrDto` maps a
  Messaging image response (attachment echoed as `AttachmentDto.Url = fileId`) back to `MessageType=Image` +
  `AttachmentFileId`; the owner reuses `MapMessagingMessage`.
- **Validator:** `SendMessageCommandValidator` now allows empty `Content` for `MediaAttachment` (an image's payload is
  the attachment, not text — matching what the SR sync produces). Text/Location still require non-empty Content.

## Design decision — SR access-check left unchanged (no SR module edit)
The doc allows "a small SR access-check touch" to move the chat prong. I deliberately **did not** remove the SR check's
chat (`sr.Messages`) prong. The two-store BFF tries Messaging first, so native chat images resolve via Messaging; the SR
chat prong then harmlessly remains as a **transition/reversibility fallback** that covers the pre-sync window for a
**flag-OFF** image (SR write is immediate; its Messaging mirror lags the sync). Removing it would open that gap for no
benefit. Net: **zero SR-module changes**, request/work-log/completion read-urls byte-identical.

## Build / QA
- **0 errors**: Messaging host, owner mobile BFF, provider BFF, owner BFF unit tests, admin BFF (consumes the Messaging
  abstraction). SR + solution unaffected (SR untouched). Abstraction additions are additive.
- **Access-check**: returns ok for a participant + a real chat fileId; denies a non-participant / unknown fileId /
  no-conversation (returns `Authorized=false` → BFF falls back to SR, which also scopes).
- **Two-store read-url**: a chat image resolves via Messaging; a request/evidence attachment resolves via the SR
  fallback — unchanged.
- **Reversible / transition-safe**: old synced images live in both stores (Messaging check finds them); new native images
  are Messaging-only (Messaging check finds them); flag OFF → SR path + SR read-url branch (still finds them).

## Live smoke — EXECUTED & PASSED (running stack, 2026-08-12)
Ran against the running Docker stack. Flag **ON** on all three BFFs (`bff-marine-mobile`, `bff-marineprovider`,
`bff-marineprovider-2`); owner `qa.owner.aug5@inktavia.com`, provider `provider2@inktavia.com` (portal OTP login).
Conversation **27** on **SR 55** (owner UserId 100029 · Role 1, provider profile 100011 · Role 2). All three WC3a
services were clean-rebuilt (`--no-cache`, images removed first — the recurring stale-BFF/-image docker gotcha:
the earlier partial rebuild left the messaging-api validator + BFF image-branch stale, surfacing as a spurious
`"Message content cannot be empty."` 500 until the full fresh rebuild).

| # | Check | Result |
|---|-------|--------|
| 1 | **Owner** image send (flag ON) | **HTTP 200**, echo `messageType=MediaAttachment`. Landed as Messaging msg **156**: `Type=5`, **`SourceKey=NULL`** (native), `SenderRole=1`, attachment `FileStorageId=321098b9…7f9629b80`, `FileType=image`. **No new `sr.Messages` image row** (stayed at 1). ✅ |
| 2 | **Provider** image send via **portal chat UI** (flag ON) | Sent through the real composer (provider2 OTP session). Landed as Messaging msg **158**: `Type=5`, **`SourceKey=NULL`** (native), `SenderRole=2`, `SenderUserId=100011`, attachment `FileStorageId=91f922c4…7fc14bc`, `FileType=image`. **No new `sr.Messages` image row** (stayed at 2). Rendered in the thread. ✅ |
| 3 | **Two-store read-url — Messaging** (native chat image) | Owner read-url for the native image (SR 55, no `sr.Messages` row) → **HTTP 200**, real MinIO presigned URL (len 355). Owner also reads the **provider's** native image (91f922c4) → **HTTP 200** (cross-participant Messaging check). Both native images **render** in the UI (owner→orange tile, provider→navy tile). ✅ |
| 4 | **Old synced image** still displays | Pre-cutover synced image (msg 153, `SourceKey=sr:55:33`, fileId `…000000aa`) read-url → **HTTP 200** for both owner and provider (Messaging check finds the synced attachment). ✅ |
| 5 | **SR fallback** — request attachment | SR 9011 request attachment (`a0a0a0a0-…e4e4`) read-url → **HTTP 200**, authorized via the **SR** check (Messaging denies a non-chat fileId → SR fallback). URL is empty only because that seed attachment has no backing MinIO object (known objectless-seed degradation, not an auth failure — a real chat image mints a URL in #3). ✅ |
| 6 | **Deny** — bogus fileId | Unknown fileId on SR 55 → **HTTP 400** vague not-found (both stores deny; no info leak). ✅ |
| 7 | **Flag OFF** reversibility | Recreated `bff-marine-mobile` with the flag **OFF**; owner image → **HTTP 200**, echo `messageType=Image` (SR echo), a **new `sr.Messages` image row** created (1→2), **no new native Messaging message**. Flipped back **ON** afterward (compose restored, zero git drift). ✅ |

**Net:** owner + provider images write natively to Messaging as `MediaAttachment` (SourceKey NULL) with no SR
duplication; the two-store read-url renders native + old-synced chat images via Messaging and request/evidence
attachments via the SR fallback; a bad fileId is denied; and the flag cleanly reverts to the SR path. Task #81 closed
for chat, live-verified. No code committed; no credentials changed.

## Deferred / next
Task #81 **closed for chat** (chat images now read+write on Messaging). Next: **WC3b** (dispute composer transcript →
Messaging — decide remote-call vs FE-loads fork) and **WC3c** (retire the legacy SR chat read-endpoints), then **WC4**
(retire the one-directional sync consumer once the DB unique index is the sole idempotency guard).
