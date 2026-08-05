# N-D — live support channel (provider & participant → admin, grouped by topic/reason)

> **Repos:** `addesso-project` (Messaging module + BFFs) + `inktavia-marine-provider-web` / `inktavia-marine-admin-web`.
> Phase N-D of the notification/support roadmap. A "**Canlı desteğe bağlan**" flow: a user opens a support request with a
> **reason/topic**, it becomes a live chat, and the admin panel surfaces open support requests **grouped by topic** so
> customers aren't missed. **Reuses the messaging machinery just completed** (conversations, admin observe/intervene,
> unread, live socket, notifications, attachments) — this is mostly a new **context + reason + a create flow + an admin
> support-queue view + a "new support request" notification**.
>
> **Owner-app note:** provider side is fully buildable/testable; the **participant (owner) side is spec-only** — no owner
> web app exists yet (recurring limit). Build provider now; wire participant when the owner surface lands.

## Foundation (investigated — reuse)
- `ConversationEntity` is **context-agnostic** (`ContextType`+`ContextId`, Title, Status Active/Flagged, `UnreadCountByAdmin`,
  LastMessageAt/Preview). `MessagingContextType` = {ServiceRequest, CommerceOrder, CargoDrySupport, VenueInquiry,
  DirectMessage}. `GetConversationList` already filters by **ContextType + Status**. `CreateConversation` /
  `EnsureConversationForContext` commands exist. Admin already observes/intervenes/gets unread + live frames.

## D1 — Support context + reason/topic (Messaging module)
- Add `MessagingContextType.Support = 6` (general live support; distinct from the domain-specific CargoDrySupport).
- Add a **support reason category** — enum `SupportTopic` { Payment, ServiceRequest, Account, Billing, Technical, Other }
  (or a ReferenceData lookup) — and store it on the conversation: add a nullable `Topic`/`SupportTopic` field to
  `ConversationEntity` (used for Support context; null elsewhere). Migration.
- `CreateSupportRequestCommand(requesterUserId, topic, subject, firstMessage?)` → creates a `Support` conversation
  (ContextId = a support sequence or the requester's id; decide whether a user may have multiple open requests — MVP:
  allow a new one per "connect", or reuse an open one for the same topic), Title = subject, requester as participant,
  Topic set; optionally posts the first message. Reuses the conversation/message plumbing.

## D2 — "Canlı desteğe bağlan" entry (provider now; participant spec)
- **Provider portal:** a support entry (in a Help/Support area or the messages surface) → pick a **topic** + type a
  **subject/first message** → `CreateSupportRequest` (via provider BFF → Messaging) → open the resulting conversation in
  the existing provider chat UI (send/receive, attachments — all reused). Provider sees admin replies live.
- **Participant (owner):** same flow spec'd; **deferred** until an owner surface exists — note it, don't build a fake
  entry.

## D3 — admin support queue surface (don't miss customers)
- New admin view (sibling to Communication Audit — e.g. a **"Destek"** segmented tab / `/app/messages/support` or a
  sidebar entry): list **open Support conversations** via `GetConversationList(ContextType=Support)`, **grouped by
  Topic**, each showing subject, requester, unread badge, last message, age/priority, status. Reuse the existing
  conversation list + the two-pane observe/reply (admin opens → reads → replies with the existing admin send + the
  W2 intervention tools + attachments). Admin unread clears correctly (the N-C/messaging-completion behavior).
- Filter/sort by topic + unread + age so nothing is missed.

## D4 — "new support request" notification to admins (the guarantee)
- On support-request creation (and/or first message), notify **admins** so they don't miss it: add
  `NotificationType.SupportRequestOpened` (map to a **Support/Broadcast** category in the N-B map) → notify admin users
  (in-app badge + push if subscribed, honoring N-B preferences). Reuse N-A/N-B/N-C. (Resolve "admin users" the same way
  the admin surface authorizes — the admin role/group.)

## Don't-break / QA
- Additive: new context value + reason field + create command + admin view + one notification type. Existing
  conversations/contexts, admin messaging (W1–W5), N-A/N-B/N-C, and the bus fixes untouched. Migration applies cleanly;
  the new Topic field is null for non-support conversations. Envelope-correct BFF calls; same-image redeploy; builds
  clean; FE typecheck/lint clean; tr+en for new UI.

## Verify (on-screen)
1. Provider clicks "Canlı desteğe bağlan", picks topic = Payment + subject → a Support conversation opens; provider can
   chat + attach.
2. Admin panel **Destek** queue shows the new request **under its topic (Payment)** with an unread badge; admins get a
   **notification** (in-app + push if subscribed) — customer not missed.
3. Admin opens it → reads (admin unread clears) → replies → provider sees it **live**; attachments/location work both
   ways; the admin message shows as "Yönetici" on the provider side.
4. Grouping/sort by topic correct; a second request under a different topic lands in its own group. N-B gating honored.

## Report
`docs/V1.0.1/Notification/REPORT_N-D.md`: the Support context + Topic model + create flow, the provider entry (+
participant-deferred note), the admin support-queue view (grouped by topic), the new-support notification, and the
on-screen provider↔admin support round-trip. Next: N-E (reject/cancel reason taxonomy).
