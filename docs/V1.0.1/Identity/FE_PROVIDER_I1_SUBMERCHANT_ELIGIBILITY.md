# FE_PROVIDER_I1 — Sub-merchant onboarding durumu + split-eligibility (Ödeme Profili genişletme) — Provider FE Spec

> **Repo:** `inktavia-marine-provider-web`. **Backend:** BE-I1 (`ProviderSubMerchantOnboardingStatus`, `IsSplitEligible`,
> `GetProviderSplitEligibility`, sub-merchant register/verify) + iyzico P9-fix §5 (tip-bazlı sub-merchant, IBAN→eligibility).
> **Dalga 1 pilotu** — `docs/V1.0.1/FE_ROLLOUT_STRATEGY.md`'nin additive/bozmama pattern'ini küçük, canlı bir yüzeyde kanıtlar.
> **İlke:** MEVCUT ekranı **genişlet**, yeniden yazma. Tüm yeni alanlar **opsiyonel + null-safe + flag**; eski BFF response
> hâlâ çalışır. Nautical token + mevcut form/DetailDrawer pattern'leri. **BFF hazır olmadan mutasyon yok.**

## 0. Doğrulanan mevcut yüzey (genişletilecek)
- `src/features/finance/api/financeApi.ts`: `getPaymentProfile()` / `upsertPaymentProfile(body)`; tip **`PaymentProfile`**
  (PAY-2, `ProviderPaymentProfileDto` mirror): `{ gatewayProvider?, ibanMasked?, legalName?, taxNumberMasked?,
  status: 'Active'|'OnHold'|'Blocked'|string, verifiedAt? }`; `UpsertPaymentProfileBody { iban, legalName?, taxNumber? }`.
  Ekran: **Ödeme Profili** (finance feature, L15 ile eklendi).
- `src/features/onboarding/*`: mevcut provider onboarding lifecycle (`ProviderOnboardingStatus`, step guards,
  `/me/status.requiredNextStep`). I1 bununla **uyumlanır**, çakışmaz.
- **BFF (mount değil):** provider-portal-bff payment-profile endpoint'i BE-I1 alanlarını expose etmeli (audience-mapper +
  typed DTO). Bu spec'in **Dalga-0 ön koşulu** = BFF'e `onboardingStatus` + `isSplitEligible` (+ sub-merchant tip alanları)
  eklenmesi; FE tarafı hazır olana kadar null-safe gizler.

## 1. Tip genişletme (additive, opsiyonel) — envelope-tolerant
`PaymentProfile`'a **opsiyonel** alanlar ekle (mevcut alanlara dokunma; eski `status` string kalır — BE-I1 legacy Status'ü
mirror ediyor):
```ts
onboardingStatus?: 'NotStarted'|'DataSubmitted'|'SubMerchantCreated'|'Verified'|'Rejected'|'Suspended'|'Blocked'
isSplitEligible?: boolean
subMerchantType?: 'PERSONAL'|'PRIVATE_COMPANY'|'LIMITED_OR_JOINT_STOCK_COMPANY'
subMerchantKeyMasked?: string | null
ibanRequired?: boolean          // BE: IBAN yoksa eligible değil (P9-fix §5)
rejectionReason?: string | null
```
`UpsertPaymentProfileBody`'ye tip-bazlı zorunlu alanlar (P9-fix §5): `subMerchantType` + PERSONAL→`identityNumber,
contactName, contactSurname`; PRIVATE_COMPANY→`taxOffice, legalCompanyTitle`; LIMITED→`+taxNumber`. **Hepsi backend
required'ına göre koşullu.** Eski gövde (yalnız iban/legalName/taxNumber) hâlâ kabul edilir (BFF geriye uyumlu).
- **Null-safe:** `onboardingStatus`/`isSplitEligible` gelmezse (eski BFF) → yeni bölüm **gizlenir**, ekran eskisi gibi çalışır.

## 2. Ödeme Profili ekranı — additive bölümler
Mevcut forma **dokunmadan** ekle (flag `FEATURE_SUBMERCHANT_ONBOARDING` arkasında):
- **Onboarding durum rozeti** (status → nautical renk): `Verified`/`SubMerchantCreated`=gold/success, `DataSubmitted`/
  `NotStarted`=warning #FFBF00, `Rejected`/`Blocked`=danger #8B0000, `Suspended`=warning. `verifiedAt` varsa göster.
- **Split-eligibility banner:** `isSplitEligible===false` iken üstte uyarı: "Ödemelerin sana aktarılabilmesi için ödeme
  kurulumunu tamamla" + eksik adım (IBAN yok / doğrulama bekliyor / reddedildi+`rejectionReason`). `true` iken yeşil "Ödeme
  kurulumun tamam" rozeti. **Bu, aşağıdaki teklif-kabul gate'inin (bölüm 3) kaynağıdır.**
- **Tip-bazlı KYC alanları:** `subMerchantType` seçimi → PERSONAL/PRIVATE/LIMITED'e göre **koşullu zorunlu alanlar**
  (TCKN/vergi no/vergi dairesi/ünvan). Mevcut IBAN/legalName/taxNumber alanları korunur. IBAN **zorunlu** (eligibility için).
- **Güncelleme→OnHold uyarısı:** profil düzenleme yeniden doğrulama gerektirir (BE-I1 `UpdateProfileAndResetVerification`
  → DataSubmitted); kullanıcıya "düzenleme doğrulamayı sıfırlar" notu.

## 3. Teklif-kabul gate'i (cross-feature, `features/offers`) — kırık akışı önle
BE-P8/I1 split-eligible olmayan provider'ın teklifini **`ProviderNotSplitEligible` (5091)** ile reddediyor. FE bunu
**önden** yakalamalı (accept'in patlamasını bekleme):
- Offer detay/kabul ekranında `isSplitEligible` (payment-profile query'sinden, cache paylaşımı) `false` ise **kabul butonu
  disabled** + "Teklifi kabul etmek için ödeme kurulumunu tamamla → Ödeme Profili" CTA (deep-link).
- Eğer yine de 5091 dönerse (race) → aynı mesajla graceful hata; sessiz fail yok.
- **Additive:** mevcut kabul akışı korunur, yalnız ön-guard + CTA eklenir.

## 4. Hook / query
- `usePaymentProfile()` (mevcut) → yeni opsiyonel alanları taşır. Gerekiyorsa hafif `useSplitEligibility()` (BE
  `GetProviderSplitEligibility`) — ama tek payment-profile query yeterliyse ayrı çağrı ekleme (over-fetch yok). TanStack
  Query cache anahtarı paylaşılır (offers gate aynı veriyi okur).

## 5. Bozmama / QA
- Yeni alanların hepsi opsiyonel; eski BFF response → yeni bölümler gizli, **regresyon yok** (mevcut Ödeme Profili + Finance
  sekmeleri aynı çalışır — snapshot/görsel doğrulama).
- Flag kapalıyken hiçbir yeni UI görünmez.
- i18n: yeni anahtarlar (durum etiketleri, banner, CTA) TR/EN.
- Test: eligible/non-eligible/rejected/suspended durumları; tip-bazlı form validasyonu; offer gate disabled+CTA; eski-BFF
  null-safe; mevcut ekranların regresyonu.

## 6. Ön koşul & sıra
1. **Dalga-0:** provider-portal-bff payment-profile endpoint'ine BE-I1 alanları (audience-mapper + typed DTO) + upsert'e
   tip-bazlı alanlar. (Backend hazır; BFF exposure + tip.)
2. Bu spec (provider FE) uygulanır.
3. **Eşi:** `FE_ADMIN_I1` (sub-merchant KYC inceleme/verify-reject) — admin app foundation'dan ayağa kaldırıldığında ilk
   admin feature'ı olarak. Provider gönderir → admin doğrular.

> **Not:** BFF repo şu an mount değil. Uygulamadan önce provider-portal-bff'in payment-profile endpoint'inin BE-I1
> alanlarını gerçekten döndürdüğü doğrulanmalı; spec BE-I1 `REPORT_BACKEND.md`'deki gerçek DTO adlarına hizalanacak.
