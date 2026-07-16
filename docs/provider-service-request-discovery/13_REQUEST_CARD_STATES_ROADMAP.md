# 13 — Request-card states: roadmap (mockup → what we build)

The three mockups show one card in several states. This doc turns those states into a data model, finds what the
backend does **not** yet supply, and lays out the backend + frontend work. MVP priority, per your instruction.

---

## The card states in the mockup

| # | Example | Visual signal | Meaning |
|---|---|---|---|
| 1 | SR-9921 | **ACİL** (red chip) + red left border | priority Urgent/Emergency |
| 2 | SR-9921 | **YENİ** (gold chip) | published since I last looked |
| 3 | SR-9854 | **AÇIK** (navy chip) | open, biddable, nothing special |
| 4 | SR-9620 | **GÜNCELLENDİ** (amber chip), "2 gün önce" | edited **after** publication |
| 5 | SR-9712 | **TEKLİF VERİLDİ** (gold chip) + "Teklifiniz: ₺4.200" + single **Teklifi Yönet** button | I have already bid; show my amount; no "Teklif Ver" |
| all | — | "2 saat önce" / "Dün" / "2 gün önce" | relative time from **publishedAt** |
| all | — | "Çeşme Marina **(12km)**" | distance from **my browser location** |
| all | — | category chips (Bakım, Gövde…) | `serviceCategoryCode` / `serviceTypeCode` |

One rule for the chip in the top-left: it is a **single status chip** with a priority. The card shows exactly one
of: `ACİL` (if urgent) → else `TEKLİF VERİLDİ` (if I bid) → else `GÜNCELLENDİ` (if edited after publish) → else
`AÇIK`. `YENİ` is a **separate** badge that can sit alongside any of them.

### Budget — stays OUT (decision 2026-07-14 stands)

The mockup shows "₺18.000 – ₺25.000". We decided **not** to ask owners for a budget (it anchors offers to the
ceiling; owners can't state one for marine work). That decision has not changed, so **the card does not show a
budget** — no field, no section, no placeholder. If you want to revisit it, that is a separate product decision
(doc 00 §2), not part of this card work. Flagging it because the mockup will keep tempting us.

---

## What the backend already supplies (verified in source)

`ProviderDiscoveryItemDto` already carries: `Status` (string), `Priority` (string), `PublishedAt`, `DistanceKm`
(from browser geo), `HasProviderOffer`, `ProviderOfferId`, `ProviderOfferStatus`, `OfferCount`,
`AttachmentCount`, category/type codes, marina/city. So states **1, 2, 3, 5-partial** and distance already have
their data.

## Gaps — what states 4 and 5 need and don't have

| Gap | For | Where | Size |
|---|---|---|---|
| **My offer amount** (`ProviderOfferTotalAmount`) | State 5 — "Teklifiniz: ₺4.200" | module projection → BFF DTO | S |
| **"Updated after publish" signal** | State 4 — GÜNCELLENDİ | module projection → BFF DTO | S |

Both are the same shape of fix: one more projected field, carried through the BFF DTO. Details below.

### Gap A — my offer amount

The offer entity has `TotalAmount` (`ServiceRequestOfferEntity.TotalAmount`, `decimal`). The discovery projection
already selects the caller's own offer id and status via a filtered subquery; it just doesn't select the amount.
Add `ProviderOfferTotalAmount` (`decimal?`) to the projection and the DTO, from the same
`Offers.FirstOrDefault(o => o.ProviderProfileId == me && !o.IsDeleted)`. **Only the caller's own amount** — never
another provider's, which the privacy rule already forbids.

### Gap B — "updated after publish"

`ServiceRequestEntity : AizenEntityWithAudit` has an audit `UpdateDate`. "Updated" means the owner edited the
request **after** it was published — not every audit touch (a status transition or an offer arriving must not
light up GÜNCELLENDİ). Two honest options; pick one and state it:

- **(preferred) A dedicated `ContentUpdatedAt`** set only by the owner-edit command (title/description/dates/
  location change), null otherwise. Precise, but needs the update command to set it.
- **(cheaper) Derive `IsUpdated = UpdateDate > PublishedAt + small epsilon`** in the projection. No new column,
  but it flips on for any audit write after publication, which overstates "updated".

Recommendation: **the cheaper derived flag for MVP**, documented as approximate, with `ContentUpdatedAt` as the
post-MVP precise version. Expose it as `IsUpdated` (bool) on the DTO so the frontend never does date math.

---

## Distance from browser location — already wired, make it prominent

This already works: the page requests geolocation on demand, feeds `centerLat/Lng` into the query, and the
server returns `DistanceKm` (haversine, snapped-coordinate based). The card must render it inline with the marina
("Çeşme Marina (12 km)") **when present**, and omit it when location is off — no "0 km". The formatter
(`formatDistanceKm`) already returns null in that case.

One product note carried from doc 00 §1: browser location is *where the provider is standing*, not their service
area. Distances are honest but momentary. The durable fix (service area in onboarding) is a separate, larger
piece; this card uses the browser distance as-is.

---

## Roadmap

### Phase C1 — Backend: two projected fields (S)

**Module** (`Aizen.Modules.ServiceRequest`):
- Add `ProviderOfferTotalAmount` (`decimal?`) to `ProviderDiscoveryItemDto`; project it from the caller's own
  offer subquery in `ServiceRequestRepository.GetDiscoveryAsync`.
- Add `IsUpdated` (`bool`) to the DTO; project `PublishedAt != null && UpdateDate > PublishedAt` (document as
  approximate). Confirm the exact audit field name on `AizenEntityWithAudit`.
- No new endpoint, no migration (unless you choose the precise `ContentUpdatedAt` column — then a migration).

**BFF**: carry `ProviderOfferTotalAmount` and `IsUpdated` through `ProviderDiscoveryBffItemDto` and the enrich
handler (pass-through; no new remote call).

**Acceptance**: a request the provider bid on returns `providerOfferTotalAmount`; an owner-edited request returns
`isUpdated=true`; neither exposes any other provider's data. Paste a row of each.

### Phase C2 — Frontend: the card, all states (M)

Rebuild `DiscoveryRequestCard` to the mockup:
- **Single status chip** by precedence: ACİL (danger) → TEKLİF VERİLDİ (gold) → GÜNCELLENDİ (amber) → AÇIK
  (navy). Plus a separate **YENİ** badge when `isNew`.
- **Red left border** only for urgent/emergency (already there — keep).
- **Relative time** top-right from `publishedAt` (formatter exists).
- **Distance** inline with marina when `distanceKm != null`.
- **Offer-given variant**: when `hasProviderOffer`, show "Teklifiniz: {amount}" (locale money) and a single
  **Teklifi Yönet** button → the offer detail; hide "Teklif Ver".
- **Otherwise**: "Detayı Gör" (filled) + "Teklif Ver" (outline).
- Category + type chips. Description excerpt (`line-clamp-2`). **No budget.**
- Every status/priority value translated via i18n — never the raw enum.

New i18n keys: `card.status.open` (AÇIK), `card.status.updated` (GÜNCELLENDİ), `card.status.offered`
(TEKLİF VERİLDİ), `card.urgent` (ACİL), `card.myOfferAmount` ("Teklifiniz: {{amount}}"), `card.manageOffer`
(Teklifi Yönet).

**Acceptance**: each of the five states renders correctly against real data; the offer-given card shows the real
amount and the manage button; distance shows only with location granted; no budget anywhere; typecheck + lint +
build clean; visual match to the mockup (minus budget).

### Phase C3 — Verify in the browser

Drive the real screen: confirm an urgent request shows ACİL + red border; a bid-on request shows TEKLİF VERİLDİ
+ amount + Teklifi Yönet; an edited request shows GÜNCELLENDİ; granting location adds "(N km)"; denying it hides
distance and keeps the list working.

---

## Order

C1 (backend fields) → rebuild service-request-api + bff → C2 (card) → C3 (browser verify). C1 and the C2 card
markup can be written in parallel, but the card can't show the amount / updated state until C1 ships.

## Not in this card work (recorded, not silently dropped)

- Budget (decision: no).
- Real service-area distance (needs onboarding capture — doc 00 §1).
- `serviceType`/`plannedStart`/`hasAttachment` filters (backend has no fields yet — separate work).
- Precise `ContentUpdatedAt` (post-MVP; MVP uses the approximate `IsUpdated`).
