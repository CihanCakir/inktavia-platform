# Claude Code Prompt — Phase 3b: prove Phase 3 in a browser, then close what it left open

Phase 3 (server-side onboarding drafts + admin approval queue) was implemented and reported complete. A code
review found that **the headline behaviour did not actually work**, plus three smaller gaps. Two frontend fixes
have already been applied by hand (see "Already fixed" below). Nothing has been verified in a running system.

Your job: **bring the stack up, prove it in a real browser, then close the remaining gaps.**

---

## 0. Bring the stack up first

```bash
docker compose up -d
docker compose up -d --build file-storage-api    # has un-built changes, see below
docker ps --format '{{.Names}}\t{{.Status}}' | sort
```

`file-storage-api` contains two changes that have **never been built or run**:

- `FileStorageService.DeleteFileAsync` — now releases active owner references, deletes the object from the bucket,
  then soft-deletes the row. Previously it only soft-deleted, so the owner reference stayed **active** and the
  orphan-cleanup sweep (which skips both claimed and soft-deleted files) could never purge the object. The bytes
  leaked forever.
- `Modules/FileStorage/.../Jobs/OrphanFileCleanupJob.cs` — a new `AizenRecurringJob` that publishes
  `OrphanFileCleanupRequestedMessage` hourly. The cleanup consumer existed but **nothing published to it**, so it
  never ran at all.

Confirm both compile and that the job registers (it is picked up by assembly scanning because FileStorage has
`AppType.Scheduler` in `TypeInclude`).

Wait for Keycloak — it starts slowly, and when it is not up the BFF cannot resolve `keycloak:8080`. That failure
is currently invisible; see §4.

---

## 1. Prove Phase 3 in a real browser — this is the point of this phase

Log in as a provider (use the one-time-code / OTP login; do not use a password).

**The headline test:**

1. Open `/onboarding/business-identity`, fill in the form, press "Kaydet ve Devam Et".
2. Confirm `PUT /provider/onboarding/steps/BusinessIdentity` returns 200 **and** that
   `GET /provider/onboarding` now contains that step in `draft` and `stepStatuses`.
3. **Clear `localStorage` entirely** (`localStorage.clear()`) and hard-reload the page.
4. **The answers must still be in the fields.** If they are not, the server draft is not reaching the form and
   Phase 3 is not real — fix it and say so.

Then: a failing save must **not** advance the wizard; an empty submit must be rejected with the *specific* missing
items; a complete submit must flip the onboarding to `Submitted` and land the provider on the review screen; and
the provider must then appear in the admin queue's `pending` bucket (a provider who typed nothing must appear only
in `incomplete`).

**Regression:** the document chain must still work — upload a PDF → `PUT` 200 → `complete` → attach → listed →
survives reload → opens via a signed URL → delete. After the delete, **check MinIO**: the object must be gone from
the bucket (this is what the un-built `DeleteFileAsync` change is for). Then upload a file and abandon it, run the
cleanup sweep, and show it reaped.

`curl` cannot reproduce the bugs that keep appearing in this chain — it sends no `Origin`, no duplicate headers,
and never runs the SPA's own code path. If you cannot drive a browser, **say so plainly in the report** instead of
substituting curl and calling it verified.

### Already fixed by hand — verify, do not re-do, do not regress

- **`useHydrateFormFromServerDraft`** (`features/onboarding/hooks/useOnboardingDraft.ts`). Every step page passed
  the server draft into react-hook-form's `defaultValues` — which is read **only on the first render**. The draft
  arrives asynchronously, so on a cold load (a new device, a cleared cache — the exact case server drafts exist
  for) the query was still pending, the form initialised empty, and nothing ever wrote the answers into it. The
  code looked right and silently did nothing. The hook now `reset()`s once when the draft first lands, with
  `keepDirtyValues` so a later refetch cannot clobber what the user is typing.
- **`success: false` inside a 200 envelope.** All five step pages checked only `result.ok` on `saveStep`, so a
  rejected save looked successful and the wizard advanced. They now check `result.data.success` too. This trap
  already hid the broken document attach for a whole debugging session — **check the body, never just the
  envelope.**

---

## 2. Submit must reject documents that were turned down

`ProviderOnboardingDomainService.SubmitAsync` validates that required steps have data and that at least one
verification document is attached. It does **not** check the documents' review state, so a provider whose document
was **`Rejected`** by an admin can resubmit unchanged and go straight back into the review queue.

Reject the submit when any attached document is in a `Rejected` review state, and name those documents in the
missing-items list so the SPA can tell the provider **which** document to replace.

## 3. The admin queue still reports a registration date as a submission date

`GetProfileApprovalQueueBffQueryHandler`:

```csharp
SubmittedAt = item.SubmittedAtUtc?.ToString("O") ?? item.CreateDate?.ToString("O"),
```

For a provider who has not submitted, this reports the **registration** date as `SubmittedAt` — the original bug,
reintroduced as a fallback. The queue is sorted by this field, so the `incomplete` bucket is ordered by a date that
means something else entirely.

`SubmittedAt` must be `null` when there is no submission. Rows without one sort last. Both list projections
(organizer and venue) have the same fallback — fix both.

## 4. A Keycloak outage is reported to the user as "code sent"

`RequestOtpLoginCommandHandler` catches the exception from the Identity call and returns the enumeration-safe
response:

```json
{ "accepted": true, "loginRequestId": "", "message": "If an account exists, a verification code has been sent." }
```

When Keycloak is down (`Name or service not known (keycloak:8080)`), the user is told a code was sent and waits
for a code that will never arrive. During this phase's own debugging that message was read as "wrong email
address" and sent us chasing the wrong thing.

Separate the two cases:

- **Unknown account** — a business decision. Keep the generic "if an account exists…" response. This is deliberate
  anti-enumeration behaviour and must not change.
- **The remote call failed** (transport error, 5xx, timeout) — a technical fault. Return a technical error the SPA
  can surface as "service temporarily unavailable, please try again", and log it at error level. Do **not** dress a
  broken dependency up as success.

Apply the same rule to the OTP *resend* path and to password recovery if it shares the pattern — check, and say
what you found.

---

## Report

Write `docs/provider-onboarding-phase3b-report.md`. Paste real HTTP statuses, response bodies, and the MinIO bucket
listing before and after the delete.

**Report failures plainly.** Several reports in this module have described work as complete that the code
contradicted — a consumer with no publisher, a delete that could never purge, a form hydration that never ran. An
honest "not done" is worth more than a confident "done" that the next browser run disproves.

## Constraints

- The server is the source of truth for the draft; localStorage is a cache, never the record.
- Never treat a 200 envelope as success without checking `success` in the body.
- Fail closed: resolve identity before any module call; a missing `UserId` is a rejection, not `0`.
- Never fabricate an identifier and never default a user id to `0`.
- Signed URLs are minted per click, used immediately, never stored — admin side included.
- Do not weaken a validation rule to make a test pass. If a rule is wrong, change it deliberately and say so.
- Parameterless entity constructors are `protected`, never `private` (a `private` one makes EF's lazy-loading
  proxy throw the moment the first row exists).
- If something cannot be finished, leave the TODO **and say so in the summary**.

## Not in this phase

FileStorage still has open gaps — phantom claims, the size lie being logged instead of rejected, upload-session
rate limiting, and a complete absence of tests. Those are specified in
`docs/prompts/filestorage-phase2i-close-the-gaps-claude-code-prompt.md`. Do not start them here.
