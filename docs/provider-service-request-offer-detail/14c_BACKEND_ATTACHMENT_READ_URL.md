# 14c — Backend: attachment signed read-URL (+ a seed to prove it)

Closes the last data gap on the detail page: the photo/document gallery can list attachments (metadata is already
in the P1 detail response) but cannot open them. This adds the signed-read-URL endpoint, **access-scoped**, and a
seed so it can be verified end-to-end. Frontend gallery wiring follows separately.

## The security-critical rule (do not get this wrong)

A signed read URL must be minted **only** for an attachment on a request the calling provider is authorized to
see. The onboarding read-url handler already models this correctly — it refuses unless the file belongs to the
caller's own profile:

> `// could read another provider's documents. Only files that are attached to THIS profile may be read.`

Mirror that here: **before** calling `CreateReadUrl`, verify the provider's relationship to the request (the same
access check the detail query uses: biddable | has offer | assigned), and that the `fileId` is actually an
attachment **on that request**. A handler that blindly proxies `CreateReadUrl(fileId)` lets a provider read **any
file in the system** by guessing a GUID. Fail closed: unknown request, no relationship, or a fileId not attached
to that request → the same "not found" the detail uses, no signed URL.

## Verified in source (2026-07-15)

- The BFF already has `IProviderFileStorageRemoteCall.CreateReadUrl(fileId, CreateReadUrlRequest { ExpiresIn })`
  → `FileAccessUrlDto { ReadUrl, ExpiresAt }`. Onboarding uses it in
  `GetDocumentAccessUrlCommandHandler` (with the ownership check above) behind
  `POST /onboarding/documents/{fileId:guid}/access-url`.
- The detail response already carries `ProviderAttachmentMetaDto { Id, FileId (Guid), AttachmentType, Title,
  CreatedAt }` — **no signed URL, no object key** (correct; keep it that way).
- **No request in the seed data has any attachment.** So end-to-end verification needs a seeded file + attachment.

## Work

### 1. BFF endpoint (access-scoped)
`GET /api/v1/provider/service-requests/{serviceRequestId:long}/attachments/{fileId:guid}/read-url`
→ `{ url, expiresAt }`.

Handler:
1. Resolve the provider from the assertion (never the client).
2. **Authorize**: confirm the provider may see this request (reuse the module's detail access check — call the
   detail/attachment-scoped module endpoint, or a small "is this fileId an attachment on a request this provider
   may see" check). Do **not** trust the client's claim that the fileId belongs to the request.
3. Only then `CreateReadUrl(fileId, new CreateReadUrlRequest { ExpiresIn = TimeSpan.FromMinutes(5) })`.
4. Return `{ url, expiresAt }`. **Never** return the object key/bucket; **never** store the URL; mint per click.
5. Fail closed on any authorization miss — same "not found" as detail; log the denial (without the token/URL).

Add the Refit method + a small module query if needed to answer "is `fileId` an attachment on request `id`, and
may this provider see it?" Prefer reusing the existing detail access check rather than inventing a second one.

### 2. Seed — one request with a real attachment
So the gallery has something to open, seed:
- one FileStorage file (a small image; follow the FileStorage seeder/mechanism — a real object in MinIO, or the
  seeder's existing way of creating files), and
- a `ServiceRequestAttachment` row linking that `FileId` to a biddable request in city `35` (so provider2 sees
  it). Reuse the emergency seed's request if simplest, or a dedicated one.
- Idempotent (fixed ids / guard), like the other seeds.

If seeding a real MinIO object is heavy, say so and seed just the attachment row pointing at a file the
FileStorage seeder already creates — but the read-url must resolve to a real object for the image to open.

## Acceptance — observed
- `GET …/attachments/{fileId}/read-url` for an attachment on a request **provider2 may see** → returns a
  short-lived `url` that actually opens the file. Paste the response (URL redacted is fine, but confirm it works).
- The **same** call for a `fileId` **not** attached to that request → rejected (not found), no URL.
- A request provider2 has **no relationship** to → rejected.
- No object key/bucket in any response or log. URL TTL ~5 min.
- Detail still returns attachment **metadata only** (no URLs embedded).

## Constraints
- Access check **before** minting. Never proxy `CreateReadUrl` unscoped. Provider identity from the assertion.
- Signed URL per click, short-lived, never stored, never in the detail aggregate. No object keys to the SPA.
- No change to the offer or calculation paths.

## Report
Append to `REPORT_BACKEND.md` (section "14c"): the working read-url response, the two rejection cases
(wrong-file, no-relationship), the seed's request/file ids, and confirmation no object key leaks. Unfinished is
**not done**.

## Frontend (after this lands — I will do it)
The gallery tiles call this endpoint on click and open the returned URL in a lightbox; until clicked, only
metadata is shown. No URLs are prefetched or stored.
