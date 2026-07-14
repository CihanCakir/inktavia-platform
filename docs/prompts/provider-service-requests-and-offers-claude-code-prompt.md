# Claude Code Prompt — Provider portal: service request discovery, offers, and the job lifecycle

This turns three "coming soon" tiles into the core of the provider portal: **find work → bid → win → do the job →
get it signed off.** Everything else in the product exists to feed this loop.

Realtime (SignalR) is **out of scope**. Build the REST path first; the hub decision is a separate phase.

---

## What exists, and the hole in the middle

`Modules/ServiceRequest` is substantial. It already has:

- `POST/PUT /api/v1/service-requests` + `GET /{id}`, `GET /my`, cancel, publish, attachments
- Offers: `POST /{srId}/offers`, `PUT /offers/{offerId}`, accept, reject, withdraw
- Assignment, Completion, WorkLog, Dispute controllers
- `GET /api/v1/service-requests/provider/jobs` — the provider's assigned jobs (already wired to the BFF)

**But there is no way for a provider to find work.** `GET /service-requests/my` is the *customer's* own list.
There is no "open requests a provider may bid on" query anywhere — no `open`, no `available`, no feed. There is
also no "my offers" list: a provider can create an offer and then never see it again.

So the two screens the provider portal is built around have **no query behind them.** Writing a BFF controller
alone will not fix that — the module needs new read paths.

---

## PART A — ServiceRequest module: the two missing queries

Follow the module's existing CQRS conventions exactly (Query + QueryHandler + Validator + Response DTO, cacheable
where the module already does that). Do not invent a new pattern.

### A1. Open service requests a provider may bid on

`GET /api/v1/service-requests/provider/open`

- Only requests in a biddable state (`Published`/`Open` — use the module's real enum; state which).
- Exclude requests the provider has **already** offered on, and requests already assigned.
- Filter by the provider's service categories and operating region **when the caller supplies them** — the BFF
  passes the provider's onboarding profile. Do **not** reach into Identity from ServiceRequest; the filters are
  parameters.
- **Geo/radius search is explicitly NOT this module's job** (that is the future GeoDiscovery module). City/region
  is a plain equality filter here. If you feel the need to write a distance query, stop and say so in the report.
- Paged, sorted newest first. Returns enough to decide: title, category, city, budget/expected range if the model
  has one, requested dates, attachment count, offer count, created date.

### A2. The provider's own offers

`GET /api/v1/service-requests/provider/offers`

- Every offer this provider has made, with the offer's status (`Draft`/`Submitted`/`Accepted`/`Rejected`/
  `Withdrawn` — use the real enum) and a snapshot of the request it belongs to.
- Filterable by status, paged.

### A3. Authorisation — the part that must not be hand-waved

Both queries are provider-scoped. The provider identity arrives as the BFF's asserted user
(`X-Aizen-Bff-Assertion` + `X-Aizen-User-Id`), exactly as the jobs query already does. **Never take the provider
id from the request body or query string** — that is an authorisation bypass, and it is how one provider reads
another's offers.

Offer mutations (`create`, `update`, `withdraw`) must verify that the offer belongs to the calling provider.
Check whether they already do; if not, that is a live IDOR and fixing it is part of this phase — say so plainly.

---

## PART B — MarineProvider BFF

New controller(s) alongside `ProviderJobsController`, same shape as everything else:

- `GET /provider/service-requests/open` → A1
- `GET /provider/service-requests/{id}` → detail (module `GET /{id}`), but only for a request the provider may see
  (open, or one they have an offer on, or one assigned to them). A provider must not be able to read an arbitrary
  request by guessing an id.
- `GET /provider/offers` → A2
- `POST /provider/service-requests/{id}/offers` → create
- `PUT /provider/offers/{offerId}` → update
- `POST /provider/offers/{offerId}/withdraw` → withdraw
- Job lifecycle for the existing jobs screen: accept/start/complete + work logs, mapped onto the module's
  Assignment/Completion/WorkLog controllers.

**Every handler resolves identity first** (`IProviderProfileResolver.ResolveAsync`) and **fails closed** when the
profile id or user id is missing. Skipping this is the single most repeated bug in this codebase: the holder stays
empty, the module sees `UserId = 0`, and the call is silently rejected or — worse — attributed to nobody.

Two traps this repo has already been bitten by, do not repeat them:

- **No `System.Text.Json.JsonElement` on any wire contract.** MVC binds with Newtonsoft, `AizenRemoteCall`/Refit
  writes with STJ; a `JsonElement` binds to `default` and the payload vanishes with no error. Use typed properties.
- **A rejection can arrive as HTTP 200 with `success: false` inside a successful envelope.** Surface the module's
  real business message (`header.errorMessage`) instead of a generic string — `AttachOnboardingDocumentCommandHandler`
  shows the pattern.

---

## PART C — provider-web

The shell, navigation, i18n and the dashboard tiles are already in place; these screens replace `PlannedPage`.

### C1. Service requests (discovery) — `/app/service-requests`

List of open requests with the filters the query supports (category, city, date). Each row opens a detail page.
Empty state must be honest: "no open requests matching your categories" is different from "you have no categories
set" — the second one links back to onboarding.

### C2. Request detail + bidding — `/app/service-requests/:id`

Everything the provider needs to decide, plus the offer form (price, estimated duration, note, proposed dates).
If the provider already has an offer on this request, show it and allow edit/withdraw instead of a second create.
Attachments open through **short-lived signed URLs minted per click** — the same rule as onboarding documents:
never store a URL, never pre-fetch one for a list.

### C3. Offers — `/app/offers`

The provider's own offers grouped by status. An accepted offer links to the job it became.

### C4. Jobs — `/app/jobs` (already real, extend it)

The list exists. Add the detail page and the lifecycle actions the BFF now exposes (accept, start, log work,
submit completion), each with optimistic-free, server-confirmed state — check `success` in the body, not just the
envelope.

### C5. Dashboard

Replace the `planned` tiles for **Open offers** and **Service requests** with real counts from the new endpoints.
Leave the rest as they are. Do not fabricate a number for anything that still has no backend.

---

## Acceptance — in a real browser

Write `docs/provider-service-requests-report.md` with real HTTP statuses and payloads.

1. A customer publishes a request → it appears in the provider's **open** list; a request outside the provider's
   categories/region does not.
2. The provider bids → the offer appears in `/app/offers` as `Submitted`, and the request disappears from the open
   list (already bid on).
3. **Cross-provider isolation:** provider B cannot see, edit or withdraw provider A's offer — by API, not just by
   UI. Try it with a raw request and paste the response.
4. A provider cannot read an arbitrary `serviceRequestId` they have no relationship with.
5. The customer accepts the offer → the provider sees a job; the offer shows `Accepted` and links to it.
6. The provider runs the job lifecycle to completion.
7. A rejected/failed action shows the **server's** reason, not a generic message.
8. Dashboard counts match the lists.
9. **Regression:** onboarding, document upload and the approval flow still work end to end.

`curl` will not catch the class of bug that keeps appearing here. If you cannot drive a browser, **say so in the
report** rather than substituting curl and calling it verified.

## Constraints

- Provider identity comes from the BFF assertion. Never from the body, never from a query param.
- Fail closed. A missing `UserId` is a rejection, not `0`.
- Signed URLs: minted per click, used immediately, never stored.
- No `JsonElement` on wire contracts. No fabricated identifiers. No silent no-ops.
- Geo/radius belongs to GeoDiscovery, not here.
- Realtime is out of scope — the screens must be fully usable with plain REST + refetch.
- If something cannot be finished, leave the TODO **and say so in the summary**.
