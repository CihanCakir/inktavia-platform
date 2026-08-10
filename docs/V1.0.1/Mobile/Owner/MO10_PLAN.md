# MO10 — owner ↔ provider chat (text + image + location) — PHASED PLAN

> **Why this matters:** chat is the last MO1 stub (owner SR detail shows a "coming-soon" chat) and the owner side of a
> capability the **provider FE–BFF–Messaging path already does correctly**. MO10 brings the owner to full parity —
> **text, image, and location** messages — by mirroring the provider path, reusing the module verbatim. Phased so
> each layer closes cleanly. Additive; identity from token; cost-free. **Do not commit.**

## What the backend already gives us (investigated — nothing new in the module)
- **Read (unified Messaging store, participant-scoped):** `IMessagingRemoteCall` → `GET /api/v1/conversations/mine`
  (inbox) + `GET /api/v1/conversations/by-context/mine` (thread by SR context). These resolve the caller from the
  assertion — **no user id on the wire** — so the owner (a conversation participant) reads only their own threads.
  This is exactly what the provider does after the Phase-3 read cutover.
- **Write (SR module, symmetric to the provider):** `SendServiceRequestMessage` already supports
  **`SenderType = Owner`** and branches to **text**, **image** (`AttachmentFileId` → `MessageType.Image`), or
  **location** (`CreateLocation(lat, lng, label)`). It publishes `ServiceRequestMessageSentMessage` (drives the
  provider's realtime, the `NewMessageReceived` notification, and the SR→Messaging live-sync) **and** the SR realtime.
  The **anti-harassment gate is provider-only** — the owner can always open the channel. So the owner write = the same
  command the provider uses (`SenderType = Owner` instead of `Provider`); no module change.
- **Message shape (both sides):** `ServiceRequestMessageDto { SenderType, MessageType, Content, AttachmentFileId,
  LocationLat/Lng/Label, IsRead, ReadAt, CreatedAt }` — image + location are first-class fields already.
- **Image transport:** `POST /api/v1/conversations/{id}/messages/attachment-upload-url` (presigned **PUT**) → client
  uploads the image → send the message with the resulting file id. **Display** = a presigned **read-url**; the SR
  module's `GetAttachmentAccessCheck` validates the file id belongs to a message/attachment/evidence on the SR before
  signing. **Gotcha (task #81):** the access check must read from the **Messaging store** (post-unification), not the
  legacy `sr.Messages` — the owner read-url must use the corrected check (or an owner-scoped equivalent).
- **Location decision (memory):** MVP = client-side navigation + JSON coords (lat/lng/label); render a static map
  preview + "open in maps". Image was previously deferred — **MO10 includes it** (the user is prioritising it and the
  module + provider already support it end-to-end).

## Mount / wiring prerequisites (surface early, like every MO)
- `messaging-api` **BffAssertion allowlist must include `marine-mobile-bff`** (mirror the MO9c notification-api fix;
  provider/admin were listed, mobile likely not) — else the owner's `mine`/`by-context/mine` reads are ignored.
- Confirm `service-request-api` already whitelists `marine-mobile-bff` (MO1 added it) for the owner send.
- File read-url/upload-url: confirm the mobile BFF can reach FileStorage/Messaging for the presigned URLs.

## Phase map

### MO10a — owner chat BFF surface (read + text write) · **mobile BFF + mount fixes**
- `IMessagingRemoteCall` on the mobile BFF → `conversations/mine` (inbox) + `by-context/mine` (thread by SR id),
  participant-scoped via BffAssertion. Cost-free `MobileConversation*`/`MobileChatMessageDto` (SenderType, MessageType,
  Content, AttachmentFileId, Location*, IsRead, CreatedAt — no economics; system messages rendered as lifecycle pills).
- **Text send:** reuse `IServiceRequestRemoteCall` → SR `SendServiceRequestMessage` with **`SenderType = Owner`**
  (owner identity from the token; the SR id is the context). Mirror the provider's `SendProviderMessage` shape minus
  the harassment gate.
- Mount fix: add `marine-mobile-bff` to `messaging-api` BffAssertion allowlist.
- `MobileChatController` (`api/v1/mobile/chat` or under `.../service-requests/{srId}/messages`): GET inbox, GET thread
  by SR id, POST text message.
- **DoD:** owner can list conversations, open a thread by SR, and send/receive **text**; provider sees it live
  (existing realtime); the owner gets the `NewMessageReceived` bell (MO9). Kickoff `BE_MO10a_OWNER_CHAT_SURFACE.md`.

### MO10b — rich content: image + location · **mobile BFF (+ owner read-url access check)**
- **Image send:** passthrough `attachment-upload-url` (presigned PUT) → client uploads → POST message with
  `AttachmentFileId` (SR send sets `MessageType.Image`).
- **Image display:** an **owner-scoped read-url** — reuse/adapt the SR attachment access check so it (a) confirms the
  owner owns the SR and (b) resolves the file from the **Messaging store** (apply the task #81 fix; never the legacy
  `sr.Messages`). Return a short-TTL presigned GET url.
- **Location send + render:** POST message with `LocationLat/Lng/Label` (SR `CreateLocation`); the thread DTO already
  carries them. No new backend.
- **DoD:** owner sends/receives image + location; images display via presigned read-url; cost-free.
  Kickoff `BE_MO10b_CHAT_IMAGE_LOCATION.md`.

### MO10c — FE owner chat (Expo) + stub removal · **inktavia-marine-mobile**
- Full chat screen on the SR detail: message list (text / image thumbnail→viewer / location card→map preview +
  open-in-maps), sticky composer with **send text**, **attach image** (expo-image-picker → upload-url → upload →
  send), **share location** (expo-location → send lat/lng/label), system lifecycle messages as pills, unread/read,
  auto-scroll (mirror the provider web chat behaviour).
- **Realtime:** new-message updates the open thread live (via the MO9b `/hubs/notification` bell → refetch, and/or the
  SR/messaging realtime frame); the frame is a hint, the API is truth.
- **Remove the MO1 "chat coming-soon" stub** — this closes the last MO1 owner stub (offers→MO2, complete→MO4 already
  done).
- tr+en; mock parity; cost-free. Kickoff `FE_MO10c_OWNER_CHAT.md`.

## Sequencing & gates
MO10a (text spine + mounts) → MO10b (image/location backend) → MO10c (FE consumes all). No external secret gate — chat
works fully in dev (image upload needs storage reachable, already used by Vessel M4e/M4f). Realtime live smoke needs
the running stack + Redis (documented manual step).

## Scope notes (what MO10 is NOT)
- **Not** a messaging-store write cutover. Owner writes go through the **SR module send** (same as the provider today)
  — the canonical, downstream-correct path. The eventual Phase-4 (both sides write straight to the Messaging module)
  stays a separate infra epic, not owner debt. MO10 does not regress the strangler state.
- Cost-free: chat never shows provider cost/commission/net/margin; only the human message + attachment + location.

## Definition of done (MO10 closed)
Owner has full text + image + location chat with the provider, mirroring the provider path, with the MO1 chat stub
removed, realtime + notifications wired, and the `messaging-api` mobile assertion mount fixed — the owner side of the
Messaging capability is at parity with the provider side.
