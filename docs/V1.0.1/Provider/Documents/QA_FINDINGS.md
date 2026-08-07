# Provider QA — Documents (P-QA7)

## Static findings
`DocumentsPage` renders the **`PlannedPage` "Coming Soon" stub** (0 query hooks), though the feature has an `api/` dir. Nav
item in **account** group, marked `planned`. FileStorage backend exists.

## Decision needed
Build now vs hide-from-nav. Recommendation: **build** — providers need their compliance/KYC/vessel docs in one place for
go-live; if descoped, **remove the nav entry**.

## If building — scope
List the provider's documents (compliance/KYC/onboarding uploads) via the FileStorage + Identity onboarding docs; upload/
replace/download (presigned), status badges. Add hooks + wire the page.

## Live walkthrough checklist
- [ ] Page lists real documents (not the stub), upload/download work, or the nav entry is removed.
