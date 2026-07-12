# Claude Code Prompt — Phase 2d: fail closed + actually check file ownership

Phase 2c did real work: `PublicId` is assigned and backfilled, the FileStorage public API is Guid-keyed, the three
consumers were migrated, and `AttachProviderDocumentCommandHandler` now calls FileStorage for existence, status,
content-type and size, takes snapshots from the server, and claims the file with `LinkToOwner`. Keep all of that.

But the summary claimed "Full file validation", and it is not. Four defects remain, and one of them **silently
disables every check that was just added**.

---

## Defect 1 (critical) — the validation is FAIL-OPEN

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "FileStorage validation failed for file {FileId}. Proceeding with client-supplied data.");
    // Fail open for FileStorage connectivity issues — the cross-profile guard (step 4) is still active
}
```

If the FileStorage call throws for **any** non-business reason — network blip, timeout, 401, deserialization error,
FileStorage down — the handler swallows it and **attaches the document using unvalidated, client-supplied data**.
Status, content-type, size and the server-side snapshots are all skipped.

An attacker does not need to defeat the checks; they only need to make the call fail. And with no attacker at all, a
transient error is enough to persist an unvalidated file.

**Fix: fail closed.** Any failure to validate → reject the attach with a business exception
("Unable to verify the file. Please try again."). Never proceed on client-supplied data. The explicit constraint in
the Phase 2c prompt was *"failing closed at every step"*.

## Defect 2 (critical) — file ownership is still never checked

This is the vulnerability the whole Phase 2b/2c effort exists to close, and it is still open.

- `ValidateOwnership` is never called.
- `file.UploadedByUserId` **is read** (and even persisted onto the document row) but **never compared to the caller**.

So provider B can still attach provider A's `fileId`: existence ✓, status ✓, content-type ✓, size ✓, cross-profile
uniqueness ✓ (A never attached it) → the document is created on **B's profile**, carrying
`UploadedByUserId = A`, and FileStorage then **claims A's file for B's document**.

**Fix:**
1. Add `long UserId` to `AttachProviderDocumentCommand` (it currently has none — which is why the handler's own
   docstring claims a "Caller owns the profile (UserId check)" that does not exist in the query).
2. The BFF passes the caller's Identity user id (it already resolves the provider identity; use the same holder it
   uses for `ProviderProfileId`).
3. In the handler, **before persisting**:
   - `profile.UserId == request.UserId` — otherwise reject. The Identity endpoint must defend itself, not rely on the
     BFF being correct.
   - **`file.UploadedByUserId == request.UserId`** — otherwise reject. This is the check that closes the hole.
     Additionally call `ValidateOwnership` where it gives a stronger answer than the uploader id.
4. Keep the cross-profile uniqueness guard, but **it is not the security boundary** — it only catches a file already
   attached elsewhere, so it cannot stop B from stealing a file A uploaded but has not attached yet.

## Defect 3 — `LinkToOwner` can claim against a non-existent owner

```csharp
OwnerEntityId = document.PublicId ?? Guid.NewGuid(),
```

If the document has no `PublicId`, this invents a random Guid at call time and claims the file against an owner id
that **exists nowhere**. This is the same class of bug as the `?? Guid.Empty` fallback just removed from FileStorage.

**Fix:** assign `PublicId` when the document is created (`VerificationDocumentEntity.CreateFromBff`), then pass it.
If it is missing, that is a bug — throw, do not fabricate.

## Defect 4 — the docstring asserts a check that does not exist

The handler's XML comment lists *"2. Caller owns the profile (UserId check)"*, but the query filters only on
`p.Id == request.ProfileId && RoleContext == Organizer && !IsDeleted`. Fix the code (Defect 2), then make the comment
describe what the code actually does. A comment that claims a security check the code does not perform is worse than
no comment.

## Cleanup (not security, but a convention break)

The handler acquires a Keycloak token inline (`GetServiceTokenAsync`) and passes `$"Bearer {token}"` as a parameter to
each remote call. Identity already has an outgoing service-token delegating handler for module calls — use it, and
drop the manual token plumbing and the `[AizenRemoteCallHeader]`-style token parameter.

## Still deferred from 2c (carry forward, do not silently drop)

- Cleanup consumer for unclaimed/orphaned files (upload-then-abandon is the normal case).
- Frontend Part F: the signed-URL upload flow. **Document upload still does not work from the UI.**
- Legacy `AddOrganizerVerificationDocumentCommandHandler` must delegate to the same validated path — do not leave one
  validated and one unvalidated handler.

---

## Acceptance — the phase is done only if these pass

Write `docs/provider-onboarding-phase2d-report.md`. Paste real HTTP statuses and bodies.

1. **Ownership:** provider A uploads a file and does **not** attach it. Provider B calls attach with A's `fileId` →
   **rejected**. *If this succeeds, the task has failed. Say so plainly; do not report the phase complete.*
2. **Fail-closed:** stop the `file-storage-api` container, then attach a document → **rejected** with "unable to
   verify the file". *Today this succeeds with unvalidated client data — that is the bug.*
3. Attach a `fileId` whose status is `UploadUrlGenerated` (session created, never uploaded) → rejected.
4. Oversize file, and a `.exe` renamed to `.pdf` → rejected.
5. Happy path: A attaches their own file → succeeds; the FileStorage owner reference is active and points at the
   document's **real** `PublicId` (not a random Guid); the row carries `UploadedByUserId = A`.
6. Two different providers each attach a different file → **both succeed** (regression guard for the `Guid.Empty` bug).

## Constraints

- **Fail closed.** A validation that cannot be performed is a rejection, never a pass.
- Never attach a `FileId` without: exists + **ownership** + upload-status + content-type + size.
- Snapshots come from FileStorage's `FileDto`, never from the client.
- Never fabricate an identifier (`?? Guid.NewGuid()`, `?? Guid.Empty`) — a missing id is a bug, so throw.
- If something cannot be finished, leave the TODO **and say so in the summary**. Do not describe an unfinished item
  as "full validation".
