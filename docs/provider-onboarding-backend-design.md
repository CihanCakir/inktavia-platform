# Provider Onboarding — Backend Design

**Problem.** Onboarding is a UI shell. `src/features/onboarding/` makes **zero backend calls**, no onboarding
endpoints exist, drafts live in `localStorage`, and `ReviewSubmitPage`'s submit is an explicit no-op
(`disableSubmit`, "the POST /onboarding/submit endpoint is planned"). Meanwhile `RegisterOrganizer` creates the
provider profile immediately with `ApprovalStatus.Pending`, and `/me/status` has no concept of onboarding, so its
final `else` branch returns **`AwaitApproval`** for every freshly registered provider.

Consequence: the provider lands on `/status/account` → "Your application is under review" with a **disabled** CTA, and
`OnboardingResumeGuard` (which redirects `InReview`/`Submitted` away from `/onboarding/*`) actively bounces them out.
**A provider can never reach onboarding, yet is told their (empty) application is being reviewed.**

The frontend model is already well specified and explicitly waiting for this backend — `onboardingStatusModel.ts`
says *"until GET /onboarding/status + GET /onboarding/draft ship"*. This design fills that gap.

---

## What already exists (reuse, don't rebuild)

`UserProfileEntity` (Identity) already carries the approval domain: `ApprovalStatus`, `Status`, `ApprovedAt`,
`RejectedAt`, `RejectReason`, `RejectionCategory`, `InternalNote`, `ReviewedBy`, `ReviewedAt`, `CompanyName`, `City`,
`Country`, `TaxpayerType`, `NationalityId`, `Bio`, `PhoneVerified`, plus `VerificationDocuments` and `RiskSignals`
collections. Commands exist for `ApproveOrganizerProfile`, `RejectOrganizerProfile`, `SuspendOrganizerProfile`,
`ReactivateOrganizerProfile`, `AddOrganizerVerificationDocument`, `UpdateOrganizerProfile`.
`VerificationDocumentEntity` already references a `FileId` (file-storage-api).

Only the **provider's own submission progress** is missing.

---

## Decision 1 — Do NOT extend the shared `ApprovalStatus` enum

`ApprovalStatus` (`Pending=0, Approved=1, Rejected=2`) lives in **`Aizen.Core.Domain`** and is shared with Venue and
Participant. Adding `Submitted`/`InReview`/`NeedsRevision` there would leak provider-onboarding semantics into other
modules and change the meaning of stored values.

Instead, split the two concerns cleanly:

| Concern | Owner | Meaning |
|---|---|---|
| `ApprovalStatus` (existing, Core) | **Admin** | Have we approved this provider? `Pending → Approved / Rejected` |
| `ProviderOnboardingStatus` (new, Identity) | **Provider** | Has the provider given us their data? `NotStarted → InProgress → Submitted → (NeedsRevision) → Completed` |

`ApprovalStatus.Pending` finally becomes meaningful: it means "submitted, awaiting review" — not "just registered".

## Decision 2 — New `ProviderOnboardingEntity` (1:1 with the Organizer profile)

Table `provider_onboarding`, one row per Organizer `UserProfile`, created at registration in `NotStarted`.

| Column | Notes |
|---|---|
| `ProfileId` | unique FK → `UserProfiles` (Organizer context) |
| `UserId` | denormalized for lookup by user |
| `Status` | `ProviderOnboardingStatus` |
| `SchemaVersion` | int; matches the FE draft `schemaVersion` |
| `StepStatusesJson` | jsonb: `{ BusinessIdentity: "Completed", ... }` |
| `DraftJson` | jsonb: the `OnboardingDraft` payload (per-step data) |
| `RevisionStepsJson` | jsonb: steps an admin sent back |
| `RevisionNote` | admin's revision message |
| `LastSavedAtUtc`, `SubmittedAtUtc`, `ReviewedAtUtc` | timestamps (UTC, Postgres-safe) |

**Why jsonb for the draft:** the FE already defines a versioned `OnboardingDraft` shape with six optional step
payloads. Normalizing that into six tables now is premature — the shape is still moving, and every field change would
be a migration. jsonb keeps it evolvable and is still queryable in Postgres if reporting needs it later.

**But the draft is not the source of truth for the business.** On **submit**, the authoritative fields are mirrored
onto `UserProfileEntity` (`CompanyName`, `City`, `Country`, `TaxpayerType`, `NationalityId`, `Bio`, owner name,
contact phone) so the admin panel, risk signals and every existing query keep working off the profile exactly as they
do today. The draft is the provider's workspace; the profile stays the record.

## Decision 3 — Documents reuse the existing pipeline

No new document model. Provider upload → BFF → **file-storage-api** (returns `FileId`) →
`AddOrganizerVerificationDocument` → `VerificationDocumentEntity`. The FE's local `DocMeta` mock is replaced by the
real document list returned from `GET /onboarding`.

## Decision 4 — `/me/status` gains `CompleteOnboarding` (this is what unblocks the guards)

`GetProviderStatusQueryHandler` becomes onboarding-aware. New precedence:

```
!EmailVerified                          → VerifyEmail
no provider profile                     → CompleteProfileLink
ProfileStatus == Suspended              → Suspended
ApprovalStatus == Rejected              → Rejected
Approved && Active                      → EnterWorkspace
onboarding == NeedsRevision             → NeedsRevision
onboarding == Submitted                 → AwaitApproval        ← only now is this honest
otherwise (NotStarted / InProgress)     → CompleteOnboarding   ← NEW
```

`CompleteOnboarding` maps to `InProgress` on the frontend, so `canResume` is true and `OnboardingResumeGuard` renders
the flow instead of bouncing to `/status/in-review`.

## Decision 5 — Submit is a real transition

`POST /onboarding/submit` validates that the four **required** steps (`BusinessIdentity`, `ServiceCapabilities`,
`OperatingRegion`, `ComplianceVerification`) are `Completed` — `CargoDryInterest` is optional and never blocks —
then: `Status = Submitted`, `SubmittedAtUtc = now`, mirror fields onto the profile, re-run `RiskAssessmentService`
(it currently runs at registration with `documentCount: 0`, which is meaningless), and leave
`ApprovalStatus = Pending` for the admin. Idempotent: re-submitting an already-`Submitted` record is a no-op.

## Decision 6 — Close the review loop

- `ApproveOrganizerProfile` (existing) also sets onboarding `Completed`.
- New admin command `RequestOnboardingRevision(profileId, steps[], note)` → onboarding `NeedsRevision` + the named
  steps flipped to `NeedsRevision` + note. The provider's guard then routes to `/status/needs-revision`, they fix the
  flagged steps and re-submit.

---

## Endpoints

**BFF (provider-facing, authenticated, `/api/v1/provider`)**

| Method | Path | Purpose |
|---|---|---|
| GET | `/onboarding` | status + step statuses + draft + documents + revision note |
| PUT | `/onboarding/steps/{step}` | save one step's data; sets `InProgress` or `Completed` |
| POST | `/onboarding/submit` | validate required steps → `Submitted` |
| POST | `/onboarding/documents` | multipart upload → file-storage → record document |
| DELETE | `/onboarding/documents/{id}` | remove a document (only while not `Submitted`) |

**Identity (service-token, `IdentityWrite`/`IdentityRead`)**

| Method | Path |
|---|---|
| GET | `/api/v1/identity/provider-onboarding/{profileId}` |
| PUT | `/api/v1/identity/provider-onboarding/{profileId}/steps/{step}` |
| POST | `/api/v1/identity/provider-onboarding/{profileId}/submit` |
| POST | `/api/v1/identity/provider-onboarding/{profileId}/revision` (admin) |

The BFF resolves the caller's `profileId` from the provider context — it is never taken from the request body.

---

## Phases

**Phase 1 — MVP (unblocks the flow, end-to-end real)**
1. `ProviderOnboardingStatus` enum + `ProviderOnboardingEntity` + migration; row created by `RegisterOrganizer`
   (and back-filled for existing profiles).
2. Identity: `GetProviderOnboarding`, `SaveProviderOnboardingStep`, `SubmitProviderOnboarding` (+ validator) and the
   `ApproveOrganizerProfile` hook.
3. BFF: the five provider endpoints above; `/me/status` returns `CompleteOnboarding`.
4. Frontend: `onboardingApi`, `onboardingStatusModel` reads the server (localStorage demoted to an offline cache),
   step pages `PUT` on save, **ReviewSubmit's submit enabled**, `AccountStatusPage` CTA + `mapRequiredNextStepToRoute`
   route `CompleteOnboarding` → `/onboarding/welcome`.
5. Real document upload wired to file-storage.

**Phase 2 — Review loop**
`RequestOnboardingRevision` (admin panel), per-step revision surfacing, document review states
(`NEEDS_REVIEW`/`REJECTED` + resolution notes) shown to the provider, notification on every status change.

**Phase 3 — Scale/polish**
Cross-device draft conflict handling (`lastSavedAt` optimistic concurrency), audit trail of submissions, file
scanning/AV on upload, and reporting queries over `DraftJson`.

---

## Risks

- **Existing providers** (already `Pending` with no onboarding row) must be back-filled, otherwise `/me/status`
  cannot classify them. The migration seeds `NotStarted` for every Organizer profile that has no row.
- **`ApprovalStatus.Pending` changes meaning** (from "just registered" to "submitted, awaiting review"). The admin
  panel's pending queue should filter on onboarding `Submitted` so it stops showing empty applications.
- Draft `jsonb` is schemaless by design — `SchemaVersion` must be checked on read, and unknown versions rejected
  rather than silently mis-parsed.
