# SMOKE — WC1 System/lifecycle messages (live parity verification)

> **Run this in Claude Code against the running stack** (it has messaging-api + servicerequest-api + the bus + Redis +
> Postgres `inktavia_store` + admin :3000 + provider :3002 + owner mobile). Goal: prove the 5 lifecycle System messages
> render **exactly once** on all three surfaces, that the SourceKey unique index **dedupes** the parallel producers,
> and that the `Messaging:WriteCutover:SystemMessages` flag **flips cleanly and reverses**. Read-only except the flag
> toggle + normal app actions. **Nothing to commit.**

## The 5 codes under test
`OFFER` (offer card, on `SubmitOffer`) · `OFFER_ACCEPTED` (`AcceptOffer`) · `JOB_STARTED` (`StartAssignment`) ·
`JOB_COMPLETED` (`ApproveCompletion`) · `CONVERSATION_CLOSED` (`CancelServiceRequest`). SourceKey =
`sys:{srId}:{CODE}` in `messaging.conversation_messages`.

## The flag
`Messaging:WriteCutover:SystemMessages` (bool, default **OFF**), bound by `MessagingWriteCutoverOptions` in the **SR
host** via `IOptionsMonitor`. **OFF** = SR writes the System rows (+ sync consumer mirrors → Messaging). **ON** =
Messaging is the sole producer (from the WC1 domain-event consumers); SR skips its System writes.
- Flip: set `Messaging__WriteCutover__SystemMessages=true` on the **servicerequest-api** service env → restart it
  (env changes aren't hot-reloaded; `IOptionsMonitor` hot-reloads only from a reloadable JSON source). The Messaging
  consumers already run regardless.

## Helper — find the conversation + its System rows (Postgres `inktavia_store`)
```sql
-- Conversation for an SR (ContextType ServiceRequest = 1):
SELECT "Id" FROM messaging.conversations WHERE "ContextType" = 1 AND "ContextId" = :srId;

-- System/lifecycle rows for that SR (one per code expected, no dupes):
SELECT "SourceKey", "SenderRole", "Type", "Content", "CreatedDate"
FROM messaging.conversation_messages
WHERE "ConversationId" = :convId AND "SourceKey" LIKE 'sys:' || :srId || ':%'
ORDER BY "CreatedDate";

-- Dupe check (must return 0 rows):
SELECT "SourceKey", COUNT(*) FROM messaging.conversation_messages
WHERE "ConversationId" = :convId GROUP BY "SourceKey" HAVING COUNT(*) > 1;

-- SR-store System rows (SenderType System = the code strings); used to confirm the flag stops SR writes:
SELECT "MessageType", "Content", "CreateDate" FROM servicerequest.service_request_messages
WHERE "ServiceRequestId" = :srId AND "SenderType" = 3 /* System */ ORDER BY "CreateDate";
```
(Adjust schema/column casing to the actual DB; the enum ints: MessagingContextType.ServiceRequest, SR SenderType.System.)

## Part A — baseline with the flag OFF (confirm the "before" is correct)
Use one fresh SR (owner creates → publishes). Drive the happy path and watch each surface:
1. **Provider submits an offer** → `OFFER` card appears in the provider thread (:3002) + owner mobile chat + admin
   Audit (:3000) — **once** each. SQL: one `sys:{srId}:OFFER…` row.
2. **Owner accepts** → `OFFER_ACCEPTED` pill once on all three. SQL: one `sys:{srId}:OFFER_ACCEPTED`.
3. **Provider starts the job** → `JOB_STARTED` once. SQL: one row.
4. **Provider/owner completes + owner approves** → `JOB_COMPLETED` once. SQL: one row.
5. On a **second** SR, **cancel it** → `CONVERSATION_CLOSED` once. SQL: one row.
Confirm the dupe check returns 0, and the SR-store query shows the System rows (flag OFF → SR still writes them).

## Part B — flip the flag ON (Messaging becomes the sole producer)
1. Set `Messaging__WriteCutover__SystemMessages=true` on servicerequest-api → restart it. Confirm the option is ON
   (log/health) and the Messaging lifecycle consumers are up.
2. On a **fresh** SR, repeat all 5 transitions (offer → accept → start → complete; cancel on another fresh SR).
3. For each transition verify:
   - The System message renders **exactly once** on **admin + provider + owner** (all read from Messaging).
   - SQL: exactly one `sys:{srId}:{CODE}` row per code; **dupe check = 0**.
   - **SR-store query now shows NO new System rows** for these SRs (the flag stopped the SR writes) — this is the key
     proof that Messaging alone produced them.
   - Correct **order** (offer → accepted → started → completed) and **content** (offer card `offer:{offerId}|{total}
     {ccy}`; the code strings for the rest); SenderRole = System (Provider for the offer card).

## Part C — dedup / idempotency evidence
- With the flag OFF (Part A), both producers ran (SR System write → sync consumer → Messaging **and** the WC1
  consumer) yet the dupe check was 0 → the `(ConversationId, SourceKey)` partial unique index collapsed them. Confirm
  the messaging-api logs show the benign `23505` unique-violation swallow (no error, no duplicate).
- Optional: force a redelivery (or just observe normal retries) and re-run the dupe check → still 0.

## Part D — notifications stay silent for System/offer
After each lifecycle transition, check the owner + provider notification inbox (or `notification.notifications`):
**no** `NewMessageReceived` (or other) notification should fire for the System/offer messages (RecipientUserIds empty
by design). Only a real user chat message notifies (that's WC2's path, unchanged here).

## Part E — reversibility
Set the flag back **OFF** → restart servicerequest-api → on a fresh SR, confirm the prior behaviour resumes (SR writes
the System rows again, the sync consumer mirrors, still one row per code via the index). This proves a safe rollback
before WC2.

## Pass criteria (record in the report)
1. Every code renders **once** on admin + provider + owner, flag OFF **and** ON.
2. `sys:{srId}:{CODE}` — exactly one row per code; **dupe check 0** in both states.
3. Flag **ON** → **no new SR-store System rows** (Messaging is sole producer); flag **OFF** → SR rows resume.
4. Correct order + content + SenderRole; offer card intact.
5. **No** notification fires for System/offer.
6. Logs show the `23505` swallow (dedup working), no errors.

## Report
`docs/V1.0.1/Architecture/REPORT_SMOKE_WC1.md`: per-code results (OFF + ON), the SQL row counts + dupe-check outputs,
the SR-store "no new System rows when ON" proof, the notification-silence check, the reversibility result, and any
anomaly. On green → **WC2** (chat write cutover). On any duplicate / missing / mis-ordered System message → stop and
fix before WC2.
