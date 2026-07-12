# Claude Code Prompt — Provider Onboarding Backend (Phase 1 / MVP)

Read `docs/provider-onboarding-backend-design.md` first; it contains the decisions this prompt implements. Do not
re-litigate them. Follow the existing Aizen conventions exactly (CQRS `AizenCommand`/`AizenCommandHandler`/
`AizenQueryHandler`/`AizenValidator`, `IAizenCQRSProcessor.ProcessAsync`, `AizenWebApiController` + `SetResponse`,
FluentValidation, EF Core, repository pattern, UTC `DateTime`). Produce complete, compile-ready files — no TODOs.

## Context / the bug you are fixing

Onboarding is a UI shell: `src/features/onboarding/` makes zero backend calls, no onboarding endpoints exist, drafts
live in `localStorage`, and `ReviewSubmitPage`'s submit is a no-op. `RegisterOrganizer` creates the profile with
`ApprovalStatus.Pending`, and `GetProviderStatusQueryHandler` has no onboarding concept, so its final `else` returns
`AwaitApproval` for every new provider. The provider is therefore parked on `/status/account` ("under review", CTA
disabled) and `OnboardingResumeGuard` bounces them out of `/onboarding/*`. They can never fill in the application
they are told is being reviewed.

---

## Part A — Identity module

### A1. Status enum (Identity-owned; do NOT touch `Aizen.Core.Domain.ApprovalStatus`)

`Modules/Identity/src/Aizen.Modules.Identity.Domain/Enum/ProviderOnboardingStatus.cs`

```csharp
public enum ProviderOnboardingStatus
{
    NotStarted = 0,
    InProgress = 1,
    Submitted = 2,
    NeedsRevision = 3,
    Completed = 4
}
```

`ApprovalStatus` keeps its meaning (the admin's decision). This enum tracks the provider's submission progress.

### A2. Entity + configuration + migration

`ProviderOnboardingEntity : AizenEntityWithAudit` (table `provider_onboarding`), one row per Organizer profile:

- `long ProfileId` (unique index), `long UserId`
- `ProviderOnboardingStatus Status` (default `NotStarted`)
- `int SchemaVersion` (default 1)
- `string StepStatusesJson` (jsonb, default `{}`), `string DraftJson` (jsonb, default `{}`)
- `string? RevisionStepsJson` (jsonb), `string? RevisionNote`
- `DateTime? LastSavedAtUtc`, `DateTime? SubmittedAtUtc`, `DateTime? ReviewedAtUtc`

Use `column type: jsonb` in the EF configuration. Expose DDD methods on the entity — do not let handlers mutate
state directly:

- `static Create(profileId, userId)` → `NotStarted`
- `SaveStep(step, stepStatus, draftJson, nowUtc)` → sets `InProgress` when currently `NotStarted`; updates the step
  map + draft; sets `LastSavedAtUtc`. **Throws if `Status == Submitted`** (no edits while under review).
- `Submit(nowUtc)` → guards required steps completed; sets `Submitted` + `SubmittedAtUtc`. Idempotent when already
  `Submitted`.
- `RequestRevision(steps, note, nowUtc)` → `NeedsRevision` + flips those steps to `NeedsRevision` + stores the note.
- `MarkCompleted(nowUtc)` → `Completed` (called on approval).

Migration name: `AddProviderOnboarding`. **It must back-fill**: insert a `NotStarted` row for every existing
`UserProfiles` row with `RoleContext = Organizer` that has no onboarding row, otherwise existing providers cannot be
classified by `/me/status`.

Steps and required-step rules must mirror the frontend exactly:
- steps: `BusinessIdentity, ServiceCapabilities, OperatingRegion, ComplianceVerification, CargoDryInterest, ReviewSubmit`
- required: `BusinessIdentity, ServiceCapabilities, OperatingRegion, ComplianceVerification` (**CargoDryInterest is
  optional and must never block submit**)
- step statuses: `NotStarted, InProgress, Completed, NeedsRevision, Blocked`

### A3. Repository + domain service

- `IProviderOnboardingRepository` (+ impl): `GetByProfileIdAsync`, `AddAsync`, `Update`.
- `IProviderOnboardingDomainService` (+ impl in `Repository/Service/Onboarding/`):
  - `GetAsync(profileId, ct)` → status, step map, draft, revision info, documents
  - `SaveStepAsync(profileId, step, stepStatus, stepDataJson, ct)` → merges the step payload into `DraftJson`
    (server-side merge — never overwrite the whole draft with a partial one), validates `SchemaVersion`
  - `SubmitAsync(profileId, ct)` → validates required steps; **mirrors the authoritative fields onto
    `UserProfileEntity`** (`CompanyName`, `City`, `Country`, `TaxpayerType`, `NationalityId`, `Bio`, owner first/last
    name, contact phone) via the existing profile domain methods; re-runs `RiskAssessmentService.Evaluate` with the
    real `documentCount`; sets `Submitted`. Leaves `ApprovalStatus = Pending`.
  - `RequestRevisionAsync(profileId, steps, note, ct)`
- Reject an unknown `SchemaVersion` with a business exception rather than mis-parsing it.

### A4. Commands / queries (Identity Application)

Namespace pattern: `Aizen.Modules.InktaviaStore.Application.Identity.{Command|Query}.Onboarding.*`

- `GetProviderOnboardingQuery` / handler → `ProviderOnboardingResponse`
- `SaveProviderOnboardingStepCommand` / handler / **validator** (step must be a known step; payload required)
- `SubmitProviderOnboardingCommand` / handler / **validator**
- `RequestProviderOnboardingRevisionCommand` / handler / validator (admin; at least one step, note required)

Hook the existing `ApproveOrganizerProfileCommandHandler`: after `profile.Approve(...)`, also mark the onboarding row
`Completed`. Best-effort — never fail an approval because of it (mirror how the Keycloak role sync is handled there).

Hook `RegisterOrganizerCommandHandler` (or `OrganizerRegistrationDomainService.RegisterOrAttachAsync`): create the
`NotStarted` onboarding row for the new Organizer profile.

### A5. Controller + DTOs + DI

`Controller/V1/Identity/ProviderOnboardingController.cs`, route `api/v1/identity/provider-onboarding`,
`[Authorize(Policy = "IdentityWrite")]` (read endpoint may use `IdentityRead`):

```
GET  /{profileId}
PUT  /{profileId}/steps/{step}
POST /{profileId}/submit
POST /{profileId}/revision
```

Add DTOs under `Aizen.Modules.Identity.Abstraction/Dto/Onboarding/ProviderOnboardingDtos.cs` (request + response),
and register the repository/domain service in `Aizen.Modules.Identity.Repository/DependencyInjection.cs`.

---

## Part B — MarineProvider BFF

### B1. Provider-facing endpoints

`Controllers/V1/Onboarding/ProviderOnboardingController.cs`, route `api/v1/provider/onboarding`, authenticated
(provider policy — same as the other `/me` endpoints):

| Method | Path | Notes |
|---|---|---|
| GET | `/` | status + steps + draft + documents |
| PUT | `/steps/{step}` | save one step |
| POST | `/submit` | validate + submit |
| POST | `/documents` | multipart → file-storage-api → `AddOrganizerVerificationDocument` |
| DELETE | `/documents/{id}` | only allowed while not `Submitted` |

**Security: the `profileId` is resolved from `IProviderContext` / `IProviderProfileResolver` — never read it from the
request body or route.** A provider must not be able to write another provider's onboarding.

Add the matching methods to `IProviderIdentityRemoteCall` and thin delegating command/query handlers, following the
exact shape of the existing `Auth/OtpLogin` + `Me` handlers (contracts under `Application/Contracts/Onboarding/`).

Document upload: post the file to **file-storage-api** first (reuse the existing remote call / options used
elsewhere), take the returned `FileId`, then call Identity's `AddOrganizerVerificationDocument`. Enforce a size and
content-type allowlist (pdf/jpg/png) in the validator.

### B2. `/me/status` becomes onboarding-aware  ← this is what unblocks the guards

In `GetProviderStatusQueryHandler`, fetch the onboarding status alongside the profile and replace the final
`else → AwaitApproval` with this precedence:

```
!EmailVerified                        → "VerifyEmail"
no profile                            → "CompleteProfileLink"
ProfileStatus == Suspended            → "Suspended"
ApprovalStatus == Rejected            → "Rejected"
Approved && Active                    → "EnterWorkspace"
onboarding == NeedsRevision           → "NeedsRevision"
onboarding == Submitted               → "AwaitApproval"      // only now is this honest
otherwise (NotStarted / InProgress)   → "CompleteOnboarding" // NEW
```

Add `OnboardingStatus` to `GetProviderStatusResponse` so the frontend does not need a second round-trip.
If the onboarding lookup fails, degrade to today's behaviour and add a `ProviderBffWarning` — never 500 `/me/status`.

---

## Part C — Frontend (`inktavia-marine-provider-web`)

1. `src/shared/api/endpoints.ts` → add `onboarding: { root, step, submit, documents }`.
2. New `src/features/onboarding/api/onboardingApi.ts` (authenticated `httpClient` + `normalizeEnvelope`):
   `get()`, `saveStep(step, data, status)`, `submit()`, `uploadDocument(file, type, issuer?)`, `deleteDocument(id)`.
3. `onboardingStatusModel.ts` → **the server becomes the source of truth**. Read status/steps/draft/documents from
   `GET /onboarding`; demote `onboardingDraftStorage` to an offline cache (hydrate instantly, then reconcile with the
   server response). Delete the "until GET /onboarding/status ships" comment.
4. `deriveStatus`: map `'CompleteOnboarding'` → `'InProgress'` **explicitly** (do not rely on the `default:` arm).
5. `mapRequiredNextStepToRoute` (`src/shared/auth/providerStatus.ts`): `CompleteOnboarding` → `paths.onboarding.welcome`.
6. `AccountStatusPage.getCta()`: add `case 'CompleteOnboarding'` → label `status:account.continue`, navigate to
   `paths.onboarding.welcome` (today every non-`VerifyEmail`/`CompleteProfileLink` step falls into the **disabled**
   `AwaitApproval` default, which is exactly what traps the provider).
7. Step pages (`BusinessIdentityPage`, `ServiceCapabilitiesPage`, `OperatingRegionPage`,
   `ComplianceVerificationPage`, `CargoDryInterestPage`): on save/continue call `saveStep(...)` with
   `Completed` when the step's zod schema validates, `InProgress` otherwise. Keep the local draft write for offline.
8. `ComplianceVerificationPage`: replace the mocked `DocMeta` upload with the real `uploadDocument` /
   `deleteDocument` calls; render the document list from the server.
9. `ReviewSubmitPage`: **remove `disableSubmit` and the "planned backend" warning banner**; wire the submit button to
   `submit()`; on success invalidate the `/me/status` query so the guards re-route (the provider should land on
   `/status/in-review`).
10. Keep i18n keys in `tr` + `en` for every new string.

---

## Part D — Tests + smoke

Unit tests (Identity):
- `SaveStep` throws when `Status == Submitted`.
- `Submit` fails when any of the 4 required steps is not `Completed`; **succeeds when only `CargoDryInterest` is
  incomplete** (it is optional).
- `Submit` is idempotent when already `Submitted`.
- `Submit` mirrors the business fields onto `UserProfileEntity`.
- `RequestRevision` flips only the named steps.
- Unknown `SchemaVersion` is rejected.
- `/me/status` precedence table: one test per branch, especially `NotStarted/InProgress → CompleteOnboarding` and
  `Submitted → AwaitApproval`.

Smoke (write the results into `docs/provider-onboarding-backend-phase1-report.md`):
1. Register a new provider → `GET /me/status` returns **`CompleteOnboarding`** (today it wrongly returns
   `AwaitApproval`).
2. In the browser: log in → land on **`/onboarding/welcome`**, not the "under review" page.
3. Fill the four required steps → each `PUT` persists; reload the page and confirm the data comes back **from the
   server** (clear `localStorage` first to prove it is not the local draft).
4. Submit → `GET /me/status` returns `AwaitApproval`; the guard routes to `/status/in-review`.
5. Approve via the admin flow → onboarding `Completed`, `EnterWorkspace`, provider reaches the dashboard.

## Constraints

- Do not modify `Aizen.Core.Domain.ApprovalStatus` or any other shared Core type.
- Do not let the BFF accept a `profileId` from the client.
- All `DateTime` values UTC.
- `CargoDryInterest` must never block submission.
- Keep `/me/status` resilient: an onboarding lookup failure degrades gracefully with a warning, it never 500s.
