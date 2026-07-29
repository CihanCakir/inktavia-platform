# BE-P9 — iyzico auth-mode policy + pre-send split-math guard + item-level approve/disapprove/PUT-item + **SANDBOX SPLIT TEST = PRODUCTION GATE** — Backend Prompt

> **Module:** `Aizen.Modules.Payment` (gateway extension). **Phase:** Payment P9 — **the production gate.**
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §10 (iyzico split), §21.3–21.4 (auth-mode: Capture default /
> PreAuth optional; 25-day PostAuth ceiling), `PAYMENT_MODEL_DECISION`.
> **Rule:** EXTEND the existing `IyzicoMarketplacePaymentGatewayProvider` + `IyzicoHttpClient`; do NOT rewrite the working
> checkout/approve/refund. Inspect first.
> **Two parts:** **P9-code** (auth-mode + pre-send split guard + item-level ops + mock-client tests — no external keys) and
> **P9-gate** (LIVE sandbox split verification — needs the user's real iyzico sandbox ApiKey/SecretKey; Claude Code cannot
> complete the live gate without them, so build + mock-verify now and document the live procedure).

## 0. Verified current state
- `IyzicoHttpClient`: `InitializeCheckoutFormAsync` (single-basket; basketItem carries `SubMerchantKey` +
  `SubMerchantPrice`=providerNet; `Price`=`PaidPrice`=gross), `RetrieveCheckoutFormAsync`, `CreateSubMerchantAsync`,
  `ApproveMarketplacePaymentAsync` (whole-payment approve by `PaymentTransactionId` = escrow release/split), `RefundAsync`.
- `IyzicoConfiguration`: `ApiKey`/`SecretKey`/`BaseUrl`(=`https://sandbox-api.iyzipay.com`)/`WebhookSecret`/`CallbackUrl`/
  `Locale`. **appsettings keys are placeholders** (`sandbox_api_key_placeholder`).
- BE-P8 now produces the immutable snapshot with `CustomerTotalAmount` (charge) + `ProviderNetTotal` (split) and creates
  the escrow via this gateway. BE-I1 guarantees the recipient is `IsSplitEligible` (has a `SubMerchantKey`) before escrow.
- **Missing (this phase):** auth-mode policy, a pre-send split-math guard, item-level approve/disapprove/`PUT /payment/item`,
  and the live sandbox split verification.

## 1. Auth-mode policy (§21.3–21.4)
- `PaymentAuthMode` enum `{ Capture=1, PreAuth=2 }`. **Default = Capture** (funds captured into the protected pool at
  acceptance; released by approve after completion/approval — matches the marine 7-day window > 25-day PostAuth ceiling).
  PreAuth = optional per-offer/category policy (funds reserved, PostAuth within iyzico's 25-day BKM ceiling).
- Resolve the mode from a small **admin-configurable policy** (per category / default), NOT hardcoded — reuse the
  effective-date/single-active pattern if a table is warranted, else a config-backed default with a category override map.
  Store the resolved `AuthModeSnapshot` on the transaction/economics snapshot for audit.
- **MVP:** Capture path is the one exercised by the gate; PreAuth wired as selectable + a guard that a PreAuth escrow must
  PostAuth within the configured ceiling (a job/reminder hook — actual PostAuth job can be a documented follow-up). Do not
  block MVP on PreAuth.

## 2. Pre-send split-math self-verification guard (zero tolerance) — the safety net
Before ANY checkout/split call is sent to iyzico, assert (in the gateway, from the P8 snapshot figures):
```
Price == PaidPrice == CustomerTotalAmount           (what the customer is charged)
Σ basketItem.Price == CustomerTotalAmount            (basket sums to the charge)
Σ basketItem.SubMerchantPrice == ProviderNetTotal    (split to the provider sub-merchant)
Retained (main merchant) == CustomerTotalAmount − ProviderNetTotal == TransactionCommission + PlatformFeeGross
SubMerchantKey present for every split line (BE-I1 IsSplitEligible)
```
Any mismatch → **do NOT call iyzico**; throw `IyzicoSplitMathMismatch` (new error code) + log the exact figures. This
guarantees a mis-split can never reach the gateway even if upstream regresses. Unit-tested with synthetic snapshots.

## 3. Basket construction from the snapshot
- **MVP (single sub-merchant, full release):** one offer = one provider = one sub-merchant → a **single basket item** with
  `Price = CustomerTotalAmount` and `SubMerchantPrice = ProviderNetTotal` is correct and simplest; retained =
  commission + platform fee stays with the main merchant. Keep the current single-basket path but feed it the **snapshot**
  figures (CustomerTotal / ProviderNetTotal), not `offer.TotalAmount`.
- **Item-level (extension, for partial/dispute/change-order):** a **multi-item basket** built from the S8 line snapshots
  (each eligible line → basketItem with `SubMerchantPrice = line ProviderNet`; commission-exempt/pass-through and the
  platform fee → main-merchant retained). Enables `item/approve` per line. Build the multi-item builder + client methods
  now; **MVP gate uses full-release single-approve** (canonical §21: MVP full-release, partial fast-follow).

## 4. Item-level operations on `IyzicoHttpClient` (§10, §21)
Add client methods + typed request/response models (never `object`):
- `ApproveItemAsync` → `POST /payment/iyzipos/item/approve` (per basket item — partial/native).
- `DisapproveItemAsync` → `POST /payment/iyzipos/item/disapprove`.
- `UpdateSubMerchantShareAsync` → `PUT /payment/item` (change provider share — for change-orders/partial per §20.13/§21).
- (PreAuth) the auth/postauth pair if PreAuth mode is selected.
Wire `ReleaseEscrowAsync` to use item-level approve when a multi-item basket was used, whole-payment approve otherwise
(keep the existing `ApproveMarketplacePaymentAsync` for the single-basket MVP path). Idempotent; SigV3/PKI auth via the
existing client signing.

## 5. Idempotency + webhook (reuse)
Keep the existing `ConversationId=IdempotencyKey` + `ProcessedGatewayEvent` webhook idempotency. A duplicate approve/refund/
item-approve must be a no-op. No double release.

## 6. `PaymentErrorCode` additions
`IyzicoSplitMathMismatch = 5093`, `IyzicoItemApproveFailed = 5094`, `IyzicoItemDisapproveFailed = 5095`,
`IyzicoUpdateShareFailed = 5096`, `PaymentAuthModeInvalid = 5097` (align with the next free block after I1's 5090–5092).

## 7. Tests — P9-code (mocked `IyzicoHttpClient`, no external keys)
- **Pre-send guard:** correct snapshot → passes + basket built with Price=CustomerTotal, SubMerchantPrice=ProviderNet;
  tampered (SubMerchantPrice≠ProviderNet, or Σbasket≠CustomerTotal) → `IyzicoSplitMathMismatch`, **no client call**.
- **Auth-mode:** Capture default resolved; PreAuth selectable via policy; AuthModeSnapshot recorded.
- **Single vs multi basket:** single-basket MVP full-release approve; multi-item basket → item-level approve per line;
  Σ line SubMerchantPrice == ProviderNetTotal.
- **Item ops:** approve/disapprove/PUT-item call the right endpoint with typed payloads (mocked responses); failures →
  the mapped error codes; idempotent duplicate = no-op.
- **Split composition:** retained == commission + platform fee; matches the P8 snapshot exactly.
- **No regression:** existing checkout/approve/refund + BE-P8/I1 tests green.

## 8. P9-gate — LIVE sandbox split verification (PRODUCTION GATE, needs user's sandbox keys)
Document + provide a runnable procedure (executed once the user supplies real `Iyzico:ApiKey`/`Iyzico:SecretKey` for the
sandbox, e.g. via `appsettings.Local.json` or env/secrets — **not committed**):
1. Register a test provider sub-merchant (BE-I1 `RegisterSubMerchant`) in sandbox → real `SubMerchantKey`.
2. Accept a test offer → BE-P8 snapshot (e.g. Service 5000 @STANDARD 0.12 → commission 600, providerNet 4400; +Travel 800
   exempt → providerNet 5200; platform fee via P3) → CustomerTotal.
3. Complete the sandbox checkout form payment (test card) → webhook → escrow captured.
4. `ReleaseEscrow` (approve) → **verify on the iyzico sandbox dashboard + API:** `price == CustomerTotal`,
   `Σ subMerchantPrice == ProviderNetTotal (5200)`, `retained == commission + platform fee`, funds shown against the
   provider sub-merchant. Refund path: `Refund` returns funds correctly.
5. **GATE PASS criterion:** the sandbox split figures equal the snapshot figures to the cent; the pre-send guard never
   fired on a correct case and always fires on a tampered one. Only then is the marketplace flow production-eligible.
> Claude Code: build steps 1–5 as a documented manual/integration checklist + an optional `[Trait("Category","LiveSandbox")]`
> integration test that is **skipped unless real keys are present** — do NOT hardcode keys, do NOT commit secrets.

## 9. Acceptance criteria
- Auth-mode policy (Capture default / PreAuth optional, admin-configurable, mode snapshotted); **pre-send split-math guard
  makes a mis-split unsendable** (zero tolerance); item-level approve/disapprove/`PUT /payment/item` client methods +
  multi-item basket builder present; MVP full-release single-approve path fed by the **snapshot** figures (CustomerTotal /
  ProviderNetTotal), not `offer.TotalAmount`.
- P9-code fully built + mock-tested (no external keys); **P9-gate documented as a runnable sandbox procedure + a
  keys-gated skipped integration test** — the live GATE is completed by the user supplying sandbox credentials.
- Idempotent; existing gateway/webhook + BE-P8/I1 untouched-except-extended; build clean. No CargoDry.

## 10. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration (if any) append-only.
3. Tests green (paste): pre-send guard pass/fail (no call on mismatch), auth-mode, single/multi basket + item ops (mocked),
   split composition == snapshot, idempotency, no regression.
4. **State the P9-gate status explicitly:** "LIVE sandbox split test NOT run — awaiting real iyzico sandbox keys" (or, if
   the user provided keys, paste the sandbox split verification: price/subMerchantPrice/retained == snapshot).

## 11. Report
`REPORT_BACKEND.md` ("BE-P9"): auth-mode policy + pre-send split-math guard + item-level approve/disapprove/PUT-item +
multi-item basket builder + snapshot-fed single-basket MVP path + mock tests + the **keys-gated live sandbox procedure**.
Note: **the production GATE (live sandbox split verification) requires the user's iyzico sandbox ApiKey/SecretKey** — flag
it as the one remaining external step; PreAuth PostAuth job = follow-up; partial/item-level release exercised end-to-end in
P10 (refund/dispute). Next: **BE-P10 (refund allocation + chargeback/clawback)** and/or wire the deferred fast-follow (S6
line discount funding + P6/P7 application into P8). Do not touch CargoDry.
