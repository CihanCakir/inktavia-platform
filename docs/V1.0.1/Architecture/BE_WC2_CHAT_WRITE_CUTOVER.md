# BE_WC2 — chat WRITE cutover (owner + provider → Messaging native send) · TEXT + LOCATION

> **Repo:** `addesso-project` (Messaging + BFFs + provider realtime). Phase-4 **WC2** per `PHASE4_WRITE_CUTOVER_PLAN.md`:
> flip the owner + provider chat **write** from the SR module (`SendServiceRequestMessage`) to the **Messaging native
> `SendMessageCommand`**. **Scoped to TEXT + LOCATION** — images are deferred to WC3 (they're coupled to the
> `sr.Messages` read-url). Requires WC0/WC1. Behind a reversible flag. Additive where possible. **Do not commit.**

## Investigated baseline (confirmed in code)
- **`SendMessageCommand` (Messaging):** takes a **`ConversationId`** (must already exist — throws otherwise), resolves
  the sender from `UserInfo.UserId` (must be a conversation participant, or Admin), enforces participant membership,
  runs content-policy, persists a `ConversationMessageEntity`, and publishes **`MessagingMessageSentMessage`**
  (→ admin realtime + Notification) **plus** in-process realtime (`PublishMessageSentAsync`). It supports
  `MessageType.Text` and `MediaAttachment` and has a **`Location`** type that skips text moderation — **but the handler
  does NOT persist `LocationLat/Lng/Label`** (the WC0 columns are still unused; location currently rides JSON-in-Content).
- **Conversation resolution exists:** `EnsureConversationForContextCommand` + `GetMyConversationByContext` + the
  `by-context` endpoints — the BFF can turn an **SR id → conversation id** (get-or-create with owner+provider
  participants), same semantics the sync consumer/backfiller use.
- **The Messaging mirror is complete:** the sync consumer's `MapMessage` already mirrors `AttachmentFileId` +
  `LocationLat/Lng/Label` into Messaging, so **reads are fully served by Messaging** today (provider Phase-3, owner MO10a).
- **`MessagingMessageSentMessage`** carries `ConversationId, ContextType, ContextId (=srId), SenderUserId, SenderName,
  RecipientUserIds, IsInternalNote, SentAt` — **no ProviderProfileId, no Content/Type**.
- **Realtime today:** admin maps `MessagingMessageSentMessage` → single admin group; **provider** maps
  `ServiceRequestMessageSentMessage` → `provider:{ProviderProfileId}` group. Owner gets message realtime via the **MO9
  `NotificationSentMessage` bell** (not via a chat event).
- **Anti-harassment gate** (`HasOwnerMessageAsync`) lives **only** in the SR write handler (provider can't free-text
  until the owner has replied).
- **Image coupling (why images are deferred):** an image sent via the Messaging native path is stored as a
  `MessageAttachmentEntity.FileStorageId`, **not** the SR `AttachmentFileId`; the owner/provider read-url
  access-check still validates against **`sr.Messages`** → a Messaging-only image would 400. Migrating that check is
  **WC3**. So WC2 leaves images on the SR write path (+ sync) and cuts over only text + location, which render from the
  thread DTO (already read from Messaging) with **no per-message read-url**.

## BE — WC2 changes

### 1. Persist location in the Messaging send (parity)
Extend `SendMessageCommand` + handler to accept `LocationLat` / `LocationLng` / `LocationLabel` and persist them onto
the `ConversationMessageEntity` (the WC0 columns) when `Type == Location`. (Keep the existing Content/attachment paths.)
Additive; existing callers (admin/support) pass none.

### 2. Anti-harassment gate on the Messaging store
Add a Messaging repo query **`HasOwnerMessageAsync(conversationId)`** (does the conversation have an **Owner-role**,
non-System message?). In the `SendMessageCommand` handler, when the sender's role is **Provider**, enforce the gate
(reject with the same business error `SR_MSG_CHANNEL_LOCKED`) unless the owner has a message. Owner/Admin/System sends
are ungated (mirrors the SR handler exactly).

### 3. BFF write flip (owner + provider), flagged + reversible
- Behind **`Messaging:WriteCutover:ChatMessages`** (default OFF; reversible):
  - **OFF** → current behaviour (BFF → SR `SendServiceRequestMessage`; sync mirrors to Messaging).
  - **ON** → BFF resolves the conversation via **`EnsureConversationForContext(ServiceRequest, srId, [owner, provider])`**
    → calls Messaging **`SendMessage(conversationId, …)`** with the mapped sender role, for **text + location**.
    **Images still route to the SR path** even when ON (until WC3) — the BFF branches: image → SR send; text/location →
    Messaging send.
- Owner (`MobileChatController`, MO10a) + provider (`SendProviderMessage`) both flip. Identity from the token
  (BffAssertion); the sender must be a conversation participant (EnsureConversation guarantees it).
- Auth for the BFF→Messaging call uses the established service-token path (consistent with the S2S fixes).

### 4. Repoint provider realtime + notification to `MessagingMessageSentMessage`
- **Provider realtime:** add a **per-recipient user group** `user:{userId}` to `ProviderRealtimeHub` (joined on
  connect from the resolved provider **Identity user id**), and route **`MessagingMessageSentMessage`** (filter
  `ContextType == ServiceRequest`) → the `RecipientUserIds`' user groups as a thin `MessageAdded` frame
  (ServiceRequestId = `ContextId`). Add the matching `RealtimeEventConsumer<MessagingMessageSentMessage>`. Keep the
  existing offer/city events on `ServiceRequestMessageSentMessage`.
  - **Why user-group, not `provider:{profileId}`:** `MessagingMessageSentMessage` carries no ProviderProfileId; it
    carries `RecipientUserIds`. Targeting the recipient user id is the clean, profile-independent route.
  - **Reversible-safe:** this works in **both** flag states — with the flag OFF, the SR write → sync →
    `MessagingMessageSentMessage` still fires, so the provider realtime keeps working; with it ON, the native send
    fires it directly. So the realtime repoint is decoupled from the write flip.
- **Notification:** already fires from the Messaging send (`RecipientUserIds` = participants − sender → the Notification
  `MessagingMessageSentConsumer`). Confirm owner + provider both get `NewMessageReceived`; no change expected.
- **System messages** (WC1) publish `MessagingMessageSentMessage` with **empty** `RecipientUserIds` → the provider
  user-group route targets nobody (correct — System is a lifecycle pill, refetched; admin still sees it on its group).

## Don't-break / QA
- **Images unchanged** (SR write path + sync + the sr.Messages read-url) — verify an image still sends + displays with
  the flag ON. Text + location cut over to Messaging.
- **Reversible:** flag OFF restores the SR write; the realtime repoint works in both states (sync republishes
  `MessagingMessageSentMessage`).
- **No double message:** with the flag ON, text/location go to Messaging **only** (no SR write → no sync row → no
  dup); with OFF, SR + sync as before. Confirm a cutover message appears **once** on owner + provider + admin.
- **Anti-harassment gate** still blocks provider free-text pre-owner-reply (now on the Messaging store), and the offer
  card / owner sends stay ungated.
- Tests: (1) Messaging + BFFs + provider realtime + solution build 0 errors; (2) `SendMessage` persists location
  coords (WC0 cols) for `Type==Location`; (3) provider send blocked pre-owner-reply, allowed after (Messaging gate);
  (4) BFF flag ON → owner/provider **text + location** land via Messaging (one row, no SR row, no sync dup), images
  still via SR; (5) provider realtime fires for an owner text via `MessagingMessageSentMessage` (flag ON and OFF);
  (6) owner gets the MO9 bell + notification; (7) admin still sees the message live; (8) flag OFF → prior behaviour.
  Live smoke (owner↔provider text + location, flag ON, all three surfaces + realtime) needs the stack — document it.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC2_CHAT_WRITE_CUTOVER.md`: the location-persist extension, the Messaging
anti-harassment gate, the BFF flagged write flip (text/location→Messaging, image→SR), the provider-realtime repoint
(user-group targeting on `MessagingMessageSentMessage`, reversible-safe), and the tests. **Deferred to WC3:** the image
write cutover + the attachment read-url (owner+provider) + dispute composer migration to the Messaging store — after
which images can flip too and the sync can be retired (WC4).
