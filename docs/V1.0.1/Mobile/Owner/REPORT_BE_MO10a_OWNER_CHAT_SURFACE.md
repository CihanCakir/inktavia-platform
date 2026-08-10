# REPORT — BE_MO10a owner chat BFF surface (read + text write)

> MO10 **phase a**: the owner reads conversations/threads from the unified Messaging store and sends **text** messages
> through the SR module — mirroring the provider path (`ProviderMessagingController` read + `SendProviderMessage`
> write), minus the provider-only anti-harassment gate. Mobile BFF + a compose mount fix. Text end-to-end;
> image/location are MO10b/MO10c. Additive; identity from token; cost-free. **NOT committed.**
>
> Repo: `addesso-project` (`Bff/src/Marine.Participant.Mobile` + docker-compose).

## Outcome
- **Mobile BFF builds clean** — 0 errors (the Application csproj already referenced `Messaging.Abstraction` +
  `ServiceRequest.Abstraction`; no csproj change).
- **Tests: 16/16 green** in the mobile BFF `…UnitTests` (4 new MO10a mapper cases).
- **No module change** — the owner send already exists in the SR module (`SenderType=Owner`), and the reads already
  exist in Messaging (`/conversations/mine` + `/by-context/mine`). The provider messaging path is untouched.

## Mirror-of-provider (confirmed)
- **Reads = Messaging module** (`/api/v1/conversations/mine` + `/by-context/mine`), participant-scoped: the caller is
  resolved from the assertion (the delegating handler injects `X-Aizen-User-Id`), so no user id is on the wire. The
  by-context read is authorized module-side (`conversation.Participants.Any(p => p.UserId == caller)` → 403 otherwise),
  so an owner reads only their own threads with no BFF-side gate.
- **Write = SR module** (`POST .../{srId}/messages`) with `SenderTypeOverride = Owner`. The anti-harassment gate is
  `if (SenderType == Provider) HasOwnerMessageAsync(...)` — **provider-only**, so the owner send is automatically
  ungated (the owner always opens the channel). The SR send publishes `ServiceRequestMessageSentMessage`, which the
  Messaging module live-syncs into the conversation — so an owner write appears in the Messaging read model.

## BE — mobile BFF (new, thin)
- **`IMessagingRemoteCall`** (Refit, new) → the Messaging reads: `GetMyConversations(contextType, skip, take)` +
  `GetMyConversationByContext(contextType, contextId)`. DI-registered; base URL
  `RemoteCalls:IMessagingRemoteCall:BaseUrl` (→ messaging-api).
- **`IServiceRequestRemoteCall.SendMessage`** added → `POST /api/v1/service-requests/{srId}/messages` (reuses the
  existing SR client + its `service-request-api` base URL — no new client for the write).
- **Cost-free mobile DTOs + mapper** (`Contracts/Chat/` + `Chat/MobileChatMapper.cs`):
  - `MobileConversationDto` (inbox: `ServiceRequestId` from the Messaging `ContextId`, title, last-message preview,
    `LastMessageAt`, `UnreadCount`, `Status`, `ChannelOpen`).
  - `MobileChatMessageDto` (`SenderType` string, **`IsOwn`** computed, `SenderName`, `MessageType` string, `Content`,
    `AttachmentFileId`, `Location*` — carried for MO10b, `IsRead`, `CreatedAt`). **System messages ride through**
    (`SenderType = "System"`) so the FE renders lifecycle pills.
  - `MobileChatThreadDto` (`ServiceRequestId`, title, status, **`CounterpartyName`** = the Provider participant's
    display name, `ChannelOpen`, `Messages`, `TotalCount`).
  - Enums → **string** names (the AdminPanel numeric-enum gotcha). One mapper handles both the Messaging thread message
    (sender role as a string) and the SR send-result message (sender type as an enum).
- **Handlers** (`Chat/`): `GetMobileConversationsQuery` (inbox), `GetMobileChatThreadQuery` (thread by SR id — the SR
  id is preserved even when the conversation doesn't exist yet, for a fresh SR), `SendMobileChatMessageCommand`
  (validate non-empty + ≤4000 chars → SR send with `SenderType=Owner`). Each resolves the participant then forwards —
  no owner id in the body.
- **Controllers**: `MobileConversationsController` (`GET api/v1/mobile/conversations` — inbox) +
  `MobileChatController` (`GET/POST api/v1/mobile/service-requests/{srId}/messages` — thread + text send).
  `[Authorize(ParticipantAuthenticated)]`, callable by the SA via BffAssertion.

## Mount / wiring (docker-compose)
- Added `RemoteCalls__IMessagingRemoteCall__BaseUrl: http://messaging-api:8080` to the `bff-marine-mobile` service (the
  owner send reuses the existing `service-request-api` base URL).
- **Added `BffAssertion__AllowedClientIds__2: marine-mobile-bff` to the `messaging-api` service** — it previously
  listed only provider/admin, so the owner's `/conversations/mine` + `/by-context/mine` reads would have been silently
  ignored (assertion dropped). Mirrors the MO9c notification-api fix.
- `service-request-api` already whitelists `marine-mobile-bff` (BE_MO1) → the owner send is already authorized.
- Runtime prereq (env-gated, as with every MO task): the mobile BFF service-account token must carry the
  `messaging-api` audience for the S2S read.

## Tests (`MobileChatMapperMo10aTests`, mobile BFF UnitTests — 16/16)
- **Inbox → cost-free DTO**: `ServiceRequestId` from `ContextId`, `ChannelOpen` from status, empty preview → null.
- **Thread**: per-message `IsOwn` (`SenderRole=="Owner"`), enums as string names, `CounterpartyName` = the Provider
  participant, **System messages carried through**.
- **SR send-result**: maps to the same message DTO with `SenderType=Owner ⇒ IsOwn`.
- **Cost-free** reflection guard on the chat DTOs (no amount/commission/net/margin/price).

### How the remaining acceptance criteria are enforced
- **(4) text send reaches SR `SendServiceRequestMessage` with `SenderType=Owner`** — the handler hard-codes
  `SenderTypeOverride = Owner` (build-verified); the module reads the sender id from the assertion, never the body.
- **(7) messaging-api allowlist includes marine-mobile-bff** — added (grep the compose service).
- **Live smoke** (owner sends → provider sees it live via the SR realtime → owner's bell via `NewMessageReceived`)
  needs the running stack + Redis + the messaging-api audience token — documented.

### Live smoke (manual, env-gated)
Deploy messaging-api (with `marine-mobile-bff` allowlisted) + the mobile BFF (with the messaging base URL) → owner:
`GET /api/v1/mobile/conversations` (their SR threads) → `GET .../service-requests/{srId}/messages` (the thread) →
`POST .../messages { content }` → the message appears in the provider's live chat (SR realtime) and the Messaging
read model, and the owner's notification bell (MO9) fires `NewMessageReceived` for the provider.

## Don't-break / QA
Additive: a new remote-call + DTOs/mapper + two BFF controllers + the messaging-api mount fix. No module change; the
provider messaging path is untouched; WebSocket/realtime is unchanged. Cost-free (message text + sender + timestamps
only). Identity from token; reads use `mine`/`by-context/mine` (no id on the wire); the send sets `SenderType=Owner`
and never takes an owner id from the body — an owner can only read/write their own SR threads.

## Next
**MO10b** — image via attachment-upload-url + owner-scoped read-url (Messaging-store access check); location send/render.
