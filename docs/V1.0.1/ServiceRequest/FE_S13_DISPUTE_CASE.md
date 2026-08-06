# FE_S13 — admin dispute case view + resolution outcome picker + provider auto-approve countdown

> **Repos:** `inktavia-marine-admin-web` (main) + `inktavia-marine-provider-web` (light). BFF passthrough for the S13
> aggregate + resolution is **already built** (`GetDisputeCaseBff`, `ResolveDisputeBff` in the AdminPanel BFF). This
> surfaces **S13** (dispute case) + the **N3** completion auto-approval countdown. All **additive**; server owns the case
> data + refund outcome — the FE only displays + submits a chosen outcome. tr + en. **Do not commit** until the user says.

## Headline constraint — cost confidentiality (carries from S5/S13)
The dispute case aggregate is **cost-free** by construction (Payment keeps supplier cost / dealer margin internal). The FE
must render only what the aggregate returns; **never** display or request a cost/margin field. If one appears in the
payload, it's a backend bug — surface nothing.

## Current state (investigated)
- Admin-web already has `pages/app/DisputesPage.tsx` (list), `pages/app/CompletionDisputeReviewPage.tsx` (a review page),
  and `features/service-requests/hooks/useServiceRequestDisputeMutation.ts`.
- AdminPanel BFF **already exposes** `GetDisputeCaseBff` (the S13 aggregate) + `ResolveDisputeBff` (resolution with
  outcome). Wire the FE to these — no new BFF work unless a field is missing.
- Provider-web job/completion surface: `features/jobs/pages/JobDetailPage.tsx` + `components/JobsActionPanel.tsx` +
  `api/providerJobsApi.ts`.

## PART A — Admin: dispute case page (main deliverable)
- Add/upgrade a **Dispute Case** page (extend `CompletionDisputeReviewPage` or add `DisputeCasePage`) reached from the
  dispute list row → consumes `GetDisputeCaseBff(disputeId)`. Render the consolidated case file in clear sections:
  - **Dispute:** reason, opener actor, status, description, resolution notes, status timeline.
  - **Service request:** SR summary + **status-history timeline** + the **N-E structured cancel/reject reason** that led
    here.
  - **Offer economics (cost-free):** line breakdown, provider net, commission, platform fee, customer total (exactly the
    fields the aggregate returns — no cost/margin).
  - **Evidence trail:** work-logs, completion evidence (files/photos), and the **conversation/messages**.
  - **Payment / refund state:** escrow/settlement state, any existing refund allocation, chargeback record.
- **Resolution action** (via `ResolveDisputeBff`): an **outcome picker** —
  `FavorPayerFullRefund` / `FavorPayerPartialRefund` (+ amount) / `FavorProviderRelease` / `Split` (+ amount) — plus the
  resolution notes. Client-validate the amount (≤ the refundable shown in the payment state); show the outcome's plain-
  language effect ("Müşteriye ₺X iade edilecek" / "Kalan sağlayıcıya bırakılacak"). On success, refresh the case (status →
  Resolved, refund state updated). Surface any server business-error (idempotency/amount) verbatim.
- Keep the existing dispute list + status-change actions working; this adds the rich case view + the outcome-driven
  resolve.

## PART B — Provider: completion auto-approve countdown (light)
- The provider submitted the completion and is **awaiting owner approval**; N3 sets `AutoApproveAt`. On the job/completion
  detail (`JobDetailPage` / `JobsActionPanel`), show a **countdown**: "Sahip onayı bekleniyor — {date} tarihinde otomatik
  onaylanacak ({n} gün)" / "Awaiting owner approval — auto-approves on {date} ({n} days)". Read `autoApproveAt` from the
  provider job/completion DTO — **add the field to the provider BFF job-detail passthrough (additive)** if it isn't carried
  yet.
- Purely informational — no action, no computation of the window (server owns it). When the completion is approved/rejected/
  disputed, the countdown disappears (status no longer pending).

## Don't-break / QA
- Additive: admin dispute case page + resolution outcome picker + provider countdown (+ the additive `autoApproveAt` BFF
  field if missing). Existing dispute list, review page, job detail unchanged otherwise. Server owns case data + refund
  outcome; the FE computes no money and no threshold.
- **Confidentiality:** grep the dispute-case payload — no cost/margin field renders anywhere.
- tr + en for every new string; typecheck + lint clean both repos; any BFF change builds clean + envelope-correct.

## Verify (on screen)
1. Admin opens a dispute from the list → the case page shows the full consolidated file (dispute + SR timeline + cost-free
   economics + work-logs + evidence + messages + refund/payment state + N-E reason); no cost/margin anywhere.
2. Admin resolves with `FavorPayerPartialRefund` (amount ≤ refundable) → succeeds, case refreshes to Resolved with the
   refund reflected; an amount over the refundable is rejected (client + server); `FavorProviderRelease` shows the release
   effect.
3. Provider job/completion detail shows the auto-approve countdown from `autoApproveAt`; it disappears once the completion
   is approved/rejected/disputed.
4. Existing dispute list + status actions + job detail behave as before.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_S13.md`: the admin dispute case page + resolution outcome picker (wired to
`GetDisputeCaseBff`/`ResolveDisputeBff`), the confidentiality check, the provider auto-approve countdown (+ any additive BFF
field), i18n keys, and on-screen verification. This closes the dispute vertical (S13 + N3) end-to-end. Then next SR phase
(S12 + N2 recurring). **Do NOT commit.**
