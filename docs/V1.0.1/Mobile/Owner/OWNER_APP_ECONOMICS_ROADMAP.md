# Owner App — Economics Surface Roadmap (V1.0.1 backend → owner mobile)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile`) + `inktavia-marine-mobile` (Expo client). This is the
> **economics expansion of the mobile M-roadmap's M6** and its neighbours. The whole **V1.0.1 payment-economics revision +
> go-live hardening is DONE on the backend (Payment P1–P12, SR S1–S13, RefData R1/R3/R4, Identity I1/I2, Notification
> N1–N4)** and surfaced on admin + provider web — but the **Owner is the actor for a large slice of it that has no client
> yet.** This roadmap plans that owner surface, phase by phase, on top of the existing mobile foundation.
>
> **Canonical references (do not rewrite — mirror/extend):** BFF pattern = `Bff/src/MarineProvider` + the existing
> `Marine.Participant.Mobile` (Auth/Profile/Vessel already built this way); `MOBILE_ROADMAP.md` (M1–M8) + its invariants;
> `MOBILE_STATIC_TO_API_ROADMAP.md` (FE static→API cadence). Each phase → `BE_MO<n>_*.md` prompt + `REPORT_BE_MO<n>_*.md`.

## Baseline (investigated, 2026-08)
**Done (mobile):** M1 Foundation, M2 Auth (OTP/password/social/register/recovery/refresh), M3 Profile+Reference, **M4
Vessels** (read/create/edit/archive/documents/media, on-device QA). Mobile BFF RemoteClients today:
`IIdentity`, `IReferenceData`, `IVessel`, `IFileStorage`. **Not yet present:** ServiceRequest, Payment, CargoDry,
Notification, Messaging clients → M5–M8 unopened.

**The gap this roadmap fills:** the existing `MOBILE_ROADMAP.md` **M6** ("3-step create, offers accept, chat, complete,
dispute") was written **before** the economics work. It does **not** cover what the owner now actually needs: the **offer
economics breakdown**, **checkout/payment (P8→P9 iyzico)**, **structured completion/dispute with reasons (N-E)**, the
**dispute case view (S13)**, **change-order approval (S11)**, **subscription/plan + discount transparency (I3/P6)**, and
**owner-managed maintenance schedules (S12)**. This roadmap decomposes M6 into economics-aware phases and cross-references
M5/M7/M8.

## Owner-actor capability map (what the finished backend exposes to the owner)
| Owner capability | Backend (done) | Live gate |
|---|---|---|
| Create/list/track a service request | SR create/list/detail | — |
| Receive provider offers + **full line-item economics** (items, KDV, discounts, customer total) | SR S1–S8 offer + `GetOfferCommissionPreview` / customer-payable | — |
| **Accept an offer → checkout** (acceptance economics + escrow + split) | P8 `CalculateServiceRequestEconomics` + P9 iyzico split | **iyzico keys** (P9 live) |
| Reject an offer / cancel with a **structured reason** | N-E `OfferRejectReason`/`ServiceRequestCancelReason` | — |
| Pay (iyzico), see payment status, invoices/receipts | P9 checkout + Payment invoices | **iyzico keys** |
| **Review completion** — approve/reject (reason) + evidence + **auto-approve countdown** | SR completion + N3 auto-approve (`AutoApproveAt`) | — |
| **Open a dispute** (reason) + view the **dispute case** + lifecycle status | SR S13 dispute + `GetDisputeCaseDetail` (owner-scoped, cost-free) | — |
| **Approve/reject a provider change order** → incremental checkout | SR S11 `ServiceChangeOrder` (customer-approve is the blocked step) | **iyzico keys** for the increment |
| **Subscription / membership plan** + benefit/discount transparency | Payment participant subscribe (I3) + P6 customer discount/budget | — |
| **Maintenance schedules** (own vessels) + reminders | SR S12 `MaintenanceSchedule` + N2 reminder | — |
| Notifications (offers, payment, completion, dispute, maintenance, change-order) + push + realtime | Notification N1–N4 + delivery platform | Expo push channel (M7) |
| Chat with the provider | Messaging module | — |
| CargoDry (owner kits, QR activation, renewal) | CargoDry (M5) | — |

## Guardrails (every phase — inherit the mobile roadmap's invariants)
- **Envelope:** client `ApiResponse<T>`/`PagedResponse<T>`; BFF CQRS `AizenApiResponse<T>` → `SetResponse`. Mapping is a
  per-phase acceptance criterion.
- **Identity from the token only:** `participantProfileId`/`userId` never from body/query — resolved server-side from
  validated claims; module calls carry `X-Aizen-Bff-Assertion` (+ user/profile headers) via `ModuleAssertionSecret`; the
  inbound user JWT is never forwarded. (Aligns with the hardening-2 fail-closed BffAssertion.)
- **Additive & isolated:** only `Bff/src/Marine.Participant.Mobile/` + the Expo client change; every other BFF, module, and
  web panel stays byte-for-byte clean. Each phase adds its module RemoteClient (mirror MarineProvider's).
- **Confidentiality:** the owner surface is **cost-free** — never expose S5 supplier cost/dealer margin or provider-internal
  economics; show only customer-facing figures (customer total, KDV, discounts, what the owner pays).
- **Server owns money:** the client displays economics + submits intent (accept/approve/pay); it never computes a total,
  rate, or threshold.
- Each phase = a BFF slice + FE wiring (same cadence as `MOBILE_STATIC_TO_API_ROADMAP`) + a `REPORT_BE_MO<n>_*.md`.

## Phases (Owner Economics track — MO-series)
> Dependency: all consume module clients that must be added to the mobile BFF first (mirror MarineProvider). Backend for
> every phase is **ready**; the only live gates are **iyzico keys** (payment/checkout) and Expo push (M7).

- **MO1 — Service request create + list + detail.** Add `IServiceRequestRemoteCall` to the mobile BFF; owner 3-step create,
  my-requests list, request detail + status timeline. (Foundation for everything below; no payment yet.) *Backend: SR
  create/list/detail.*
- **MO2 — Offers inbox + economics breakdown.** Owner sees each provider offer with the **full line-item breakdown**
  (items, quantities, KDV, discounts, **customer total**) — cost-free, server-computed. Compare offers. Reject with an
  N-E structured reason. *Backend: SR offer + customer-payable/commission preview + N-E.* No money moves yet.
- **MO3 — Accept → checkout (iyzico).** Accept an offer → P8 acceptance economics + escrow + **P9 iyzico split checkout**;
  payment status, receipt/invoice. This is the core economic transaction. Add `IPaymentRemoteCall`. **Live gate: iyzico
  sandbox keys** (the flow is built + testable; live split needs the keys). *Backend: P8 + P9-code + invoices.*
- **MO4 — Completion review.** Approve/reject the provider's completion (N-E reason) with evidence review; show the **N3
  auto-approve countdown** ("auto-approves on {date}"); approval releases escrow via the existing path. *Backend: SR
  completion + N3.*
- **MO5 — Disputes.** Open a dispute (N-E reason) on a completed/contested request; view the **owner-scoped dispute case**
  (status, timeline, the customer-facing economics, evidence, messages) and its lifecycle; receive resolution outcome
  (admin resolves). *Backend: SR S13 (owner-scoped `GetDisputeCaseDetail`) + N3 dispute notifications.*
- **MO6 — Change orders (unblocks S11).** Receive a provider's proposed change order (added/removed work) → **approve or
  reject**; approval runs the **incremental checkout** (new snapshot + incremental iyzico split under the CO key). The owner
  approve step is exactly what S11 left blocked. **Live gate: iyzico keys** for the increment. *Backend: SR S11.*
- **MO7 — Subscription / membership + discount transparency.** Owner's participant plan (subscribe/view), plan benefits, and
  the **customer discount** they receive (P6) shown transparently on offers/checkout. *Backend: Payment participant
  subscribe (I3) + P6.* (Note: I3 owner-membership entry point lands here.)
- **MO8 — Maintenance schedules (owner self-service).** Owner creates/views maintenance schedules for their vessels
  (interval + reminder lead) and sees upcoming/overdue; feeds the N2 reminder. *Backend: SR S12 (admin upsert exists; add an
  owner-scoped surface).*
- **MO9 — Notifications & realtime (owner) = mobile M7.** All owner notifications (offers, payment, completion, dispute,
  change-order, maintenance, price-change), push-token registration (Expo), preferences, realtime bell. *Backend:
  Notification N1–N4 + delivery platform.* (This is the existing M7 — scope it for the owner event set above.)
- **Cross-ref (existing mobile phases):** **M5 CargoDry** (owner kits/QR/renewal), **M8 Files** (presigned upload/get),
  **Messaging** (owner↔provider chat) fold in where each MO phase needs them (chat in MO4/MO5; files in MO2/MO5 evidence).

## Sequencing recommendation
MO1 → MO2 (economics visible, no money) → **MO3 (checkout — the first iyzico-gated phase; do the BFF + FE, verify against
sandbox when keys arrive)** → MO4 → MO5 → MO6 (change-order, iyzico-gated increment) → MO7 → MO8 → MO9. MO1–MO2 + MO4–MO5 +
MO8 are **fully unblocked today** (no external gate); MO3/MO6 build fully but their **live** payment verification waits on
the iyzico keys; MO7 discount display is unblocked, its live checkout effect rides MO3.

## Gates & open decisions
- **iyzico keys** (P9 live) — gates the *live* verification of MO3/MO6 checkout; the flow is built + sandbox-testable.
- **YMM / KDV** (R2) — the KDV figures shown to the owner must use the YMM-approved rates once confirmed (display is
  ready; values are config-driven).
- **Expo push channel** — MO9 push needs the Notification module's Expo channel (M7 prerequisite in the mobile roadmap).
- **commerce / discovery** owner surfaces remain **out of MVP** (client screen-stubs; product decision) per the mobile
  roadmap.

## Next step
Start the transition with **MO1** (add the ServiceRequest mobile client + owner SR create/list/detail) — it unblocks the
whole owner economics flow and has no external gate. Then MO2 (economics breakdown) makes the value visible before the
first payment phase. Each phase: a `BE_MO<n>_*.md` prompt (BFF slice mirroring MarineProvider) + FE wiring + a
`REPORT_BE_MO<n>_*.md`, same cadence as M2a–M4f.
