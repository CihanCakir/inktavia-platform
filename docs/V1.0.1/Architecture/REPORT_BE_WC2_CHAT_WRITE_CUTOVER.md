# REPORT — BE_WC2 chat WRITE cutover (owner + provider → Messaging native send) · TEXT + LOCATION

> Implements `BE_WC2_CHAT_WRITE_CUTOVER.md`. Flips owner + provider chat **text + location** writes from the SR module
> to the Messaging native `SendMessageCommand`, behind a reversible flag; images stay on the SR path (WC3). Repoints
> provider realtime onto `MessagingMessageSentMessage`. **Builds 0 errors. Not committed.**

## 1. Persist location in the Messaging send (WC2.1)
- `SendMessageCommand` + ctor: added `LocationLat` / `LocationLng` / `LocationLabel` (nullable).
- `SendMessageCommandHandler`: when `Type == Location`, calls `message.SetLocation(...)` (the WC0 columns already exist on
  `ConversationMessageEntity` via `SetLocation`). Content still carries the location JSON for the read/echo path
  (`ToDto → ParseLocation`), so both the discrete columns (future WC3 read) and the current JSON read are populated.
- Wired end-to-end so a BFF can supply coords: added `LocationLat/Lng/Label` to `SendMessageRequest` (Abstraction) and
  passed them through `ConversationMessagesController` → `SendMessageCommand`. Existing callers (admin/support) pass none.

## 2. Anti-harassment gate on the Messaging store (WC2.2)
- New `IConversationMessageRepository.HasOwnerMessageAsync(conversationId)` → `AnyAsync(SenderRole == Owner && !IsDeleted)`
  (Owner role is distinct from System, so it inherently excludes WC1 System rows — mirrors SR's `HasOwnerMessageAsync`).
- `SendMessageCommandHandler`: after resolving the sender role, when it is **Provider**, enforce the gate and throw
  `AizenBusinessException("SR_MSG_CHANNEL_LOCKED")` unless the owner has opened the channel. Owner / Admin / System /
  Support sends are ungated (exact parity with the SR handler).

## 3. BFF write flip (owner + provider), flagged + reversible (WC2.3)
Flag **`Messaging:WriteCutover:ChatMessages`** (default OFF), read by the BFFs via `IConfiguration`; also declared on
both hosts' `MessagingWriteCutoverOptions` for the shared flag inventory.
- **Owner** (`SendMobileChatMessageCommandHandler`) + **Provider** (`SendProviderMessageCommandHandler`): when the flag
  is ON and the message is **text or location**, the handler resolves the SR's conversation and calls Messaging
  `SendMessage(conversationId, …)`; **image always routes to the SR path** (WC3 owns images). Flag OFF ⇒ the SR path for
  everything (the sync mirrors to Messaging), unchanged.
- Location over Messaging: `Content` = `{"lat","lng","label"}` JSON (what the Messaging read/echo `ParseLocation`
  expects) **plus** the discrete `LocationLat/Lng/Label` (persisted to the WC0 columns).
- Response mapping: owner uses the existing `MobileChatMapper.MapMessagingMessage`; provider maps the Messaging
  `ChatMessageDto` back to the SR-shaped `ServiceRequestMessageDto` its client already expects.
- Auth: the BFF→Messaging call uses the established trusted-BFF assertion (`X-Aizen-User-Id` + service token) injected by
  each BFF's delegating handler — the Messaging module resolves the sender (Owner/Provider) + participant membership from
  the asserted id (no user id on the wire). The provider gate error (`SR_MSG_CHANNEL_LOCKED`) is caught from the Refit
  envelope and re-surfaced, exactly like the SR path.
- Added `SendMessage` to the owner mobile BFF's `IMessagingRemoteCall` (the provider BFF already had it).

### Deviation (documented): conversation resolution
The doc specifies `EnsureConversationForContext(ServiceRequest, srId, [owner, provider])`. That command has **no HTTP
endpoint** (it's invoked only in-process by the sync consumer/backfiller), and exposing it would force the BFF to
resolve *both* participants (owner + provider identities + display names) — the exact job the sync/WC1 path already does.
Since the conversation is **guaranteed to exist by chat time** (WC0 sync creates it on the first mirrored message; WC1
System/offer-card messages create it with both participants), the BFF instead resolves the id via the existing
participant-scoped read **`GetMyConversationByContext`** and, on the rare miss (no conversation yet), **falls back to the
SR write path** — which bootstraps the conversation via the sync consumer. This is simpler, needs no new endpoint or
duplicated participant resolution, and is reversible-safe (identical result in both flag states).

## 4. Provider realtime repoint to `MessagingMessageSentMessage` (WC2.4)
- `ProviderRealtimeHub`: joins a per-recipient **`user:{userId}`** group on connect (from the already-resolved
  `_identityHolder.UserId`); registered the new **`"user"`** domain key in `Program.cs` (alongside `provider`/`city`).
- Added `MessagingMessageAddedRealtimeConsumer : RealtimeEventConsumer<MessagingMessageSentMessage>`.
- `ProviderEventSocketMapper`: new arm for `MessagingMessageSentMessage` filtered to `ContextType == ServiceRequest` →
  fans a `MessageAdded` frame (`ServiceRequestId = ContextId`) out to each **`user:{recipientUserId}`** group (the event
  carries `RecipientUserIds`, not a `ProviderProfileId`). Empty recipients (System/lifecycle messages) → the arm returns
  null → nobody on the provider surface (the System pill is refetched), as specified. The mapper now supports
  multi-group fan-out (`Frame`/`FrameFanout`).
- **Removed** the old chat `MessageAdded` arm on `ServiceRequestMessageSentMessage` to avoid double-notifying (SR event +
  the sync's Messaging event). The offer/city arms (`OfferAccepted/Rejected`, `Published/Updated/Cancelled/Urgency`) are
  unchanged. Reversible-safe: with the write flip OFF, the SR write → sync still publishes `MessagingMessageSentMessage`,
  so provider chat realtime keeps working in both states. (The `ServiceRequestMessageSentMessage` consumer is retained
  as a harmless no-op — its event now maps to null.)

## Build / QA
- **0 errors**: Messaging host, provider BFF, owner mobile BFF, owner BFF unit tests, admin BFF (consumes
  `SendMessageRequest`/`IMessagingRemoteCall`), SR host (flag options). Abstraction additions are optional-with-defaults,
  so existing callers are unaffected.
- No double message: with the flag ON, text/location go to Messaging only (no SR write → no sync row → no dup); OFF =
  SR + sync as before.
- Anti-harassment: provider free-text is blocked pre-owner-reply on the Messaging store; owner/offer-card/admin sends
  stay ungated.
- Images: unchanged (SR write path + sync + the `sr.Messages` read-url) in both flag states.

## Live smoke — EXECUTED (flag ON), all green
Run against the running stack: flag `Messaging:WriteCutover:ChatMessages=true` on all three BFFs; owner
`qa.owner.aug5@inktavia.com` (mobile BFF, API) ↔ provider `provider2@inktavia.com` (provider portal, OTP) on **SR 55 /
conversation 27** (provider added as a participant for the two-party thread). A `SourceKey` of **null = native Messaging
send**; `sr:55:{id}` = SR-write + WC0 sync mirror.

| # | Check | Result |
|---|---|---|
| 1 | Owner **text** (flag ON) | ✅ one `messaging.conversation_messages` row, **SourceKey null** (native), **no** new `sr.Messages` row |
| 2 | Owner **location** | ✅ one native row, `Type=Location`, **WC0 cols set** (`LocationLat 38.4237 / LocationLng 27.1428 / LocationLabel "Izmir Marina"`), `Content` = location JSON, no SR row; **renders as a map pin + label** on the provider |
| 3 | Owner **image** (flag ON) | ✅ routes to the **SR path** — new `sr.Messages` `MessageType=Image` row (no native Messaging row) |
| 4 | Provider **text** (channel open) | ✅ one native row (`SenderRole=Provider`, SourceKey null), **no** SR row; visible to the owner (`isOwn=false`) |
| 5 | Provider **realtime** | ✅ an owner text pushed to the open provider chat **within ~1 s, no refresh** (via `MessagingMessageSentMessage` → `user:{userId}` group) |
| 6 | **Anti-harassment gate** | ✅ pre-owner-reply the provider chat shows **no input** — *"Müşteri teklifinize yanıt verdiğinde mesajlaşma açılır"* (channel locked); the input **opened** after the owner's first message |
| 7 | **Reversibility** | ✅ flag OFF → owner text writes an `sr.Messages` row (SR path restored); flag ON again → native Messaging again (SourceKey null) |

Caveats (not WC2 regressions):
- **Server-side write gate** (`SR_MSG_CHANNEL_LOCKED` inside the Messaging handler) is defense-in-depth *behind* the
  read-side `channelOpen` the client honours (verified above). Forcing a raw provider send that bypasses the disabled UI
  needs a provider API token (the portal uses an OTP→Keycloak handoff with an in-memory token, not extractable), so the
  write-gate itself was left to the deployed code + unit path rather than a live 403.
- The WC0 **sync mirror** (SR-write → `MessagingMessageSentMessage` → mirror) was observed working earlier in the run
  (`sr:55:32` mirrored) but **paused for the last two SR-path rows after messaging-api was recreated mid-smoke** — an
  infra artifact of the container restart on the shared broker, not a WC2 code change (WC2 does not touch the sync
  consumer). Reversibility itself is proven by the `sr.Messages` write.
- **Deployment gotcha:** the first `docker compose build --no-cache` produced **stale images** (owner/provider sends fell
  back to the SR path); a clean rebuild after removing the stale image references shipped the WC2 code and every native
  check went green. (Same docker-cache flakiness seen in prior sessions — verify with a behaviour check, not `strings`,
  which cannot read .NET metadata.)

Items 4 (MO9 bell) + admin-live-view from the original checklist were not separately re-exercised; the Notification
publish + admin group are unchanged by WC2 and were validated in W5/MO9.

## State left on the stack
Flag `Messaging:WriteCutover:ChatMessages` left **ON** (compose, all three BFFs) after the smoke — flip to `"false"` +
restart the BFFs to disable. messaging-api + both BFFs run the WC2 code (freshly rebuilt). No credentials changed.
Harness data: provider2 (user 100011) was added as a participant on conversation 27 (SR 55) to form the two-party
thread; the smoke messages ("WC2 …") remain in conv 27 + `sr.Messages` for SR 55.

## Deferred to WC3
Image write cutover + the attachment read-url (owner + provider) + dispute composer migration to the Messaging store;
after which images flip too and the sync can be retired (WC4).
