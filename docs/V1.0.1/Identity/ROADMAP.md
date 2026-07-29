# Identity Modülü — V1.0.1 Roadmap
> iyzico sub-merchant onboarding, provider origin/hizmet alanı, participant (müşteri) plan bağı.
> Kanonik: `../COMMISSION_PACKAGE_PRICING.md` + `PAYMENT_MODEL_DECISION`. Mevcut Identity (Provider/Participant/Venue)
> **genişletilir**, yeniden yazılmaz. Her faz → `BE_I<n>_*.md`.

## Backend fazları
| Faz | Kapsam | Kanonik § | Bağımlılık / Not |
|---|---|---|---|
| **I1** ✅ | **Provider iyzico sub-merchant onboarding** — KYC/belge akışı, sub-merchant oluşturma/güncelleme, **sub-merchant key ↔ provider profil** eşlemesi. Sub-merchant yoksa marketplace split **çalışmaz**. **→ Payment modülünde uygulandı (BE-I1): onboarding lifecycle + `IsSplitEligible` + BE-P8 split-eligibility gate.** | Payment Model Decision, §21.3–21.4 | Payment P9 **öncesi** hazır olmalı |
| **I2** | **Provider origin lokasyonu + hizmet alanı (service area)** yakalama — travel/mesafe fiyatlaması için. Hafıza notu: bugün provider'da yalnız City/Country var; **service-area onboarding'de eksik** → travel'ın gerçek önkoşulu. GeoDiscovery ertelendi; MVP için en azından origin nokta + yarıçap. | §20.8 | ServiceRequest S4 **öncesi** |
| **I3** | **Participant (müşteri) plan/üyelik bağı** — `CustomerDiscountRule.CustomerPlanId` ve `CustomerBenefitBudget` participant aboneliğine bağlanır. Müşteri aboneliği **Payment**'ta (`ParticipantPlanSubscription`); Identity participant profilini/kimliğini sağlar. | §19.6–19.7 | Payment P6 ile koordine |

## Durum / kapsam düzeltmesi (2026-07-28)
**İnceleme bulgusu:** sub-merchant onboarding'in çoğu **zaten Payment'ta kurulu** (Identity'de değil): `ProviderPaymentProfileEntity`
(SubMerchantKey/AccountId, IBAN encrypted, LegalName, TaxNumber, Status, VerifiedAt) + `RegisterSubMerchantCommand`
(iyzico `CreateSubMerchantAsync` çağırır, KYC alanları, idempotent) + `Upsert/GetProviderPaymentProfile`. **I1 kodu Payment'ta
kalır; Identity yalnız `ProviderProfileId` sağlar** (S7/S8'in SR-fazı ama Payment-kodu olması gibi). Gerçek boşluk dar ve P9-odaklı:
(1) **split-eligibility gate yok** → sub-merchant'ı olmayan provider'ın teklifi kabul edilebiliyor, P8 escrow'u `SubMerchantPrice=null`
ile açar → split sessizce olmaz, para ana merchant'a gider; (2) `Status` raw string, onboarding lifecycle yok; (3) KYC doküman akışı MVP-light.
- **BE-I1 ✅ TAMAM** (2026-07-28, 187 Domain +12 / 37 Repo / 30 SR.App test): `ProviderSubMerchantOnboardingStatus` enum + guarded transitions (illegal→`ProviderSubMerchantInvalidTransition` 5090, legacy Status mirror) + **`IsSplitEligible`** (key + status∈{SubMerchantCreated,Verified}) + `RegisterSubMerchant`→`MarkSubMerchantCreated` + admin verify/reject + **BE-P8 gate** (create-escrow handler idempotency guard'dan hemen sonra `IsSplitEligible` assert → `ProviderSplitEligible=false` response flag, snapshot/escrow yok → SR `ProviderNotSplitEligible` 5091 → transactional rollback) + `GetProviderSplitEligibilityAsync` remote-call (typed, [Authorize]) + migration `AddProviderSubMerchantOnboardingStatus` + backfill. error 5090-5092. **Design note:** split-block P8 response'unda ayrı flag (modül sınırında throw değil) → SR bunu profit-protection Rejected'dan ayrı yüzeye çıkarır. **Kod Payment'ta; Identity ROADMAP I1 ✅ (implemented in Payment).** P9 split-garantisiyle açık. Sonraki: **BE-P9** (iyzico sandbox key ön-koşulu senin tarafında).
- **BE-I1 promptu (arşiv)** (`BE_I1_SUBMERCHANT_ONBOARDING_GATE.md`): `ProviderSubMerchantOnboardingStatus` enum (NotStarted→
  DataSubmitted→SubMerchantCreated→Verified/Rejected/Suspended/Blocked, legacy Status mirror) + guarded transitions +
  **`IsSplitEligible`** (key var AND status∈{SubMerchantCreated,Verified}) + `RegisterSubMerchant`'ı lifecycle'a bağlama +
  **BE-P8'de split-eligibility gate** (`ProviderNotSplitEligible` → escrow/snapshot yok, offer half-accepted kalmaz) +
  `GetProviderSplitEligibility` remote-call + migration/backfill. error 5090+. P9'u split-garantisiyle açar.
- **BE-I1 UYGULANDI (2026-07-28)** — kod Payment modülünde: `ProviderSubMerchantOnboardingStatus` enum + `ProviderPaymentProfileEntity`
  guarded transitions + `IsSplitEligible`; `RegisterSubMerchantCommand` → `MarkSubMerchantCreated`; **BE-P8 create-escrow
  yolunda split-eligibility gate** (`ProviderNotSplitEligible` 5091 → snapshot/escrow yok, SR kabulü bloke); `GetProviderSplitEligibility`
  internal remote-call; admin verify/reject; migration `AddProviderSubMerchantOnboardingStatus` + backfill. Detay: `ServiceRequest/REPORT_BACKEND.md` ("BE-I1").

## FE — Provider portalı (`inktavia-marine-provider-web`) → `FE_PROVIDER_*.md`
- **Sub-merchant onboarding** UI (banka/vergi kısmen mevcut "Ödeme Profili"nde) — KYC/belge tamamlama akışı ·
  **hizmet alanı / origin lokasyon** yakalama (harita/adres).

## FE — Admin panel (`react-admin-panel-foundation`) → `FE_ADMIN_*.md`
- Provider **sub-merchant durumu / KYC** inceleme + onay · provider service-area inceleme.

## Açık kararlar (Identity)
- Sub-merchant onboarding'in iyzico tarafındaki KYC gereksinimleri (belge tipleri, doğrulama) · provider origin/hizmet
  alanının veri modeli (nokta+yarıçap mı, poligon mu; GeoDiscovery ile ilişki) · müşteri (participant) üyelik akışının
  nerede başlatılacağı (customer surface açık noktasıyla bağlantılı).
