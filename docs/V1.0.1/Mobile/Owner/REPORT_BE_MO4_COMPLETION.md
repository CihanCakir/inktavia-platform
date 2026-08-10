# REPORT — BE_MO4 owner completion review + auto-approve countdown (mobile)

> **Status:** implemented, builds clean (BE 0 errors, FE `tsc --noEmit` 0 errors), 152/152 SR unit tests green
> (7 new). **NOT committed.** Additive throughout — the approve/reject commands, the SR→Completed transition, the
> `CompletionApproved` event, and the **decoupled escrow release** are reused unchanged. Identity from token;
> cost-free; envelope per MO invariants.
>
> **Repos/layers:** `addesso-project` (ServiceRequest module + `Marine.Participant.Mobile` BFF) + `inktavia-marine-mobile` (Expo RN).

---

## What the owner gets

On the SR/job detail, when the provider has submitted a completion, a **Completion Review** section appears:
- the provider's **notes** + a single **evidence image** (via a freshly-minted read URL, with a graceful
  "image unavailable" placeholder — shares the attachment read-url 400 fix),
- the **submitted-at** timestamp,
- while it's still pending: the **auto-approve countdown** — *"Onaylamazsan {tarih} tarihinde otomatik onaylanacak
  ({n} gün)"* / *"If you don't approve, it will be auto-approved on {date} ({n} days)"* — derived from `AutoApproveAt`,
- **Approve** (optional 1–5 star rating + note) → SR `Completed` → the existing decoupled escrow release runs, or
- **Reject** with an N-E `CompletionRejectReason` picker + note (SR → `InProgress`; no money moves).

Once acted on, the section shows the outcome (the rating stars / the surfaced reject reason) instead of the CTAs.

---

## BE — ServiceRequest module (additive only)

1. **`AutoApproveAt` exposed on the read DTO** (the N3 countdown source). Added `AutoApproveAt`,
   `AutoApproveReminderSentAt`, and `RejectReasonCode` to `ServiceRequestCompletionDto`
   (`…/Abstraction/Dto/ServiceRequestCompletionDto.cs`) and mapped them in the single `ToDto(this
   ServiceRequestCompletionEntity)` mapper (`…/Repository/Mapping/ServiceRequestMappingExtensions.cs`). The entity
   already exposed the getters — this is a pure read-field addition, so it flows through **every** place the DTO is
   produced (SR detail + the approve handler's realtime publish).
2. **Approve accepts an optional rating.** Added `int? ClientRating` to `ApproveServiceRequestCompletionRequest`
   and wired it into `ApproveServiceRequestCompletionCommandHandler` — when present it calls the existing
   `completion.RateByClient(rating)` (1–5, validated) right after `ApproveByOwner`, **before** the SR→Completed
   transition, the `CompletionApproved` publish, and the JOB_COMPLETED system message — all of which are untouched.
   The N3 auto-approval job passes no rating, so its path is byte-for-byte unchanged.

There is **no new module read endpoint** — the completion already rides `ServiceRequestDetailDto.Completion`
(eager-loaded in `ServiceRequestRepository.GetDetail`, `Include(x => x.Completion)`), which the BFF already fetches
and owner-gates. Nothing on the reject command, the escrow-release consumers
(`ServiceRequestCompletedConsumer` / `PaymentAutoReleaseEligibilityJob`), or the auto-approval job was modified.

## BE — Mobile BFF (`Marine.Participant.Mobile`)

3. **Remote calls** (`IServiceRequestRemoteCall`): `ApproveOwnerCompletion` (PATCH `…/completion/approve`) and
   `RejectOwnerCompletion` (PATCH `…/completion/reject`), mirroring the module verbs. The read reuses the existing
   `GetDetail`.
4. **Cost-free contracts** (`Contracts/ServiceRequest/MobileCompletionDtos.cs`):
   `MobileServiceRequestCompletionDto` (status / notes / `EvidenceFileId` + resolved `EvidenceUrl` / `SubmittedAt` /
   `AutoApproveAt` / review fields / `ClientRating` / `IsPendingReview`) — **drops** `ProviderUserId` and
   `ReviewedByUserId`, carries no cost/commission/net field. Plus `ApproveMobileCompletionRequest {Rating, Note}` and
   `RejectMobileCompletionRequest {ReasonCode, Note}`.
5. **Mapper** (`MobileServiceRequestMapper`): `MapCompletion` + `MapCompletionWithEvidenceUrlAsync` (resolves the
   evidence `fileId` → presigned read URL via `IFileStorageRemoteCall.CreateReadUrl`, best-effort — a failure leaves
   the URL null → the FE placeholder), and `ParseCompletionRejectReason`.
6. **CQRS + controller** (`ServiceRequestsController`, MO4 banner):
   - `GET  …/completion` → `GetMobileServiceRequestCompletionQuery` — resolves the participant, **`EnsureOwnedAsync`**
     (owner-gate; a foreign/unknown SR is a clean not-found), then maps `detail.Completion` (+ evidence URL). Returns
     `null` when no completion exists yet (the FE hides the section).
   - `POST …/completion/approve` → `ApproveMobileServiceRequestCompletionCommand` — validates rating 1–5,
     `EnsureOwnedAsync`, proxies the module approve (rating + note), re-reads → returns the ApprovedByOwner completion.
   - `POST …/completion/reject` → `RejectMobileServiceRequestCompletionCommand` — `EnsureOwnedAsync`, proxies the
     module reject (N-E reason + note), re-reads → returns the RejectedByOwner completion.

   Identity is server-side only: the resolver + identity holder feed the BffAssertion (`X-Aizen-User-Id`) so the
   module reads `UserInfo.UserId`; the owner id is never taken from the body. The module approve/reject do **not**
   owner-check, so the BFF gates ownership before every proxy (same pattern as MO3 accept/reject-offer).

## FE — `inktavia-marine-mobile`

- **API/hooks/endpoints/keys**: `ENDPOINTS.SERVICES.{COMPLETION,COMPLETION_APPROVE,COMPLETION_REJECT}`,
  `queryKeys.services.completion`, `serviceRequestsApi` types + `fetch/approve/rejectServiceRequestCompletion`,
  and hooks `useServiceRequestCompletion` / `useApproveCompletion` / `useRejectCompletion` (mutations invalidate the
  completion, the SR detail, and the payment status so the released/paid state refreshes).
- **`CompletionReviewSection`** (`features/services/components/CompletionReviewSection.tsx`) — self-contained: status
  pill, provider notes, `EvidenceView` (image with `onError` → placeholder), the auto-approve countdown
  (`daysUntil` + `formatDate` in `utils/display.ts`), an **Approve** sheet (self-contained `StarRating` +
  optional note) and a **Reject** sheet (N-E `COMPLETION_REJECT_REASON_CODES` picker + note; mirrors the offer-reject
  sheet). Renders nothing until a completion exists. Dropped into `ServiceRequestDetailScreen` in one line.
- **i18n**: full `services.completion.*` block in **tr + en** (34-key parity verified) — title/notes/evidence/
  countdown/status/approve/reject + reason labels. Dates via `toLocaleDateString`.
- **Mock parity**: `serviceRequests.handlers.ts` gained a `MO4_COMPLETIONS` map (SR 101 seeded pending, ~6-day
  deadline) + GET completion / POST approve / POST reject handlers that mutate the completion **and** the SR detail
  status/timeline (approve → Completed, reject → InProgress), so the whole flow is exercisable with
  `EXPO_PUBLIC_MOCK_MODE=true`.

---

## Tests (`Aizen.Modules.ServiceRequest.Application.UnitTests` — `CompletionMo4ReadMappingTests`, 7 green)

| # | Requirement | Coverage |
|---|---|---|
| 4 | countdown reflects `AutoApproveAt` | `ToDto_exposes_the_auto_approve_deadline_for_the_countdown` (+ reminder), and `…_is_null_when_never_scheduled` (pre-N3-C rows) |
| 2 | approve (+rating) → SR Completed, release path unchanged | `ToDto_exposes_the_client_rating_captured_at_approval` (rating rides the read DTO; approve reuses the same command → the decoupled release is untouched by construction) |
| 3 | reject with an N-E reason transitions + surfaces the reason | `ToDto_surfaces_the_reject_reason` |
| 6 | owner action before the deadline cancels the auto-approval (N3 guard) | `Owner_action_before_deadline_cancels_the_pending_auto_approval` (approve/reject theory) |
| 5 | cost-free payload | `ToDto_carries_no_cost_or_commission_fields` (reflection: no Cost/Commission/Net/Margin/Funding) |
| 1 | owner reads own SR completion (non-owner rejected) w/ evidence URL | enforced by `EnsureOwnedAsync` in every MO4 handler (same gate proven across MO1–MO3); the evidence URL is minted in `MapCompletionWithEvidenceUrlAsync` |

Full suite: **152/152 green** (145 prior + 7 new). Builds: SR Repository/Application/host + Mobile BFF host — **0 errors**. FE: `npx tsc --noEmit` — **0 errors** (no lint config exists in the repo; tsc is the only gate).

**Evidence view** shares the attachment read-url 400 fix: the URL is minted best-effort and the FE degrades to a
placeholder on any failure (null URL or a broken image), so an objectless/expired evidence file never breaks the screen.

---

## Next — MO5 (disputes)

Owner dispute case: view + open + lifecycle. The SR detail already carries `Dispute` (cost-free), the S13 dispute
aggregate + resolution→P10 refund path exists, and the BFF `EnsureOwnedAsync` gate + cost-free mapper pattern from
MO1–MO4 carry straight over.
