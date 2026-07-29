# BE-I1 — Provider sub-merchant onboarding lifecycle + **split-eligibility gate** (P9 prerequisite) — Backend Prompt

> **Primary module:** `Aizen.Modules.Payment` (the sub-merchant profile + iyzico registration already live here — code
> stays here; Identity only supplies `ProviderProfileId`, which is already referenced). Same precedent as S7/S8 (an
> SR-roadmap phase built in Payment). **Phase:** Identity I1 (roadmap `docs/V1.0.1/Identity/ROADMAP.md`) — **hard
> prerequisite for Payment P9** (no marketplace split without a registered, split-eligible sub-merchant).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §21.3–21.4 + `PAYMENT_MODEL_DECISION` (each provider = iyzico
> alt üye işyeri; customer funds split to the provider's sub-merchant).
> **Rule:** EXTEND the existing `ProviderPaymentProfileEntity` + `RegisterSubMerchantCommand`; do NOT rewrite them, do NOT
> duplicate sub-merchant storage in Identity. Inspect first.

## 0. Verified current state (most of onboarding already exists — scope is the GATE + lifecycle)
- `ProviderPaymentProfileEntity` (Payment.Domain/Entities/PaymentProfile) already stores: `SubMerchantKey`,
  `SubMerchantAccountId`, `IbanEncrypted`/`IbanLast4`, `LegalName`, `TaxNumber`, **raw string `Status` ("Active"/"OnHold"/
  "Blocked")**, `VerifiedAt`; methods `RegisterSubMerchant(key, accountId)` (sets `VerifiedAt=UtcNow`), `UpdateIban`,
  `UpdateProfileAndResetVerification` (→ Status "OnHold", VerifiedAt null), `Suspend/Reactivate/Block`. 1:1 with provider.
- `RegisterSubMerchantCommand(Handler)` already **calls iyzico** `CreateSubMerchantAsync(IyzicoSubMerchantRequest{...KYC:
  address/contact/email/gsm/iban/taxOffice/taxNumber/legalTitle, subMerchantType PERSONAL|PRIVATE_COMPANY|
  LIMITED_OR_JOINT_STOCK_COMPANY, currency TRY})`, idempotent (returns existing key), persists the profile, error
  `SubMerchantRegistrationFailed`.
- `UpsertProviderPaymentProfile`, `GetProviderPaymentProfile` exist. iyzico gateway split uses `SubMerchantKey` +
  `SubMerchantPrice` (comment: "SubMerchantKey must be pre-registered before payments").
- **THE GAPS (this phase):**
  1. **No split-eligibility gate.** Nothing prevents an offer from a provider WITHOUT a sub-merchant key from being
     accepted → BE-P8 would create an escrow whose `SubMerchantPrice` is null → **the split silently doesn't happen and
     funds land on the main merchant.** This is the critical P9 safety gap.
  2. **`Status` is a raw string with no onboarding lifecycle** and `RegisterSubMerchant` sets `VerifiedAt` immediately —
     there is no explicit state machine or derived split-eligibility.
  3. KYC document flow (roadmap) — MVP-light: iyzico performs its own sub-merchant KYC; model a completeness/state signal,
     defer document-upload UI to FE.

## 1. Onboarding lifecycle on `ProviderPaymentProfileEntity` (§21.3)
Add `ProviderSubMerchantOnboardingStatus` enum (Payment.Abstraction/Enum):
`NotStarted=0, DataSubmitted=1, SubMerchantCreated=2, Verified=3, Rejected=4, Suspended=5, Blocked=6`.
- Add `OnboardingStatus` (enum, `HasConversion<int>()`) to the entity; **keep the legacy string `Status` as a mirrored
  display/legacy field** (like `MonthlyPriceTRY` in P4) — map transitions to both, do not break existing readers.
- Transitions (guarded domain methods, illegal transition → `AizenBusinessException(ProviderSubMerchantInvalidTransition)`):
  `SubmitOnboardingData()` (NotStarted/Rejected → DataSubmitted), `MarkSubMerchantCreated(key, accountId)` (DataSubmitted →
  SubMerchantCreated; folds the existing `RegisterSubMerchant`), `MarkVerified()` (SubMerchantCreated → Verified, sets
  `VerifiedAt`), `Reject(reason)` (→ Rejected), `Suspend()/Block()` (→ Suspended/Blocked), `Reactivate()` (Suspended →
  its prior active state). `UpdateProfileAndResetVerification` → back to DataSubmitted (re-verify).
- **`IsSplitEligible`** (computed, the single source of truth for the gate): `SubMerchantKey` non-empty **AND**
  `OnboardingStatus ∈ {SubMerchantCreated, Verified}` **AND** not `{Suspended, Blocked, Rejected}`. (Document the choice:
  in iyzico's model a created sub-merchant can already receive split; real KYC "Verified" is iyzico-side. So
  SubMerchantCreated is split-eligible unless suspended/blocked — flag `MarkVerified` as the point where our own KYC
  review, if any, completes. Make the exact eligible-set a **documented decision**, default = {SubMerchantCreated,
  Verified}.)

## 2. Wire the existing registration into the lifecycle
- `RegisterSubMerchantCommandHandler`: on successful iyzico create → call `MarkSubMerchantCreated(key, accountId)` (instead
  of the bare `RegisterSubMerchant`), so status advances to `SubMerchantCreated` (split-eligible). Keep idempotency
  (existing key → return, no state regression). No behavior change to the iyzico call itself.
- Add `SubmitOnboardingData` path (or fold into `UpsertProviderPaymentProfile`): capturing KYC data moves NotStarted →
  DataSubmitted. Admin/verification `MarkVerified`/`Reject` commands (admin-only) for the review step (FE later).

## 3. Split-eligibility gate (the P9-enabling deliverable)
- **Query** `GetProviderSplitEligibilityQuery(providerProfileId) → { IsSplitEligible, OnboardingStatus, Reason? }` (pure
  read) + expose via the internal `IPaymentModuleRemoteCall` (mirror escrow/economics pattern, typed DTO, `[Authorize]`
  service-to-service) so ServiceRequest can check it.
- **Enforce at BE-P8's calculate/create-escrow path (extend, do not rewrite P8):** before creating the escrow with a
  split, assert the recipient provider `IsSplitEligible`. If not → **do NOT create escrow/snapshot**; return a decision/
  error `ProviderNotSplitEligible` so the SR acceptance gate blocks the offer (consistent with P8's Rejected handling — no
  half-accepted offer). This guarantees P9 never runs a split against a non-sub-merchant.
- **Earlier surfacing (preferred UX):** also expose the eligibility so ServiceRequest can block/flag **offer submission**
  by a non-eligible provider (a provider who can't be paid shouldn't be biddable). MVP: enforce at acceptance (hard gate);
  offer-submission warning optional. Document which is enforced.

## 4. `PaymentErrorCode` additions
`ProviderSubMerchantInvalidTransition = 50XX`, `ProviderNotSplitEligible = 50XX`, `ProviderSubMerchantOnboardingIncomplete
= 50XX` (pick the next free block after P8's 5080–5082, e.g. 5090+).

## 5. Persistence + migration (append-only)
- Add `OnboardingStatus` (int) column to `provider_payment_profiles` + backfill: rows with a non-empty `SubMerchantKey` →
  `SubMerchantCreated` (or `Verified` if `VerifiedAt` set); rows without a key → `NotStarted`; "Blocked"/"OnHold" strings →
  `Blocked`/`Suspended`. Keep the legacy `Status` string mirrored. Append-only, idempotent, reversible.
- EF config update; DbSet unchanged; repo gains `GetSplitEligibility`/`GetByProviderProfileId` (latter exists). DI.

## 6. Tests
- **Lifecycle:** NotStarted → DataSubmitted → SubMerchantCreated → Verified; illegal transitions throw; Suspend/Block/
  Reactivate; UpdateProfile resets to DataSubmitted.
- **IsSplitEligible:** key + SubMerchantCreated → true; no key → false; Suspended/Blocked/Rejected → false; Verified → true.
- **Registration wiring:** RegisterSubMerchant success → status SubMerchantCreated + IsSplitEligible true; idempotent
  re-call no regression.
- **Gate at P8:** accept an offer from a non-eligible provider → `ProviderNotSplitEligible`, **no escrow/snapshot, offer
  not half-accepted**; eligible provider → proceeds (existing P8 happy path still green).
- **Remote-call:** `GetProviderSplitEligibility` typed round-trip + `[Authorize]` 401 without service token.
- **Migration/backfill:** keyed rows → SubMerchantCreated/Verified; keyless → NotStarted; legacy string mirrored.

## 7. Acceptance criteria
- Explicit sub-merchant onboarding lifecycle on the existing `ProviderPaymentProfile` (enum + guarded transitions), legacy
  `Status` mirrored; `RegisterSubMerchantCommand` advances it; `IsSplitEligible` is the single computed gate signal.
- **BE-P8 blocks acceptance of an offer from a non-split-eligible provider** (`ProviderNotSplitEligible`, no escrow/
  snapshot) — so P9's split can never target a non-sub-merchant; eligibility exposed to SR via the internal remote-call.
- Code stays in Payment (extends existing entity/command); Identity supplies only `ProviderProfileId`; no duplicate
  sub-merchant store; iyzico registration call unchanged. Append-only migration + backfill; existing profile/escrow/P8
  paths green; build clean. No CargoDry.

## 8. Verify — run and PASTE output
1. `dotnet build` Payment + ServiceRequest: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api service-request-api`; clean boot; migration + backfill
   applied.
3. DB: `SELECT "ProviderProfileId","OnboardingStatus","Status","SubMerchantKey" IS NOT NULL AS has_key,"VerifiedAt" FROM
   payment.provider_payment_profiles;` (backfill correct).
4. Tests green (paste): lifecycle transitions, IsSplitEligible matrix, registration wiring, **P8 gate reject (no escrow) +
   eligible passes**, remote-call round-trip + 401, migration/backfill.
5. Smoke: provider without sub-merchant → accept offer → `ProviderNotSplitEligible`, no escrow; register sub-merchant →
   eligible → accept → escrow with split created.

## 9. Report
`REPORT_BACKEND.md` ("BE-I1"): onboarding lifecycle + `IsSplitEligible` on existing ProviderPaymentProfile + registration
wiring + **BE-P8 split-eligibility gate** + `GetProviderSplitEligibility` remote-call + migration/backfill. Note: code lives
in Payment (Identity supplies ProviderProfileId); KYC document-upload UI = FE follow-up; admin verify/reject review = admin
FE; real iyzico KYC is iyzico-side. **Roadmap correction: "Identity I1" is implemented in the Payment module (sub-merchant
profile already lives there); update the roadmap index note.** Next: **BE-P9 (iyzico auth-mode + item/approve split +
sandbox split verification = PRODUCTION GATE)** — now unblocked by a split-eligibility guarantee (still needs iyzico
sandbox keys). Do not touch CargoDry.
