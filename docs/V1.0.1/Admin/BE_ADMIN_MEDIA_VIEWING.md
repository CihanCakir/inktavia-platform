# Admin-wide media/evidence viewing (presigned) — service requests, job records, provider proofs, customer images

Goal: the admin must be able to **view every uploaded image/file** across the platform from the admin panel —
vessel media (done), plus service-request attachments, provider work-log/job evidence, completion proofs, dispute
evidence, and customer-supplied request images/message images. Today the admin panel cannot show these: the file IDs
exist in the modules but the admin BFF does not resolve them into viewable presigned URLs, and the admin surfaces
have no image UI.

The plumbing already exists — this is about **wiring the existing presigner across the remaining admin surfaces**, not
inventing new infra:
- `IFileStorageRemoteCall.CreateReadUrl(fileId)` → presigned GET, already used for vessel media/documents +
  profile-approval documents.
- Generic BFF commands `CreateFileReadUrlBff` and `BulkGenerateReadUrlsBff` already exist.

## What's missing (verified)
- **ServiceRequest module** holds the file IDs: request attachments, message attachments, work-log evidence
  (`Assignment.WorkLogs[].AttachmentFileId`), completion evidence (`Completion.EvidenceFileId`), dispute evidence
  (`DisputeCase.EvidenceFileId`).
- There are **owner** and **provider** attachment access-url paths (`GetOwnerAttachmentAccessCheck`,
  `GetProviderJobDetail` exposes `CompletionEvidenceFileId`, `GetAttachmentAccessUrl`), but **no admin path** — admin
  is trusted but has no attachment read-url endpoint, and the admin SR detail BFF DTOs don't carry the file IDs or URLs.

## Phase 1 — module: admin attachment read-url (trusted, no per-owner check)
Add an admin-scoped attachment access/read-url query in the ServiceRequest module that, given an SR id + fileId,
verifies the fileId genuinely belongs to that SR (reuse the same membership predicate as the owner/provider
access-checks: request attachment ∥ message attachment ∥ work-log evidence ∥ completion evidence ∥ dispute evidence),
then returns a presigned read URL — but **without** the per-owner/provider identity gate (admin is authorized at the
BFF controller). Alternatively, expose the raw fileIds on the admin SR detail read model and let the BFF presign them
directly via `CreateReadUrl` (simpler — admin is trusted, and the BFF already owns the presigner). Prefer the
**expose-fileIds + BFF-presign** route to avoid a new module endpoint per surface.

## Phase 2 — admin BFF: presign SR file surfaces into the detail response
In the admin SR detail composer (`GetServiceRequestDetailBff` / the operation-detail handler), collect every fileId on
the SR — request attachments, each message attachment, each work-log `AttachmentFileId`, `Completion.EvidenceFileId`,
dispute `EvidenceFileId` — dedupe, and **bulk-presign** via `IFileStorageRemoteCall.CreateReadUrl` (one pass, best-
effort per item → `AdminBffWarning{FileStorage}`, never 500). Attach the resulting URLs (+ mimeType/isImage) to the
response so the FE can render thumbnails/lightbox. Mirror the vessel `PresignAsync` helper already in
`GetVesselByIdBffQueryHandler`.

Surfaces to cover (each an admin BFF read that currently omits URLs):
- SR detail → request attachments gallery.
- SR detail → completion evidence (provider proof) gallery.
- SR detail → work-log / job records photos.
- SR detail → dispute evidence.
- SR messages → inline image attachments (admin messaging already has message attachment fileIds; presign the image
  ones for inline preview).

## Phase 3 — admin-web FE: display
Reuse the existing `MediaGallery` + lightbox (from vessels) for image sets. On the SR detail page add:
- an **Attachments / Evidence** section grouping request attachments, completion evidence (provider proof), work-log
  photos, dispute evidence — each an image thumbnail grid (lightbox on click) with a file-download affordance for
  non-images (PDF icon + download, like the documents table).
- inline image thumbnails in the admin message view for image attachments.
Guard every URL (`url ? <img/> : icon`), tr-TR labels + i18n, empty states. No fabricated data.

## Answer to "is it also a FE problem?"
- **Vessel images:** not a FE bug. The FE `MediaGallery` renders the presigned URL; placeholders on seed vessels are
  because those seed fileIds are objectless/unpresignable in this env (and, until `BE_VESSEL_DOC_MEDIA_CACHE_FIX`
  lands, a just-uploaded image can lag behind the stale read cache). Real uploaded files render.
- **SR/provider/customer images:** currently not viewable admin-side — but that's the BFF-resolve + FE-display gap
  above, not a bug in existing code. This kickoff closes it.

## Verify
- Admin SR detail returns loadable presigned URLs for request attachments, completion evidence, work-log photos,
  dispute evidence; kill FileStorage → URLs empty + warning + HTTP 200.
- FE renders each image set (thumbnail grid + lightbox), non-images as download rows; tr-TR; console clean;
  tsc/eslint clean.
- **DO NOT COMMIT.**
