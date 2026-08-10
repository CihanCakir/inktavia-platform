# BE_WC1 — System/lifecycle messages generated in Messaging (from SR domain events)

> **Repo:** `addesso-project` (Messaging module + a small additive event in the ServiceRequest module). Phase-4 **WC1**
> per `PHASE4_WRITE_CUTOVER_PLAN.md`: make the **Messaging module** produce the thread's System/lifecycle messages
> from **first-class SR domain events**, so that when WC4 removes the SR chat write + the sync consumer, the System
> messages still appear. **Parallel-then-flip**, idempotent on the WC0 `SourceKey` unique index. Requires WC0.
> Additive; flagged. **Do not commit.**

## Prerequisite (do this first)
**Fix the pre-existing `Messaging.Application.UnitTests` compile break** before adding any WC1 test — its
`IConversationRepository` test fake is missing newer interface members (a pre-existing break surfaced in the WC0
report, **not** caused by WC0; git status confirms it predates this work). WC1 adds Messaging consumers + tests, so the
test project must compile first: update the fake to implement the current `IConversationRepository` surface (test-only,
no production change) so the suite builds, then add the WC1 tests.

## Baseline (investigated — the 5 lifecycle messages + their event coverage)
Today five SR commands create a thread message in `sr.Messages` and publish `ServiceRequestMessageSentMessage`
(mirrored into Messaging by the sync consumer). Each has a **code**:

| Code (thread) | SR command | Message shape | First-class domain event? |
|---|---|---|---|
| `OFFER` (offer card) | `SubmitOffer` | Sender **Provider**, Type **Offer**, content `offer:{offerId}|{total} {ccy}` | `ServiceRequestOfferCreatedMessage` **exists** (confirm SubmitOffer publishes it; if it only publishes the chat event, add the publish — additive) |
| `OFFER_ACCEPTED` | `AcceptServiceRequestOffer` | Sender System, Type StatusChange | `ServiceRequestOfferAcceptedMessage` ✅ |
| `JOB_STARTED` | `StartServiceRequestAssignment` | Sender System, Type StatusChange | **NONE** — only the chat event today ⇒ **add** `ServiceRequestAssignmentStartedMessage` (additive) |
| `JOB_COMPLETED` | `ApproveServiceRequestCompletion` | Sender System, Type StatusChange | `ServiceRequestCompletionApprovedMessage` ✅ |
| `CONVERSATION_CLOSED` | `CancelServiceRequest` | Sender System, Type StatusChange | `ServiceRequestCancelledMessage` ✅ |

**The gap:** `JOB_STARTED` (and possibly the `OFFER` card) have no clean dedicated event — they ride only
`ServiceRequestMessageSentMessage`. WC1 must give every lifecycle message a **first-class event** so the System-message
generation is decoupled from the chat-event byproduct (which WC4 deletes).

## BE — WC1 (two small SR additions + Messaging consumers)
1. **Close the event gap in the SR module (additive, no behaviour change):**
   - Add **`ServiceRequestAssignmentStartedMessage`** (fields: `ServiceRequestId`, `RequestCode`, `AssignmentId`,
     `ProviderProfileId`, `OccurredAt`) and publish it from `StartServiceRequestAssignment` **alongside** the existing
     publishes (do not remove anything yet).
   - Confirm `SubmitOffer` publishes `ServiceRequestOfferCreatedMessage` (`OfferId`, `TotalAmount`, `CurrencyCode`,
     `ProviderProfileId`); if it currently only emits the chat event for the offer card, add the domain-event publish
     (additive).
2. **Messaging consumers — one per lifecycle event** (zero-logic-beyond-mapping; auto-discovered by the messagebus
   scan, like the sync consumer): each `EnsureConversationForContext(ServiceRequest, srId, participants)` then writes a
   **System** (or **Provider/Offer** for the offer card) `ConversationMessageEntity` with:
   - `SourceKey = "sys:{srId}:{CODE}"` (offer card → `"sys:{srId}:OFFER:{offerId}"`) → **idempotent via the WC0
     partial unique index** (a redelivered event or the parallel sync-consumer row collapses to one).
   - Content: the StatusChange code string as today (`OFFER_ACCEPTED`, `JOB_STARTED`, `JOB_COMPLETED`,
     `CONVERSATION_CLOSED`); the offer card reconstructs `offer:{offerId}|{total} {ccy}` from the event.
   - `SenderRole` mapped (System; Provider for the offer card); `Type` mapped — ensure the Messaging `MessageType` has
     **StatusChange** + **Offer** members (additive if missing; WC0 added Location).
   - **No new notifications** for System/offer (RecipientUserIds empty — matches the existing notification-fix: System/
     lifecycle/Provider-self are silent).
3. **Parallel run (safe by construction):** both paths now write the same System message with the **same `SourceKey`**
   → the unique index keeps exactly one row. Deploy, exercise each lifecycle transition, verify the thread shows each
   System message **once**, correct order/content, on admin + provider + owner (all read from Messaging).
4. **Flip (flagged):** behind `Messaging:WriteCutover:SystemMessages` (from WC0), stop the 5 SR commands from creating
   `sr.Messages` System/offer rows **and** from publishing the System/offer `ServiceRequestMessageSentMessage`. Once
   on, System messages come **only** from the Messaging consumers. Leave the flag reversible (off → SR resumes + sync
   mirrors, the prior working state).

## Don't-break / QA
- Additive until the flag flips: new SR event + Messaging consumers run in parallel with the old path, deduped by
  `SourceKey`. The user chat send (`SendServiceRequestMessage`) is **untouched** (that's WC2). Reads unaffected.
- **Idempotency:** relies on the WC0 `(ConversationId, SourceKey)` partial unique index — WC1 must not merge without it.
- **No duplicate / no missing System messages** across: parallel run, redelivery, multi-replica, and the flag flip.
- Tests: (1) Messaging + SR + solution build 0 errors; (2) each lifecycle event → exactly one System message with the
  right `SourceKey`/content/role/type; (3) redelivery + parallel sync-consumer row → no duplicate (unique index);
  (4) the new `ServiceRequestAssignmentStartedMessage` is published by StartAssignment and consumed → `JOB_STARTED`;
  (5) offer card reconstructs `offer:{id}|{total} {ccy}` from `ServiceRequestOfferCreatedMessage`; (6) with the flag
  **on**, the SR commands no longer write `sr.Messages` System rows and the thread still shows every System message
  (from Messaging); with it **off**, prior behaviour; (7) no new notifications fire for System/offer. Live smoke
  (accept an offer → `OFFER_ACCEPTED` pill appears once on all three surfaces) needs the stack — document it.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC1_SYSTEM_MESSAGES_IN_MESSAGING.md`: the new `ServiceRequestAssignmentStartedMessage`
(+ confirmed `OfferCreated` publish), the Messaging lifecycle consumers (SourceKey scheme, content/role/type mapping,
Offer/StatusChange enum additions), the parallel-run parity evidence, the `Messaging:WriteCutover:SystemMessages` flip
behaviour, and the tests. Then **WC2** (user chat write cutover — owner+provider BFF → Messaging native send with
text/image/location + the anti-harassment gate on the Messaging store; provider realtime + notification repoint to
`MessagingMessageSentMessage`).
