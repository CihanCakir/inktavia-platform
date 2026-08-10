# REPORT — BE_WC1 — System/lifecycle messages generated in Messaging

> **Repo:** `addesso-project` (Messaging module + small additive ServiceRequest events). **Status:** implemented, **not
> committed**. Additive + flagged; parallel-then-flip; idempotent on the WC0 `(ConversationId, SourceKey)` partial
> unique index. Requires WC0.

## Goal
Make the **Messaging module** generate the thread's five System/lifecycle messages from **first-class SR domain
events**, so that when WC4 removes the SR chat write + the sync consumer, the System messages still appear. Both paths
run in parallel and **collapse to one row per lifecycle message** via a shared `sys:{srId}:{CODE}` SourceKey.

## Prerequisite (done first)
Fixed the pre-existing `Messaging.Application.UnitTests` compile break: `FakeConversationRepository` now implements the
three newer `IConversationRepository` members (`GetListForParticipantAsync`, `CountForParticipantAsync`,
`GetByContextWithMessagesAsync`) as `NotSupportedException` stubs (test-only, no production change). The suite compiles
again — a prerequisite for adding WC1 tests.

## 1. ServiceRequest module — two additive events (no behaviour change)
- **`ServiceRequestAssignmentStartedMessage`** (new): `ServiceRequestId, RequestCode, AssignmentId, ProviderProfileId,
  StartedByUserId, OccurredAt`. Published from `StartServiceRequestAssignment` **alongside** the existing WorkStarted
  realtime + the (gated) chat event. Closes the `JOB_STARTED` event gap (it previously rode only the chat event).
- **`ServiceRequestOfferSubmittedMessage`** (new): `ServiceRequestId, OfferId, ProviderProfileId, ProviderUserId,
  TotalAmount, CurrencyCode, OccurredAt`. Published from `SubmitOffer` (the exact point the offer card is created).
  - **Deviation from the spec's tentative naming (documented):** the spec suggested publishing
    `ServiceRequestOfferCreatedMessage` from `SubmitOffer`. Investigation showed that event is already published by
    **`CreateServiceRequestOffer` for a *Draft* offer** and is **consumed by the Notification module**. Reusing it from
    `SubmitOffer` would (a) fire the card at draft-time with no submit guarantee and (b) **double-fire the provider
    offer notification** — violating WC1's "no new notifications". A dedicated submit event is the decoupled, silent,
    final-total trigger the card needs. `OfferCreated`/`CreateServiceRequestOffer` are left untouched.

## 2. Messaging module — five lifecycle consumers + a shared writer
`Modules/Messaging/src/Aizen.Modules.Messaging/Consumers/ServiceRequest/Lifecycle/`:
- **`ServiceRequestLifecycleMessageWriter`** (scoped): resolves owner/provider from the SR schema (read-only raw SQL,
  same pattern as the sync consumer), `EnsureConversation`-style get-or-create, writes ONE
  `ConversationMessageEntity` with `SourceKey = sys:{srId}:{code}`, swallows the `23505` unique-violation as a benign
  no-op, and on a genuine insert republishes `MessagingMessageSentMessage` with **empty `RecipientUserIds`** (admin
  realtime fires once; no notification). An in-process short-circuit skips a guaranteed-duplicate round-trip on
  same-replica redelivery; the DB index is the authoritative guard.
- **Five zero-logic consumers** (auto-discovered by the messagebus scan, like `ServiceRequestMessageSyncConsumer`):

  | Consumer ← event | Code / `SourceKey` | Role · Type · Content |
  |---|---|---|
  | `…OfferSubmittedCardConsumer` ← `ServiceRequestOfferSubmittedMessage` | `OFFER:{offerId}` → `sys:{srId}:OFFER:{offerId}` | Provider · StatusChange · `offer:{offerId}\|{total:F2} {ccy}` |
  | `…OfferAcceptedSystemMessageConsumer` ← `ServiceRequestOfferAcceptedMessage` | `OFFER_ACCEPTED` | System · StatusChange · `OFFER_ACCEPTED` |
  | `…AssignmentStartedSystemMessageConsumer` ← `ServiceRequestAssignmentStartedMessage` | `JOB_STARTED` | System · StatusChange · `JOB_STARTED` |
  | `…CompletionApprovedSystemMessageConsumer` ← `ServiceRequestCompletionApprovedMessage` | `JOB_COMPLETED` | System · StatusChange · `JOB_COMPLETED` |
  | `…CancelledSystemMessageConsumer` ← `ServiceRequestCancelledMessage` | `CONVERSATION_CLOSED` | System · StatusChange · `CONVERSATION_CLOSED` |

- **Enum decision (documented):** the offer card is stored as **`Provider` + `StatusChange` + `offer:` content** —
  byte-identical to the existing SR→Messaging mapping (`MapMessageType` collapses SR `Offer`→Messaging `StatusChange`;
  the read side renders the card from the `offer:` content, not the type). So **no new `Offer` enum member is added**:
  adding one and repointing the card to it would change the DTO's `Type` string and diverge from the sync-consumer row
  during the parallel run. `StatusChange` (required) already exists; `Location` was added in WC0.

## SourceKey convergence (the crux of "no duplicate / no missing")
The two producers must land on the **same** key for the same logical message:
- WC1 consumers key straight off the event (`sys:{srId}:{CODE}`, offer → `sys:{srId}:OFFER:{offerId}`).
- The **sync consumer** now derives the same key from the mirrored chat row via
  `ServiceRequestMessageMapping.LifecycleCode(senderType, messageType, content)` — lifecycle rows (System pills +
  offer card) key on `sys:{srId}:{CODE}`; regular chat (Text/Image/Location) still keys on `sr:{srId}:{srMessageId}`.
Result: during the parallel run the sync row and the WC1 row **collapse to one** via the WC0 unique index, whichever
inserts first; the loser 23505s and stays silent (one admin-realtime frame).

## 4. Flip (flagged, reversible)
Behind `Messaging:WriteCutover:SystemMessages` (renamed from WC0's `GenerateSystemMessages` to match the plan's
canonical key; read by the **SR host** via a small SR-side `MessagingWriteCutoverOptions` bound from the shared
`Messaging:WriteCutover` section, `IOptionsMonitor` for live reversibility):
- **OFF (default):** the 5 SR commands keep writing their `sr.Messages` System/offer rows **and** publishing the
  System/offer `ServiceRequestMessageSentMessage` (prior behaviour) → sync mirrors → same `sys:` key → one row. The new
  domain events are **always** published (additive) → the WC1 consumers also write → same key → still one row.
- **ON:** the 5 SR commands **skip** the `sr.Messages` System/offer write **and** the chat-event publish; the System/
  offer messages come **only** from the Messaging lifecycle consumers (driven by the always-published domain events).
- Flip back to OFF and the SR path resumes — the prior working state.

The user chat send (`SendServiceRequestMessage`) is **untouched** (that's WC2).

## Verification
- **(1) Build 0 errors** — the full `Aizen.sln` builds **0 errors** (Messaging + ServiceRequest + all modules).
- **Unit tests** — `Messaging.Application.UnitTests`: **15 passed** (2 pre-existing + 13 new
  `ServiceRequestLifecycleSourceKeyTests` covering `LifecycleCode` mapping for all 5 codes + regular-chat→null + the
  sync⇄WC1 key convergence for System messages and the offer card). `ServiceRequest.Application.UnitTests`: **178
  passed, 0 failed** (the 5 handler ctor changes broke nothing).
- **(2)(3)(6) Idempotency validated on PostgreSQL** — a scratch DB with the post-WC0 `SourceKey` column + partial
  unique index confirmed, against the **actual** index:
  - parallel run (sync row + WC1 row, same `sys:101:OFFER_ACCEPTED`) → **one row** (`23505`);
  - redelivery of a WC1 event → **no-op**;
  - the offer card (`sys:101:OFFER:5001`, Provider/StatusChange) collapses the parallel sync row → **one row**;
  - the five distinct codes → **exactly 5 rows**, correct role/type/content;
  - a second SR (different conversation) with the same code does **not** collide.
- **(4) `JOB_STARTED`** now flows from the first-class `ServiceRequestAssignmentStartedMessage` (published by
  `StartAssignment`) → its consumer → `sys:{srId}:JOB_STARTED`.
- **(5) Offer card** reconstructs `offer:{offerId}|{total:F2} {ccy}` from the submit event (unit-tested + SQL row).
- **(7) No new notifications** — all five consumers publish `MessagingMessageSentMessage` with empty `RecipientUserIds`
  (Notification returns early); no `SendNotificationCommand` is issued by WC1.
- **(6/live smoke — needs the running stack, documented):** with the flag ON, exercise each transition (submit offer,
  accept, start, approve completion, cancel) and confirm each System/offer message appears **once** on admin + provider
  + owner (all read from Messaging) and that the SR `sr.Messages` System/offer rows are no longer written; flip OFF and
  confirm prior behaviour. Requires messaging-api + servicerequest-api + the bus + Redis; not runnable from this
  workstation.

## Next — WC2
User chat write cutover: owner + provider BFF → Messaging **native send** (text/image/location) with the
anti-harassment gate on the Messaging store; provider realtime + notification repoint to `MessagingMessageSentMessage`.
Then WC3 (repoint the `sr.Messages` readers per the WC0 inventory) and WC4 (retire the 5 SR System writers + the sync
consumer + its semaphore).
