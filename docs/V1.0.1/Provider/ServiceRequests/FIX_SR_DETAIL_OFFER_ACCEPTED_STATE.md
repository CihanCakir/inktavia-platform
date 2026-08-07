# FIX — SR detail (provider) doesn't reflect "offer made / accepted / assigned"; lifecycle-aware buttons+fields

> **Repos:** `inktavia-marine-provider-web` + (diagnostic) ServiceRequest module / MarineProvider BFF. On
> `/app/service-requests/:id` (e.g. **9011**, which is accepted + assigned to this provider) the page renders as if **no
> offer was ever made and nothing was accepted** — wrong buttons + fields. Fix the state reflection **and** make the page
> lifecycle-aware. **Diagnostic-first** (root needs the live payload). **Do not commit** until reviewed.

## What the code review already established (so don't re-chase these)
The routed page is `service-requests/detail/pages/RequestDetailPage.tsx` (not the other `ServiceRequestDetailPage.tsx`). Its
state derives from `myOffer`:
```
offerReadOnly = offerStatusName(myOffer?.status) ∈ {Submitted, Accepted, UnderReview}
```
- **Identity:** `GetProviderServiceRequestDetailQueryHandler` resolves `providerProfileId` from
  `KeycloakTokenInfo.ProviderProfileId` — the **same accessor** as `GetMyOffers` + `GetProviderJobs`, which work (the
  offers list + the assigned job render correctly for this provider). So identity is not the divergence.
- **Includes:** `GetByIdWithDetailsAsync` DOES `.Include(x => x.Offers).ThenInclude(...)`.
- **Mapping:** `ToProviderDetailDto` sets `MyOffer = entity.Offers.FirstOrDefault(o => o.ProviderProfileId ==
  providerProfileId && !o.IsDeleted)?.ToDto()`.
- **Enum:** backend `ServiceRequestOfferStatus {Draft=1,Submitted=2,UnderReview=3,Accepted=4,…}` == the FE `OFFER_STATUS`
  map. So the enum mapping is correct.
Given all four look right, the failure is **data/payload-specific** → confirm on the wire.

## Phase 0 — diagnose the 9011 payload (needs a logged-in session)
Fetch `GET .../service-requests/9011` detail (or read the network response in the running provider app) and check:
1. **Is `myOffer` null or present?**
   - **If null:** the provider's accepted offer isn't being matched — inspect the actual offer row for SR 9011: its
     `ProviderProfileId` vs the token's `providerProfileId`, and `IsDeleted`. A mismatch (offer created under a different
     provider-id representation, or a soft-deleted superseding draft leaving the accepted one filtered) is the likely root.
     Also confirm `entity.Offers` is actually populated at mapping time (lazy vs the Include path).
   - **If present:** check `myOffer.status` on the wire (numeric 4 vs a string) and how `offerStatusName` normalises it —
     if it doesn't resolve to `Accepted`, `offerReadOnly` stays false and the page shows "create offer".
2. **SR status:** is 9011's `status` still a biddable value (Open/WaitingForOffer/OfferReceived) rather than advanced to
   Assigned/OfferAccepted after acceptance? A stale SR status compounds the wrong presentation.
Fix the confirmed root (matching / soft-delete filter / status advance / serialization).

## FE — make `RequestDetailPage` lifecycle-aware (the "arrange buttons+fields per scenario" ask)
Render distinct states from `myOffer.status` + the SR status + assignment, with the right primary action:
- **No offer** (`myOffer == null`, biddable) → primary **"Teklif Ver"**; show the request + work scope.
- **Draft** → **"Taslağı Sürdür/Düzenle"** (opens the builder); a "draft not submitted" hint.
- **Submitted / UnderReview** → header "**Teklifin gönderildi / inceleniyor**"; show the submitted amount/terms; actions
  **Geri Çek** + manage (read-only builder). No "create offer".
- **Accepted (+ assigned to this provider)** → header "**Teklifin kabul edildi — İş sana atandı**"; show the accepted
  amount/terms; **primary link to the Job** (`/app/jobs/:id`) instead of any offer create/manage; do **not** show the
  make-offer affordance.
- **Rejected / Withdrawn / Expired** → the terminal state clearly, with re-offer only if still biddable.
- **Review the two disabled `title=actions.soon` buttons** (Save request / Share): implement or remove for go-live (a dead
  "soon" button on a key screen reads unfinished).

## Verify (on screen — localhost:3002/app/service-requests/9011, logged in)
- [ ] For 9011 (accepted+assigned): the page shows **"Teklifin kabul edildi — İş sana atandı"** + the accepted amount + a
      link to the Job; **no** "create offer" affordance.
- [ ] A biddable request with no offer shows "Teklif Ver"; a submitted one shows pending + Geri Çek/manage; a draft shows
      "Sürdür".
- [ ] `myOffer` renders its real status badge + amount; no "offer.none" for an SR that has the provider's offer.
- [ ] No dead "soon" buttons (implemented or removed); tr+en; no console errors.

## Report
`docs/V1.0.1/Provider/ServiceRequests/REPORT_FIX_SR_DETAIL_OFFER_STATE.md`: the diagnosed payload root (myOffer null vs
mis-rendered + the underlying data/status cause), the backend fix (if any), the FE lifecycle states + buttons, and the
on-screen verification for each state.
