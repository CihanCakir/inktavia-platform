# BE — Vessel detail resolve + document actions (admin)

Context: admin-web `/app/vessels` QA. The FE now renders vessel **media**, **owner**, and **documents** on the
detail page, wires the document **Approve** button, and exports the list to CSV. Several of these depend on the
BFF/module actually resolving values and exposing routes that are currently missing. This kickoff closes those gaps.
Standing rules apply: modular monolith, CQRS, typed bodies, BFF-composes cross-module, presigned URLs from storage,
best-effort BFF (`AdminBffWarning`, never 500), UTC/timestamptz-safe, **DO NOT COMMIT**.

Reference file: `Bff/src/AdminPanel/…/Vessels/Query/GetVesselByIdBff/GetVesselByIdBffQueryHandler.cs`
(currently a pure passthrough — `_vessel.GetVesselById → Body`, resolves nothing).
Controller: `Bff/src/AdminPanel/…/Controllers/V1/VesselsController.cs`.

---

## Part A — Resolve owner display names on vessel detail (R5)

The detail response includes `owners[]` but the module returns identity ids, not display names (same class of gap
QA-3 fixed for SR). The FE shows `owner.name ?? #userId`.

1. In `GetVesselByIdBffQueryHandler`, after fetching the vessel, collect the distinct `owners[].userId`,
   batch-resolve display names via the Identity remote call already used in QA-3
   (`IIdentityRemoteCall.GetDisplayNamesByUserIds` / the scoped display-name endpoint), and populate `owners[].name`
   (+ `email`/`avatarUrl` if the scoped endpoint returns them).
2. Best-effort: on Identity failure, leave names empty and append an `AdminBffWarning{ module = "Identity" }` — HTTP 200.

## Part B — Presigned read URLs for media & documents (R5)

The FE renders `media[].url` / `media[].thumbnailUrl` in `<img>`/`<video>` and links `documents[].fileUrl`. If the
module persists **storage keys** (MinIO object keys) rather than time-limited URLs, images won't load.

1. Confirm whether the Vessel module returns keys or URLs for `media[].url/thumbnailUrl`, `documents[].fileUrl/thumbnailUrl`.
2. If keys: in the BFF detail handler (and in `GetVesselMediaBff` / `GetVesselDocumentsBff`), convert each key to a
   presigned GET URL via the shared storage/presigner service (the same one used by messaging image read-urls,
   `14c`-style). Do it in one pass; skip nulls. Keep expiry consistent with other admin read-urls.
3. Best-effort per item; a presign failure drops that item's URL + warning, never a 500.

## Part C — Document Approve route (B2)

The FE calls `PATCH /vessels/{vesselId}/documents/{documentId}/approve` (`vesselApi.approveDocument`) but
`VesselsController` has **no such route** → 404 today.

1. Add `ApproveVesselDocumentBff` command (mirror `RemoveVesselDocumentBff`): command + handler + `IVesselRemoteCall.ApproveVesselDocument`.
2. Controller: `[HttpPatch("{vesselId:long}/documents/{documentId:long}/approve")]`.
3. Module side: if a document-approve command/endpoint doesn't exist, add one (sets `ApprovedAt`/`ApprovedByUserId`,
   status transition to `valid`/approved). Idempotent (re-approve is a no-op). Resolve `approvedByName` on read.
4. Tests: BFF handler test + module approve command test (state transition, idempotency).

## Part D — Document & media Upload / Replace routes (B3, B4)

The FE Upload (header) and Replace (slide-panel) buttons are currently **disabled** because there is no backend.
To enable them:

1. Module: add a vessel-document upload command (create new document + first version) and a replace/new-version
   command (adds a `DocumentVersion`, flips `isCurrent`). Same for vessel media upload.
2. BFF: presigned **PUT** upload-url endpoints (mirror the profile-approval `documents/upload-url` +
   `documents` register two-step pattern) — `POST …/documents/upload-url`, `POST …/documents` (register),
   `POST …/media/upload-url`, `POST …/media` (register), and `POST …/documents/{id}/versions` (replace).
3. Then the FE can re-enable the buttons (remove `disabled` / `replaceDisabled`, wire the two-step upload like the
   profile-approval document upload flow already in admin-web).

## Part E — List resolve sanity (thumbnail / ownerName)

The list already reads `thumbnailUrl`/`coverMediaUrl`/`ownerName`/`operationalStatusLabel`. Verify the list BFF
actually populates these (owner name via Identity batch; thumbnail via primary media presign). If any are empty,
resolve them the same way as Part A/B. The FE degrades gracefully (icon fallback, `—`) so this is non-blocking.

## Verification
- `GET /vessels/{id}` (or `/detail`) returns `owners[].name` populated, `media[].url`/`documents[].fileUrl` as
  loadable presigned URLs; kill Identity → names empty + warning, HTTP 200.
- `PATCH …/documents/{id}/approve` flips status + sets approver; visible in the FE slide panel; re-approve idempotent.
- Upload/Replace endpoints accept a file via presigned PUT then register; FE buttons re-enabled and functional.
- All new unit tests green; `dotnet build` 0 errors; admin-web `tsc`+`eslint` clean after re-enabling buttons.
- **No commit.**
