# 00 — Phase 0: Product Decisions & Missing Producers

Phase 0 exists because two things the discovery screen wants **have no producer**. Not "no field" — no producer.
A column nobody writes is worse than a missing column: it looks like data, renders as emptiness, and nobody can
tell which.

This document designs the producers. Nothing in Phase 1 starts until the decisions below are taken.

| # | Missing capability | Consumer that needs it | Producer to build |
|---|---|---|---|
| 1 | **Provider service area** | KPI "in my service area", realtime relevance, distance origin | Onboarding (Identity) |
| 2 | **Budget** | Discovery card budget range, budget filter | Owner request creation (ServiceRequest) |
| 3 | **`ExpiresAt` meaning** | "offer deadline" on the card | A decision, not a field |
| 4 | *(adjacent)* **Provider categories** | Realtime relevance, eligibility | Onboarding (Identity) |

---

## 1. Provider service area — the capability that makes "my area" true

### Why it must exist

Today the provider has `City` (a plate code) and nothing else. Every consequence of that shows up as a lie the UI
would have to tell:

- The KPI cannot say **"in your service area"** — we do not know the area. Doc 07 therefore returns a
  `LocationMode` code and the UI says "Nearby" / "In map area" / "In my city". Honest, but a workaround.
- **Distance is measured from the phone**, not from where the provider works. A provider in Bodrum having coffee
  in İzmir sees İzmir jobs as "nearby". The number is real and the meaning is wrong.
- **Realtime relevance is city-only.** A provider serving Bodrum + Marmaris (both in `48`) and one serving only
  Fethiye (also `48`) get identical notifications.
- **Location permission denial** degrades the whole screen, because the device is our only origin.

Once a service area exists, all four fix themselves — and browser geolocation becomes what it should have been
all along: a **convenience** for the map, not the foundation of the feature.

### Model (Identity module)

Keep it in Identity — this is provider profile data, not geography-as-a-service. Live geo *search* still belongs
to GeoDiscovery; *storing where a provider works* does not.

```
ProviderServiceAreaEntity            (1 row per provider profile)
  ProviderProfileId       long       PK/FK
  BaseLatitude            decimal?   the yard / office / home marina
  BaseLongitude           decimal?
  ServiceRadiusKm         int?       how far they will travel  (cap: e.g. 300)
  IsMobile                bool       false = they only work at their own yard

ProviderServiceAreaCityEntity        (N rows — the cities they cover)
  ProviderProfileId       long
  CityCode                string     canonical plate code, ReferenceData-validated
```

Two representations because they answer different questions, and collapsing them would break one of them:

- **Cities** answer *"should this provider hear about this request?"* — a set membership test, indexable, and
  the exact shape the realtime group mechanism already uses (`city:{code}`). It works with no coordinates at all.
- **Base point + radius** answers *"how far is this job from them, and is it worth their drive?"* — distance,
  sorting, the radius filter.

A provider may have cities without a base point (they picked "İzmir, Muğla" and skipped the map). The system must
work in that state: city matching yes, distance no. **Partial data is the normal case, not an error state.**

Polygons are **out of scope**. When they arrive, they arrive in **GeoDiscovery**, and that is one of the recorded
migration triggers.

### Onboarding (the producer)

Extend the existing `OperatingRegion` step — it already exists, is already server-persisted, and already writes
`UserProfile.City`.

- **Cities served**: multi-select, ReferenceData-backed, **required**, at least one. (Today's single-select
  becomes the first entry — see backfill.)
- **Base location**: optional map pin or "use my current location" → `BaseLatitude/Longitude`.
- **Service radius**: slider (e.g. 10–300 km), enabled only when a base point exists.
- **Mobile / fixed**: does the provider travel to the vessel, or does the vessel come to them?

Validation is server-side, fail-closed, and identical in spirit to the city-code rule we just enforced: unknown
city code ⇒ **reject**; radius out of range ⇒ reject; a radius with no base point ⇒ reject (it means nothing).

### Backfill

Every existing provider has exactly one `City`. Backfill it as their single service-area city. **Do not invent a
base point or a radius** — leave them null, and let those providers fill them in. A guessed coordinate is worse
than a null one: null is visible, a wrong coordinate silently mis-sorts every job they see.

### What it unlocks (and what must change when it lands)

| Consumer | Before | After |
|---|---|---|
| KPI label | `LocationMode` code, "in my city" | a fourth mode: **`ProviderServiceArea`** — and only then may the UI say "in your service area" |
| Distance origin | browser only | **persistent base point**, with the browser as an override for "what's near me right now" |
| Realtime groups | `city:{profile.City}` — one city | join **every** city in the service area: `city:35`, `city:48`, … (the hub already decides groups server-side; this is a loop, not a redesign) |
| Location denial | degrades the screen | barely matters — the area is known without the device |

**Note the realtime consequence:** `ProviderRealtimeHub.OnConnectedAsync` currently joins one city group. With a
service area it joins N. That is a small change *and* the first time a provider hears about work in a city they
did not literally register in — verify it end to end with the two-replica test, because the group key is the one
thing that has silently broken this feature twice already.

---

## 2. Budget — DECIDED: NO (2026-07-14)

**We do not ask the owner for a budget. No columns, no DTO fields, no card section, no placeholder.**

The mock shows `₺18.000 – ₺25.000`; nothing in the system writes it, because the owner is never asked. The
question was never "add three columns?" — it was **"do we ask the owner for a budget?"** The answer is no:

- A published budget **anchors every offer to the ceiling.** Providers price to the stated maximum rather than to
  the work. With supply still thin, publishing the buyer's maximum is a straight transfer from the buyer — our
  primary customer — to the seller.
- **Owners cannot state one.** Antifouling on a 45 ft hull varies threefold with hull condition and paint. Asking
  yields a made-up number or a blank field, and a blank field just greys out half the card.
- **It is not the information the provider needs.** Vessel type/length, category, urgency, marina and distance
  determine whether a job is worth the drive. A budget replaces none of them.
- It also pulls the product toward "cheapest bid wins", which is not what this brand sells.

**Instead (post-MVP, `08_IMPLEMENTATION_ROADMAP.md`): a market price range** derived from historical offers —
"recent offers in this category, on vessels this size, ranged ₺14.000–19.000" — shown to **both sides**. It
informs the owner without whispering a ceiling to the supply side, and it improves with data, which an
owner-stated budget never does.

### If a future decision reverses this — the full chain, all of it or none

**Domain (ServiceRequest):**

```
BudgetMin           decimal?    nullable
BudgetMax           decimal?    nullable
BudgetCurrencyCode  string?     ReferenceData currency code, validated, rejected when unknown
```

**Validation** (FluentValidation, server-side):
- both absent — a request without a budget is normal and stays normal;
- or both present, with `BudgetMin <= BudgetMax` and a currency;
- **or** the documented partial rule: `BudgetMax` alone = "up to X" (a ceiling), which is the only partial form
  that means anything. `BudgetMin` alone is rejected — "at least ₺18.000" is not a thing a buyer says.
- Currency required whenever either bound is present. A number without a currency is not money.

**Producer:** `CreateServiceRequestCommand` / the owner edit command gain the three fields. **The owner app is the
producer** — if there is no owner-side surface to set them, we are back to dead columns and the answer is NO.

**Offer relation:** the budget is a **signal, not a cap**. An offer above `BudgetMax` is accepted; the UI may warn
the provider ("above the stated budget") and may show the owner that it is over. Enforcing it in the domain would
make the marketplace lie about what a job actually costs.

**Discovery contract:** `BudgetMin`, `BudgetMax`, `BudgetCurrencyCode` on the item DTO, nullable. Budget **filter**
is post-MVP — filtering on a field most rows lack mostly hides work.

**Display:** the card renders the range **only when present**, locale-formatted. Never `₺0`, never a dash styled
to look like data.

**Backfill:** none. Existing requests have no budget and never will. That is correct, not a gap.

---

## 3. `ExpiresAt` — one field, one meaning

The entity has **one** deadline (`ExpiresAt`). The design shows **one** deadline. Decide which it is and write it
down:

- **(a) Offer deadline** — "bids close at T". After T, no new offers; the request may still be assigned from the
  bids received. *This is what the discovery card implies, and it is the recommended reading.*
- **(b) Request expiry** — "if nothing happens by T, the request dies" (→ `Expired`).

They are not the same thing, and shipping both meanings in one column is how a field becomes untrustworthy. If
the product needs both, that is a second column **with a name that says which is which** — and a second
conversation, not a silent addition.

---

## 4. Provider categories (adjacent — decide with the service area)

Identity has **no provider category list**, so realtime cannot filter by trade: a provider in `35` is notified
about every job in `35`, including work they do not do. The `ServiceCapabilities` onboarding step already collects
something like this on the **client**; it does not reach a queryable profile table.

Either promote it to a real, queryable `ProviderServiceCategoryEntity` (ReferenceData-validated codes) — and then
realtime and eligibility can be honest — or accept the noise and do not claim relevance in the UI. The one thing
we must not do is imply "jobs for you" while sending "jobs near you".

Same shape as the service area, same onboarding step family, same fail-closed validation. If both are being built,
**build them together** — they are one migration and one onboarding pass.

---

## Phase 0 exit criteria

- [ ] **Service area**: model approved · onboarding UX approved · backfill policy approved (cities from `City`;
      **no invented coordinates**) · the realtime multi-city group change is understood and scheduled.
- [x] **Budget**: **answered NO** (2026-07-14). No columns, no DTO fields, no card section. The market-range
      alternative is recorded as post-MVP in doc 08. This gate is closed — do not reopen it inside a migration.
- [ ] **`ExpiresAt`**: its single meaning is written into the domain (XML doc on the property) and doc 07.
- [ ] **Categories**: promoted to a queryable profile table, or explicitly deferred with the UI copy adjusted so
      it never promises relevance it cannot deliver.

## Sequencing

Service area and categories are **Identity + onboarding** work: they do not block the discovery backend, and
discovery does not block them. Run them in parallel with Phase 1, and land them **before Phase 5 (map)** so the
KPI can graduate from "in my city" to "in my service area" without a second frontend pass.

Budget must be decided **before Phase 1**, because Phase 1 owns the migration.

## Documents to update once these decisions land

`05` (matrix: budget row, service-area rows) · `06` (a fourth `LocationMode`: `ProviderServiceArea`; distance
origin) · `07` (DTO fields, `LocationMode` enum, KPI label) · `08` (Identity/onboarding phase) · `09`/`10` (the
prompts) · `11` (checklist: no invented coordinates; multi-city realtime groups verified).
