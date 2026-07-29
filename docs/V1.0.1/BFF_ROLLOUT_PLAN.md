# BFF Rollout Planı — Backend P1–P12 + SR + I1 → Provider & Admin BFF (dikey dilim disiplini)

> **Amaç:** Modül katmanında biten 12 fazın **provider BFF** (`Bff/src/MarineProvider/Aizen.Bff.MarineProvider`) ve
> **admin BFF** (`Bff/src/AdminPanel/Aizen.Bff.AdminPanel`) tarafındaki köprülerini **roadmap ve test senaryoları
> kapsamında** tamamlamak; **en son FE.** Her faz = **dikey dilim: Module (✅) → BFF → FE.**
> **Yeniden kullanılacak pattern'ler (varsayım değil, kodda mevcut):**
> - **Provider:** `Application/Payment/Query|Command/*Bff` + `Common/RemoteClients/IPaymentRemoteCall.cs`
>   (`[AizenRemoteCallGet/Put] /api/v1/payment/provider/*` → module) + `Controllers/V1/PaymentController.cs`. DTO'lar
>   `AizenApiResponse<...>` envelope. Şablon: **`GetProviderPaymentProfileBff` + `UpsertProviderPaymentProfileBff`**.
> - **Admin:** `Application/AdminPayment/Query|Command/*` + `Common/RemoteClients/IAdminPaymentBffRemoteCall.cs` +
>   `Controllers/V1/AdminPaymentController.cs` (+ `AdminFinance`, `AdminProfileApprovals`). Şablon (kodda hazır):
>   **CommissionRule CRUD** (`GetCommissionRulesPaged/ById/Stats/Create/Update/Deactivate/Reactivate`).
> **Kural:** additive, envelope korunur, mevcut dilimler yeniden yazılmaz; her yeni endpoint modülün gerçek
> REPORT_BACKEND.md DTO adlarına dayanır.

## 0. Mevcut BFF kapsamı (zaten bağlı — dokunulmaz, sadece genişletilir)
- **Provider `IPaymentRemoteCall`:** payouts(+summary), **payment-profile GET/PUT**, transactions, invoices(+byId,+pdf,+receipt),
  subscription, plans.
- **Admin `IAdminPaymentBffRemoteCall`:** transactions(+byId), escrow/capture/release/**refund**/cancel/reinstate/**reverse-refund**,
  refund-history, pending-payouts/mark-complete, provider+participant subscription (subscribe/get/cancel), plans,
  ResolveCommissionRate, **CommissionRule CRUD**, invoices. AdminPayment slice'ında 28+ command/query hazır.

## 1. Faz → BFF eşlemesi (eksik köprüler)

### Provider BFF (MarineProvider) — provider yüzeyleri
| Faz | Eksik BFF işi | Şablon |
|---|---|---|
| **I1** | `GetProviderPaymentProfileBff` DTO'ya `isSplitEligible/onboardingStatus/subMerchantType/subMerchantKeyMasked/ibanRequired/rejectionReason` ekle; `UpsertProviderPaymentProfileBff` tip-bazlı KYC alanları; `IPaymentRemoteCall` → BE-I1 module endpoint'i (`GetProviderSplitEligibility`/genişletilmiş profile) | mevcut payment-profile dilimi |
| **P4** | subscription/plans DTO'ya **launch vs list** fiyatı + yaklaşan değişim (ProviderPlanPrice) | GetProviderPlans/Subscription |
| **P8/S8** | transactions DTO'ya **snapshot kırılımı** (ServiceAmount/PlatformFee/CommissionBase/CommissionBenefit/funding) | GetProviderTransactions |
| **P10** | transactions/payouts DTO'ya **refund/chargeback durumu + negative-balance/clawback** alanları | GetProviderTransactions/Payouts |
| **P11** | **yeni** `PurchaseOfferBoostBff` command + `GetOfferBoostStatusBff` query (offers/premium) | yeni dilim + IPaymentRemoteCall |
| **S1/S6/S7** | **yeni** `GetOfferCommissionPreviewBff` + `GetOfferCustomerDiscountPreviewBff` (Offers slice → SR/Payment internal remote-call) — teklif-builder ekonomisi | Offers slice + remote-call |

### Admin BFF (AdminPanel) — admin yönetim ekranları
| Faz | Eksik BFF işi | Şablon |
|---|---|---|
| **P2** | CommissionRule CRUD ✅ (zaten var — conflict alanı DTO'da doğrula) | — |
| **P3** | **PlatformFeeRule CRUD** (paged/byId/create/update/deactivate) | CommissionRule CRUD |
| **P4** | **ProviderPlanPrice CRUD** (launch/list, effective-date, overlap/gap) | CommissionRule CRUD |
| **P5** | **ProfitProtectionPolicy CRUD** | CommissionRule CRUD |
| **P6** | **CustomerDiscountRule CRUD + CustomerBenefitBudgetPolicy CRUD** | CommissionRule CRUD |
| **P7** | **ProviderCommissionBenefitRule CRUD + entitlement grant/revoke** | CommissionRule CRUD |
| **I1** | **sub-merchant KYC queue + verify/reject** (AdminProfileApprovals veya AdminPayment) | AdminProfileApprovals |
| **P10** | **RefundAllocationPolicy CRUD** + **refund/chargeback queue** + **ProviderNegativeBalance ledger** görünümü | AdminPayment/AdminFinance |
| **P11** | **PremiumProduct/Price CRUD** | CommissionRule CRUD |
| **P12** | **Financial reporting** (`GetFinancialSummaryReport` + drill-down) → AdminFinance | AdminFinance query |

> Not: P2 CommissionRule admin CRUD BFF **zaten var**; sadece BE-P2'nin conflict/line-dim alanlarının DTO'da yansıdığını
> doğrula. P8b/S8 (economics) provider-facing kırılım dışında admin'e yeni endpoint gerektirmez (snapshot admin transaction
> detayında zaten görünebilir — doğrula).

## 2. Dalgalar (BFF; FE ile eşleşir, ama önce tüm BFF)
**BFF-Dalga 1 — I1 dikey dilimi (pilot):** Provider payment-profile eligibility genişletme + Admin sub-merchant KYC queue.
Bittiğinde FE_PROVIDER_I1 flag'i açılabilir hale gelir (kanıt).
**BFF-Dalga 2 — Admin rule CRUD (P3/P4/P5/P6/P7):** CommissionRule CRUD şablonunu 6 kurala çoğalt (düşük risk, mekanik).
**BFF-Dalga 3 — Provider okuma zenginleştirme (P4/P8/P10):** subscription launch-list + transaction snapshot kırılımı +
refund/negative-balance alanları (DTO genişletme, additive).
**BFF-Dalga 4 — Para geri akışı + premium (P10/P11):** Admin RefundAllocationPolicy + refund/chargeback queue +
negative-balance ledger; Premium CRUD; Provider boost command/status.
**BFF-Dalga 5 — Offer economics + raporlama (S1/S6/S7 + P12):** Provider offer commission/discount preview; Admin financial
reporting.
**Sonra:** FE dalgaları (`FE_ROLLOUT_STRATEGY.md`) — BFF hazır olduğu için FE sözleşmeleri ilk seferde tutar.

## 3. Her BFF dilimi için standart (şablon)
Yeni bir admin CRUD dilimi = CommissionRule CRUD'un birebir kopyası, entity adı değişir: (1) `IAdminPaymentBffRemoteCall`'a
`[AizenRemoteCall*]` metotları (module `/api/v1/payment/admin/<x>` endpoint'lerine); (2) `AdminPayment/Query|Command/<X>*`
handler + BFF DTO (`AizenApiResponse` envelope, typed body — `object` DEĞİL); (3) `AdminPaymentController` endpoint'leri
(`[Authorize(Roles=Admin)]`); (4) conflict/validation hatalarını BFF DTO'ya taşı (fail-loud UI için). Provider dilimi =
`GetProviderPaymentProfileBff` kopyası. **Her dilim modülün REPORT_BACKEND.md'sindeki gerçek endpoint/DTO adlarına hizalanır.**

## 4. Test senaryoları (her dalga)
- **Remote-call round-trip:** BFF → module endpoint doğru path/typed body; `AizenApiResponse` envelope korunur; audience/
  service-token doğru (provider by-subject; admin service-token).
- **Yetki:** admin endpoint'leri `[Authorize(Roles=Admin)]` (401/403 kanıtı); provider by-subject.
- **Additive/regresyon:** mevcut dilimler değişmez; genişletilen DTO'lar eski alanları korur; envelope-tolerant.
- **Fail-loud:** conflict/validation (CommissionRuleConflict, ProviderPlanPriceConflict, ProfitProtectionPolicyConflict,
  ProviderNotSplitEligible, RefundAllocationMismatch…) BFF üzerinden anlamlı hata olarak yüzeye çıkar.
- **Idempotency:** mutasyon endpoint'leri idempotency-key/typed body (BE ile aynı anahtar).

## 5. Kabul kriteri
- 12 fazın (+SR+I1) etkilediği provider+admin BFF köprüleri **mevcut pattern'lerle** eklenir; mevcut dilimler bozulmaz;
  her endpoint modül DTO'suna hizalı; audience/yetki doğru; conflict'ler fail-loud yüzeyde. Build 0 hata; mevcut BFF
  testleri yeşil. **FE ancak tüm BFF köprüleri hazır olunca** (FE dalgaları).

> **İlk icra:** BFF-Dalga 1 (I1 dikey dilimi). Sonra 2→5 sırayla; en son FE. Her dalga ayrı Claude Code kickoff'u, ilgili
> modül REPORT_BACKEND.md'sine dayanır.
