# Claude Code Prompt — Provider portal: messaging (REST first, realtime deferred)

Run **after** the service-requests/offers phase. Messaging without a job or a request to talk about is a chat app
with nothing to say — the conversation is always *about* something.

**Realtime (SignalR) is deliberately out of scope here.** The screens must be fully usable with plain REST +
refetch. The hub decision (a hub on the BFF vs. exposing the module hub directly to the browser) is an
architectural fork that deserves its own phase; see "The realtime decision" below and do **not** pre-empt it.

---

## What exists

`Modules/Messaging` already has real endpoints:

- `GET /api/v1/conversations` — list
- `GET /api/v1/conversations/{id}` — detail
- `GET /api/v1/conversations/by-context` — the important one: the conversation for a given domain object
- `POST /api/v1/conversations` — create
- `POST /api/v1/conversations/{id}/messages` — send
- `PATCH /api/v1/conversations/{id}/messages/mark-read`
- `POST /api/v1/conversations/{id}/messages/attachment-upload-url` — attachments via a signed URL
- `MessagingHub` (SignalR) with `messaging:conv:{id}` groups
- Moderation + reporting controllers (admin-facing; not this phase)

`ServiceRequest` also has its own `Message` controller (`/api/v1/messages`, `/service-requests/{id}/messages`).
**Two messaging implementations is one too many.** Before writing anything, determine which one is authoritative
for provider↔customer conversation, say so in the report, and route the BFF at exactly one of them. If the
ServiceRequest one is legacy, say that plainly — do not quietly build on top of both.

**Nothing in the MarineProvider BFF touches messaging today, and the SPA has no messaging code at all** beyond a
`PlannedPage`.

---

## PART A — MarineProvider BFF

New `ProviderMessagingController`:

- `GET /provider/conversations` — the calling provider's conversations, paged, newest activity first, with unread
  count per conversation.
- `GET /provider/conversations/{id}` — detail + messages, paged (a long thread must not be fetched whole).
- `GET /provider/conversations/by-context?type=ServiceRequest&id=…` — open (or create) the conversation attached
  to a request/job. This is how the provider gets into a thread from a job screen; there is no free-floating
  "start a chat with anyone".
- `POST /provider/conversations/{id}/messages` — send.
- `PATCH /provider/conversations/{id}/messages/mark-read`.

**Authorisation is the whole game here.** A conversation is between specific participants; reading someone else's
thread is the worst failure this feature can have.

- Every handler resolves identity first (`IProviderProfileResolver.ResolveAsync`) and **fails closed** when the
  user id is missing. This exact omission has silently produced `UserId = 0` in five handlers in this repo.
- The BFF must **verify the calling provider is a participant** of the conversation before returning it or posting
  to it — do not assume the module checks. Check whether the module checks; if it does not, that is a live IDOR
  and fixing it is part of this phase. Say what you found.
- Never accept a participant id, provider id or user id from the request body.

**Attachments:** the same rules as onboarding documents, which are already proven end to end. The BFF hands the
browser a **presigned upload URL**; the bytes never pass through the BFF. Read URLs are **minted per click,
short-lived, and never stored** — not in state, not in a list payload. Bucket names, object keys and signed URLs
must never reach the SPA or a domain table.

Two traps already paid for in this codebase:

- **No `System.Text.Json.JsonElement` on any wire contract** — MVC binds with Newtonsoft, Refit writes with STJ; a
  `JsonElement` binds to `default` and the payload disappears with no error.
- **A rejection can arrive as HTTP 200 with `success: false`** inside a successful envelope. Surface the module's
  real message; the SPA must check the body, not just the envelope.

---

## PART B — provider-web

### B1. Conversation list — `/app/messages`

Threads with the counterparty, the request/job they belong to, last message preview, unread badge, relative time.
Empty state: "conversations start from a request or a job" — with a link there, because that is the truth.

### B2. Thread — `/app/messages/:conversationId`

Message list (paged / infinite-scroll upward), composer, attachments, read receipts if the model has them.
Poll or refetch on focus while realtime is absent — and **do not pretend it is live**: no fake "typing…", no
optimistic message that might never have been sent. A message appears once the server confirms it.

### B3. Entry points

From a job and from a request detail: "Message the customer" → `by-context` → thread. That is the only way a
conversation begins for a provider.

### B4. Unread count

The bell in the top bar and the Messages nav item show the unread count from `GET /provider/conversations`.
Replace the dashboard's `planned` "Unread messages" tile with the real number.

---

## The realtime decision — do NOT implement, just prepare

The module hubs (`MessagingHub`, `ServiceRequestHub`) are `[Authorize]` and group by `user:{id}`. The browser
authenticates against the **BFF** with a Keycloak token; module calls carry the BFF's service token plus an
assertion. So the browser cannot simply connect to a module hub — that would be the first time it talks directly
to a module, and `Context.UserIdentifier` would have to mean the same thing on both sides. Get that mapping wrong
and a provider joins another provider's group.

Two options, to be decided in a dedicated phase:

- **Hub on the BFF** — the BFF consumes module events off RabbitMQ and fans out to browser clients. Keeps the
  BFF-only browser surface intact and reuses the already-resolved provider identity. Costs: realtime
  infrastructure on the BFF + a Redis backplane.
- **Module hub exposed directly** — less code, but the browser now authenticates against a module, and the group
  key mapping (`sub` → Identity `UserId`) becomes a security boundary we have to get exactly right.

In this phase: keep the data layer transport-agnostic (a query hook the UI reads from), so a realtime channel can
later push into the same cache without rewriting the screens. **Write your recommendation and its reasoning in the
report.**

---

## Acceptance — in a real browser

Write `docs/provider-messaging-report.md` with real statuses and payloads.

1. From a job, the provider opens the conversation via `by-context` and sends a message; the customer sees it.
2. The customer replies; the provider sees it after a refetch, and the unread badge clears on read.
3. **Cross-provider isolation:** provider B cannot read or post to provider A's conversation — by API, with a raw
   request, not just by hiding the UI. Paste the response.
4. Attachment: uploaded via a presigned URL (bytes do not touch the BFF), listed, and opened through a
   short-lived signed URL. Bucket/object key appear **nowhere** in any response the SPA receives.
5. A long thread pages correctly and does not fetch thousands of messages at once.
6. Unread counts on the nav, the bell and the dashboard agree with the list.
7. A failed send shows the server's reason and the message does **not** appear as sent.
8. **Regression:** onboarding, documents, approval, service requests, offers and jobs all still work.

## Constraints

- Exactly one messaging implementation. Name the loser.
- Participant checks on every read and every write. Never trust an id from the client.
- Fail closed. A missing `UserId` is a rejection, not `0`.
- Attachments follow the proven signed-URL pattern; no bucket/objectKey/URL ever reaches the SPA or the domain DB.
- No `JsonElement` on wire contracts. No fabricated identifiers. No silent no-ops.
- Realtime stays out. Do not half-build a hub.
- If something cannot be finished, leave the TODO **and say so in the summary**.
