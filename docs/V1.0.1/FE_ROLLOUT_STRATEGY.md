# Frontend Rollout Stratejisi — Backend P1–P12 + SR + I1 → Provider & Admin panelleri (tasarım bozmadan)

> **Amaç:** Tamamlanan payment/economics backend'ini (Payment P1–P12, SR S1/S6/S7/S8, Identity I1, iyzico P9-fix) iki FE
> yüzeyine **additive, geri-uyumlu, tasarımı bozmadan** taşımak. Her faz → `FE_PROVIDER_<x>.md` / `FE_ADMIN_<x>.md` spec.
> **İki yüzeyin doğası farklı:**
> - **Provider** (`inktavia-marine-provider-web`) = **canlı, tasarımı oturmuş** feature-sliced uygulama → **genişlet**.
> - **Admin** (`react-admin-panel-foundation`) = **foundation blueprint** (Stitch design system, BFF contract, routing/guard
>   dökümanları var; feature ekranları henüz yok) → foundation'a uygun **net-yeni ekran kur**.

## 1. Bozmama ilkeleri (her FE işinde zorunlu)
1. **Contract-first + envelope-tolerant.** Backend değişiklikleri **yeni alan ekler, mevcut alanı değiştirmez.** FE parsing
   (`normalizeEnvelope`, Zod/TS tipleri) **eksik/yeni alanlara toleranslı** olmalı — eski response'lar crash etmemeli. Yeni
   DTO, mevcut DTO'yu kırmadan. (Aizen BFF envelope pattern korunur; provider BFF audience-mapper + typed-body kuralları.)
2. **Additive feature-slice.** Provider'da yeni yüzey = mevcut `features/*` içine **yeni sekme/bölüm/alt-slice** ya da yeni
   slice; **mevcut Finance/Offers ekranları yeniden yazılmaz.** Admin'de her feature = foundation'a uyan **yeni slice**.
3. **Feature flag / progressive disclosure.** Yarısı roll edilmiş backend fazı UI'da kırık görünmesin — yeni yüzeyler
   flag arkasında; alan yoksa bölüm gizlenir (null-safe), hata basmaz.
4. **Tek tasarım dili.** Provider = mevcut **nautical token** (navy #002147 / gold #C5A059 / danger #8B0000 / warning
   #FFBF00), TanStack Query, DesktopShell+MobileShell, DetailDrawer/RowActions pattern'leri **yeniden kullanılır.** Admin =
   **Stitch design system** (doc 08) + foundation routing/guard/i18n. **Yeni tasarım dili icat edilmez.**
5. **Backend → BFF → FE sırası.** Her fazın FE'si, BFF endpoint'i (provider-portal-bff audience-mapper; admin BFF) hazır
   olmadan başlamaz. Read-only sürüm önce (görüntüle), mutasyon sonra.
6. **Değer + risk sıralaması.** En sık kullanılan + tasarıma en duyarlı yüzey (provider teklif-oluşturma ekonomisi) erken
   ve dikkatli; hard-gate (I1 sub-merchant — onsuz provider işlem yapamaz) erken; düşük-riskli admin rule-CRUD paralelde;
   raporlama dashboard'u (P12) sonda.

## 2. Backend fazı → FE yüzeyi eşlemesi

### Provider paneli (genişlet — `inktavia-marine-provider-web`)
| Backend | Provider FE yüzeyi | Nitelik |
|---|---|---|
| **I1** sub-merchant onboarding + split-eligibility | **`features/settings` (Ödeme Profili)** genişletme: KYC/IBAN/tip alanları + **onboarding durumu** + "split-eligible değilsen teklif kabul edilmez" uyarısı | **Hard gate — erken.** Mevcut Ödeme Ayarları (L15) üzerine additive |
| **S1/S6/S7 + P8 preview** line economics | **`features/offers` teklif oluşturma** (en büyük iş): kalem + `PricingMethod` + per-line komisyon önizleme (S7 `GetOfferCommissionPreview`) + müşteri indirim önizleme (S6) + net/KDV/brüt **canlı** | **En yüksek değer + en duyarlı.** Yeni alanlar mevcut offer item satırına additive |
| **P4** plan price (launch/list) | **`features/settings` abonelik** (L14 üzerine): lansman vs liste fiyatı, yaklaşan fiyat değişimi | Additive rozet/not |
| **P8/S8** snapshot kırılımı | **`features/finance` işlem detayı** (DetailDrawer): ServiceAmount / PlatformFee / CommissionBase / CommissionBenefit / funding kırılımı | Mevcut DetailDrawer'a yeni satırlar |
| **P10** refund/chargeback + negative balance | **`features/finance`**: işlem **İtirazlı/İade** durumu + **negatif bakiye/mahsup** şeffaflığı | Mevcut transaction/payout ekranına durum + rozet |
| **P11** OFFER_BOOST_7D | **`features/offers`**: teklif üstünde **boost satın al** + entitlement durumu/kalan süre | Yeni aksiyon + rozet |
| **P12** | Provider'a özel gerek yok (provider zaten kendi komisyon/net kırılımını görür) | — |

### Admin paneli (foundation'a kur — `react-admin-panel-foundation`)
| Backend | Admin FE ekranı | Nitelik |
|---|---|---|
| **P2** CommissionRule | CRUD + **specificity/conflict uyarısı** (fail-loud'u UI'da göster) | Net-yeni, düşük risk |
| **P3** PlatformFeeRule | CRUD (4 model) | Net-yeni |
| **P4** ProviderPlanPrice | CRUD (launch/list, effective-date, overlap/gap uyarısı) | Net-yeni |
| **P5** ProfitProtectionPolicy | min katkı tutar/oran + expected expense + AdjustmentOrder editörü | Net-yeni, kritik |
| **P6** CustomerDiscountRule + BudgetPolicy | CRUD + funding modu + budget policy | Net-yeni |
| **P7** ProviderCommissionBenefitRule + entitlement grant | CRUD + provider'a entitlement ver/iptal | Net-yeni |
| **I1** sub-merchant KYC | **inceleme + verify/reject** kuyruğu | Net-yeni, hard-gate destekler |
| **P10** RefundAllocationPolicy + kuyruk | policy CRUD + **refund/chargeback kuyruğu** + ProviderNegativeBalance ledger + override(audit) | Net-yeni, kritik |
| **P11** PremiumProduct/Price | CRUD | Net-yeni |
| **P12** finansal raporlama | **dashboard** (gelir/gider/contribution satırları + NetMarketplaceContribution + drill-down) | **En büyük admin işi — sonda** |
| **SR S13** dispute case | **Dispute Case** inceleme + item-level release/refund/partial + audit | Sonraki dalga |
| **SR S2–S5** attribute/price-book/travel/part-term | admin CRUD | Sonraki dalga |

## 3. Önerilen rollout sırası (dalgalar)
**Dalga 0 — hazırlık (ortak):** provider-portal-bff + admin-bff için yeni endpoint'lerin **audience-mapper + typed DTO**
sözleşmeleri; FE tarafında **envelope-tolerant tip katmanı** + feature-flag altyapısı. (Kod yok; sözleşme + flag.)

**Dalga 1 — provider işlem yapabilsin (hard gate + para doğruluğu):**
1. `FE_PROVIDER_I1` sub-merchant onboarding + split-eligibility (Ödeme Profili genişletme).
2. `FE_ADMIN_I1` sub-merchant KYC inceleme/verify.
3. `FE_PROVIDER_OFFER_ECONOMICS` (S1/S6/S7 + P8 preview) — teklif oluşturma ekonomisi (en büyük, en duyarlı; read-only
   önizleme önce, sonra kalem düzenleme).

**Dalga 2 — admin kuralları yönetsin (düşük risk, paralel):**
4. `FE_ADMIN_RULES` — P2/P3/P4/P5/P6/P7 rule CRUD ekranları (foundation-conformant, mevcut hiçbir şeyi kırmaz).
5. `FE_PROVIDER_FINANCE_SNAPSHOT` (P8/S8 kırılım) + `FE_PROVIDER_SUBSCRIPTION_PRICE` (P4 launch/list) — mevcut Finance/
   abonelik ekranlarına additive.

**Dalga 3 — para geri akışı + premium:**
6. `FE_ADMIN_REFUND_QUEUE` (P10 refund/chargeback/negative-balance) + `FE_PROVIDER_REFUND_TRANSPARENCY`.
7. `FE_ADMIN_PREMIUM` (P11) + `FE_PROVIDER_BOOST` (teklif boost).

**Dalga 4 — taçlandırma:**
8. `FE_ADMIN_REPORTING` (P12 dashboard — NetMarketplaceContribution + drill-down).
9. Sonraki: `FE_ADMIN_DISPUTE` (SR S13), attribute/price-book/travel (SR S2–S5), customer-facing surface (açık nokta).

## 4. Her FE spec'inin içereceği (şablon)
Her `FE_PROVIDER_*.md` / `FE_ADMIN_*.md`: (1) hangi backend endpoint/DTO'ları tükettiği (BFF sözleşmesi net), (2) hangi
mevcut slice/ekranı **genişlettiği** (provider) veya hangi foundation contract'a uyduğu (admin), (3) additive alan listesi +
**null-safe/flag** davranışı, (4) design token/pattern referansı (nautical / Stitch), (5) durum/edge-case (eligible değil,
indirim yok, negatif bakiye, boost expired), (6) test/QA (mevcut ekranların regresyonu dahil), (7) i18n anahtarları.

## 5. "Tasarımı bozmama" garantileri (özet)
- Mevcut ekranlar **hiç yeniden yazılmaz**; yalnız additive alan/sekme/rozet.
- Backend yeni alanları **opsiyonel** gelir; FE eksikken bölümü gizler (flag/null-guard) → eski + yeni response birlikte çalışır.
- Provider **nautical token** / admin **Stitch** dışına çıkılmaz; DetailDrawer/RowActions/shell pattern'leri yeniden kullanılır.
- Her yüzey **BFF sözleşmesi hazır olduktan sonra**; provider audience-mapper + typed-body kuralları korunur.
- Sıra: hard-gate (I1) + para doğruluğu (offer economics) önce; düşük-riskli admin CRUD paralel; raporlama sonda.

> **Sonraki adım:** Dalga 0 sözleşmeleri + `FE_PROVIDER_I1` / `FE_ADMIN_I1` spec'leriyle başlanabilir. Her spec, ilgili
> backend fazının REPORT_BACKEND.md'sindeki gerçek endpoint/DTO adlarına dayanır (varsayım değil).
