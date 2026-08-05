# REPORT — N-D live support channel (provider → admin, grouped by topic)

> A "Canlı desteğe bağlan" flow reusing the messaging machinery (conversations, admin observe/reply, unread, attachments)
> + a new Support context/topic + an admin support queue + a "new support request" admin notification. **Provider side
> fully built + verified; participant (owner) side is spec-only** (no owner web app). Additive — existing contexts,
> messaging W1–W5, N-A/N-B/N-C, and the bus untouched.

## D1 — Support context + reason/topic (Messaging module)
- `MessagingContextType.Support = 6` + a new `SupportTopic { Payment, ServiceRequest, Account, Billing, Technical, Other }`.
- Nullable `Topic` on `ConversationEntity` (set only for Support; null elsewhere) + EF conversion + index + migration
  `AddSupportTopicToConversation` (auto-applied at boot). `ConversationSummaryDto.Topic` added + mapped so the admin
  queue can group by it.
- `CreateSupportRequestCommand(topic, subject, firstMessage?, displayName, role)` — resolves the requester **server-side**
  (`IAizenInfoAccessor`), derives **ContextId = requesterUserId·100 + (int)topic** (one reusable thread per (requester,
  topic); different topics ⇒ separate conversations ⇒ separate groups; idempotent reuse on reconnect), creates the
  `Support` conversation (Title = subject, Topic set), adds the requester as a participant, optionally posts the first
  message, and publishes a `SupportRequestOpenedMessage`. Endpoint `POST /api/v1/conversations/support`. (Admin replies
  reuse the existing send + the W2 admin-send membership bypass — no admin membership needed.)

## D2 — provider "Canlı desteğe bağlan" (built) / participant (spec-only)
- **Provider BFF**: new `ProviderSupportController` (`api/v1/provider/support`) + `IMessagingRemoteCall` additions
  (create / send / attachment) — all participant-scoped (the Messaging module authorizes from the forwarded assertion;
  ContextId derived from the resolved provider id, so the FE only deals in a topic).
- **Provider FE**: `/app/support` — topic picker + subject + first message → create → live chat (poll the thread; send;
  admin replies render as **"Yönetici"** with the gold shield bubble). Nav item flipped to visible; tr + en.
- **Participant (owner)**: same flow spec'd, **deferred** — no owner surface exists yet; not built.

## D3 — admin "Destek" queue (grouped by topic)
- Admin-web: a **Destek** tab in the messaging segmented nav + `/app/messages/support` → `MessagesSupportPage`. Lists
  `GetConversationList(ContextType=Support)` **grouped by Topic** (unread badge, preview per row), reuses the existing
  `MessageBubble` + the send/mark-read hooks (admin unread clears on open). tr + en. No admin-BFF endpoint change — the
  existing `GetConversations` already passes `contextType` through (the BFF only needed a rebuild to carry the new
  `Topic` field).

## D4 — "new support request" notification to admins
- `NotificationType.SupportRequestOpened = 910` → **Broadcast** category (N-B map extended to 900–999), template
  `SUPPORT_REQUEST_OPENED_INAPP`. New `SupportRequestOpenedConsumer` (Notification) consumes the Messaging event,
  resolves admin user ids from a new internal Identity read `GET /api/v1/identity/admin/user-ids` (via
  `INotificationIdentityRemoteCall.GetAdminUserIds`), and sends one notification per admin **sequentially**. In-app +
  live badge always; push only if the admin opted into Broadcast (N-B), reusing N-A/N-B/N-C.

## Deploy / quality
`messaging-api`, `identity-api`, `notification-api`, `bff-adminpanel`, and both `bff-marineprovider` replicas rebuilt +
redeployed (same-image). Migration auto-applied. Backend 0 errors; admin-web + provider-web `tsc` + ESLint clean; tr+en.

## Verify (on-screen, live provider↔admin round-trip)
1. **Provider** opened `/app/support`, topic **Ödeme (Payment)** + subject + message → **Canlı desteğe bağlan** → a
   Support conversation opened (conv 15, ContextId 10001101, Topic Payment, unread 1); the provider chats in it. ✓
2. **Admin Destek** queue showed the request under **ÖDEME (Payment)** with an unread badge; admins were **notified**
   (`SupportRequestOpened … → notified 3 admins`; the admin notification bell incremented). ✓
3. **Admin** opened it → **unread cleared** → replied; the reply appeared on the **provider** side **live as "Yönetici"**
   (gold shield bubble); the provider's own messages render as "Siz". ✓
4. **Grouping by topic** correct (ÖDEME group); a request under another topic lands in its own group by construction
   (ContextId encodes the topic → separate conversation → separate group). N-B gating holds (SupportRequestOpened is the
   Broadcast category, delivered through the same gated SendNotification path proven in N-B/N-C).

## Notes / gotchas
- Provider endpoints had a pre-existing `support: '/support'` key; the new object was renamed `liveSupport` to avoid the
  duplicate-key override.
- The create handler must add the requester participant + first message via the **navigation collections** and let EF
  cascade the inserts on the new (temp-keyed) conversation — calling `Update()` on the Added entity threw
  "Id has a temporary value".
- The admin BFF had to be rebuilt so its `GetConversationListResponse` carries the new `Topic` (otherwise the passthrough
  drops it and everything grouped under "Other").

## Next
N-E — structured reject/cancel reason taxonomy (SR + Payment, auto-linking).
