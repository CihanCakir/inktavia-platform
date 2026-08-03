# REPORT — FE_PROVIDER Wave B (finance transparency)

**Repo:** `inktavia-marine-provider-web` (FE only) · **Scope:** display-only extension of `src/features/finance/`
**Status:** code complete · typecheck clean · zero new lint errors · on-screen verification blocked by environment (see §5)
**Kickoff:** `docs/V1.0.1/Payment/FE_PROVIDER_WAVE_B_FINANCE_TRANSPARENCY.md`

Additive, null-safe, server-owns-every-total. No backend/BFF, admin, or CargoDry changes. P11 offer-boost **not** built
(separate purchase-flow kickoff — see §6).

---

## 1. Data path confirmed (why the fields reach the FE)

The provider BFF **passes the module DTOs straight through** — the BFF query/handler response type *is* the module
Abstraction DTO, so every Wave B field the module added (BFF-Wave 3/4) serializes to the FE untouched. Verified:

- `GetProviderPayoutsBffQuery : AizenQuery<ProviderPayoutPagedResultDto>` → handler returns `(...).Body` verbatim.
- Same shape for transactions and subscription. No BFF-local response model re-maps (and thus drops) fields.
- Module DTOs confirmed to carry the fields (`Modules/Payment/.../Abstraction/Dto/`):
  `ProviderTransactionDto.EconomicsBreakdown/DisputedAt/RefundSummary`,
  `ProviderPayoutPagedResultDto.NegativeBalance`, `ProviderSubscriptionDto.ActivePrice/HasUpcomingPriceChange/…`.

**Envelope tolerance:** `normalizeEnvelope` returns `body` as-is (no field stripping), so a missing/renamed field simply
arrives `undefined`. Every new FE field is declared optional and read through a null-safe selector → legacy rows render
exactly as before, never throw.

> **Correction vs. the kickoff wording:** `NegativeBalance` lives on the **paged result**
> (`ProviderPayoutPagedResultDto`), **not** on each `ProviderPayoutDto` row. Typed and read accordingly (on the payouts
> envelope, not the row).
>
> **Config note (not a defect):** `IPaymentRemoteCall` has no `BaseUrl` in the BFF `appsettings.*` files, but the
> deployment supplies it via env — the `bff-marineprovider` container sets
> `RemoteCalls__IPaymentRemoteCall__BaseUrl=http://payment-api:8080`. So provider finance works in the containerized
> stack; only a bare `dotnet run` from the host needs the env var passed explicitly.

---

## 2. Fields typed (additive) — `src/features/finance/api/financeApi.ts`

| DTO (BFF/module, PascalCase) | FE type (camelCase) | Notes |
|---|---|---|
| `ProviderTransactionEconomicsBreakdownDto` | `ProviderTransactionEconomicsBreakdown` | all 14 fields incl. `serviceVatAmount`, `commissionRate`, and the three discount-funding lines (`totalCustomerDiscount`, `totalProviderFundedDiscount`, `totalPlatformFundedDiscount`) |
| `ProviderTransactionRefundSummaryDto` | `ProviderTransactionRefundSummary` | incl. `cause?`/`releaseState?` + the three P10 clawback amounts |
| `ProviderBalanceSummaryDto` | `ProviderBalanceSummary` | `balance`, `negativeAmount`, `negativeBalanceLimit`, `isOverLimit`, `currencyCode` |
| `ProviderPlanActivePriceDto` | `ProviderPlanActivePrice` | `priceType` (`'Launch' \| 'List' \| string`), `priceAmount`, effective window, `priceCode?` |

Extended existing types additively (new fields all optional):
- `ProviderTransaction` → `disputedAt?`, `economicsSnapshotId?`, `economicsBreakdown?`, `refundSummary?`
- `ProviderPayoutPaged` → `negativeBalance?`
- `ProviderSubscription` → `activePrice?`, `hasUpcomingPriceChange?`, `upcomingPriceAmount?`, `upcomingPriceChangeAt?`

**Selectors** (`hooks/useFinance.ts`, envelope-tolerant, null-safe): `selectEconomicsBreakdown`,
`selectRefundSummary`, `selectNegativeBalance`, `selectActivePrice`.

---

## 3. Where each piece renders — `pages/FinancePage.tsx`

New display components (all after the shared `Field` helper): `DisputeChip` (L485), `EconomicsBreakdownSection`
(L497), `RefundBreakdownSection` (L543), `NegativeBalanceStrip` (L567). Helper `isDisputed(x)` guards a real
`DisputedAt` (`isEmptyDate`-aware).

### Part A — P8/S8 economics breakdown (headline "where the money goes")
- **TransactionDetail drawer** → `{breakdown && <EconomicsBreakdownSection …/>}` (L1168). Walks:
  Müşteri öder (`customerTotalAmount`) → Hizmet tutarı (`serviceAmount`) → Platform ücreti (`platformFeeGrossAmount`
  with a `net + VAT` sub-line) → Komisyon matrahı (`commissionBaseAmount`) → Komisyon (`commissionAmount`) →
  **Sana geçen net (`providerNetAmount`) as the gold takeaway.** `platformGrossShare` and the provider-/platform-funded
  discount lines render only when `> 0`. Existing gross/commission/VAT/net summary is kept above it (additive).
- **Legacy tx (no snapshot):** `breakdown` is null → section omitted, summary-only, no error.

### Part B — P4 launch-vs-list + upcoming change
- **SubscriptionTab** current-plan card → `{activePrice && (…badge…)}` (L1439): **"Lansman fiyatı"** (gold, Sparkles) when
  `priceType === 'Launch'` with a "kampanya dönemi" note, else **"Liste fiyatı"** (navy, Tag). Next to the amount.
- Upcoming notice → `{sub.hasUpcomingPriceChange && (…)}` (L1481): "Yenilemede fiyat değişecek: `{amount}` — `{date}`"
  (`upcomingPriceAmount` money-formatted, `upcomingPriceChangeAt` date-formatted; `—` when absent). Informational only.

### Part C — P10 negative balance + disputed / refund
- **PayoutsTab** → `{negativeBalance && <NegativeBalanceStrip …/>}` (L821, derived L781 from the payouts envelope):
  current owed (`negativeAmount`), `negativeBalanceLimit`, `isOverLimit` → **danger** state + "Limit aşıldı" chip and an
  offset/paused-payouts explanation; otherwise a warning-toned "offset first against future payouts" note.
- **Transactions — "İtirazlı" chip** driven by `DisputedAt`: desktop row status cell (L1065), mobile card badge (L1122),
  and drawer header next to the status chip (L1151). Drawer also adds an "İtiraz tarihi" field.
- **Refund breakdown** in the drawer → `{refundSummary && <RefundBreakdownSection …/>}` (L1171):
  Platformun karşıladığı iade (`platformAdvancedRefundAmount`), Senden mahsup edilecek (`providerRecoveryAmount`, shown
  as `−`), Kalan negatif bakiye (`remainingProviderNegativeBalance`) + a plain-language clawback note.

---

## 4. Null-safe / legacy handling (by construction)

- Every new field optional; every new section gated behind a presence check (`&&`) or a `select…() ?? null` selector.
- `isDisputed` treats `null`/`DateTime.MinValue`-ish dates as not-disputed (reuses `isEmptyDate`).
- Discount-funding and `platformGrossShare` lines are additionally value-gated (`> 0`) so a zero snapshot renders lean.
- No FE arithmetic on totals/shares anywhere — only `money()`/`dateLabel()` formatting of server decimals.
- Nautical tokens only: navy / gold (takeaway, launch) / warning (refund, negative balance) / danger (over-limit,
  dispute). No new design language.

**Static verification run:**
- `npm run typecheck` → **clean** (no output).
- `npx eslint` on the 3 changed source files → **5 errors, all pre-existing** `set-state-in-effect` on the tabs'
  `useEffect(() => setPage(1), …)` (baseline count == post-change count == 5). **Zero new lint issues introduced.**
- Both `finance.json` (tr/en) parse; new keys added with **full tr↔en parity**: `dispute.*`, `econ.*`, `refund.*`,
  `negBal.*`, `price.*`, `detail.disputedAt`.

---

## 5. On-screen verification — live transcript (provider-web, logged-in provider)

**Stack:** brought the down `bff-marineprovider` (:17002) + `bff-marineprovider-2` (:17012) containers up
(`docker compose up -d`); provider-web on :3002 → container BFF; logged in as **Provider 2 AS**. Finance route loads,
no console/network errors.

| Part | Branch | Result | Evidence |
|---|---|---|---|
| **B** | **Launch price (positive)** | ✅ **VERIFIED** | Abonelik tab, Standard 499 TRY/ay → gold **"Lansman fiyatı"** badge + **"kampanya dönemi"** note (`activePrice.priceType === 'Launch'`). No upcoming-change notice (provider's `hasUpcomingPriceChange=false` → correctly hidden). |
| **A** | Legacy (no snapshot) | ✅ **VERIFIED** | İşlemler → TXN-20260610-P202 (Serbest bırakıldı) drawer shows summary only: Brüt 18.500 → Komisyon −2.220 → KDV 444 → gold **Net hakediş 15.836 TRY**. No "Kazanç kırılımı" section, no error (tx has no `economicsBreakdown`). |
| **C** | Legacy (no ledger / not disputed) | ✅ **VERIFIED** | Ödemeler → no negative-balance strip (envelope `negativeBalance` null); İşlemler rows → no "İtirazlı" chip (no `disputedAt`). Clean null-safe render. |
| **A** | Economics breakdown (positive) | ⏳ not exercised | Provider 2's transactions carry no `EconomicsBreakdown` snapshot → section correctly absent. Needs seed. |
| **C** | Dispute/refund + negative balance (positive) | ⏳ not exercised | No disputed tx / refund allocation / negative-balance ledger row for this provider. Needs seed. |

**Conclusion:** the full FE mechanism (type → null-safe selector → render → BFF pass-through) is proven end-to-end by
**Part B's positive branch rendering live**, and all three parts' **legacy/null-safe paths render cleanly with no
regression**. Parts A/C positive branches share identical mechanics (same DTO family, same selectors, same `&&`-gated
render) and are unexercised only because this provider has no P8/P10 data — not a code gap.

**Seed to light up the remaining positive branches:** for one provider — (a) a `Released` service-escrow tx linked to a
`PaymentEconomicsSnapshot`; (b) a `RefundAllocation` (release-after) → a negative-balance ledger row over its limit +
`DisputedAt` on that tx. Then: İşlemler → open (a) ⇒ "Kazanç kırılımı" walks CustomerTotal→…→gold ProviderNet;
disputed tx ⇒ İtirazlı chip + refund breakdown; Ödemeler ⇒ over-limit negative-balance strip.

---

## 6. Next provider piece — **P11 offer-boost (NOT built here)**

P11 is a **purchase flow on the offers surface**, not finance display. BFF-Wave 4 already added the provider boost
command + boost-status; the FE work is its own kickoff (buy/confirm boost, show boost state on an offer). Deliberately
out of Wave B scope.

After Wave B, remaining admin options are P10 refund/chargeback queue + I1 sub-merchant KYC, and the P12 reporting
dashboard.

---

## 7. Live positive-branch verification (Parts A + C now exercised on-screen)

The two positive branches that §5 left **⏳ not exercised** (Part A economics breakdown, Part C dispute/refund +
over-limit negative balance) are now **✅ VERIFIED live** for **"PROVIDER 2 AS"** (`RecipientProfileId 100011`). This
was a **data-only** change — no product/FE/BFF/admin/CargoDry code touched. The gap in §5 was never a code gap (proven
by Part B rendering live + the null-safe legacy paths); it was purely the absence of P8/P10 data for this provider.

### 7.1 What was seeded (domain factories, NOT raw SQL)

New dedicated dev/local mock seeder `Modules/Payment/.../Repository/Seed/Provider2PositiveBranchMockSeed.cs`, wired as
**Phase 8** in `SeedPaymentAsync` (after `TransactionRefundMockSeed`) + registered in `DependencyInjection.cs`. It
follows the existing `TransactionRefundMockSeed`/`PayoutRecordMockSeed` pattern: idempotent (find-or-create on the demo
tx codes), demo-labelled, **no migration**. Provider 2 already had mock transactions, so this only adds two more +
their linked economics/refund/balance rows. Every record is built through its **guarded construction path** — never a
column INSERT:

- **Economics** → `PaymentEconomicsSnapshotEntity.CreateFromLines(...)` (the immutable, insert-only entity whose factory
  enforces the 8 §20.15 equalities with **zero tolerance**). Both snapshots passed the invariant check at startup — the
  seeder ran clean, no `PaymentEconomicsInvariantException`.
- **Refund allocation** → the pure `RefundAllocationCalculator.Resolve(...)` (9-amount §7.5 breakdown, verified sums)
  then persisted immutably via `RefundAllocationEntity.Create(...)`.
- **Negative balance** → `ProviderBalanceEntity.Create(...)` + `.Clawback(...)` domain methods (mirroring
  `RefundAllocationService`'s §7.3 release-after recovery: platform advances the provider-net reversal, claws it back
  into the ledger).
- **Dispute + refund state machine** → the real transitions, in order: `Capture → Release → LinkEconomicsSnapshot →
  Dispute() → ResolveDispute() → ApplyRefund()`. `Dispute()` sets `DisputedAt`; `ResolveDispute()` returns the tx to
  Released **without clearing `DisputedAt`** (so it survives while the tx is still refundable — the two guards are
  otherwise mutually exclusive).

**Part A — `TXN-20260610-P204`** (Released, `EconomicsSnapshotId` → `PES-20260802-…`, snapshot id 1). Line inputs to
`CreateFromLines` (single eligible service line, no service VAT, so the walk reads clean):

| input | value | | input | value |
|---|---|---|---|---|
| line gross (net service) | 19 000 | | platform fee rate | 5% |
| commission base | 19 000 | | platform fee net / VAT / gross | 950 / 190 / **1 140** |
| commission rate / amount | 12% / **2 280** | | customer-payable service | 19 000 |
| line provider net | **16 720** | | | |

Derived aggregates (all invariants hold): **CustomerTotal 20 140** = ProviderNet 16 720 + PlatformGrossShare 3 420;
ServiceAmount 19 000; PlatformFeeGross 1 140 (= 950 + 190). tx gross set to 20 140 to mirror the customer total.

**Part C — `TXN-20260610-P205`** (Refunded, disputed, `EconomicsSnapshotId` → snapshot id 2). Same line shape, smaller
numbers: line gross 12 000 @ 12% → commission 1 440, provider net 10 560; platform fee 5% = 600 net + 120 VAT = **720**
gross; **CustomerTotal 12 720** = 10 560 + 2 160. Then a **full release-after refund** (`REF-20260610-P205`, gateway
total 12 720, `RefundReason.DisputeResolvedForPayer`), allocation cause `DisputeCustomerFavoured`,
`ReleaseState.AfterProviderRelease`, platform-fee mode `Full`:

| refund allocation amount | value |
|---|---|
| ServiceRefund | 12 000 (= ProviderNetReversal 10 560 + CommissionReversal 1 440) |
| PlatformAdvancedRefundAmount | 10 560 |
| ProviderRecoveryAmount | 10 560 |
| RemainingProviderNegativeBalance | 10 560 |

Provider 2's `ProviderBalance` (TRY): limit **5 000**, clawed back **−10 560** → `NegativeAmount 10 560 > 5 000` →
**`IsOverLimit = true`**. Startup log confirmed: `balance -10560 (over-limit True)`.

### 7.2 On-screen transcript (provider-web :3002 → container BFF :17002, logged in as **Provider 2 AS**)

| Part | Branch | Result | Evidence (İşlemler / Ödemeler / Abonelik) |
|---|---|---|---|
| **A** | Economics breakdown (positive) | ✅ **VERIFIED** | `TXN-20260610-P204` (Serbest bırakıldı) drawer → **"Kazanç kırılımı"** walks **Müşteri öder 20.140 → Hizmet tutarı 19.000 → Platform ücreti −1.140 (Net 950 + KDV 190) → Komisyon matrahı 19.000 → Komisyon −2.280 → Platform brüt payı 3.420 → gold Sana geçen net 16.720 TRY**. Legacy summary (Brüt/Komisyon/KDV/Net) kept above it. |
| **C** | İtirazlı + refund breakdown (positive) | ✅ **VERIFIED** | `TXN-20260610-P205` row shows **"İtirazlı"** chip (+ "İade edildi"). Drawer → **İtiraz tarihi 02.08.2026**; **"İade / Mahsup kırılımı"**: Platformun karşıladığı iade **10.560**, Senden mahsup edilecek **−10.560**, Kalan negatif bakiye **10.560** + clawback note. (Its Kazanç kırılımı also renders — gold Sana geçen net 10.560.) |
| **C** | Over-limit negative balance (positive) | ✅ **VERIFIED** | **Ödemeler** tab → **Negatif bakiye 10.560 TRY** strip in **danger** state with **"Limit aşıldı"** chip, "Limit: 5.000 TRY", and the paused-payouts explanation. |
| **B** | Launch price (regression check) | ✅ **UNCHANGED** | **Abonelik** → Standard 499 TRY/ay still shows the gold **"Lansman fiyatı"** badge + "kampanya dönemi". |
| **A** | Legacy (no snapshot) | ✅ **NO REGRESSION** | Legacy Provider-2 txns (`P202` 18.500, `P201` 32.000) still render summary-only — no "Kazanç kırılımı" section, no error (they carry no `economicsBreakdown`). |

**Data path confirmed end-to-end:** the seeded snapshot/allocation/balance rows flow through the module read-models
(`GetProviderTransactionsQueryHandler` maps `EconomicsBreakdown` from the linked snapshot + `RefundSummary` from the
linked allocation; `GetProviderPayoutsQueryHandler` maps `NegativeBalance` from the provider balance) → BFF pass-through
→ the null-safe FE selectors → the `&&`-gated render — exactly as §1–§4 predicted, now with live data.

### 7.3 Removing the demo seed

The seed is dev/local + idempotent, so it is harmless to leave. To remove it:

1. Delete `Modules/Payment/.../Repository/Seed/Provider2PositiveBranchMockSeed.cs`, its `AddScoped<...>` line in
   `Repository/DependencyInjection.cs`, and the **Phase 8** block in `SeedPaymentAsync`, then rebuild `payment-api`.
2. On an already-seeded DB, drop the demo rows (all keyed on the two codes):
   `TXN-20260610-P204` / `TXN-20260610-P205` (transactions), their `PaymentEconomicsSnapshot` (ids 1–2 / codes
   `PES-…`), refund record `REF-20260610-P205` + its `RefundAllocation`, and Provider 2's `ProviderBalance` (TRY) row
   + its `ProviderBalanceMovement`. No other data references them.
