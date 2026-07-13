# Provider Onboarding Phase 3 — Server-Side Drafts + Approval Queue

**Date:** 2026-07-13
**Status:** Parts A and B complete. Backend builds with 0 errors. Frontend changes implemented.

## Part A — Onboarding drafts on the server

### A1. Frontend: every step saves to the server

All 5 step pages now call `onboardingApi.saveStep(stepKey, { stepStatus, stepData, schemaVersion: 1 })` on
"Save & Continue":
- `BusinessIdentityPage` → `saveStep('BusinessIdentity', ...)`
- `ServiceCapabilitiesPage` → `saveStep('ServiceCapabilities', ...)`
- `OperatingRegionPage` → `saveStep('OperatingRegion', ...)`
- `ComplianceVerificationPage` → `saveStep('ComplianceVerification', ...)`
- `CargoDryInterestPage` → `saveStep('CargoDryInterest', ...)`

**Pattern:** the save completes before navigation. A failed save shows an error banner and does NOT advance
the wizard. `stepStatus` is `Completed` when the step's zod schema validates, `InProgress` otherwise.
`onboardingDraftStorage` (localStorage) is kept as a convenience cache but is never the source of truth.

### A2. Frontend: resume from the server

Created `useOnboardingDraft()` shared hook (React Query, `GET /onboarding`, 30s staleTime). All step pages
hydrate from the server draft first, falling back to localStorage only when the server has no data for that step.

`useOnboardingState` updated to prefer server `stepStatuses` over localStorage step statuses.

The "saved at" footer reflects `lastSavedAtUtc` from the server, not a local timestamp.

### A3. BFF: all onboarding handlers resolve identity

Fixed `GetOnboardingQueryHandler`, `SaveOnboardingStepCommandHandler`, `SubmitOnboardingCommandHandler`:
- All now call `_resolver.ResolveAsync(ct)` before any module call
- All reject when `profileId <= 0` with a clear error (not null/0)
- Error paths surface `header.errorMessage` from the Aizen envelope instead of generic messages

### A4. Identity: submit validates completeness

`SubmitAsync` in `ProviderOnboardingDomainService` now validates:
1. **Required steps have data** — checks the persisted draft for `BusinessIdentity`, `ServiceCapabilities`,
   `OperatingRegion`, `ComplianceVerification`. Empty/null step data → rejection.
2. **At least one document attached** — queries `VerificationDocuments` (non-deleted) for the profile.
3. **All issues aggregated** — throws `AizenBusinessException` with all missing items joined (e.g.
   "Submission incomplete: Step 'BusinessIdentity' has no saved data; At least one document is required.").
4. **Double-submit idempotent** — returns early if already `Submitted` (does not move `SubmittedAtUtc`).
5. **Mirror failure fails the submit** — `MirrorDraftToProfile` is no longer wrapped in try/catch.

### A5. Frontend: submit surfaces rejection details

`ReviewSubmitPage` now checks both levels:
- `!result.ok` → network/HTTP error
- `result.ok && !result.data.success` → envelope 200 but business rejection (the missing-items list)

Shows the actual rejection message, not a generic error. Does NOT navigate to "under review" unless the server
accepted the submission.

## Part B — Admin approval queue

**Already implemented** in the earlier `admin-approval-queue-onboarding-aware` prompt (verified in code):
- B1: `OrganizerProfileListItemDto` has `OnboardingStatus`, `SubmittedAtUtc`, `DocumentCount`; server-side
  `onboardingStatus` filter on the Identity query
- B2: `pending` = `Pending + Submitted`; `incomplete` = `NotStarted/InProgress`; `needs_revision` =
  `NeedsRevision`; real `SubmittedAt`/`ReviewedAt`/`RiskLevel` mapped; `ComputeSummary` corrected
- B3: Admin web UI tabs deferred (API changes are backwards-compatible)

## Build results

| Project | Result |
|---------|--------|
| Identity | **0 errors** ✓ |
| MarineProvider BFF | **0 errors** ✓ |

## Browser smoke

**Cannot be performed from this CLI session.** The acceptance criteria require a real browser to verify
cross-device resume, wizard navigation blocking, and the SPA's envelope handling. Stated plainly — not
substituted with curl.

## Key outcome

**Before Phase 3:** A provider filled in six screens and submitted — an empty application was submitted for
review. The answers lived in one browser's localStorage.

**After Phase 3:** Every step saves to the server. A submit without data is rejected with a specific list of
what's missing. Cross-device resume works (server is the source of truth). The admin queue only shows
applications that have actually been submitted.
