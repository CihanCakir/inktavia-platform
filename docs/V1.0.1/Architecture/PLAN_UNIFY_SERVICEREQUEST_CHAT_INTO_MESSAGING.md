# PLAN (design, review-before-build) — unify ServiceRequest chat into the Messaging module (one canonical messaging system)

> **Status:** Design/plan for review — **not** an implementation kickoff. Decides the target architecture, the migration
> approach, phasing, effort, and risks so we can commit with eyes open. Owner-approved direction: *unify onto the
> Messaging module.*

## Problem (confirmed)
The platform has **two parallel messaging implementations**:
- **Messaging module** — the **intended canonical hub**: `MessagingContextType { ServiceRequest, CommerceOrder,
  CargoDrySupport, VenueInquiry, DirectMessage }`, full conversations/participants/messages + attachments + **moderation
  + reporting**, publishes `MessagingMessageSentMessage`, and is what the **admin Communication Audit** already observes
  (W1–W4, live socket + intervention + panels + i18n, all done).
- **ServiceRequest module** — its **own** chat subsystem (`ServiceRequestConversationController /api/v1/messages`,
  `ServiceRequestMessageController /service-requests/{id}/messages`, entities `ServiceRequestMessageEntity` /
  `ConversationMessageEntity` / `MessageAttachmentEntity`, `OwnerUserId`, `SenderType` Owner/Provider/System, migration
  *AddConversationsAndWorkPhases*), publishing `ServiceRequestMessageSentMessage`.

**The provider portal writes to the ServiceRequest module**, so real provider↔owner chat lives there — while the admin
audits the (nearly empty) Messaging module. The realtime edge is proven healthy end-to-end; the gap is purely that the
real traffic never enters the canonical store. The two models map cleanly: an **SR conversation ≡ a Messaging
conversation with `ContextType=ServiceRequest, ContextId=srId`, participants = {Owner, Provider}**. That 1:1 mapping is
why unification (not a permanent bridge) is the right end state.

## Target architecture
The **Messaging module is the single source of truth for all conversations.** A service-request chat is a Messaging
conversation (`ContextType=ServiceRequest`). The ServiceRequest module stops owning chat — it keeps service-request
domain logic (offers, jobs, work phases) and, where it needs message context (e.g. Job Workspace), **reads from the
Messaging module** or reacts to `MessagingMessageSentMessage`. Every surface (provider, owner/participant, admin) speaks
to the Messaging module via its BFF. Realtime everywhere is the `Aizen.Core.Realtime` framework (already the standard
after the provider migration).

## Migration approach — strangler (recommended), not big-bang or permanent bridge
Rejected: **permanent bridge/dual-write** (two sources of truth forever = the smell we're removing); **big-bang cutover**
(too risky across provider + owner + data + notifications). Chosen: a **phased strangler** that keeps admin + provider
working throughout and cuts over one concern at a time behind a flag.

### Phase 0 — model + contract mapping (design, low risk)
- Confirm the field-by-field mapping SR→Messaging (sender type, owner/provider participants, attachments, timestamps,
  read state, system/lifecycle messages). Define a **"ensure conversation for context"** Messaging command
  (`EnsureConversation(ContextType, ContextId, participants)`) so an SR gets its Messaging conversation on first
  message (or at SR creation). Decide id strategy (Messaging conversation id vs srId — the FE deep-links by conversation
  id).

### Phase 1 — data migration (one-time, idempotent)
- Backfill existing ServiceRequest conversations/messages/attachments into the Messaging module keyed by
  `(ContextType=ServiceRequest, ContextId=srId)`. Idempotent (re-runnable), verifiable counts, preserves sender/role/
  timestamps/attachments. This is the riskiest data step — script + verify + reversible.

### Phase 2 — write path cutover (behind a flag)
- Repoint provider send (**MarineProvider BFF**) and owner send (**Participant Mobile BFF**) from the ServiceRequest
  chat endpoints to the **Messaging module** (auto-`EnsureConversation`). Feature-flag per surface so it's reversible.
- Notifications: the SR "new message" notification path moves to react to `MessagingMessageSentMessage`.

### Phase 3 — read path cutover (behind a flag)
- Repoint provider + owner **conversation list + thread reads** to the Messaging module. Provider/owner FE point at the
  Messaging-backed BFF endpoints (shape-compatible DTOs to minimize FE churn).

### Phase 4 — realtime source switch
- Provider realtime (just migrated to the framework, currently mapping `ServiceRequestMessageSentMessage`) switches its
  MessageAdded source to `MessagingMessageSentMessage`. **Participant Mobile BFF gains a framework realtime hub** (same
  ADR pattern: `AddAizenRealtime` + `AddDomainHub` + one `IEventSocketMapper` + generic consumer) so the owner gets live
  messages too. During transition, consume both events, de-dup.
- Admin already consumes `MessagingMessageSentMessage` → **admin now sees real provider↔owner chat live, for free.**

### Phase 5 — deprecate ServiceRequest chat
- Retire the ServiceRequest module's chat controllers/commands/entities (or leave read-only for a grace period). Keep the
  SR domain (offers/jobs/work-phases); where those need messages, read Messaging. Remove the parallel entities +
  `ServiceRequestMessageSentMessage` once nothing depends on it.

## Impact surfaces (effort map)
- **Messaging module:** `EnsureConversation` command; ensure participants/attachments/system-message parity with SR;
  possibly new context handling. (Medium)
- **MarineProvider BFF + provider-web:** repoint send/list/thread + realtime source. Provider realtime already on the
  framework — smaller. (Medium)
- **Participant Mobile BFF + owner app:** repoint + **add the framework realtime hub** (new realtime surface). (Medium-
  large; the owner side is the least-built today.)
- **ServiceRequest module:** deprecate chat; rewire Job Workspace / anything reading SR messages. (Medium; watch Job
  Workspace JD features.)
- **Data migration:** one-time backfill + verification. (Medium, high-care.)
- **Notifications:** repoint SR-message notifications to Messaging events. (Small-medium.)
- **Admin:** none — benefits automatically. ✅

## Risks / open questions
- **Job Workspace coupling:** the provider Job Workspace (JD-1/JD-2 etc.) may read SR messages directly — enumerate every
  reader of `ServiceRequestMessageEntity` before Phase 5.
- **Owner side maturity:** no owner web app runs today; the owner path + its realtime hub is the largest new build.
- **Data migration fidelity:** system/lifecycle messages, read receipts, attachments must map exactly.
- **Dual realtime window:** Phase 4 must de-dup `ServiceRequestMessageSentMessage` vs `MessagingMessageSentMessage`.
- **Conversation id contract:** FE deep-links (`/app/messages/{id}`, provider thread) — keep ids stable or provide a
  mapping so links don't break.
- **Attachments/storage:** confirm both modules use the same FileStorage; messages carry the same attachment refs.

## Recommended first step (after this plan is approved)
Do **Phase 0 + Phase 1 as a spike**: write the `EnsureConversation` command + the idempotent backfill script against a
copy, verify counts + a few threads render in the admin audit — this de-risks the whole epic and immediately lets the
admin see *migrated* real conversations before any cutover. Then Phases 2–5 sequentially behind flags.

## Not in this plan
Implementation kickoffs for each phase (written per-phase after approval), and the **timestamptz sweep** (separate
systemic task). This document is the decision + sequencing basis only.
