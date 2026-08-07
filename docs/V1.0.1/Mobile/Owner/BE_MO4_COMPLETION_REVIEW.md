# BE_MO4 — owner completion review + auto-approve countdown (mobile)

> **Repos:** `addesso-project` (ServiceRequest module + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile`. Owner
> Economics track **MO4**: the owner reviews the provider's completion (notes + evidence), sees the **auto-approve
> countdown** (N3), and **approves** (releasing the escrow to the provider) or **rejects** with a structured reason.
> Additive; identity from token; cost-free. Reuse the existing approve/reject + escrow-release path — **do not change the
> payment/release logic.** **Do not commit.**

## Baseline (investigated)
- Owner completion endpoints exist at `api/v1/service-requests/{srId}/completion`: provider **submit**
  (`POST /{assignmentId}`), owner **Approve** (`ApproveServiceRequestCompletionRequest`), owner **Reject**
  (`RejectServiceRequestCompletionRequest` — N-E `CompletionRejectReason` + note). Approve → SR `Completed` + publishes
  `CompletionApproved`; **escrow release to the provider is decoupled** (via `ServiceRequestCompletedConsumer` /
  `PaymentAutoReleaseEligibilityJob`) — MO4 reuses this, changes none of it. The completion entity supports a client rating
  (`RateByClient`).
- `ServiceRequestCompletionDto` carries: Status, CompletionNotes, **EvidenceFileId** (single evidence file), SubmittedAt,
  ReviewedAt/By, ReviewNotes, ClientRating. **Gap:** `AutoApproveAt` (added to the entity in N3) is **not on this DTO** —
  the owner needs it for the countdown.
- **Dependency:** evidence viewing uses the attachment read-url path (`GetAttachmentAccessCheck` — `EvidenceFileId` is one
  of the accepted attachment kinds). That path has the **read-url 400 fix** in flight (Messages attachment fix) — the owner
  evidence view benefits from it; note the dependency.

## BE — owner completion surface
1. **Expose `AutoApproveAt`** (+ optionally `AutoApproveReminderSentAt`) on the completion read DTO (or a mobile owner
   completion DTO) so the FE can show the countdown. Additive field only.
2. **Completion detail (read):** an owner-scoped completion read for the SR — status, notes, **evidence** (EvidenceFileId +
   an access-url the owner can open, via the existing read-url path), SubmittedAt, **AutoApproveAt**, existing review fields.
   Owner-scoped (caller owns the SR).
3. **Approve:** reuse `ApproveServiceRequestCompletion` (owner) — optional `ClientRating`; → SR `Completed` → the existing
   decoupled escrow release to the provider. **Do not touch** the release/payment logic.
4. **Reject:** reuse `RejectServiceRequestCompletion` — N-E `CompletionRejectReason` + optional note.
5. **Mobile BFF:** passthroughs — completion detail (with AutoApproveAt + evidence access-url), approve (+ rating), reject
   (reason + note); owner identity server-side (BffAssertion); typed, envelope-correct; cost-free.

## FE — completion review (`inktavia-marine-mobile`)
- On the SR/job detail, a **completion review** section/screen when a completion is submitted & pending owner review:
  provider's **notes** + **evidence** (image via the read-url; graceful placeholder if it fails — shares the attachment-400
  fix), **SubmittedAt**, and the **auto-approve countdown**: "Onaylamazsan {tarih} tarihinde otomatik onaylanacak ({n}
  gün)" / EN, from `AutoApproveAt`.
- **Approve** → confirm (optional star **rating**) → SR `Completed`; show the released/paid state (cost-free — the frozen
  economics, what was paid). **Reject** → N-E `CompletionRejectReason` picker + optional note.
- Loading/empty/error; tr+en; mock parity. Cost-free throughout (no commission/cost).

## Don't-break / QA
- Additive: `AutoApproveAt` on the read DTO + a completion read + approve/reject passthroughs + FE. The approve/reject
  commands, SR→Completed transition, `CompletionApproved` event, and the **decoupled escrow release** are unchanged (MO4
  reuses them). Identity from token; cost-free; envelope per MO invariants. BE builds clean; FE tsc+lint; tr+en.
- Tests: (1) owner reads their own SR's completion (non-owner rejected) with AutoApproveAt + evidence access-url; (2) approve
  (+rating) → SR Completed, the existing release path fires (unchanged); (3) reject with an N-E reason transitions + surfaces
  the reason; (4) the countdown reflects AutoApproveAt; (5) cost-free payload; (6) an owner action before the deadline
  cancels the auto-approval (N3 guard — unchanged, just confirm).

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO4_COMPLETION.md`: the AutoApproveAt exposure, the owner completion read (+ evidence
access-url), approve(+rating)/reject(reason) passthroughs (reusing the escrow-release path), the FE review screen +
countdown, and the tests. Note the evidence view shares the attachment read-url 400 fix. Then MO5 (disputes — owner case
view + open, lifecycle).
