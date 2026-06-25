# 06 — Implement Document and Media Commands

Implement Post-MVP commands only if dependencies are stable and required by current UI.

Commands:

- ApproveDocument
- ReplaceDocument

Approve:

- validates vessel/document relation
- sets ApprovedAt = UtcNow
- sets ApprovedByUserId from admin context

Replace:

- validates file
- uses existing FileStorage/upload convention
- creates new document version
- updates CurrentVersionId
- clears approval if required

If FileStorage pattern is not ready, return 501 from BFF and document it.
