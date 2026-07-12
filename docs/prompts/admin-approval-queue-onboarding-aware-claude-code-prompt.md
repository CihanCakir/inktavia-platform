# Claude Code Prompt — Make the admin approval queue onboarding-aware

Small, self-contained fix. Independent of Phase 2b; can run in parallel.

## The problem

Provider onboarding (Phase 1) introduced a real submission step: `ProviderOnboardingStatus`
(`NotStarted → InProgress → Submitted → NeedsRevision → Completed`) with a real `SubmittedAtUtc`. `ApprovalStatus`
(`Pending / Approved / Rejected`) now means **the admin's decision**, and `Pending` means *"submitted, awaiting
review"* — it no longer means *"just registered"*.

The admin approval queue was never updated and is now wrong in three ways
(`Bff/src/AdminPanel/.../AdminProfileApprovals/Query/GetProfileApprovalQueueBffQueryHandler.cs`):

1. **It lists empty applications.** It filters on `ApprovalStatus = Pending` via
   `GetAdminOrganizerProfilesByStatus`. Because registration sets `Pending` immediately, every provider who signed up
   and filled in *nothing* sits in the admin's review queue. Admins are triaging blank forms.
2. **`SubmittedAt` is a lie:** `SubmittedAt = item.CreateDate?.ToString("O")` — it is the **registration** date, not
   the submission date. The queue is then *sorted* by it, so ordering is meaningless.
3. **`ReviewedAt = null` and `RiskLevel = null` are hardcoded**, even though `UserProfileEntity.ReviewedAt` and the
   `RiskSignals` collection already hold this data.

## What to build

### 1. Identity — expose onboarding facts on the admin profile query

Extend the admin organizer/venue profile query (the one behind `GetAdminOrganizerProfilesByStatus`) so each item
carries:

- `OnboardingStatus` (`NotStarted | InProgress | Submitted | NeedsRevision | Completed`)
- `SubmittedAtUtc` (from the onboarding record — **not** `CreateDate`)
- `ReviewedAt` (from `UserProfileEntity.ReviewedAt`)
- `RiskLevel` derived from the profile's `RiskSignals` (e.g. `None | Low | Medium | High` — use the highest signal
  severity; if the existing `RiskSignalEntity` has no severity, derive from signal count and say so in the report)
- `DocumentCount`

Add a **server-side filter by onboarding status** alongside the existing approval-status filter, so the BFF can ask
for "submitted and awaiting a decision" in one query rather than fetching everything and filtering in memory.

Venues have no onboarding record: return `OnboardingStatus = null` for them and **do not exclude them** from the
queue — the current behaviour for venues must not regress.

### 2. Admin BFF — the queue means "waiting for a human decision"

In `GetProfileApprovalQueueBffQueryHandler`:

- **`status = pending` must mean `ApprovalStatus == Pending` AND `OnboardingStatus == Submitted`.** A provider who has
  not submitted is not awaiting review and must not appear.
- Add a distinct bucket/filter value — `incomplete` — for `OnboardingStatus ∈ { NotStarted, InProgress }`. Do not
  simply hide these providers: admins still want to see who signed up and stalled (funnel/outreach). They just must
  not be mixed into the review queue.
- Add `needs_revision` for `OnboardingStatus == NeedsRevision` (sent back, waiting on the provider).
- Map `SubmittedAt` from the onboarding `SubmittedAtUtc` (null for a not-yet-submitted profile), `ReviewedAt` from the
  profile, and `RiskLevel` from the risk signals. Delete the hardcoded `null`s.
- Sort by real `SubmittedAt` descending; items with no submission date sort last (they only appear in `incomplete`).
- `ComputeSummary` counts must follow the same rules — the pending count is currently inflated by every blank
  registration.

Keep the existing paging, the parallel organizer/venue fetch and the date-range filters working.

### 3. Admin frontend (`inktavia-marine-admin-web`, if the queue UI lives there)

- Add the `incomplete` / `needs_revision` tabs or filter chips next to the existing status filter.
- Show `OnboardingStatus` and the real submission date on each row; show `RiskLevel` and `DocumentCount` if the
  column exists.
- i18n in tr + en.

## Tests

- Register a provider, submit nothing → **does not appear** in `status=pending`; **does appear** in
  `status=incomplete`.
- Submit onboarding → appears in `status=pending`, with `SubmittedAt` equal to the onboarding `SubmittedAtUtc`
  (not the registration date).
- Admin requests revision → moves to `status=needs_revision`, leaves `pending`.
- Approve → leaves the queue; `ReviewedAt` is populated.
- Venue profiles still appear and are unaffected.
- The `pending` summary count matches the number of rows actually returned for `pending`.

## Report

Write `docs/admin-approval-queue-onboarding-aware-report.md`. State explicitly, with before/after counts, how many
blank registrations the pending queue was previously showing — that number is the size of the problem this fixes.
