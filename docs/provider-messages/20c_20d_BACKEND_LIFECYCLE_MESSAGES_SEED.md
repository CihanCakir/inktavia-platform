# 20c + 20d — Backend: lifecycle system messages + a realistic conversation seed

Completes #20. 20a (inbox) + 20b (location/image) are done. This adds the **lifecycle system messages** that show
the deal timeline in the thread, and a **seed** so the Messages page is fully populated. Provider-scoped; keeps the
#18 gate. Run 20c, then 20d.

## Verified pattern to mirror (from #18 SubmitOffer — reuse it)
```csharp
var hasOfferMsg = await _messageRepository.HasOfferMessageForOfferAsync(sr.Id, offer.Id, ct);   // idempotency
if (!hasOfferMsg) {
    var m = ServiceRequestMessageEntity.Create(
        sr.Id, currentUserId, ServiceRequestMessageSenderType.Provider,
        ServiceRequestMessageType.Offer, $"offer:{offer.Id}|{offer.GrandTotal:F2} {offer.CurrencyCode}", null);
    await _messageRepository.AddAsync(m, ct);
}
```
Lifecycle messages follow the same shape but with `SenderType.System`, `MessageType.StatusChange`, and a **machine
code** as content (the SPA translates; codes stay codes).

---

## Phase 20c — Lifecycle system messages

### 1. Idempotency helper (repository)
Add `Task<bool> HasSystemMessageAsync(long serviceRequestId, string statusCode, CancellationToken ct = default)`
to `IServiceRequestMessageRepository` (+ impl): true when a `MessageType.StatusChange` message with
`Content == statusCode` already exists on the request. Guards against duplicates on ret/re-run.

### 2. Emit at each existing lifecycle handler
Create one System message (guarded by `HasSystemMessageAsync`) in these handlers:

| Handler (exists) | Code (content) | When |
|---|---|---|
| `AcceptServiceRequestOfferCommandHandler` | `OFFER_ACCEPTED` | owner accepts the offer |
| `StartServiceRequestAssignmentCommandHandler` | `JOB_STARTED` | provider/assignment starts the job |
| `ApproveServiceRequestCompletionCommandHandler` | `JOB_COMPLETED` | owner approves completion |
| terminal close — `CancelServiceRequest…` / request → `Cancelled`/`Closed`/`Expired` | `CONVERSATION_CLOSED` | request reaches a terminal, non-completed close |

`ServiceRequestMessageEntity.Create(sr.Id, actorUserId, SenderType.System, MessageType.StatusChange, "<CODE>", null)`.
`actorUserId` = the acting user (owner/provider) already resolved in that handler. Idempotent per (sr, code).

> `JOB_COMPLETED` (completion approved) and `CONVERSATION_CLOSED` (cancel/close/expire) are **distinct**. A normal
> successful job shows `JOB_COMPLETED`; the conversation only reads "closed" on a terminal close (Cancelled/Closed/
> Expired). If you want completion to also close the conversation, emit `CONVERSATION_CLOSED` there too — flag it,
> don't assume.

### 3. Provider realtime — forward System messages (bug to fix)
`MessageAddedRealtimeConsumer` currently early-returns unless `SenderType == Owner`:
```csharp
if (message.SenderType != ServiceRequestMessageSenderType.Owner) return;   // ← lifecycle System msgs never reach the provider
```
Change so it also forwards **System** messages: notify when `SenderType is Owner or System`. (Still content-free
frame; the SPA refetches the thread + inbox.) For that, the published `ServiceRequestMessageSentMessage` must carry
the **ProviderProfileId** of the conversation's provider — resolve it from the accepted offer / assignment on the SR
in each handler (same way #18 resolves it), and publish the bus message after creating the system message. Without
this, the pill appears only on manual refresh, not live.

### 4. Feeds 20a
20a's `LifecycleStatus` already projects the latest lifecycle code — once these rows exist it reflects
Offered→Accepted→Started→Completed / Closed. `CONVERSATION_CLOSED` → the SPA locks the composer (already handled
front-end via a closed system message; also expose `LifecycleStatus=="Closed"` so the inbox can badge it).

### Acceptance — 20c
- Accept an offer → exactly one `OFFER_ACCEPTED` StatusChange message (idempotent on re-run). Start → `JOB_STARTED`.
  Approve completion → `JOB_COMPLETED`. Cancel/close → `CONVERSATION_CLOSED`.
- Each emits a **content-free** `MessageAdded` that the provider actually receives (consumer now forwards System).
- `GET /provider/service-requests/conversations` shows the latest `LifecycleStatus` for the conversation.

---

## Phase 20d — Seed a realistic conversation

On a biddable request **provider2** can see (reuse **SR 9011** — it already has the 14c attachment file), seed one
idempotent conversation (fixed ids, guard on existence). Messages, in order (set `CreatedAt` a few minutes apart):

1. **Owner text** — e.g. "Merhaba, teklifinizi aldım." (`SenderType.Owner, MessageType.Text`) → this **opens the
   channel** (`channelOpen=true`).
2. **Provider text** — e.g. "Tabii, nasıl yardımcı olabilirim?" (`SenderType.Provider, MessageType.Text`).
3. **Owner image** — `SenderType.Owner, MessageType.Image`, `AttachmentFileId =
   a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4` (the existing 14c seeded FileStorage image), so it renders via the
   attachment read-url.
4. **Owner location** — `SenderType.Owner, MessageType.Location`, `LocationLat=38.3235, LocationLng=26.3050,
   LocationLabel="Çeşme Marina"`.
5. **System** — `OFFER_ACCEPTED` (`SenderType.System, MessageType.StatusChange`, content `OFFER_ACCEPTED`).

Leave at least one owner message **unread** so the inbox shows a badge. Idempotent: guard on a fixed first-message
id / a "seeded" marker so re-boot doesn't duplicate.

### Acceptance — 20d
- `GET …/conversations` → SR 9011 with `channelOpen=true`, `unread≥1`, a sensible `LastMessagePreview`,
  `LifecycleStatus="Accepted"`.
- `GET …/9011/messages` → the 5 messages in order: owner text, provider text, image (AttachmentFileId set),
  location (lat/lng/label), and the `OFFER_ACCEPTED` StatusChange.
- The image's `AttachmentFileId` resolves through `GET …/attachments/{fileId}/read-url` (14c) to the seeded object.
- Provider can now **send** (channel open) — a provider Text message persists and returns.

---

## Constraints
- Provider-scoped; ownership enforced in the module. System/Offer messages are **exempt from the gate** (they don't
  open the channel — only an Owner message does). Provider free-text/location still requires `channelOpen`.
- Codes stay codes (SPA translates OFFER_ACCEPTED/JOB_STARTED/JOB_COMPLETED/CONVERSATION_CLOSED). No customer identity.
- Idempotent seed + idempotent lifecycle messages. Migrations (if any) ship `.Designer.cs`.
- No content on realtime frames.

## Report
Append to `REPORT_BACKEND.md` ("20c/20d"): the four lifecycle rows appearing idempotently + reaching the provider
via MessageAdded (consumer forwards System), the seeded conversation dump (5 messages, unread, LifecycleStatus), and
a provider send succeeding once the channel is open. Unfinished is **not done**.

## Frontend (I will do after this lands)
Wire the image bubble to the real thumbnail via the 14c attachment read-url (currently a placeholder); verify the
full thread on screen — owner/provider bubbles, offer card, image, location pin, and the `OFFER_ACCEPTED` pill — plus
an actual provider send now that the channel is open.
