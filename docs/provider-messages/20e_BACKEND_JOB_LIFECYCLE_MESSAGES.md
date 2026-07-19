# 20e — Backend: JOB_STARTED / JOB_COMPLETED lifecycle messages

Finishes 20c. Two more lifecycle system messages, using the **exact pattern already implemented for
`OFFER_ACCEPTED`** in `AcceptServiceRequestOfferCommandHandler` (20c). Small, additive, two handlers.

## Pattern to copy (already in the codebase — 20c)
In `AcceptServiceRequestOfferCommandHandler`:
```csharp
if (!await _msgRepository.HasSystemMessageAsync(sr.Id, "OFFER_ACCEPTED", ct)) {
    var msg = ServiceRequestMessageEntity.Create(
        sr.Id, currentUserId, ServiceRequestMessageSenderType.System,
        ServiceRequestMessageType.StatusChange, "OFFER_ACCEPTED", null);
    await _msgRepository.AddAsync(msg, ct);
    await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage {
        ServiceRequestId = sr.Id, MessageId = msg.Id,
        SenderType = ServiceRequestMessageSenderType.System,
        ProviderProfileId = <provider profile id>
    }, ct);
}
```
Mirror it in the two handlers below. Idempotent per (sr, code); `SenderType.System`, `MessageType.StatusChange`,
content = the code. The realtime consumer already forwards System messages (20c), so the pill reaches the provider
live — as long as the bus message carries the correct `ProviderProfileId`.

## Work

### 1. `JOB_STARTED` — `StartServiceRequestAssignmentCommandHandler`
- Inject `IServiceRequestMessageRepository` + `IAizenMessagePublisher` (as 20c did to AcceptOffer).
- After `assignment.Start()` / status → InProgress, create the system message with code **`JOB_STARTED`**, guarded by
  `HasSystemMessageAsync(sr.Id, "JOB_STARTED")`.
- `ProviderProfileId` is already available here: **`assignment.ProviderProfileId`** (the assignment being started).
- Publish `ServiceRequestMessageSentMessage { ..., SenderType.System, ProviderProfileId = assignment.ProviderProfileId }`.

### 2. `JOB_COMPLETED` — `ApproveServiceRequestCompletionCommandHandler`
- Same injections. After `completion.ApproveByOwner(...)` / status → Completed, create the system message with code
  **`JOB_COMPLETED`**, guarded by `HasSystemMessageAsync(sr.Id, "JOB_COMPLETED")`.
- `ProviderProfileId`: resolve from the assignment on this SR
  (`_assignmentRepository.GetByServiceRequestIdAsync(sr.Id)` → `.ProviderProfileId`) — the same source the accepted
  offer/assignment uses. If an assignment repository isn't already injected here, inject it (read-only lookup).
- Publish the bus message with that `ProviderProfileId`.

## Constraints
- Idempotent per (serviceRequestId, code) — no duplicate on retry/re-run.
- `SenderType.System` — exempt from the #18 gate (does not open the channel).
- Codes stay codes (`JOB_STARTED` / `JOB_COMPLETED`); the SPA already maps them to
  "Anlaşılan iş başlatıldı" / "Anlaşılan iş tamamlandı".
- No content on the realtime frame. No migration needed (reuses existing message table).
- Do **not** change `CONVERSATION_CLOSED` behaviour — completion shows `JOB_COMPLETED`; the conversation stays open
  (only cancel/close/expire emits `CONVERSATION_CLOSED`).

## Acceptance — observed
- Start an assignment → exactly one `JOB_STARTED` StatusChange message on that SR (idempotent on re-run); provider
  receives a content-free `MessageAdded`.
- Approve completion → exactly one `JOB_COMPLETED` message; provider notified.
- `GET /provider/service-requests/conversations` reflects the latest `LifecycleStatus` (Started → then Completed).
- The two pills render in the thread ("Anlaşılan iş başlatıldı", "Anlaşılan iş tamamlandı").

## Report
Append to `REPORT_BACKEND.md` ("20e"): both messages appearing idempotently with the correct `ProviderProfileId`
on the bus, and the conversation `LifecycleStatus` transitioning. Remove the "JOB_STARTED / JOB_COMPLETED — not
wired" line from the NOT-done table. Unfinished is **not done**.
