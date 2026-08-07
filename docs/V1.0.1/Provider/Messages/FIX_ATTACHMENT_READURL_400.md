# FIX — message image attachments 400 on read-url (post-unification access-check gap)

> **Repos:** ServiceRequest module (access-check) + MarineProvider BFF + `inktavia-marine-provider-web` (graceful handling).
> In the chat, image attachments log **AxiosError 400** from `attachmentApi.getReadUrl` → the images fail to load.
> **Diagnostic-first**, but there's a strong root hypothesis. Additive/corrective. **Do not commit** until reviewed.

## Strong root hypothesis (from code review)
`GetAttachmentAccessCheckQueryHandler` (SR module) validates the requested `fileId` belongs to the SR via:
```
isRequestAttachment  = sr.Attachments.Any(a => a.FileId == fileId)
isMessageAttachment  = sr.Messages.Any(m => m.AttachmentFileId == fileId)   // ← SR-module legacy chat
isWorkLogEvidence    = sr.Assignment?.WorkLogs...
isCompletionEvidence = sr.Completion?.EvidenceFileId == fileId
if none → throw AizenBusinessException("Service request not found")  // surfaces as 400
```
But after the **SR→Messaging unification (Phase-3 read cutover)** the provider chat is **read from the Messaging module**,
not from `sr.Messages` (the SR-module `ServiceRequestMessageEntity`). So an image attached to a **Messaging-module** message
is **not** found by `sr.Messages.Any(...)` → the access-check fails → **400**. That matches the symptom (seed/messaging
attachments 400 while an SR-module request attachment or the "Hardware Ecosystem" image that resolves works).

## Phase 0 — confirm on the wire
For a failing image `fileId`: (1) is it on a **Messaging-module** message (the current chat source) rather than an
SR-module `ServiceRequestMessageEntity`? (2) does the 400 come from the **access-check** ("Service request not found") or
later from the **FileStorage read-url** step (a seed attachment with **no backing MinIO object** — cf. the Documents seed
that returned an empty object)? Both can occur; fix the confirmed one(s).

## Fix
1. **Access-check (primary):** extend `GetAttachmentAccessCheckQueryHandler` so a fileId that belongs to the SR's
   **Messaging conversation** also passes — verify against the Messaging module (a lightweight remote-call/query: "does a
   message with this attachmentFileId exist in the conversation for this ServiceRequest context?"), in addition to the
   existing SR-module paths. Keep the ownership/relationship gate. Fail-loud only when the fileId truly belongs to none.
2. **FileStorage read-url (if that's the 400):** a valid-but-objectless attachment should not hard-400 the whole image —
   return a clean "unavailable" result the FE can render as a broken-image placeholder (don't 500/400 the render path).
3. **FE graceful handling:** in `MessagesPage` image rendering, if `getReadUrl` fails, show a small broken-image/"görsel
   yüklenemedi" placeholder instead of a broken `<img>` + console-spamming AxiosError; don't retry-loop.

## Verify (on screen — localhost:3002/app/messages/9011)
- [ ] Image attachments on messaging-sourced chat messages load (read-url 200), no AxiosError 400 in console.
- [ ] An attachment with no backing object degrades to a clean placeholder, not a broken image or error spam.
- [ ] Access-check still rejects a fileId that isn't on the SR (security intact); identity from token.
- [ ] SR-module attachments (request attachment, work-log/completion evidence) still work.

## Report
`docs/V1.0.1/Provider/Messages/REPORT_FIX_ATTACHMENT_READURL_400.md`: the confirmed root (Messaging-source access-check gap
and/or objectless seed), the access-check extension (Messaging lookup), any FileStorage/FE graceful-degradation change, and
the on-screen verification.
