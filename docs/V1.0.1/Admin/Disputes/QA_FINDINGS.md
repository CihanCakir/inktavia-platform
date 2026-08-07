# Admin QA — Disputes (A-QA10)

Routes: `/app/disputes`, `/app/disputes/:serviceRequestId/:disputeId/case`, `/app/service-requests/:id/dispute`.

## Static findings
- `DisputesPage` + `DisputeCasePage` are **REAL and exemplary** — `useDisputeListQuery` / case query + resolve mutation, all
  enum groups mapped via `enumName` (`src/entities/service-request/model/disputeEnums.ts`), TRY default, full `serviceRequests`
  i18n. Reference pattern for numeric-enum handling. (Backend: S13 dispute case + resolution → P10 refund, DisputeResolved.)
- **`CompletionDisputeReviewPage` (`/service-requests/:id/dispute`) — PARTIAL, needs work:**
  - 🔴 **Raw numeric-enum leak (X4):** L282 renders `Reason Code: ${dispute.reason}` (numeric) instead of
    `enumName('reason', …)` — the one true raw-number leak in the app. Contrast `DisputeCasePage.tsx:371` (correct).
  - L249-251 completion-status checked via ad-hoc `String(status)==='2'/'3'` instead of the `completionStatus` map in
    `disputeEnums.ts` — fragile.
  - **3 dead resolution buttons:** `Release Payment` (333), `Mediate Dispute` (337), `Request Rectification` (341) — no
    onClick (should drive P10 refund / dispute resolution).
  - Counter-evidence dropzone sets state but **never uploads**.
  - 100% hardcoded English.

## Live walkthrough checklist
- [ ] `/app/disputes` list + case: statuses/outcomes/causes show names (not numbers); resolve outcome (refund/release/split)
      works and reflects in P10.
- [ ] `/service-requests/:id/dispute`: reason shows a **name**, not `Reason Code: 3`.
- [ ] Release Payment / Mediate / Request Rectification buttons perform real actions.
- [ ] Counter-evidence actually uploads.
- [ ] tr/en parity on the completion-dispute-review page.

## Fix candidates
`FIX_A_QA10_COMPLETION_DISPUTE_REVIEW` — map reason/completion enums via `disputeEnums`, wire the 3 resolution buttons +
counter-evidence upload, i18n. (List/case need live-QA only.)
