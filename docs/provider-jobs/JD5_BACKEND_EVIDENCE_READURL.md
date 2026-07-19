# JD-5 — Backend: authorize read-URL for work-log / completion evidence

Providers can already attach a photo when adding a work log (`AttachmentFileId`, JD-3) or submitting completion
(`EvidenceFileId`, JD-2), and the signed-URL **upload** flow already exists. What's missing is **read** authorization: the
14c access-check only mints a signed read-URL for a *request* attachment or a *message* attachment, so a work-log /
completion evidence fileId collapses to "not found." This phase widens that one check. Tiny, single-handler change.

## Verified in source (2026-07-17)
- Read-URL path: SPA `GET /service-requests/{srId}/attachments/{fileId}/read-url` → BFF → module
  `GetAttachmentAccessCheckQuery(serviceRequestId, fileId)` → on allow, the BFF mints a 5-min signed URL
  (`GetAttachmentReadUrlBffQueryHandler`). No object key ever reaches the SPA.
- `GetAttachmentAccessCheckQueryHandler` today: resolves `providerProfileId`; loads the SR via
  `GetByIdWithDetailsAsync`; three-prong relationship check (has offer OR is the assigned provider); then verifies the
  fileId is **a request attachment** (`sr.Attachments`) **or a message attachment** (`sr.Messages`). Otherwise → "not
  found."
- `GetByIdWithDetailsAsync` **already includes** `Assignment.ThenInclude(WorkLogs)` and `Completion`. So the aggregate
  already carries what we need — no repository change.
- `ServiceRequestWorkLogEntity.AttachmentFileId (Guid?)`, `ServiceRequestCompletionEntity.EvidenceFileId (Guid?)` — the
  evidence fileIds live here.

## Work — one handler

In `GetAttachmentAccessCheckQueryHandler`, extend the fileId membership test (keep the relationship check as-is) to also
accept work-log and completion evidence on **this** SR:

```csharp
var isRequestAttachment = sr.Attachments.Any(a => a.FileId == request.FileId && !a.IsDeleted);
var isMessageAttachment = sr.Messages.Any(m => m.AttachmentFileId == request.FileId && !m.IsDeleted);
var isWorkLogEvidence   = sr.Assignment?.WorkLogs.Any(w => w.AttachmentFileId == request.FileId) ?? false; // NEW
var isCompletionEvidence = sr.Completion?.EvidenceFileId == request.FileId;                                 // NEW

if (!isRequestAttachment && !isMessageAttachment && !isWorkLogEvidence && !isCompletionEvidence)
    throw new AizenBusinessException("Service request not found.");
```

- Keep the relationship gate first (offer OR assigned) — a provider still can't read evidence on an SR they have no
  relationship to.
- No new endpoint, DTO, BFF change, or repository change. The existing read-URL route serves the widened set.

## Constraints
- **Access-checked** exactly as 14c: relationship + fileId-belongs-to-this-SR, else "not found" (never reveal foreign
  fileIds). Read URL stays 5-min, minted per-click, no object key to the SPA.
- Additive only — request/message attachment behavior unchanged. No customer PII. Upload authorization already exists
  (JD-2/JD-3 accept the fileIds); this is read-side only.

## Acceptance — observed
- A provider adds a work log with a photo on job 91001 (fileId F). `GET /service-requests/9011/attachments/F/read-url`
  as that provider → 200 + a signed URL (previously "not found").
- Completion evidence fileId on SR 9011 → read-url minted for the assigned provider.
- A fileId that is **not** any attachment/message/work-log/completion on the SR → "not found".
- A provider with **no relationship** to the SR → "not found" (relationship gate still first).

## Report
Append to `REPORT_BACKEND.md` ("JD-5"): the widened access-check now mints read-URLs for work-log + completion evidence
(with the relationship gate intact), and the negative cases still collapse to "not found". Unfinished is **not done**.

## Frontend (I build after this lands — FE-E)
Work-log composer: optional photo upload (reuse the signed-URL flow) → pass `attachmentFileId` on add. Work-log rows with
evidence show a thumbnail (read-URL via the existing `/attachments/{fileId}/read-url`, keyed by `job.serviceRequestId`) →
click → lightbox (reuse the Messages image lightbox). Completion modal: optional evidence upload → `evidenceFileId`.
