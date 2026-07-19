# JD-6 — Backend: completion requires an "after" photo + expose it for before/after

Product rule: a job **cannot be completed without a final "after" photo** (paired with the mandatory "before" photo the
provider takes on the first work log — enforced on the frontend). This phase makes the **after** photo mandatory on the
server and exposes it in the job aggregate so the SPA can render a before/after comparison after completion.

## Verified in source (2026-07-17)
- `SubmitServiceRequestCompletionCommandHandler`: creates `ServiceRequestCompletionEntity.Create(sr, assignment, userId,
  req.CompletionNotes, req.EvidenceFileId)`; `EvidenceFileId` is currently **optional** (nullable). Ownership + state
  guards were added in JD-2 (only from InProgress).
- `SubmitServiceRequestCompletionRequest`: `{ CompletionNotes?, EvidenceFileId? (Guid?) }`.
- JD-1 `GetProviderJobDetailResponse` returns assignment + `Request` + `WorkScope` + `Attachments` + `Timeline` +
  `AcceptedOffer`. The handler loads the SR via `GetByIdWithDetailsAsync` which **already includes `Completion`** — so
  `sr.Completion?.EvidenceFileId` is in hand, just not surfaced.
- The 14c read-url access-check already authorizes completion evidence (JD-5) → the SPA can mint a URL for it.

## Work

### 1. Guard: completion requires an evidence photo
In `SubmitServiceRequestCompletionCommandHandler`, after the ownership + state guards, add:
```
if (request.Request.EvidenceFileId is null || request.Request.EvidenceFileId == Guid.Empty)
    throw new AizenBusinessException("SR_COMPLETION_EVIDENCE_REQUIRED");
```
So a completion without the "after" photo is rejected. (The provider uploads the photo via the existing signed-URL flow;
the SPA sends its `EvidenceFileId`.)

### 2. Expose the completion evidence on the job aggregate (for before/after display)
- Add `public Guid? CompletionEvidenceFileId { get; init; }` to `GetProviderJobDetailResponse`.
- In `GetProviderJobDetailQueryHandler`, set it from `sr.Completion?.EvidenceFileId` (already loaded — no extra query).
- Optionally also `CompletedAtUtc` from the completion/assignment if handy (nice for the after caption) — optional.

## Constraints
- Additive + nullable on the response. The guard only affects the complete action. No new endpoint. Access-check for the
  evidence read-url already handled by JD-5. No customer PII. The "before" photo is enforced purely on the frontend (first
  work log must carry an attachment) — no backend change needed for it, since work-log `AttachmentFileId` already exists.

## Acceptance — observed
- `POST /provider/jobs/{id}/complete` **without** `evidenceFileId` → rejected `SR_COMPLETION_EVIDENCE_REQUIRED`.
- With `evidenceFileId` → 200, SR → CompletionSubmitted, completion carries the evidence.
- `GET /provider/jobs/{id}` after completion → `completionEvidenceFileId` populated; its read-url mints (JD-5). Before the
  completion it is null.

## Report
Append to `REPORT_BACKEND.md` ("JD-6"): the completion now rejects without evidence, and the job aggregate returns
`completionEvidenceFileId`. Unfinished is **not done**.

## Frontend (I build alongside)
Completion modal: the evidence photo becomes **required** (block submit + clear message). First work log requires a photo
(the "before"). A Before/After card shows the first work-log photo (before) next to `completionEvidenceFileId` (after)
once the job is CompletionSubmitted/Completed.
