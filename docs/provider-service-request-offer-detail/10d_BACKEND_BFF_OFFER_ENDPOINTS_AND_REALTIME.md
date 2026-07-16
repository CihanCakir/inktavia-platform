# 10d — Backend: Provider BFF offer endpoints + realtime (Claude Code)

Run **after 10c is merged and verified.** Exposes the offer draft/preview/submit/withdraw + attachment read-URL +
timeline through the Provider BFF, and adds the realtime events the detail banner needs. Do not touch the frontend.

## Auth — reuse, never reinvent

Canonical Provider flow: end-user token stops at the BFF; module calls carry the **service-account token +
`X-Aizen-Bff-Assertion` + `X-Aizen-User-Id` + `X-Aizen-Provider-Profile-Id`** via
`MarineProviderBffAuthDelegatingHandler`. Provider identity from the assertion; **never** a client
`providerProfileId`. `?access_token=` only on `/hubs`.

## Carry-over from 10c — the draft `Id = 0` bug (fix here)

10c reported honestly: `SaveOfferDraft` returns `offer.Id = 0` for a **newly created** draft, because the
unit-of-work commits after the handler returns, so EF has not assigned the database id when the response is
built. The offer persists, but the response has no usable id — and the frontend needs the offer id to submit /
update / withdraw.

**Fix it in this phase.** Either (a) the module handler saves explicitly (flush the unit-of-work) before building
the response so the generated id is present, or (b) the BFF reads the draft back after save
(`GetDraftByProviderAndRequestAsync`) and returns that. Prefer (a) — a save endpoint that cannot tell the client
what it saved is broken. **Acceptance: a first-ever `SaveOfferDraft` returns a non-zero `offerId`.** Paste it.

## Endpoints (doc 08; provider-scoped; typed DTOs only)

On the offers/detail controllers, add:

- `POST /service-requests/{id}/offer/draft` — get-or-create the caller's draft (returns the offer DTO).
- `PUT  /service-requests/{id}/offer/draft` — aggregate save (auto-save target). Body = items + terms +
  concurrency token. Maps to `SaveOfferDraftCommand`. **Totals passed through from the module untouched — the BFF
  computes nothing.**
- `POST /service-requests/{id}/offer/preview` — `PreviewOfferCommand`, no persistence.
- `POST /service-requests/{id}/offer/submit` — forwards the `Idempotency-Key` header to `SubmitOfferCommand`.
- `POST /service-requests/{id}/offer/withdraw` — existing withdraw.
- `GET  /service-requests/{id}/attachments/{fileId}/read-url` — mint a **short-lived signed read URL** via
  `IProviderFileStorageRemoteCall` (onboarding pattern). One per click. **Never** return object keys/buckets;
  never embed URLs in the detail aggregate.
- `GET  /service-requests/{id}/timeline` — durable events (or fold into detail; the P1 detail already carries a
  timeline — confirm it is enough and skip a second endpoint if so).

Wire the Refit methods on `IProviderServiceRequestRemoteCall` (note it already has CreateOffer/UpdateOffer/
WithdrawOffer — extend, do not duplicate). Reuse `IProviderVesselRemoteCall` (already in the detail handler) and
`IProviderFileStorageRemoteCall`.

## Realtime — extend the proven bridge, no parallel infra

Add module producers + BFF consumers, targeting **`provider:{profileId}`** (these are provider-specific, not city):

- `OfferViewedByCustomer` (needs `ViewedAt`, set when the customer opens the offer — the customer app calls a
  module command; if that app/command does not exist yet, **wire the field and the event contract but note the
  producer is pending** — do not fake a customer action).
- `OfferRevisionRequested` (needs `RevisionRequestedAt`).
- `OfferRejected` (the accepted event already exists; add rejected).
- Message-added to the provider, **only if** a provider-scoped message command/event exists; else defer and say so.

Idempotent consumers; dedupe by id in the UI (frontend concern). Realtime is a hint — the event invalidates; the
truth comes from the detail/offer endpoints.

## Acceptance — observed

- The five offer endpoints respond for provider2 (assertion headers), returning the module's **server-computed**
  totals unchanged by the BFF. Paste a draft-save response showing the totals came from the module.
- A `providerProfileId` sent by the client is ignored (result identical). Missing identity ⇒ reject, not empty.
- One vessel call per detail (already true from P1 — confirm still one). Attachment read-URL is short-lived and
  contains no object key.
- Auth tests: assertion propagation; empty/wrong secret; unauthorized client id.
- Realtime: a `provider:{id}`-targeted event reaches the provider; re-run the **two-replica backplane** check
  since consumers changed.

## Constraints
- No domain logic in the BFF; **no total computed or altered in the BFF.** No `object`/`JsonElement` on the wire.
  No client `providerProfileId`. No object keys to the SPA. No parallel realtime path.

## Report
Append to `REPORT_BACKEND.md` (section "10d"): a draft-save response (totals from the module), the read-URL shape
(no keys), the auth-test results, and which realtime producers are live vs pending (customer-viewed likely
pending). Unfinished is **not done**.
