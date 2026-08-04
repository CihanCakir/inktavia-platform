# SPIKE (P0 + P1) — EnsureConversation + idempotent backfill of ServiceRequest chat into the Messaging module

> **Repo:** `addesso-project` — **Messaging module** (add `EnsureConversation`) + a **one-time backfill** reading the
> ServiceRequest DB and writing the Messaging DB. First concrete step of the unification plan
> (`PLAN_UNIFY_SERVICEREQUEST_CHAT_INTO_MESSAGING.md`). **Additive + read-only on the ServiceRequest side + idempotent +
> reversible.** It does **not** touch any write/read/realtime path (those are Phases 2–5) — its only job is to de-risk
> the model mapping and let the **admin audit show migrated real conversations** before any cutover.
>
> **Sequencing:** run **after** the W5 wrap-up finishes (both touch the Messaging module — do not run concurrently, and
> avoid a second concurrent messaging-api redeploy / split-brain).

## P0 — `EnsureConversation` command (Messaging module)
Add an idempotent command `EnsureConversationForContext(MessagingContextType contextType, long contextId, participants[])`:
- Returns the existing Messaging conversation for `(ContextType, ContextId)` if present; else creates one with the given
  participants (userId + role Owner/Provider/Admin/System) and a sensible title (e.g. the SR title). **Idempotent** —
  safe to call repeatedly (unique on `(ContextType, ContextId)`; add a DB unique index if not present).
- This is the linchpin the later write-cutover (P2) will also use (first message / SR creation ensures the conversation).
- No controller/endpoint needed for the spike (invoke it from the backfill). Unit-test the idempotency.

## P1 — one-time idempotent backfill (ServiceRequest → Messaging)
**First, confirm the source-of-truth entity.** The ServiceRequest module has **two** message entities:
`ServiceRequestMessageEntity` (keyed by `ServiceRequestId` — the provider↔owner chat the portal writes via
`/service-requests/{id}/messages`) and `ConversationMessageEntity` (from *AddConversationsAndWorkPhases* — likely job/
work-phase). **Verify which holds the real provider↔owner chat** (expected: `ServiceRequestMessageEntity`) and backfill
that; note the other for a later phase if it also carries user chat.

For each distinct `ServiceRequestId` that has messages:
1. Resolve participants: **Owner** = the SR's `OwnerUserId`; **Provider** = the assigned provider's user id (from the
   assignment/accepted offer). Include a System participant if system messages exist.
2. `EnsureConversationForContext(ServiceRequest, serviceRequestId, {owner, provider})`.
3. Insert each `ServiceRequestMessageEntity` as a Messaging `ConversationMessage`, mapping:
   `SenderUserId→SenderUserId`, `SenderType(Owner/Provider/System)→role`, `MessageType→type`, `Content→Content`,
   `IsRead/ReadAt→read state`, `AttachmentFileId→attachment ref` (confirm both modules use the **same FileStorage** so
   the ref resolves), `LocationLat/Lng/Label→location content`, and **`CreateDate`/audit timestamps → occurredAt/sent-at
   preserved** (UTC-safe — mind the recurring timestamptz class).
4. **Idempotency:** key inserted rows so a re-run doesn't duplicate (e.g. carry a stable `sourceRef` = the SR message id,
   or skip when the target conversation already has N messages / a matching source key). Re-running the whole backfill
   must converge, not duplicate.
5. **System/lifecycle messages** (SR handlers post "System" messages on StartAssignment / AcceptOffer / SubmitOffer /
   Cancel / ApproveCompletion) — migrate them too, as System-role messages, so the thread reads identically in admin.

Implementation: a guarded one-time routine (a dev/admin-only seeding/migration hook or a CLI task), **not** wired into
normal runtime. It **reads** ServiceRequest DB and **writes** Messaging DB only — never mutates ServiceRequest data.

## Reversibility + safety
- Reversible: everything created is `ContextType=ServiceRequest` Messaging conversations/messages — deletable by that key
  to re-run cleanly.
- Run against a **copy/snapshot first** if possible; verify before running on the shared dev DB. If a safety guard blocks
  the write, surface the exact operation for approval rather than working around it.
- Do **not** repoint provider/owner send/read or realtime (Phases 2–5). No FE change. No ServiceRequest deprecation.

## Verification (the payoff)
1. Backfill runs; report counts: SRs migrated, messages migrated, participants created; a spot-check of 2–3 threads
   (message order, sender roles, attachments/location, system messages) matches the ServiceRequest source.
2. **Admin Communication Audit** (`/app/messages`) now lists the **migrated real SR conversations** with their messages —
   the admin can open and read an actual provider↔owner thread. (This is the whole point: real data in the canonical
   store, no cutover yet.)
3. Re-running the backfill does not duplicate (idempotency proven).
4. Messaging module builds clean; existing seeded Messaging conversations + admin W1–W4 unaffected; ServiceRequest module
   untouched (read-only).

## Report
`docs/V1.0.1/Architecture/REPORT_SPIKE_P0_P1.md`: the `EnsureConversation` design + idempotency test, the confirmed
source entity, the field mapping applied, migrated counts + spot-check, the on-screen admin view of a migrated real
thread, and any open mapping gaps (read receipts, system-message fidelity, attachment/storage parity) to carry into
Phase 2. Recommend go/no-go for Phase 2 (write cutover).
```
