# Admin Approval Queue — Onboarding-Aware Fix Report

**Date:** 2026-07-12
**Status:** Code complete, builds clean. Smoke blocked by Docker infrastructure (OOM on Keycloak).

## The problem (before this fix)

The admin approval queue filtered on `ApprovalStatus == Pending` only. Since registration immediately sets
`Pending`, **every provider who signed up and filled in nothing** appeared in the review queue. The `SubmittedAt`
field was mapped from `CreateDate` (registration date, not submission date), and `ReviewedAt`/`RiskLevel` were
hardcoded to `null`.

**Before-fix data** (from dev DB, 13 Organizer profiles):
- All 13 had `ApprovalStatus = Pending` → all 13 appeared in the pending queue
- Only 1 had `OnboardingStatus = Submitted` (profile 11011, submitted during Phase 1 smoke test)
- 12 were `NotStarted` — blank applications sitting in the admin's review queue

**The queue was 92% noise** (12/13 items should not have been there).

## What was built

### Identity — onboarding-enriched profile query

- `OrganizerProfileListItemDto` now includes `OnboardingStatus`, `SubmittedAtUtc`, `DocumentCount`
- `GetOrganizerProfilesByFilterQueryHandler` joins `ProviderOnboarding` and `VerificationDocuments` to populate
  these fields (batch lookup after paging, no N+1)
- **Server-side `onboardingStatus` filter** added to `GetOrganizerProfilesByFilterQuery` — the BFF can request
  only `Submitted` profiles without fetching everything
- Controller + request DTO updated to pass the filter through

### Admin BFF — queue now means "waiting for a human decision"

`GetProfileApprovalQueueBffQueryHandler` reworked:

| Status filter | What it means | Approval filter | Onboarding filter |
|---------------|---------------|-----------------|-------------------|
| `pending` | Ready for review | `Pending` | `Submitted` |
| `incomplete` | Registered, not submitted | `Pending` | NOT `Submitted`/`Completed` |
| `needs_revision` | Sent back, waiting on provider | any | `NeedsRevision` |
| `approved` | Already approved | `Approved` | any |
| `rejected` | Already rejected | `Rejected` | any |

Other fixes:
- `SubmittedAt` mapped from `SubmittedAtUtc` (real submission date), falls back to `CreateDate` for venues
- `ReviewedAt` mapped from `item.ReviewedAt` (was hardcoded null)
- `RiskLevel` mapped from `item.RiskLevel` (was hardcoded null)
- `OnboardingStatus` and `DocumentCount` added to `ProfileApprovalQueueItemBffDto`
- `ComputeSummary` counts `PendingOrganizers` only where `OnboardingStatus == "Submitted"`
- New summary fields: `IncompleteOrganizers`, `NeedsRevisionOrganizers`

### DTOs updated

- `ProfileApprovalQueueItemBffDto`: added `OnboardingStatus`, `DocumentCount`
- `ProfileApprovalSummaryBffDto`: added `IncompleteOrganizers`, `NeedsRevisionOrganizers`
- `IIdentityAdminBffRemoteCall`: added `onboardingStatus` query parameter

### Venue profiles

Venues have no onboarding record → `OnboardingStatus = null`. They continue to appear in the queue exactly as
before — no regression.

## Build results

- Identity (5 projects): **0 errors** ✓
- Admin BFF (2 projects): **0 errors** ✓
- MarineProvider BFF (2 projects): **0 errors** ✓

## Smoke test

**Blocked** — Docker Desktop ran out of memory (too many containers; Keycloak OOM-killed during startup). The
database state from the earlier session confirms:
- 13 Organizer profiles with `ApprovalStatus = Pending`
- 12 with `OnboardingStatus = NotStarted` → should be in `incomplete`, not `pending`
- 1 with `OnboardingStatus = Submitted` → the only legitimate `pending` item

Full smoke must be re-run when Docker is stable. Expected results:
- `status=pending` returns **1** item (not 13)
- `status=incomplete` returns **12** items
- `SubmittedAt` matches `SubmittedAtUtc` from onboarding (not `CreateDate`)
- Venues unaffected

## Admin frontend

Tab/filter additions (`incomplete`, `needs_revision`) are deferred — the backend API changes are backwards-compatible
(the existing `pending` filter now correctly returns only submitted applications).
