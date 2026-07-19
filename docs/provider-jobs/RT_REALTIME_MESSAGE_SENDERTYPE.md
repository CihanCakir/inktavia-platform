# RT — Realtime frame: carry `SenderType` on MessageAdded (BFF-only, tiny)

The provider toast for a `MessageAdded` realtime frame always reads "Müşteri size yanıt verdi." — but the same frame is
pushed for **System lifecycle** messages (JOB_STARTED / JOB_COMPLETED …), so starting your own job toasts a misleading
"customer replied." The consumer already knows the sender; just put it on the frame so the SPA can pick the right text.

## Verified in source (2026-07-17)
- `MessageAddedRealtimeConsumer` (BFF) forwards for `SenderType ∈ { Owner, System }` and builds a
  `ProviderRealtimeEvent { EventType = MessageAdded, ServiceRequestId }` — **no sender field**.
- The bus message `ServiceRequestMessageSentMessage` carries `SenderType` (and `ServiceRequestId`), but **not** the system
  code (JOB_STARTED etc.) — the code lives in the message body, off the bus. So a generic System text is the right scope;
  per-code text is out of scope here.
- `ProviderRealtimeEvent` (BFF DTO) has EventType/ServiceRequestId/RequestCode/Title/City/Marina/OfferId/OccurredAt.

## Work (BFF only — no module change)
1. Add `public string? MessageSenderType { get; init; }` to `ProviderRealtimeEvent`.
2. In `MessageAddedRealtimeConsumer`, set `MessageSenderType = message.SenderType.ToString()` ("Owner" / "System") on the
   frame it sends. Nothing else changes — still content-free, still Owner-or-System only.

## Constraints
- Metadata only (sender role), **not** message content — keeps the deliberate content-free frame contract.
- No module changes, no new events. Idempotent/stateless as before.

## Acceptance — observed
- Provider starts their own job → the `MessageAdded` frame now carries `messageSenderType: "System"`; a customer reply
  carries `"Owner"`. (Frontend already maps: System → "İş güncellemesi", Owner → "Müşteri size yanıt verdi" — falls back
  to the customer text if the field is absent.)

## Report
Append to `REPORT_BACKEND.md` ("RT realtime senderType"): the frame now includes `MessageSenderType` and its value for a
System vs Owner message. One-line change; confirm the toast differentiation end to end after a BFF rebuild.
