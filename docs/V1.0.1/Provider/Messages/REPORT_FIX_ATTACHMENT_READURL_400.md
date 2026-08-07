# REPORT — message image attachments 400 on read-url

> Executes `FIX_ATTACHMENT_READURL_400.md`. Diagnostic-first. MarineProvider BFF (graceful read-url) + `inktavia-marine-provider-web`
> (graceful render). **The access-check hypothesis was disproved on the wire** — the confirmed root is the objectless-seed
> FileStorage case. **Not committed.**

---

## Phase 0 — what the wire + logs actually show

On `/app/messages/9011` two image read-urls fire; only one 400s:

| fileId | read-url | in `sr.Messages`? | in Messaging conv? | backing object? |
|--------|----------|-------------------|--------------------|-----------------|
| `a0a0a0a0-…-e4e4e4e4e4e4` (customer msg) | **400** ×4 | ✅ (msg 80003) | ✅ (msg 63) | ❌ synthetic seed GUID |
| `e8bd3c30-…-cc19116973a4` (provider msg) | **200** | ✅ (msg 23) | ✅ (msg 120) | ✅ real upload |

**The BFF log names the exact failure:**
```
FileStorage CreateReadUrl failed for file a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4
AizenBusinessException: Failed to generate read URL.
```
So the 400 is **Step 2 (FileStorage `CreateReadUrl`)**, *not* the access-check. The access-check **passed** — `a0a0a0a0…`
is in `sr.Messages`, so `isMessageAttachment` matched. The file is simply a **synthetic seed GUID with no backing object**
in FileStorage (same class as the P-QA7 Documents empty-object case).

**The access-check "Messaging gap" hypothesis does not hold here** — and it isn't latent-and-waiting either:
- Both Messaging attachments on this SR (`a0a0…`, `e8bd…`) are **also** in `sr.Messages` (the seed/sync duplicated them),
  so `sr.Messages.Any(...)` already covers them.
- The write path still populates `sr.Messages`: the provider-sent image `e8bd…` (senderType Provider) is present as
  `ServiceRequestMessageEntity` #23. So new provider messages land in `sr.Messages`, and the access-check finds them.
- There is **no** Messaging-only attachment on this conversation that fails the check.

Given the evidence, building a cross-module SR→Messaging remote-call for the access-check (there is none today) would be
**speculative plumbing against the diagnosis** — a per-request hop for a case that does not occur. The access-check is left
intact and correct. (If the write path is ever fully cut over so messages stop landing in `sr.Messages`, that lookup would
then be needed — noted below.)

## Fix (the confirmed root + graceful render)

1. **BFF read-url graceful — `GetAttachmentReadUrlBffQueryHandler` (Step 2 only).** The access check (Step 1) already
   proved the caller owns this fileId, so a FileStorage failure here (valid attachment, object missing) is **not** a
   security event and must not hard-400 the render. On `CreateReadUrl` failure (or a null body) the handler now **returns
   a clean "unavailable" — a 200 envelope with an empty `Url`** (logged as a warning) instead of throwing 400.
   **Step 1 is untouched**: a foreign/unknown fileId still fails the module access-check and 400s there, before Step 2 —
   security is unchanged.
2. **FE graceful render.**
   - `MessagesPage` `ImageMessage`: treats `ok && empty url` the same as a failure → renders a **broken-image
     placeholder** (`ImageOff` icon + **"Görsel yüklenemedi" / "Image unavailable"**) instead of a broken `<img>`. One
     fetch per fileId, settles into the placeholder, **no retry-loop**.
   - `AttachmentGallery` (SR detail): added the same `&& result.data.url` guard so the new 200-empty degrades to its
     existing "couldn't open" state rather than a broken `<img>` (prevents a regression from the BFF change).
   - i18n: `messages.imageUnavailable` (en/tr, parity 43/43).

No access-check change, no new cross-module call, no FileStorage-module change.

## Verify (on screen — localhost:3002/app/messages/9011, PROVIDER 2 AS, tr)

- ✅ **No more 400s.** After the fix, **both** read-urls return **200** (`a0a0…` and `e8bd…`); the network shows zero 400s
  and the console has **no `AxiosError`** on a fresh load (previously `a0a0…` 400'd 4×).
- ✅ **Objectless attachment → clean placeholder.** The customer's `a0a0…` image renders a grey broken-image tile with
  **"Görsel yüklenemedi"** (Müşteri · 3 hafta önce), not a broken image or error spam.
- ✅ **Real image still loads.** The provider's `e8bd…` "Hardware Ecosystem" image loads normally (200).
- ✅ **Security intact.** The access-check handler is **git-confirmed untouched**; a foreign fileId still fails Step 1
  (module `isRequestAttachment/isMessageAttachment/isWorkLogEvidence/isCompletionEvidence` → "Service request not found"
  → 400) before ever reaching the graceful Step 2. Identity is from the token (`KeycloakTokenInfo.ProviderProfileId`).
- ✅ **SR-module attachments still work.** `e8bd…` is itself an `sr.Messages` attachment resolving 200 through the changed
  BFF path; request-attachment / work-log / completion-evidence traverse the identical access-check + read-url code, so a
  real backing object still mints a URL (only objectless ones degrade to the placeholder).
- ✅ tsc `--noEmit` = 0; eslint on the changed FE files adds **0** issues (MessagesPage stays at its pre-existing 3;
  AttachmentGallery clean); BFF builds 0 errors; `messages` i18n parity 43/43.

Test data restored: removed a leftover `autoscroll test` chat message (from the earlier thread-autoscroll verification —
its SR-module copy was deleted then, its Messaging copy is deleted now). Seed image attachments are untouched.

## Known / follow-up

- The failing images are **seed rows with no MinIO object** — expected to render as the placeholder. Real uploads work.
- **Latent (not currently triggered):** if the chat write path is ever fully cut over so new messages persist **only** in
  the Messaging module (not `sr.Messages`), the access-check's `sr.Messages` prong would miss those attachments and 400 at
  Step 1. The clean fix then is a lightweight Messaging lookup ("does a message with this attachmentFileId exist in the
  conversation for ContextId=serviceRequestId?") added as a 5th prong — deferred now because the write path still
  dual-writes to `sr.Messages` (verified), so it would be dead code today.

## Scope

**BFF** — `GetAttachmentReadUrlBffQueryHandler.cs` (Step 2 graceful). **FE** — `MessagesPage.tsx` (ImageMessage
placeholder + `ImageOff` import), `AttachmentGallery.tsx` (empty-url guard), `messages.json` en/tr (`imageUnavailable`).
No module/access-check change. **Not committed.**
