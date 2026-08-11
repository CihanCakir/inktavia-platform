# WC3 — migrate the `sr.Messages` readers to Messaging + image write cutover — SUB-PHASED PLAN

> Phase-4 **WC3** per `PHASE4_WRITE_CUTOVER_PLAN.md`. After WC2 (text+location writes on Messaging), the remaining
> `sr.Messages` **readers** must move to the Messaging store, and **image writes** must cut over — so WC4 can retire the
> sync consumer and freeze `sr.Messages` with nothing left reading/writing it. WC3 is substantial with distinct
> concerns → **split into WC3a/b/c**, each independently shippable + smoke-able. Requires WC0–WC2. **Do not commit.**

## Investigated baseline (the readers still on `sr.Messages`)
- **Attachment read-url access-check (owner + provider):** `GetOwnerAttachmentAccessCheck` + `GetAttachmentAccessCheck`
  verify a `fileId` against **all** attachment types on the SR — request attachments (`sr.Attachments`), **chat**
  (`sr.Messages`), work-log evidence, completion evidence. So they can't be wholesale-repointed to Messaging — only the
  **chat** branch moves; request/evidence stay in SR.
- **Image storage parity (confirmed):** `MapMessage` stores a synced image as
  `MessageAttachmentEntity.FileStorageId = fileId.ToString()`; a native Messaging image send stores the same. So a
  **Messaging** chat-attachment check finds both old (synced) and new (native) images uniformly.
- **Dispute composer:** `DisputeCaseComposer` reads `sr.Messages` inline into the dispute case transcript
  (`DisputeCaseMessageDto`). After WC4 freezes `sr.Messages`, post-cutover chat would be missing → must migrate to read
  Messaging.
- **Legacy SR chat read-endpoints still wired** (read `sr.Messages`): `ServiceRequestMessageController`
  (`GetServiceRequestMessages`, `MarkServiceRequestMessagesRead`), `ServiceRequestConversationController`
  (`GetConversationList`/`Detail`), `ProviderJobsController` (`GetProviderConversations`). Provider + owner already read
  chat from Messaging (Phase-3 / MO10a), so these are **likely dead** — confirm no live BFF caller, then retire.

## Sub-phases

### WC3a — chat image read + write cutover (closes task #81) · **highest priority, most coupled**
- **New Messaging chat-attachment access-check** (participant-scoped): "for the conversation by context
  (ServiceRequest, srId), am I a participant, and is `fileId.ToString()` a `MessageAttachmentEntity.FileStorageId` on
  one of its messages?" → access-ok (BFF then mints the read-url via FileStorage, unchanged).
- **Repoint the owner + provider CHAT-image read-url** to a **two-store** check (BFF-orchestrated, each module checks
  its own store): try the **Messaging** chat-attachment check first; on miss, fall back to the **SR**
  request/work-log/completion-evidence check. This preserves request/evidence read-urls (SR) while chat images resolve
  from Messaging — and it works during the transition (old chat images are in both stores).
- **Flip owner + provider IMAGE writes to Messaging native** (behind the existing WC2 `Messaging:WriteCutover:ChatMessages`
  flag): image now uses the Messaging `attachment-upload-url` → `CompleteUploadSession` / direct FileStorageId path
  (MediaAttachment), joining text + location on Messaging. Remove the `image → SR path` branch WC2 left in place.
- **DoD:** owner + provider send an image → stored in Messaging → the two-store read-url resolves it from Messaging;
  no new `sr.Messages` image row when the flag is ON; old images still display. task #81 (read-url off the legacy
  `sr.Messages`) is closed for chat. Kickoff `BE_WC3a_IMAGE_CUTOVER.md`.

### WC3b — dispute composer transcript → Messaging · **cross-module read (design fork)**
The SR `DisputeCaseComposer` must source the chat transcript from Messaging (not `sr.Messages`). **Fork:**
- **(a)** SR calls Messaging via a new **remote-call** to fetch the conversation transcript for the dispute case (keeps
  the dispute case DTO shape; adds an SR→Messaging dependency, using the established service-token pattern), **or**
- **(b)** the dispute case **drops the inline transcript**; the admin dispute FE loads it from Messaging separately
  (admin already reads Messaging conversations) — cleaner separation (dispute = economics/lifecycle; transcript =
  Messaging), but an admin-FE change.
Recommend **(a)** for the smallest blast radius (no FE change, dispute case stays self-contained), unless we prefer the
cleaner separation. Kickoff `BE_WC3b_DISPUTE_TRANSCRIPT.md` (decide the fork first).

### WC3c — retire the legacy SR chat read-endpoints · **cleanup**
Confirm `GetServiceRequestMessages` / `MarkServiceRequestMessagesRead` / SR `GetConversationList`/`Detail` /
`GetProviderConversations` have **no live BFF/FE caller** (grep + the deployed BFFs) → **retire** them (or repoint the
one still used to Messaging). If mark-read is still needed for chat, it moves to the Messaging store. Kickoff
`BE_WC3c_RETIRE_LEGACY_SR_CHAT.md`.

## Sequencing & gates
WC3a (image, unblocks the image half + task #81) → WC3b (dispute transcript) → WC3c (retire legacy readers). Only after
**all three** confirm nothing reads `sr.Messages` for chat may **WC4** retire the sync + freeze `sr.Messages`. Each
sub-phase is reversible (WC3a rides the WC2 flag; WC3b/WC3c are additive/removal with the sync still mirroring as a
safety net until WC4). Live-smoke WC3a (image end-to-end on Messaging) before WC3b, like WC1/WC2.

## Definition of done (WC3)
No `sr.Messages` reader remains for chat: the attachment read-url (chat branch), the dispute transcript, and the legacy
read-endpoints all off `sr.Messages`; image writes on Messaging; request/work-log/completion-evidence read-urls still
served by SR (unchanged). WC4 can then retire the sync + freeze `sr.Messages`.
