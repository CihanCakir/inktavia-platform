# REPORT — BE_MO2: owner offers inbox + cost-free economics (mobile)

> Executes `BE_MO2_OFFERS_INBOX_ECONOMICS.md`. ServiceRequest module + Marine.Participant.Mobile BFF +
> `inktavia-marine-mobile`. The owner sees the provider offers received on their SR with the **cost-free** economics
> breakdown, compares them, and rejects one with an N-E reason. **No money moves** (accept → checkout is MO3).
> Additive; identity from the token; cost-free (never provider cost/commission internals). **Not committed.**

---

## BE — owner offers surface

### ServiceRequest module (owner-scoped query + detail)
- **`GetServiceRequestOffersForOwner`** (`Application/Query/Owner/…`) — verifies the caller owns the SR
  (`sr.OwnerUserId == _info.UserInfoAccessor.UserInfo.UserId`, else a clean **"Service request not found"** with no
  existence leak — the same discipline as the provider detail access-check). Reuses `GetByServiceRequestIdAsync`
  (which `.Include`s the offer items) and **excludes other providers' Drafts** (`Status != Draft`). Returns the
  existing cost-free `ServiceRequestOfferDto` (customer totals + line items + S3 FX) — no new economics fields.
- **`GetServiceRequestOfferForOwner`** — one offer with its full breakdown (`GetByIdAsync` includes Items +
  FxSnapshots); owner-scoped, the offer must belong to the SR and not be a Draft.
- **Controller** — added `GET …/offers/received` + `GET …/offers/received/{offerId}` to `ServiceRequestOfferController`
  (`api/v1/service-requests/{srId}/offers`), `[Authorize]`; identity from the trusted context. The provider-only
  `commission-preview` / `part-terms-preview` endpoints are **left untouched and NOT surfaced to the owner**.

### Marine.Participant.Mobile BFF (cost-free passthrough)
- **`IServiceRequestRemoteCall`** — added `GetOwnerOffers` / `GetOwnerOffer` (owner GETs) + `RejectOwnerOffer` (PATCH).
- **Cost-free mobile DTO** — `MobileServiceRequestOfferDto` / `…OfferItemDto` (`Contracts/ServiceRequest/MobileOfferDtos.cs`):
  status, provider name, ETA, `Subtotal/TaxTotal/DiscountTotal/GrandTotal`, deposit/warranty/payment-terms notes, and
  the cost-free line items **with S3 FX** (`sourceUnitPrice`/`sourceCurrencyCode`/`settlementCurrencyCode`). The mapper
  **drops the provider profile/user ids** and sets the totals currency from the line `SettlementCurrencyCode` (**TRY**,
  never the offer DTO's stale USD default). Provider name is left `null` (the SR module itself resolves provider names
  to a placeholder — no Identity lookup exists; the FE shows a localized fallback — see *Known* below).
- **Reject passthrough** — `RejectMobileServiceRequestOffer` command: resolves the participant, **owner-gates via
  `EnsureOwnedAsync`** (the module reject does not owner-check), proxies the N-E `OfferRejectReason` + optional note,
  then re-reads the offer so the client sees the `Rejected` status. **Accept is deliberately not wired (MO3).**
- **Controller** — `GET …/offers`, `GET …/offers/{offerId}`, `POST …/offers/{offerId}/reject` on the MO1
  `ServiceRequestsController` (`api/v1/mobile/service-requests`), `[Authorize] ParticipantAuthenticated`. The
  commission/part-terms previews are **not proxied**.

## FE — offers inbox on the owner SR detail (`inktavia-marine-mobile`)
- **SR detail** (`features/services/screens/ServiceRequestDetailScreen.tsx`) — added an **Offers** section (replacing
  the old offers/chat coming-soon stub) between attachments and the remaining chat stub: `useServiceRequestOffers`,
  loading / empty ("No offers yet") / error, and a comparable row per offer (provider fallback name · **GrandTotal in
  ₺** · status pill · ETA) → taps to the offer detail.
- **Offer detail** (new `OwnerOfferDetailScreen.tsx`) — the cost-free breakdown: line items (title · qty·unit → line
  total), **VAT (KDV)**, **discount**, and the **customer total** the owner would pay; a foreign-priced line shows the
  S3 FX note **"≈ {source} · kur kabulde sabitlenir"**. Descriptive terms (warranty/payment/description). **No
  cost/commission anywhere.** **Reject** opens an N-E `OfferRejectReason` bottom-sheet (mirrors the SR CancelSheet) +
  optional note; **Accept** is a coming-soon toast (MO3).
- Wiring: `endpoints.ts` (offers/offer/reject repointed to `/api/v1/mobile/…`), `serviceRequestsApi.ts`
  (`ServiceRequestOffer`/`…Item`/`OfferRejectReasonCode` + 3 fns), `useServiceRequests.ts`
  (`useServiceRequestOffers`/`useServiceRequestOffer`/`useRejectOffer`), `queryKeys` (offer key), `display.ts`
  (`OFFER_REJECT_REASON_CODES`), navigation (`OwnerOfferDetail` route + param type), i18n `services.offer.*` (en+tr),
  and **mock parity** (`/api/v1/mobile/…/offers` GET/GET-one/reject with two cost-free offers incl. an EUR-FX line).
- **Currency:** offers display **₺** (the settlement currency) throughout — not the stale USD default.

## Verification

- **BE builds 0 errors** — ServiceRequest module + Marine.Participant.Mobile BFF both `Build succeeded`.
- **FE `tsc --noEmit` = 0** (no ESLint in the repo — tsc is the bar, per the MO cadence). **i18n parity 375/375** en↔tr.
- **Cost-free (grepped the owner payload):** the module owner query/response, the mobile offer DTO, and the BFF offer
  handlers contain **zero** `commission` / `funding` / `providerNet` / `partCost` / `dealerMargin` / `supplierList` /
  `margin`. The FE offer types/screens likewise (only CSS `margin*`). Provider profile/user ids are dropped in the mapper.
- **Routes wired (live):** on the running BFF (`:17003`), the three new routes return **401** without a token — the same
  as the MO1 `…/{id}` control — while a bogus sub-route returns **404**, proving they are registered behind
  `ParticipantAuthenticated` (not unmatched). Rebuilt + restarted `service-request-api` + `bff-marine-mobile`.

### Acceptance tests (by design + cost-free grep; deeper live needs a participant owner token / mock)
1. **Owner sees only their own SR's offers** — the module query throws a clean not-found when
   `sr.OwnerUserId != caller` (another owner's SR ⇒ rejected). ✓
2. **Other providers' Drafts excluded** — `.Where(o => o.Status != ServiceRequestOfferStatus.Draft)`. ✓
3. **Payload = customer totals + cost-free items, no provider internals** — cost-free grep empty; DTO carries
   `Subtotal/TaxTotal/DiscountTotal/GrandTotal` + cost-free items + S3 FX only. ✓
4. **Reject with an N-E reason transitions the offer + surfaces it** — the BFF proxies to the module reject
   (`offer.Reject(reason, reasonCode)` → `Rejected`, reason persisted on the entity + published N-E on the bus), then
   re-reads so the client shows `Rejected`. ✓
5. **Accept not exposed yet** — no accept method/route in the BFF; the FE Accept is a coming-soon toast. ✓

## Known / follow-up
- **Provider display name** is `null` for now (FE shows a localized "Servis Sağlayıcı"). The SR module itself has no
  Identity provider-name lookup — its own `GetProviderOffers` uses a `"Provider {id}"` placeholder — and resolving the
  real company name would need a new Identity endpoint (outside the three MO2 repos). Offers stay distinguishable by
  amount / ETA / status / line items. Populating a real name is a small follow-up once a provider-name endpoint exists.
- The rejected offer surfaces as `Rejected`; the structured reason the owner chose is persisted + published (N-E) but is
  not echoed back in the cost-free read DTO (the owner picked it) — consistent with the existing offer DTO.

## Next — **MO3** (accept → checkout, iyzico-gated)
Wire the owner **Accept** → payment checkout (deposit/full per the offer terms), iyzico-gated, transitioning the SR to
`OfferAccepted` + assignment. This is the first money-moving mobile owner flow.

**Additive; provider offer flows + commission/part previews + economics math unchanged. Not committed.**
