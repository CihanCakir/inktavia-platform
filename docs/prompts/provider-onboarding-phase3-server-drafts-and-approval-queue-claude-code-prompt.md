# Claude Code Prompt — Phase 3: server-side onboarding drafts + an approval queue that means something

Two halves of one problem. **Do them in this order — the second is meaningless without the first.**

This supersedes `admin-approval-queue-onboarding-aware-claude-code-prompt.md` (Part B folds it in, updated).

---

## The situation

Provider document upload now works end to end (Phase 2/2h — verified in a real browser: presigned PUT 200, attach,
reload-persistent list, signed read, delete). **The rest of onboarding does not reach the server at all.**

`onboardingApi.saveStep()` exists in the frontend. `endpoints.onboarding.saveStep` exists. The BFF exposes
`PUT /provider/onboarding/steps/{step}`. Identity has `SaveProviderOnboardingStepCommand` →
`ProviderOnboardingDomainService.SaveStepAsync`. The whole chain is built.

**Nothing calls it.** Grep the SPA: `saveStep` has zero callers. Five of the six onboarding steps —
BusinessIdentity, ServiceCapabilities, OperatingRegion, ComplianceVerification (the form fields; the *documents*
do reach the server), CargoDryInterest — write **only to `onboardingDraftStorage`, i.e. localStorage**.
`ReviewSubmitPage` then calls `onboardingApi.submit()` and nothing else.

So today a provider fills in six screens, presses submit, and **an empty application is submitted for review**.
Their answers live in one browser profile on one machine; a different device, a cleared cache, or a support agent
looking at the record sees nothing. The documents are on the server; the answers are not.

And `SubmitAsync` accepts it:

```csharp
var entity = await _repo.GetByProfileIdAsync(profileId, ct) ?? throw ...;
entity.Submit(DateTime.UtcNow);   // no completeness check whatsoever
```

There is **no validation that any step is complete, that required fields exist, or that any document is
attached.** Empty in, `Submitted` out. That is what the admin queue is then asked to triage.

---

# PART A — Onboarding drafts must live on the server

## A1. Frontend: every step saves to the server

Wire each of the six step pages to `onboardingApi.saveStep(step, { stepStatus, stepData, schemaVersion })` on
"Save & Continue" (and on explicit "Save"), using the step keys the backend already expects — read them from the
Identity/BFF contract, do **not** invent new ones. Check `stepStatuses` in `GET /onboarding` for the canonical
names before writing any code.

- The save must complete before navigation. A failed save must **not** advance the wizard — show the error.
- `stepStatus` follows the existing model (`InProgress` / `Completed`), computed the way each page already
  computes it locally today. Do not change the completeness rules in this phase; just move where they are recorded.
- Keep `onboardingDraftStorage` as a **local convenience cache only** (protection against a lost tab), never as the
  source of truth. On mount, the server's `draft` wins; localStorage may prefill only when the server has nothing
  for that step.

## A2. Frontend: resume from the server

`GET /onboarding` already returns `draft`, `stepStatuses`, `lastSavedAtUtc` and `documents`. Hydrate each step's
form from the server draft (react-hook-form `defaultValues` / `reset`), not from localStorage.

Prove the point that matters: **fill in step 1 in one browser, open the SPA in a fresh profile / incognito, log
in, and the answers are there.** That single test is the acceptance criterion for Part A. If it does not pass,
nothing else in this phase is real.

The "İlerleme … itibarıyla kaydedildi" footer must reflect the **server's** `lastSavedAtUtc`, not a local
timestamp. Right now it can claim a save that never left the browser.

## A3. BFF: the onboarding handlers must resolve identity

`GetOnboardingQueryHandler` and `SubmitOnboardingCommandHandler` read `_context.ProviderProfileId ?? 0` and never
call `IProviderProfileResolver.ResolveAsync`. That is exactly the defect that made every document attach fail with
*"You do not have permission to modify this profile"*: the holder stays empty, the module call carries no asserted
user, and Identity sees `UserId = 0`.

Fix all onboarding BFF handlers (`Get`, `SaveStep`, `Submit`, and the document ones already fixed) to resolve
first and **fail closed** when `ProfileId <= 0` or `UserId <= 0`. Do not default a user id to `0`.

While you are there: these handlers swallow the module's business error in `catch (Exception)` and return a
generic message. Surface `header.errorMessage` from the Aizen envelope the way
`AttachOnboardingDocumentCommandHandler` now does — otherwise the submit rejections you add in A4 will be
invisible to the provider.

## A4. Identity: `SubmitAsync` must actually validate

Submission is the gate between "the provider is still typing" and "a human must review this". It currently
validates nothing.

Add a completeness check in the domain service (not in the controller, not in the BFF) and reject with an
`AizenBusinessException` naming **what is missing**, so the SPA can show it:

- every **required** step is `Completed` (define the required set explicitly; state your choice and why),
- the required fields inside those steps are present in the persisted draft — validate against the stored draft,
  **not** against anything the client sends at submit time,
- at least the required verification documents are attached, and **none of them is in a `Rejected` review state**,
- the onboarding is not already `Submitted` (idempotency — a double-submit is not an error, but it must not move
  `SubmittedAtUtc`).

Return the list of missing items in the response so `ReviewSubmitPage` can render it instead of a generic failure.

`MirrorDraftToProfile` is currently wrapped in `try { … } catch { LogWarning }`. If mirroring the draft onto
`UserProfileEntity` fails, the submission still succeeds and the admin sees a blank profile — a silent data loss
on the exact path that feeds Part B. Make the mirror failure **fail the submit** (it is inside the same
`SaveChanges` scope; there is no reason for it to be best-effort).

## A5. Frontend: submit must respect the answer

`ReviewSubmitPage` must render the server's rejection (the missing-items list) rather than a generic error, and
must not navigate to the "under review" screen unless the server actually accepted the submission. Remember the
BFF envelope trap: a rejection can arrive as **HTTP 200 with `success: false` inside a successful envelope** —
check the body, not just the envelope. That trap is what hid the broken document attach for an entire debugging
session.

---

# PART B — The admin approval queue

Only after Part A does this queue have anything real to show.

`Bff/src/AdminPanel/.../AdminProfileApprovals/Query/GetProfileApprovalQueueBffQueryHandler.cs` is wrong in three
ways:

1. **It lists empty applications.** It filters on `ApprovalStatus = Pending`, which registration sets immediately.
   Every provider who signed up and filled in nothing sits in the review queue. Admins are triaging blank forms.
2. **`SubmittedAt` is a lie:** `SubmittedAt = item.CreateDate?.ToString("O")` — the **registration** date, not the
   submission date. The queue is *sorted* by it, so the ordering is meaningless.
3. **`ReviewedAt = null` and `RiskLevel = null` are hardcoded**, though `UserProfileEntity.ReviewedAt` and the
   `RiskSignals` collection already hold the data.

## B1. Identity — expose the onboarding facts on the admin profile query

Extend the query behind `GetAdminOrganizerProfilesByStatus` so each item carries:

- `OnboardingStatus` (`NotStarted | InProgress | Submitted | NeedsRevision | Completed`)
- `SubmittedAtUtc` — from the onboarding record, **not** `CreateDate`
- `ReviewedAt` — from `UserProfileEntity`
- `RiskLevel` — derived from the profile's `RiskSignals` (highest severity; if `RiskSignalEntity` has no severity
  field, derive from signal count **and say so in the report**)
- `DocumentCount`

Add a **server-side filter by onboarding status** alongside the approval-status filter, so the BFF can ask for
"submitted and awaiting a decision" in one query instead of fetching everything and filtering in memory.

Venues have no onboarding record: return `OnboardingStatus = null` for them and **do not exclude them** — the
current venue behaviour must not regress.

## B2. Admin BFF — "pending" must mean "waiting for a human decision"

- **`status = pending` ⇒ `ApprovalStatus == Pending` AND `OnboardingStatus == Submitted`.** A provider who has not
  submitted is not awaiting review.
- Add an `incomplete` bucket for `OnboardingStatus ∈ { NotStarted, InProgress }`. **Do not hide these providers** —
  admins want the funnel (who signed up and stalled). They just must not pollute the review queue.
- Add `needs_revision` for `OnboardingStatus == NeedsRevision` (sent back, waiting on the provider).
- Map `SubmittedAt` from `SubmittedAtUtc`, `ReviewedAt` from the profile, `RiskLevel` from the risk signals.
  Delete the hardcoded `null`s.
- Sort by real `SubmittedAt` descending; rows without one sort last (they only appear in `incomplete`).
- `ComputeSummary` follows the same rules — the pending count is currently inflated by every blank registration.

Keep paging, the parallel organizer/venue fetch, and the date-range filters working.

## B3. Admin web — surface it

The queue UI gets the new buckets (`pending` / `incomplete` / `needs_revision`), shows real submission dates, the
document count, and the risk level. An admin opening an application must be able to see the **submitted draft**
(the answers, from the server) and the attached documents — the documents open through a short-lived signed URL
minted per click, exactly like the provider side. Never store or pre-fetch those URLs.

---

## Acceptance — prove it, in a browser

Write `docs/provider-onboarding-phase3-report.md`. Paste real HTTP statuses and bodies.

1. **Cross-device resume (the headline test).** Fill steps 1–3 in one browser. Open a fresh incognito profile, log
   in as the same provider, and the answers are all there. *Today this fails completely — nothing is on the server.*
2. `PUT /provider/onboarding/steps/{step}` returns 200 and `GET /onboarding` reflects the new `draft` and
   `stepStatuses` on the very next request.
3. A failing save does **not** advance the wizard.
4. **Empty submit is rejected**, and the SPA shows *which* steps/fields/documents are missing — not a generic error.
5. A submit with a `Rejected` document is rejected.
6. Double-submit is idempotent and does not move `SubmittedAtUtc`.
7. A complete submit flips the onboarding to `Submitted`, mirrors the draft onto the profile, and the provider
   lands on the "under review" screen.
8. **The admin queue shows that provider — and only real submissions.** A provider who registered and typed
   nothing appears in `incomplete`, never in `pending`. `SubmittedAt` is the submission date, `ReviewedAt` and
   `RiskLevel` are real.
9. The admin opens the application, reads the submitted answers, and opens a document through a signed URL.
10. **Regression:** the document upload chain still works in a real browser (upload → PUT 200 → attach → listed →
    reload-persistent → signed read → delete).

`curl` will not catch the class of bug that keeps appearing here (it sends no `Origin`, no duplicate headers, and
never re-runs the SPA's own code path). If you cannot drive a browser, **say so plainly in the report** rather
than substituting curl and calling it verified.

## Constraints

- The server is the source of truth for the draft. localStorage is a cache, never the record.
- Fail closed. Resolve identity before any module call; a missing `UserId` is a rejection, not `0`.
- Never treat a 200 envelope as success without checking `success` in the body.
- Never fabricate an identifier and never default a user id to `0`.
- Signed URLs are minted per click, used immediately, never stored — on the admin side too.
- Do not weaken the submit validation to make a test pass. If a required-field rule is wrong, change the rule
  deliberately and say so.
- If something cannot be finished, leave the TODO **and say so in the summary** — do not describe an unfinished
  item as done.
