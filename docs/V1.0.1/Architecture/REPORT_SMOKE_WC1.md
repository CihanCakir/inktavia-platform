# REPORT — SMOKE WC1 (live) — System/lifecycle messages in Messaging

> **Date:** 2026-08-10. **Stack:** local docker `inktavia` (Postgres `inktavia_store`, RabbitMQ, Redis, Keycloak,
> messaging-api :7109, service-request-api :7107, mobile BFF :17003, provider portal :3002). **Nothing committed; no
> credentials changed.**
> **Outcome: the WC1 mechanism is verified live and one real bug was found + fixed. `CONVERSATION_CLOSED` passes end-to-end
> in both flag states (+ reversibility). The offer/accept/start/complete codes are blocked by two environment/design
> issues (a pre-existing Payment S2S 401, and an OFFER-card wiring gap) — both documented with fixes below.**

## Deploy (rebuilt from the working tree — the running images predated the code)
`docker compose build/up -d messaging-api service-request-api`. Verified live: WC0 migration applied (`SourceKey` +
location cols + the partial unique index), all **5 WC1 lifecycle consumers registered** on the RabbitMQ/MassTransit bus,
both new event-type exchanges present, services healthy.

## Accounts / auth
- **Owner** `qa.owner.aug5@inktavia.com` — mobile BFF password login (6 vessels). Drives create / accept / cancel.
- **Provider** `provider2@inktavia.com` — **OTP login via the provider portal (:3002) in the browser**; the Keycloak
  passwordless handoff **completes in the browser** (keycloak-js PKCE) even though the API-only `verify` returns
  `keycloak_handoff_required`. Provider actions were driven via `fetch` **inside the authenticated page** so the token
  never left the browser (not extracted, not logged — per instruction; the tooling also hard-blocks materializing it).

## 🐞 Bug found + fixed (the `CONVERSATION_CLOSED` failure)
**Symptom:** cancelling a fresh SR wrote **no** Messaging row; both consumers rolled back with
*"The property 'ConversationEntity.Id' has a temporary value while attempting to change the entity's state to
'Modified'."*
**Cause:** the WC1 writer **and** the pre-existing sync consumer called `_db.Conversations.Update(conv)` on a
**newly-created** conversation (temporary identity) — this fires whenever a lifecycle event is the **first** message on
an SR (no conversation yet). The backfiller already guarded this; these two did not.
**Fix:** `Update()` only a pre-existing conversation (`if (!created)`), matching the backfiller — in
`ServiceRequestLifecycleMessageWriter` and `ServiceRequestMessageSyncConsumer`. Rebuilt + redeployed. **Verified fixed**
(row now written; see below).

## Per-code live results
Legend: **Msg row** = `messaging.conversation_messages` where `SourceKey = sys:{srId}:{CODE}`. **Dupe** = per-conversation
`(SourceKey)` count > 1. **SR-store** = `servicerequest.service_request_messages` `SenderType=System`.

| Code | Flag OFF | Flag ON | Verdict |
|---|---|---|---|
| **CONVERSATION_CLOSED** (owner cancel) | Msg row **1** (System·StatusChange·`CONVERSATION_CLOSED`), dupe **0**, SR-store **present** | Msg row **1**, dupe **0**, SR-store **absent** (Messaging sole producer), no `_skipped`/`23505` | ✅ **PASS** both states |
| **OFFER** (provider submit) | — | not produced | ⚠️ **wiring gap** (below) |
| **OFFER_ACCEPTED** (owner accept) | — | blocked | ⛔ **Payment S2S 401** (below) |
| **JOB_STARTED** (provider start) | — | blocked | ⛔ needs the accept (assignment) |
| **JOB_COMPLETED** (approve completion) | — | blocked | ⛔ needs the accept + completion |

- **CONVERSATION_CLOSED (OFF, SR 48/50)**: exactly one `sys:{srId}:CONVERSATION_CLOSED` row (`SenderRole=System`,
  `Type=StatusChange`), dupe 0, SR-store row present. Both producers (sync + WC1) run and converge on the same key.
- **CONVERSATION_CLOSED (ON, SR 51)**: exactly one row, **SR-store row = 0** (the flag stopped the SR write — Messaging
  is the sole producer). Fully clean: no `_skipped` growth, no `23505`.
- **Reversibility (Part E, SR 53)**: flag flipped back OFF → the SR **resumes** writing its System row (SR-store = 1)
  while Messaging still writes its row. The `Messaging:WriteCutover:SystemMessages` flip is fully reversible.
- **Notifications (Part D)**: the 4 cancelled SRs produced **zero** `notification.notifications` rows → the WC1
  `CONVERSATION_CLOSED` System messages are **silent** (empty `RecipientUserIds`), as designed. (SR 52's 3 `OfferCreated`
  notifications came from the pre-existing `CreateOffer` path, not from WC1.)

The three System codes tied to accept/start/complete use the **identical** writer path as `CONVERSATION_CLOSED`
(`System·StatusChange·sys:{srId}:{CODE}`) — proven live here for `CONVERSATION_CLOSED` and covered by the WC1 unit tests
for the code mapping. They were not driven live only because of the Payment blocker below.

## ⛔ Blocker 1 — Payment S2S 401 (pre-existing, not WC1)
Owner **accept** returns 500; SR-api log:
`Refit.ApiException: … 401 (Unauthorized) … IPaymentModuleRemoteCall.CalculateServiceRequestEconomicsAsync … at
AcceptServiceRequestOfferCommandHandler`. The ServiceRequest→Payment service-to-service call is unauthorized in this
environment (payment S2S auth/config), so accept → escrow → assignment can't proceed. This blocks `OFFER_ACCEPTED`,
`JOB_STARTED`, `JOB_COMPLETED`. **Unrelated to WC1** — a payment integration/config issue to resolve separately.

## ⚠️ Finding — OFFER-card wiring gap (WC1 design, action before WC2)
The provider offer-creation path (`POST /api/v1/provider/service-requests/{id}/offers` → module
`CreateServiceRequestOffer`) produces **Submitted** offers **without** invoking the `SubmitOffer` command. WC1 wired the
OFFER card to a dedicated `ServiceRequestOfferSubmittedMessage` published only by `SubmitOffer`, so for the real create
flow that event **never fires** → **no OFFER card** (verified: 3 submitted offers on SR 52, zero `sys:52:OFFER:*` rows).
**Recommended fix:** drive the OFFER card from `ServiceRequestOfferCreatedMessage` (already published by
`CreateServiceRequestOffer`) **guarded to `Status ≥ Submitted`**, i.e. add a Messaging `OfferCreated` consumer that
writes the same `sys:{srId}:OFFER:{offerId}` card; keep the `OfferSubmitted` consumer for the draft→submit path — the
partial unique index dedups the two. (This also aligns with the original spec's `OfferCreated` naming; the WC1 impl had
deviated to `OfferSubmitted` on the assumption `OfferCreated` fires at draft-time, which does not hold for this flow.)

## ℹ️ Finding — transitional parallel-run dead-letter (harmless)
Flag **OFF** + a **fresh** SR: the sync consumer and the WC1 consumer **race to create the SR's conversation**; the
loser hits `23505` on `IX_conversations_ContextType_ContextId`, which the framework's post-consume unit-of-work
re-throws (a `ChangeTracker.Clear()` on the benign-catch path did **not** stop it — the throw is the framework's own
SaveChanges, not the consumer's), so the loser's **duplicate** message is retried and lands in
`ServiceRequestCancelled_skipped`. **The correct row always results** (winner writes; dupe check = 0). It occurs **only**
during the flag-OFF parallel run for a fresh SR; the **target state (flag ON)** has a single producer → **no race, no
dead-letter, no error log** (verified on SR 51). Recommended follow-up (non-blocking): make the get-or-create resilient
to the concurrent-create (`INSERT … ON CONFLICT DO NOTHING` for the conversation, or a shared ensure step) to remove the
transitional log noise.

## Pass criteria — status
| # | Criterion | Status |
|---|---|---|
| 1 | Renders once on the surfaces (flag OFF & ON) | ✅ CONVERSATION_CLOSED (DB-verified once, both states); other codes blocked |
| 2 | One `sys:{srId}:{CODE}` per code; dupe 0 | ✅ CONVERSATION_CLOSED (dupe 0 both states) |
| 3 | Flag ON → no new SR-store System rows; OFF → they resume | ✅ verified (ON: SR-store 0; OFF/reversal: SR-store 1) |
| 4 | Correct order/content/role (System; Provider for offer card) | ✅ System·StatusChange·code verified; offer card = wiring gap |
| 5 | No notification for System/offer | ✅ 0 notifications for the cancelled SRs |
| 6 | Logs show the 23505 swallow, no errors | ⚠️ flag-ON clean; flag-OFF parallel run logs a harmless conversation-race 23505 (documented) |

## State left on the stack
Flag reverted **OFF** (compose edit removed, SR-api restarted); messaging-api + SR-api run the fixed WC0+WC1 code;
no credentials changed; test SRs 47–53 (cancelled) + 3 duplicate offers on SR 52 remain as harmless smoke data.

## Recommendation before WC2
1. **Fix the OFFER-card wiring** (drive from `OfferCreated` with a Submitted guard) and re-run the offer→card check.
2. **Resolve the Payment S2S 401**, then drive `OFFER_ACCEPTED`/`JOB_STARTED`/`JOB_COMPLETED` end-to-end (flag OFF & ON)
   to complete criteria 1–2 for those codes on all three surfaces.
3. (Non-blocking) de-noise the transitional conversation-creation race.
The System-message engine, the flag flip + reversibility, dedup, and notification-silence are **proven live**; the
critical fresh-SR bug is **fixed**.
