# Provider Onboarding Backend — Phase 1 Report

**Date:** 2026-07-12
**Status:** Implemented and smoke-tested. All Identity (5 projects) + BFF (2 projects) build with 0 errors.

## What was built

### Identity module (Part A)

- **`ProviderOnboardingStatus`** enum: `NotStarted=0, InProgress=1, Submitted=2, NeedsRevision=3, Completed=4`
- **`OnboardingStep`** + **`OnboardingStepStatus`** enums
- **`ProviderOnboardingEntity`** (`provider_onboarding` table): DDD entity with `Create`, `SaveStep`, `Submit`,
  `RequestRevision`, `MarkCompleted`. jsonb columns for `StepStatusesJson` and `DraftJson`. Unique index on `ProfileId`.
- **Migration** `AddProviderOnboarding`: creates the table + backfills `NotStarted` rows for all existing Organizer
  profiles (13 rows seeded on the dev database).
- **`IProviderOnboardingRepository`** + impl: `GetByProfileIdAsync`, `GetByUserIdAsync`, `AddAsync`, `Update`
- **`IProviderOnboardingDomainService`** + impl: `GetAsync`, `SaveStepAsync` (server-side merge into DraftJson),
  `SubmitAsync` (validates required steps, mirrors draft to `UserProfileEntity`), `RequestRevisionAsync`,
  `EnsureOnboardingRowAsync`
- **CQRS**: `GetProviderOnboardingQuery`, `SaveProviderOnboardingStepCommand` (with validator — known steps enforced),
  `SubmitProviderOnboardingCommand`, `RequestProviderOnboardingRevisionCommand`
- **Controller**: `api/v1/identity/provider-onboarding/{profileId}` — GET, PUT steps/{step}, POST submit, POST revision
- **Registration hook**: `RegisterOrganizerCommandHandler` creates a `NotStarted` onboarding row (best-effort)
- **Approval hook**: `ApproveOrganizerProfileCommandHandler` sets onboarding `Completed` (best-effort)

### BFF (Part B)

- **Controller**: `api/v1/provider/onboarding` — GET `/`, PUT `/steps/{step}`, POST `/submit`. `[Authorize]`.
  profileId resolved from `IProviderContext` — never from the request.
- **Remote call methods**: `GetProviderOnboarding`, `SaveProviderOnboardingStep`, `SubmitProviderOnboarding` on
  `IProviderIdentityRemoteCall`
- **`/me/status` fix**: `GetProviderStatusQueryHandler` now fetches onboarding status and returns:
  - `CompleteOnboarding` when onboarding is `NotStarted` or `InProgress`
  - `AwaitApproval` only when onboarding is `Submitted`
  - `NeedsRevision` when onboarding is `NeedsRevision`
  - Degrades to `AwaitApproval` on onboarding lookup failure (never 500s)
- `OnboardingStatus` added to `GetProviderStatusResponse`

### Frontend (Part C)

- **`onboardingApi.ts`** created: `get()`, `saveStep()`, `submit()` using authenticated `httpClient`
- **`endpoints.ts`**: added `onboarding.get`, `onboarding.saveStep(step)`, `onboarding.submit`
- **`providerStatus.ts`**: `CompleteOnboarding` added to `RequiredNextStep`, mapped to `/onboarding/welcome`
- **`onboardingStatusModel.ts`**: `deriveStatus` maps `CompleteOnboarding` → `InProgress`
- **`AccountStatusPage.tsx`**: `CompleteOnboarding` CTA with "Continue" label navigating to onboarding
- **`ReviewSubmitPage.tsx`**: submit wired to `onboardingApi.submit()`, warning banner removed

## Smoke results

| Step | Result |
|------|--------|
| Save BusinessIdentity | `success=true` ✓ |
| Save ServiceCapabilities | `success=true` ✓ |
| Save OperatingRegion | `success=true` ✓ |
| Save ComplianceVerification | `success=true` ✓ |
| Status after saves | `InProgress`, all 4 steps `Completed` ✓ |
| Submit | `success=true` ✓ |
| Status after submit | `Submitted`, `submittedAtUtc` set ✓ |
| Backfill | 13 existing Organizer profiles backfilled with `NotStarted` ✓ |

## The bug fix

**Before:** Every new provider → `ApprovalStatus.Pending` → `/me/status` returns `AwaitApproval` → provider parked on
"under review" page with disabled CTA → can never reach onboarding.

**After:** New provider → onboarding row `NotStarted` → `/me/status` returns `CompleteOnboarding` → provider routed
to `/onboarding/welcome` → fills steps → submits → **then** `/me/status` returns `AwaitApproval` (honestly this time).

## Phase 2 gaps (not in scope)

- `RequestOnboardingRevision` admin panel integration
- Document upload/delete BFF endpoints (file-storage pipeline)
- Per-step revision surfacing in the frontend
- Cross-device draft conflict handling (optimistic concurrency)
- Real document review states
