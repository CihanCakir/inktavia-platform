# Karar Brief'i — production `CustomerSideVariableCostShareRate` + R2 KDV oranları

> **Amaç:** economics'te dev placeholder olarak duran iki değeri production'a taşımak için gereken kararları netleştirmek.
> **İkisi de kodda hardcoded DEĞİL — config'lenebilir**, yani iş sadece doğru değerleri belirleyip uygulamak. Bu iki
> karar **farklı doğada**: biri iş/fiyatlandırma politikası (senin kararın), diğeri vergi (YMM kararı).
>
> **Uyarı:** Ben lisanslı bir mali müşavir/vergi danışmanı değilim. Aşağıdaki KDV oran yapısı kamuya açık güncel bilgi
> olarak verilmiştir; **hangi oranın hangi kaleme uygulanacağı bir YMM teyidi gerektirir.** Ben oran uydurmuyorum.

---

## 1. `CustomerSideVariableCostShareRate` — İŞ/FİYATLANDIRMA POLİTİKASI (senin kararın)

**Ne olduğu:** Platform **değişken maliyetlerinin** (ödeme işleme masrafı, iade risk rezervi, diğer değişken giderler)
ne kadarının **müşteri tarafında**, ne kadarının **sağlayıcı tarafında** taşınacağını belirleyen oran — §19.2
kâr-koruma (profit-protection) kontrolünde kullanılıyor.

**Etkisi (hesap mantığı):**
- **Yüksek oran** (→ 1.0): değişken maliyeti müşteri/platform tarafı üstlenir → **sağlayıcı-tarafı katkısı yükselir** →
  teklifler kâr-korumadan **daha kolay geçer**, ama platform/müşteri daha çok değişken maliyet emer.
- **Düşük oran** (→ 0.0): sağlayıcı değler maliyeti üstlenir → sağlayıcı marjı daralır → **daha çok teklif reddedilir**.
- **0.5** (mevcut dev default): 50/50 nötr bölüşüm.
- **1.0**: WC1 smoke'unda deal'i açmak için geçici konulan gerçekçi-olmayan değerdi (sağlayıcı hiç değişken maliyet
  taşımıyor).

**Karar kimin:** Senin (ürün sahibi) — bu bir **marj-vs-dönüşüm** dengesi (take-rate hedefin + sağlayıcı marjını ne
kadar agresif koruyacağın). Vergi meselesi değil.

**Nasıl uygulanır:** **Admin panelinden** (ProfitProtectionPolicy CRUD — P5). Kod/deploy gerekmez; production policy'de
oranı ayarlamak yeterli. (Dev DB'de 0.5 placeholder.)

**Not:** Bu, profit-protection policy'sindeki tek knob değil — `MinProviderSideContributionRate/Amount`,
`MinCustomerSideContributionRate`, `PaymentProcessingExpenseRate/Fixed`, `RefundRiskReserveRate`,
`OtherVariableExpenseRate/Fixed` ile birlikte çalışıyor. Production'a geçerken bu setin **tamamının** gerçekçi
değerlerle bir kez gözden geçirilmesi önerilir (hepsi admin-tunable).

---

## 2. `PLATFORM_FEE_VAT_RATE` + `COMMISSION_VAT_RATE` — KDV / VERGİ (YMM kararı)

**Ne olduğu:** ReferenceData **system-parameter**'ları; economics'te `PlatformFeeCalculationService` +
`CommissionCalculationService` tarafından (adapter üzerinden) okunuyor. **Dev'de 0.20 placeholder** ile seed'li (R2
gerçek değeri beklerken).

**Güncel Türkiye KDV yapısı (2026, doğrulandı):**

| Oran | Kapsam |
|------|--------|
| **%20** | Standart oran — mal ve hizmetlerin çoğu (hizmetler dahil) |
| **%10** | İndirimli — temel gıda vb. |
| **%1** | Süper-indirimli — temel tarım ürünleri, belirli basılı yayın |

(Standart oran Temmuz 2023'te %18→%20 oldu. İhracat sıfır oranlı.)

**Marketplace KDV yapısı — YMM'ne sorulacak kilit nokta:** Platform iyzico **sub-merchant split-payment** modeli
kullanıyor (Inktavia fonu custody etmiyor; gelir = komisyon + abonelik + platform/müşteri hizmet bedeli). Bu modelde
tipik olarak:
- **Sağlayıcı**, hizmetin kendisini müşteriye faturalar → **hizmet KDV'si sağlayıcının** kendi faturasında.
- **Platform**, kendi gelirini (komisyon + platform/müşteri bedeli) faturalar → **platform KDV'si kendi gelirinde.**

Yani bu iki parametre **platformun kendi gelirinin** (komisyon + fee) KDV'si; hizmetin KDV'si değil.

**YMM'ne net sorular:**
1. Platform **komisyonuna** uygulanacak KDV oranı nedir? (hizmet → büyük olasılıkla %20, ama teyit gerekir)
2. Platform/müşteri **hizmet bedeline** (customer platform fee) uygulanacak KDV oranı nedir?
3. Sağlayıcının verdiği **marine hizmetinin** KDV'si sağlayıcının kendi faturasıyla mı yürüyor (marketplace modeli),
   yoksa platformun da bir "hizmet KDV" parametresi tutması mı gerekiyor? (Bu, yalnızca 2 parametrenin mi yoksa
   üçüncü bir hizmet-VAT parametresinin mi gerektiğini belirler.)
4. Abonelik gelirine KDV? (ayrı bir kalem — modelde var.)

**Nasıl uygulanır:** ReferenceData **system-parameter** değeri olarak (seed güncellemesi veya admin). Kod değişmez —
adapter değeri değişiklik olmadan okur (WC/adapter işinde doğrulandı).

---

## 3. Uygulamaya-hazır tablo (değerler belirlenince doldurulur)

| Parametre | Doğa | Dev placeholder | Nerede ayarlanır | Kim karar verir | Durum |
|-----------|------|-----------------|------------------|-----------------|-------|
| `CustomerSideVariableCostShareRate` | İş/politika | `0.5` | Admin → ProfitProtectionPolicy (P5) | **Sen** (ürün) | ⬜ bekliyor |
| `PLATFORM_FEE_VAT_RATE` | Vergi | `0.20` | ReferenceData system-param | **YMM** | ⬜ bekliyor |
| `COMMISSION_VAT_RATE` | Vergi | `0.20` | ReferenceData system-param | **YMM** | ⬜ bekliyor |
| (varsa) hizmet-VAT / abonelik-VAT | Vergi | — | (YMM belirlerse eklenir) | **YMM** | ⬜ soru |

## 4. Riskler
- **Yanlış KDV** → vergi uyumu/ceza riski. Bu yüzden YMM teyidi şart; ben oran koymuyorum.
- **Yanlış cost-share** → ya sağlayıcı marjı erir (çok düşük) ya da meşru teklifler gereksiz reddedilir (çok
  düşük/yüksek dengesizliği). Production'da profit-protection setinin bütünüyle gözden geçirilmesi önerilir.
- **Placeholder'la canlıya çıkmak:** 0.20 KDV ve 0.5 cost-share dev için makul ama **production'da doğrulanmamış**
  sayılmalı — canlı finansal işlem öncesi gerçek değerler girilmeli.

## 5. Sıradaki adım
Değerleri belirlediğinde (KDV'yi YMM'den, cost-share'i sen), bana ilet:
- **KDV** → system-parameter seed'ini (dev placeholder'ları) gerçek değerlerle güncelleyen bir kickoff/PR hazırlarım.
- **cost-share** → admin ProfitProtectionPolicy'de ayarlanır (kod gerekmez); istersen production policy setinin
  tamamı için önerilen-değer + gerekçe tablosu çıkarırım.

---

**Sources (KDV oran doğrulaması):**
- [Value Added Tax in Turkey and Its Rate "KDV" for 2026 — Damas Group](https://www.damasgroup.com.tr/en/blog/Value-Added-Tax-VAT-in-Turkiye-and-its-value)
- [VAT in Turkey 2026: Rates, Exemptions and Compliance — Çelikel CPA](https://celikelcpa.com/blog/vat-in-turkey-2026-guide/)
- [Turkey VAT / Sales Tax Rates (2026) — TaxAtlas](https://taxatlas.io/country/turkey/vat-sales-tax)
