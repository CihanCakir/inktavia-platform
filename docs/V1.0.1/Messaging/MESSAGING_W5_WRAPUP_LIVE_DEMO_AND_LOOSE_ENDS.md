# Messaging Wave 5 — wrap-up: prove provider→admin live, then close the loose ends (commit · switcher · live moderation · cleanup)

> **Repos:** `addesso-project` (Messaging module + AdminPanel BFF) and `inktavia-marine-admin-web` /
> `inktavia-marine-provider-web` (FE). One consolidated pass to finish the messaging epic. **Do the parts in order** —
> Part 0 (the primary want) first; it likely already works. **Out of scope (separate dedicated pass): the timestamptz
> sweep.**

## PART 0 (do first) — prove provider→admin live on-screen (the primary goal)
The admin live feed already consumes `MessagingMessageSentMessage` from RabbitMQ (W1). So a real provider send should
already surface in the admin panel. **Demonstrate it end-to-end:**
1. Provider session on **`:3002/app/messages`** — open one of the logged-in provider's conversations and **send a
   message** (no owner/customer login needed; the provider is a participant).
2. Admin session on **`:3000/app/messages`** with that same conversation open — the new message must appear **live**
   (list bumps + unread badge; with the thread open, the message renders after the thin-frame refetch), dot **Live**.
3. Capture the transcript (provider sends → admin sees, no manual refresh). **If it does NOT appear live, diagnose + fix**
   (check: module published `MessagingMessageSentMessage` on the provider send; admin BFF `AdminMessagingRealtimeConsumer`
   received it; frame delivered to `admin:messaging`; FE refetched). This part is the acceptance test for the whole epic.

## PART 1 — commit the pending work
Commit the uncommitted W3 + W4 admin-web changes (currently on `feature/messaging-w3-observation-panels`) as coherent
commits (W3 observation panels + report typing; W4 i18n). Confirm the provider-migration + messaging backend changes are
committed too (or stage them). Do not force-push; keep the branch history clean. Report the commit hashes.

## PART 2 — language switcher (verify/finish — it already exists)
`features/settings/components/LanguageSwitcher.tsx` + `pages/app/settings/PreferencesPage.tsx` already exist. Verify the
switcher is **reachable and functional**: Preferences page renders it (not a "coming soon" stub), selecting tr/en calls
`i18n.changeLanguage`, persists (`i18nextLng`), and flips the whole app live (no reload needed). If it's stubbed/unwired,
wire it: render `LanguageSwitcher` in Preferences, ensure it changes + persists language. Verify on-screen tr↔en from the
UI (not localStorage hacks).

## PART 3 — live moderation events (the deferred W1-boundary, layer-2)
Today flag/moderate/status changes reach admins only via the 30s moderation-queue poll — the module publishes them
through `MessagingRealtimePublisher` (`PublishConversationStatusChangedAsync` / `PublishModerationEventAsync`), which fed
the now-retired module hub, **not** the RabbitMQ bus the admin edge consumes. Make them live:
1. **Module:** on flag / moderate / conversation-status-change, publish a **bus integration event** (mirror how
   `SendMessageCommandHandler` publishes `MessagingMessageSentMessage`) — e.g. `MessagingModerationEventMessage {
   ConversationId, MessageId?, Kind (Flagged|Moderated|StatusChanged), NewStatus, OccurredAt }`. Keep the existing
   `MessagingRealtimePublisher` calls or replace them — but the **bus publish** is what reaches the admin edge. No PII/
   content on the event (thin).
2. **AdminPanel BFF:** extend `AdminMessagingEventSocketMapper` (+ register a generic `RealtimeEventConsumer<
   MessagingModerationEventMessage>`) to map it to a thin admin frame (e.g. `Type="moderationEvent"`, payload `{ kind,
   conversationId, messageId }`) targeting the `admin:messaging` group.
3. **FE (`useMessagingHub`):** on a `moderationEvent` frame, invalidate the moderation queue + the affected conversation
   detail/list (refetch) — same thin-frame → refetch pattern as messages. So flag/moderate/status now update **live**
   across admin sessions instead of after the poll.
4. Follows the realtime ADR (layer-2 only: one event type + mapper + generic consumer; no new plumbing).

## PART 4 — test-data cleanup
Reset the verification residue: the two test messages in **#9010**, the flagged/blocked state on **#9004**, and the
injected events on **SR 9011 / offer 90001**. Since flag/moderate are one-way and this touches shared seeded rows, these
are **shared-DB mutations** — if a safety guard blocks a write, **surface the exact statements for the user to run/
approve** rather than working around it. Prefer restoring to the original seeded state; document what was changed.

## Out of scope — timestamptz sweep (separate dedicated pass)
The recurring Postgres timestamptz/date-aggregation bug class (P12 ledger + both messaging reporting endpoints) likely
affects other date-bucketing endpoints. That is a **systemic, investigation-heavy** job — do it as its own scoped audit+
fix, **not** in this batch (bundling it risks this wrap-up). Note it in the report as the recommended next task.

## Don't-break / QA
- Part 0 must pass before the rest matters. Part 3 is additive (new event type; existing message live-feed unchanged;
  follows the ADR — no hand-rolled SignalR/consumers). No regression to W1/W2/W3/W4 or provider realtime.
- Backend builds clean; FE typecheck/lint clean; both admin-panel + messaging containers redeployed (same image on all
  replicas — avoid the split-brain trap). tr+en parity preserved.

## Verification (on-screen)
1. **Provider→admin live** (Part 0): provider sends on `:3002` → admin sees it live on `:3000` (transcript).
2. Language switcher flips tr↔en from Preferences and persists.
3. Flag/moderate a message → it appears **live** in another admin session's moderation queue / conversation (no 30s
   wait).
4. Test data reset (or the exact statements surfaced for approval).
5. Commits recorded; builds + typecheck/lint clean.

## Report
`docs/V1.0.1/Messaging/REPORT_MESSAGING_W5_WRAPUP.md`: the Part 0 provider→admin transcript, commit hashes, the switcher
verification, the live-moderation event (module publish + BFF mapper + FE), the cleanup (done or statements surfaced), and
the timestamptz sweep flagged as the next dedicated task.
