# REPORT — SR detail (provider) now reflects offer made / accepted / assigned; lifecycle-aware buttons+fields

> Executes `FIX_SR_DETAIL_OFFER_ACCEPTED_STATE.md`. ServiceRequest module + MarineProvider BFF (both fixed) +
> `inktavia-marine-provider-web`. Diagnostic-first. **Not committed.**

---

## Diagnosed payload root (the confirmed cause)

`GET /app/service-requests/9011` was rendering "make an offer" for a request this provider had **already won**. On the
wire the offer summary showed a **"Taslak" (Draft) badge + ₺3.732,00** — i.e. `myOffer` was a *stale draft*, not the
accepted offer.

The DB explains it: **SR 9011 has two non-deleted offers from the same provider (profile 100011)** —

| Offer | Status | IsDeleted | Amount |
|-------|--------|-----------|--------|
| **10** | 1 = **Draft** | false | ₺3.732 |
| **90001** | 4 = **Accepted** | false | ₺3.756 |

The mapping picked the wrong one:
```csharp
// ToProviderDetailDto (before)
var myOffer = entity.Offers.FirstOrDefault(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted);
```
`FirstOrDefault` with **no ordering** yields the collection's first row (lowest id → the **Draft #10**). So
`myOffer.status = Draft` → `offerStatusName = "Draft"` → `offerReadOnly = false` → the page showed "Teklif Oluştur", the
draft's ₺3.732, and **no accepted/assigned state**.

Everything the code review pre-cleared was indeed fine: identity resolved correctly (the accepted offer matched the same
`providerProfileId`), `Offers` was populated, the enum mapping was correct, and the **SR status was already `21`
(Assigned)** with a live assignment (id 91001) — so the root was *purely* the multi-offer `FirstOrDefault`, nothing else.

## Backend fix (ServiceRequest module)

1. **`ServiceRequestMappingExtensions.ToProviderDetailDto`** — select the provider's **furthest-progressed** offer, not
   the first by id. Added `MyOfferRank` (Accepted `4` > Submitted/UnderReview `3` > terminal `2` > Draft `1`) and:
   ```csharp
   var myOffer = entity.Offers
       .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
       .OrderByDescending(o => MyOfferRank(o.Status))
       .ThenByDescending(o => o.Id)
       .FirstOrDefault();
   ```
   Now the Accepted offer (₺3.756) wins over the stale Draft.
2. **`ProviderServiceRequestDetailDto.AssignmentId` (new `long?`)** + set in
   `GetProviderServiceRequestDetailQueryHandler` (`detail.AssignmentId = isAssignedToMe ? assignment!.Id : null`) — so an
   accepted+assigned request can link straight to its Job. The handler already loaded the assignment for its access check.

**Secondary discovery (BFF rebuild required).** The MarineProvider BFF deserializes the module's detail response into a
strongly-typed Refit contract using its *own compiled* `...Abstraction` DLL. Until the BFF was rebuilt, that DLL predated
`AssignmentId`, so the field was **silently dropped in the BFF round-trip** (the offer-selection fix showed immediately —
that's server-side in the module — but the Job link stayed hidden until `bff-marineprovider` + its replica were rebuilt).
No BFF *code* change was needed (`GetServiceRequestDetailBff` is a passthrough) — only a rebuild.

## FE — `RequestDetailPage` is now lifecycle-aware

Replaced the single `offerReadOnly` (manage vs create) with explicit states derived from `myOffer.status` + SR status +
`assignmentId`, each with the right primary action:

| State | Header / card | Primary action |
|-------|---------------|----------------|
| **Accepted + assigned** | gold banner **"Teklifin kabul edildi — İş sana atandı"** + accepted amount | **"İşi Görüntüle"** → `/app/jobs/:assignmentId`; **no** offer affordance |
| **Submitted / UnderReview** | "Gönderildi" badge + amount + submitted note | **"Geri Çek"** (inline two-click confirm, reuses `offersApi.withdraw`) + **"Teklifi Görüntüle"** (read-only) |
| **Draft** | "Taslak" badge + amount + "not submitted" hint | **"Taslağı Sürdür"** → opens the builder seeded with the draft |
| **No offer + biddable** | "offer.none" | **"Teklif Ver"** → opens the builder |
| **Terminal** (Rejected/Withdrawn/Expired) | terminal note; re-offer only if still biddable | — |

- **Removed the two dead `title=actions.soon` buttons** (Save request / Share) — they read as unfinished on a key screen.
- Withdraw uses an **inline confirm** (button → "Geri çekmeyi onayla" / "Vazgeç"), never a blocking `window.confirm`.
- New DTO field: `assignmentId` on `ProviderRequestDetail` (the BFF passes the module aggregate through, so it flows
  automatically once the BFF carries the field). New i18n `lifecycle.*` block (en/tr, **parity 142/142**).

## Verify (on screen — localhost:3002, PROVIDER 2 AS / profile 100011, tr)

- ✅ **Accepted+assigned (SR 9011).** Gold banner **"Teklifin kabul edildi — İş sana atandı"**, **Toplam: ₺3.756,00**
  (the accepted amount, not the ₺3.732 draft), primary **"İşi Görüntüle"** → clicked → navigated to **`/app/jobs/91001`**
  (job detail, accepted offer #OFF-90001, ₺3.756). **No** create-offer button, **no** "soon" buttons.
- ✅ **Draft (SR 45).** Header + card **"Taslağı Sürdür"**, "Taslak" badge + ₺3.732 + hint "Henüz gönderilmemiş kayıtlı
  bir taslağın var."
- ✅ **Biddable / no offer (SR 1).** Header + card **"Teklif Ver"** + "Bu talebe henüz teklif vermediniz."
- ✅ **Submitted / pending (SR 45, offer temporarily flipped to Submitted then reverted).** Header + card **"Geri Çek"** +
  **"Teklifi Görüntüle"**, "Gönderildi" badge + submitted note. Clicking "Geri Çek" showed the inline confirm
  ("Geri çekmeyi onayla" / "Vazgeç"); cancelled — no withdrawal performed.
- ✅ `myOffer` renders its real status badge + amount in every case; no "offer.none" when the provider has an offer.
- ✅ **No console errors** from this change on a fresh load. (Transient `Bookmark is not defined` errors appeared only
  mid-edit via HMR — the final source has zero `Bookmark` refs, tsc-verified — and cleared on reload. The page also logs
  pre-existing image-attachment `getReadUrl` 400s + SignalR reconnects from the BFF restart, both unrelated.)
- ✅ **tsc `--noEmit` = 0, eslint = 0** on the changed FE files; **both backend projects build 0 errors**; en/tr
  `requestDetail` parity **142/142**.

Test data restored: SR 45's offer is back to Draft (the Submitted flip was reverted). SR 9011's two offers are untouched.

## Scope

**Backend** — `ServiceRequestMappingExtensions.cs` (rank-based `myOffer` + `MyOfferRank`),
`ProviderServiceRequestDetailDto.cs` (`AssignmentId`), `GetProviderServiceRequestDetailQueryHandler.cs` (set it); BFF
**rebuilt** (no code change). **FE** — `RequestDetailPage.tsx` (lifecycle states + withdraw + removed soon buttons),
`detailTypes.ts` (`assignmentId`), `requestDetail.json` en/tr (`lifecycle` block). No economics change. **Not committed.**
