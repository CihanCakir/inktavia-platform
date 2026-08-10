# BE_MO5 — owner disputes: open + my-disputes list + case view + lifecycle (mobile)

> **Repos:** `addesso-project` (ServiceRequest module + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile`. Owner
> Economics track **MO5**: the owner **opens** a dispute (structured reason), sees **their disputes**, and views a **dispute
> case** (cost-free) with its **lifecycle/status** and the **resolution outcome** (the admin resolves; the owner is read-only
> + N3-notified — the owner does **not** resolve). Additive; identity from token; cost-free. **Do not commit.**

## Baseline (investigated)
- **Open (owner) exists:** `ServiceRequestDisputeController` `POST api/v1/service-requests/{srId}/dispute` →
  `OpenServiceRequestDisputeCommand(srId, actorType, req)`; handler uses `UserInfo.UserId` (token) + N-E
  `ServiceRequestDisputeReason` + description. Owner opens with `actorType = Owner`. ✅
- **Case detail exists but is UNGATED:** `GetDisputeCaseDetail(disputeId)` composes the cost-free case (dispute + SR
  timeline + S8 economics + work-logs + evidence + messages + P10 payment/refund state) but performs **no ownership check**
  (loads any dispute by id). → the **owner gate must be applied** (BFF `EnsureOwnedAsync`, consistent with MO1–MO4).
- **No owner "my disputes" list** — only `GetAdminDisputeList` (admin) + `GetProviderDisputes` (provider, MO-adjacent). MO5
  adds the owner one.
- **Resolution is admin-only** (S13 `ResolveDispute` + outcome → P10). The owner is **read-only** on resolution; N3
  notifies the owner of DisputeResolved. Status enum: Open/UnderReview/PendingOwnerResponse/PendingProviderResponse/
  Escalated/Resolved/Closed.

## BE — owner dispute surface
1. **Open:** reuse `OpenServiceRequestDispute` (owner, `actorType = Owner`, N-E `ServiceRequestDisputeReason` + description).
   The owner can open only on their own SR (owner-gate).
2. **My disputes list:** add an owner-scoped query `GetOwnerDisputes(ownerUserId, status?, paging)` — disputes on SRs the
   owner owns (`sr.OwnerUserId == token owner`), + an open/actionable count. Mirror `GetProviderDisputes`; identity
   server-side; cost-free (dispute + SR header, no economics internals).
3. **Case view:** reuse the cost-free `GetDisputeCaseDetail` — but **owner-gate it** (verify the caller owns the SR behind
   the dispute) at the BFF via `EnsureOwnedAsync` (the module query is ungated). Returns the composed case (cost-free —
   never S5 cost/margin or provider internals).
4. **Lifecycle/status:** the case carries the dispute status + resolution notes/outcome when resolved. The owner is
   **read-only** here — **no resolve/status-change from the owner** (admin owns `ResolveDispute`/`ChangeStatus`); MO5 does
   not expose those to the owner.
5. **Mobile BFF:** passthroughs — open (owner-gated), my-disputes list, case detail (owner-gated); owner identity via
   BffAssertion; typed, envelope-correct; cost-free.

## FE — owner disputes (`inktavia-marine-mobile`)
- **Open a dispute** from the SR/completion context (e.g. after a rejected/contested completion): a reason picker (N-E
  `ServiceRequestDisputeReason`) + description → open. Guard: only where a dispute is allowed (own SR, appropriate state).
- **My disputes** list: status + SR + opened date; loading/empty/error.
- **Case detail (read-only):** dispute reason/description, **status + lifecycle timeline**, the cost-free case (SR summary,
  economics customer totals, evidence, messages), and — when resolved — the **resolution outcome** ("İtiraz çözüldü:
  {outcome}"). No resolve/status controls for the owner. Evidence via the read-url path (shares the attachment-400 fix).
- Cost-free throughout; tr+en; mock parity.

## Don't-break / QA
- Additive: reuse open + case; new owner-disputes list; BFF owner-gates. **Resolution/status-change stays admin-only**
  (owner read-only). The dispute case composer, S13 resolution→P10, and N3 notifications are unchanged. Identity from token;
  cost-free (grep the owner payload — no S5 cost/margin/provider internals). BE builds clean; FE tsc+lint; tr+en.
- Tests: (1) owner opens a dispute on their own SR (N-E reason), rejected on a non-owned SR; (2) my-disputes returns only
  the owner's disputes; (3) case view owner-gated — non-owner rejected; (4) case is cost-free; (5) owner cannot
  resolve/change-status (no such owner path); (6) a resolved dispute surfaces the outcome read-only.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO5_DISPUTES.md`: the owner open (actorType=Owner), the owner my-disputes list, the
owner-gated cost-free case view (+ the ungated-module-query note), the read-only lifecycle/resolution surfacing, and the
tests. Then MO6 (change orders — approve/reject → incremental checkout, iyzico-gated).
