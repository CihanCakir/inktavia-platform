# BE_MO10a — owner chat BFF surface (read + text write)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile` + a compose mount fix). MO10 **phase a** per
> `MO10_PLAN.md`: the owner reads conversations/threads from the unified Messaging store and sends **text** messages
> through the SR module — **mirroring the provider path** (`ProviderMessagingController` read + `SendProviderMessage`
> write), minus the provider-only anti-harassment gate. Text end-to-end; image/location are MO10b/MO10c. Additive;
> identity from token; cost-free. **Do not commit.**

## Baseline (investigated — reuse the module verbatim, mirror the provider)
- **Read (Messaging module, participant-scoped):** `GET /api/v1/conversations/mine` (inbox) + `GET
  /api/v1/conversations/by-context/mine` (thread by SR context) — the caller is resolved from the assertion, **no user
  id on the wire** (see `IMessagingRemoteCall` doc). The owner is a conversation participant → reads only their own
  threads. Identical to the provider's Phase-3 read cutover (`ProviderMessagingController`).
- **Write (SR module):** `SendServiceRequestMessage` already supports **`SenderType = Owner`** and publishes
  `ServiceRequestMessageSentMessage` (→ provider realtime + `NewMessageReceived` notification + SR→Messaging
  live-sync) **and** the SR realtime. **The anti-harassment gate is provider-only** (`if SenderType == Provider …
  HasOwnerMessageAsync`) — the owner always opens the channel. This is the exact command the provider's
  `SendProviderMessage` wraps, with `SenderType = Owner`.
- **Message DTO:** `ServiceRequestMessageDto { SenderUserId, SenderType, MessageType, Content, IsRead, ReadAt,
  AttachmentFileId, LocationLat/Lng/Label, CreatedAt }` — text uses `Content`; image/location fields ride along for
  MO10b.

## Mount / wiring prerequisite (surface first)
- **Add `marine-mobile-bff` to `messaging-api` `BffAssertion__AllowedClientIds`** in `docker-compose.yaml` (mirror the
  MO9c notification-api fix — provider/admin were listed, mobile was not). Without it the owner's `mine` /
  `by-context/mine` reads are silently ignored (assertion dropped).
- Confirm `service-request-api` already whitelists `marine-mobile-bff` (added in MO1) for the owner send — reuse.

## BE — mobile BFF (new, thin; mirror `ProviderMessagingController` + `SendProviderMessage`)
1. **`IMessagingRemoteCall`** (Refit) on the mobile BFF → `/api/v1/conversations/mine` (skip/take) +
   `/api/v1/conversations/by-context/mine` (by SR context/id). Typed concrete DTOs (BFF typed-body rule). DI + config
   (`messaging-api` base url + `depends_on`).
2. **Read handlers + cost-free mobile DTOs:**
   - `MobileConversationListDto` (inbox: SR id/code, counterparty display name via the MO2b `ProviderNameResolver`,
     last-message preview, unread count, updated-at) and `MobileChatMessageDto` / `MobileChatThreadDto` (SenderType,
     `IsOwn` computed, MessageType, Content, AttachmentFileId, Location*, IsRead, CreatedAt). **Cost-free** — no
     economics; **System** messages carried through so the FE renders lifecycle pills (mirror the provider thread).
   - Map enums → **string** names (the AdminPanel-BFF numeric-enum gotcha).
3. **Text write:** reuse `IServiceRequestRemoteCall` → SR `SendServiceRequestMessage` with **`SenderType = Owner`**
   (identity from the token; `ServiceRequestId` = context; `Content` = text). Do **not** send owner id in the body.
   Mirror `SendProviderMessageCommand` fields (drop the provider gate). Validate non-empty content, length.
4. **`MobileChatController`** (`api/v1/mobile/service-requests/{srId}/messages` + `api/v1/mobile/conversations` for the
   inbox; pick one consistent shape and keep it): `GET` inbox, `GET` thread by SR id, `POST` text message. `[Authorize]`
   participant, callable by the SA via BffAssertion.

## Don't-break / QA
- Additive: new remote-call(s) + DTOs + one BFF controller + the messaging-api mount fix. **No module change** (owner
  send already exists in the SR module; reads already exist in Messaging). The provider messaging path is untouched.
- **Cost-free:** grep the chat payloads — no amount/commission/net/margin; message text + sender + timestamps only.
- **Identity from token:** reads use `mine`/`by-context/mine` (no id on the wire); the send sets `SenderType = Owner`
  and never takes an owner id from the body. An owner can only read/write their own SR threads.
- Tests: (1) mobile BFF + solution build 0 errors; (2) inbox maps to the cost-free DTO (enums as strings, IsOwn
  computed); (3) thread-by-SR returns the owner's thread; (4) text send reaches SR `SendServiceRequestMessage` with
  `SenderType = Owner`; (5) System messages surface as lifecycle entries; (6) cost-free reflection/grep guard; (7)
  messaging-api allowlist includes marine-mobile-bff. Live smoke (owner sends → provider sees live → owner bell) needs
  the stack + Redis — document it.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO10a_OWNER_CHAT_SURFACE.md`: the `IMessagingRemoteCall` + cost-free chat DTOs +
`MobileChatController` (inbox / thread-by-SR / text send via SR `SenderType=Owner`), the messaging-api mobile
assertion mount fix, the mirror-of-provider confirmation (reads = Messaging `mine`, write = SR send, no gate for
owner), and the tests. Then **MO10b** (image via attachment-upload-url + owner-scoped read-url with the Messaging-store
access check per task #81; location send/render).
