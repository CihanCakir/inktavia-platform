# ServiceRequest Modülü — V1.0.1 Roadmap
> Itemized (kalem bazlı) deniz-servisi teklif ekonomisi + dispute case. Kanonik: `../COMMISSION_PACKAGE_PRICING.md` §20–§21.
> **Reuse (yeniden yazma):** `ServiceRequestOfferEntity`/`ServiceRequestOfferItemEntity`/`OfferCalculationService`/
> `ProviderOfferTemplate` + escrow/completion/work-logs/evidence altyapısı — **genişlet**. Her faz → `BE_S<n>_*.md`.

## Backend fazları
| Faz | Kapsam | Kanonik § | Bağımlılık |
|---|---|---|---|
| **S1** | OfferItem/Offer **ekonomi + `PricingMethod`** + `ServiceRequestOfferItemType` enum genişletme (Consumable/Travel/ExternalService/EquipmentRental/MarinaOrLiftFee/OtherApprovedExpense) + `OfferCalculationService` **line ekonomisi** | §20.3–20.5 | Payment P2 |
| **S2** | `PricingAttributeDefinition`/`Value` + validation (category/template bazlı; motor/tekne/boya değişkenleri) | §20.6 | — |
| **S3** | **Provider price book** (`ProviderPriceBookEntry` veya `ProviderOfferTemplate` genişletme) + **FX snapshot köprüsü** (EUR/USD→settlement TL, kabul sonrası re-valuation yok) | §20.7 | RefData R1 |
| **S4** | `TravelPricingRule` + **server-side mesafe** doğrulama + snapshot (client finansal kaynak değil) | §20.8 | Identity I2 |
| **S5** | `PartCommercialTerm` (supplier/dealer margin, funded splits, **MinProviderReceivable**; maliyet gizliliği) | §20.9 | — |
| **S6** | **Line-level indirim** eligibility + funding allocation (order-level voucher deterministik allocation) | §20.10 | Payment P6 |
| **S7** | **Line-level komisyon** eligibility/base (Payment `CommissionRule` boyutlarıyla çözülür; toplam = line toplamı) | §20.11 | Payment P2 |
| **S8** | **Line snapshots** (`OfferLineEconomicsSnapshot`/Discount/Commission/Travel/Attribute) → aggregate türetme + **8 eşitlik** (0 tolerans) | §20.15 | S1–S7, Payment P1 |
| **S9** | **Line-level profit protection** entegrasyonu + transaction kapıları (bir kalemin zararı diğerinde saklanamaz) | §20.12 | Payment P5, S8 |
| **S10** | **Kabul akışı**: `CalculateServiceRequestPaymentEconomics` çağrısı → snapshot → checkout başlat | §20.16, §19.9 | Payment P8, P9 |
| **S11** | `OfferType` (Fixed/Estimate/RequiresInspection/T&M) + **`ServiceChangeOrder`/`OfferRevision`/`ExtraWorkApproval`** (kabul sonrası snapshot değişmez; müşteri onayı) | §20.13 | S10 |
| **S12** | **Periyodik bakım**: `RecommendedIntervalMonths`/`LastPerformedAt`/`NextDueAt`/`ReminderLeadDays` (CargoDry'den ayrı) | §20.14 | Notification N2 |
| **S13** | **`ServiceRequestDispute`** case + `OpenDispute` + **Dispute Case agregasyonu** (mevcut work-log/evidence/mesaj) | §21.5–21.6 | Payment P10 |

## Durum / sıralama kararı (2026-07-28)
Payment P1–P7 ✅ bitti; kritik yol gereği **P8'den önce SR itemized çekirdeği** gerekiyor. Karar: **dar çekirdek
`S1 → S7 → S8 → P8`**; S2 (attribute) / S3 (price-book+FX) / S4 (travel) / S5 (part-terms) / S6 (line-discount-funding)
**sonra girdi olarak** eklenir (P8 satırda ne varsa okur). Mevcut zemin doğrulandı: offer/item zaten itemized
(`LineSubtotal/TaxAmount/LineTotal/DiscountAmount` + tip-toplamları), `OfferCalculationService` server-authoritative
(pro-rata indirim + line tax), accept handler escrow + `ServiceRequestOfferAcceptedMessage` publish ediyor (P8/S10 hook).
- **BE-S1 ✅ TAMAM** (2026-07-28, 15 test): item-type genişletme (Consumable=9..OtherApprovedExpense=14, Other'a roll) + `PricingMethod` (descriptive, math değişmedi) + per-line `LineCommissionEligibility` (default-by-role, admin-tunable + override) + computed `CommissionBaseAmount` (Eligible=pre-tax post-discount, Exempt=0) + offer `CommissionBaseTotal` (Σ, `≤ Subtotal` guard) via `OfferCalculationService` line-economics pass (tax pass sonrası, mevcut math'e dokunmadan). Migration `AddOfferLineEconomics` + backfill. **Flag:** FluentValidation yerine mevcut handler-guard (`SaveOfferDraft.ValidateItems`) genişletildi — SR offer item'da FV yok, kullanılmayan pipeline eklemekten kaçınıldı (proje pattern'i). Enum default-0 trap nullable ile çözüldü. ProviderNet/PlatformContribution per line YOK (S7/S6). Sonraki: **BE-S7**.
- **BE-S1 promptu (arşiv)** (`BE_S1_OFFER_LINE_ECONOMICS.md`): item-type genişletme (Consumable/Travel/ExternalService/
  EquipmentRental/MarinaOrLiftFee/OtherApprovedExpense=9..14) + `PricingMethod` (descriptive, math değişmez) + per-line
  `CommissionEligibility` (Eligible/Exempt/InheritFromCategory, default ItemType rolüne göre ama admin-tunable) + computed
  `CommissionBaseAmount` (Eligible = pre-tax post-discount `Max(LineSubtotal−proRataDiscount,0)`, Exempt = 0) + offer
  `CommissionBaseTotal` (≤ Subtotal) — `OfferCalculationService` line-economics pass'i mevcut math'e dokunmadan ekliyor.
  Rate/amount YOK (S7), funding YOK (S6), snapshot YOK (S8), FX/TL YOK (S3), acceptance/Payment değişmez.
- **BE-S7 ✅ TAMAM** (2026-07-28, 177 test): Payment'ta `LineCommissionResolver` (BE-P2 per-line, tek aktif-rule load; Exempt→0/providerNet=tam satır, Eligible→Round(base×rate), InheritFromCategory→cat/plan/global; Σ per-line=transaction, per-line yuvarlama korunur — 20.02 not blended 20.01; conflict propagate; NO MarkApplied) + `GetActiveAtAsync` repo read + typed remote-call `ResolveLineCommissions` internal `[Authorize]` (401 canlı kanıtlı) + SR `GetOfferCommissionPreviewQuery` (persistence yok, migration yok, yalnız Payment.Abstraction). **Flag→P8 gereksinimi:** resolver pure kaldı (plan id caller'dan) — **P8 kabul anında provider aktif planını authoritative çözüp resolver'a geçmeli (null'a güvenme)**; FE preview de plan id geçmeli yoksa yanlış oran. Sonraki: **BE-S8**.
- **BE-S8 ✅ TAMAM** (2026-07-28, 164 test: 24 BE-P1 + 15 yeni): 3 immutable child (OfferLineEconomics/CommissionAllocation/DiscountAllocation, FK→snapshot Restrict, no-mutator, internal Create, ItemType/PricingMethod raw SR enum int → cross-module enum dep yok) + `CreateFromLines` (aggregate yalnız satır toplamı, 5 decomposition kolonu) + 8 eşitlik 0-tolerans (her tamper ayrı exception) + **BE-P1 uzlaştırma kanıtlı: aggregate `CommissionRateSnapshot` reporting-only (amount/base 4dp), bağlayıcı=line-sum — 0.39 vs 0.38 divergent gösterildi**. Migration `AddLineEconomicsSnapshots` (3 tablo + 5 kolon NOT NULL DEFAULT 0). Discount 0 (S6), Travel(S4)/Attribute(S2) reserved. Pure — acceptance/MarkApplied YOK (P8). **Narrow P8 çekirdeği tamam (S1+S7+S8). Sonraki: BE-P8.**

### İkinci dalga (S2–S5) — descriptive/config fazları TAMAM (2026-08, backend+FE)
- **S2 (pricing attributes) ✅** — `PricingAttributeDefinition` (admin CRUD, kategori-scope, R4 lookup-backed) + `PricingAttributeValue` offer-line başına (validation via remote `GetLookupItemsByGroup`) + immutable `OfferLineAttributeSnapshot` (S8 rezerve slotu, denormalized label). Descriptive — 8-eşitlik el değmemiş. Payment 272/272 + SR 68/68. Rapor `REPORT_S2.md`. FE: admin CRUD + provider picker (birleşik `REPORT_FE_S2_S3.md`).
- **S3 (offer FX snapshot) ✅** — settlement=TRY; submit-anında R1 ile non-TRY satır TRY'ye çevir (`SR_FX_RATE_UNAVAILABLE` fail-loud), `OfferFxSnapshotEntity` + kabulde freeze, `OfferLineEconomicsSnapshot`'a nullable FX + tamper guard. 8-eşitlik tek-para TRY, FX'li/siz byte-identical. Payment 277/277 + SR 73/73. Rapor `REPORT_S3.md`. FE: provider ₺converted + freeze note, admin FX blok.
- **S4 (travel pricing snapshot) ✅** — `TravelPricingMethod{FlatMobilization,PerKm}` + `TravelPricingDetail` (validation: Quantity==distanceKm, UnitPrice==perKmRate, KILOMETER unit, yalnız Travel line) + immutable `TravelPricingSnapshot` (rezerve slot dolduruldu). Mesafe provider-declared (geo-compute GeoDiscovery'ye ertelendi). Descriptive. Payment 285/285 + SR 85/85. Rapor `REPORT_S4.md`. FE: provider Travel sub-form + admin travel block.
- **S5 (part commercial terms) ✅** (Payment-coded) — `PartCommercialTerm` (dealer margin/funded split/minReceivable/maxDiscountable, scoped+versioned, P4/P6 resolver deseni) + pure resolver → **cost-free `PartLineAllowanceDto`** = Payment'tan çıkan tek projeksiyon. **Gizlilik headline:** raw cost hiçbir SR/BFF/FE payload'ında yok (codified test korur). Yalnız tanımlar+resolve — uygulama S9. Payment 307/307 + SR 92/92. Rapor `REPORT_S5.md`. FE: admin CRUD (cost yalnız burada) + provider cost-free allowance hint. Birleşik FE raporu `REPORT_FE_S4_S5.md`.
- **S9 (line-level profit protection) ✅** (Payment-coded, UNCOMMITTED) — §20.12: transaction P5 kapılarından ÖNCE pure `LineProfitProtectionEngine` (part floor=S5 allowance, gerisi policy default; 4 kontrol: min-receivable/funded-cap/commission-floor[P7 reuse]/negative-contribution-ban; **no netting**; strategic-loss exception default off). 8 admin-tunable policy alanı (launch no-op → geçen teklif byte-identical). Combiner'a line-then-transaction sıralı; ihlal→Rejected/ConfigError→snapshot/escrow yok→SR rollback; kod 5130-5134. S8 line snapshot+3 kolon + insert-only eval log. **Karar:** gate final committed ekonomi (post safe-max) üzerinde → P8b ApprovedWithAdjustment'ı yanlış reddetmez. Payment 324+78 test (307+78 regresyon + 17 yeni). Rapor `REPORT_S9.md`.
- **S9 FE ✅** (UNCOMMITTED) — admin ProfitProtection form/detail'e 8 line-level alan + BFF passthrough + i18n; provider 5130-5134 kod-mapping (`lineProtectionErrors.ts`); **S9-preview** backend follow-up işaretli. İki dil ekranda kısmen doğrulandı. Rapor `REPORT_FE_S9.md`.
- **S13 (dispute case) ✅** (UNCOMMITTED) — `GetDisputeCaseDetail` agregasyon (dispute+SR timeline+N-E reason+cost-free S8 economics+work-log+evidence+mesaj+P10 refund state) + `DisputeResolutionOutcome{FavorPayerFull/Partial,FavorProviderRelease,Split}`→P10 RefundAllocationService reuse (idempotent DISPUTE-{id}) + `ServiceRequestDisputeResolvedMessage`. Split=payer partial refund+provider kalanı normal release (tested rails). Rapor `REPORT_S13.md`. Kalan: S13 FE.
- **N3 (dispute/chargeback/auto-approve bildirimleri) ✅** (UNCOMMITTED, Notification/Payment/SR) — DisputeResolved consumer+targeting fix; chargeback event(`PaymentChargebackRecordedMessage`)+tip 159+consumer; completion auto-approval (`AutoApproveAt`+Hangfire job: 133 reminder + deadline'da onay komutunu system-actor reuse→aynı ödeme sonucu). **BULGU:** completion onayı escrow'u senkron bırakmaz (decoupled release); auto=manuel sonuç. **Gotcha:** template'siz tip sessiz no-op→131/133/141/159 seed. Rapor `Notification/REPORT_N3.md`.
- **S13 FE ✅** (UNCOMMITTED, canlı) — `DisputeCasePage`→`GetDisputeCaseBff` + resolution outcome picker + provider auto-approve countdown; 2 follow-up (500 config-bug fix + dispute-list all-status) da bitti; disputes Servis Talepleri submenu + SR↔dispute çift-link.
- **S12+N2 (recurring maintenance) ✅** — `MaintenanceScheduleEntity`+`MaintenanceReminderDueJob`(AizenRecurringJob)+tip 103, CargoDry'den ayrı. Rapor `REPORT_S12_N2.md`.
- **S11 (change-order + OfferType) ✅** (UNCOMMITTED) — `OfferType` (default FixedPrice byte-identical) + `ServiceChangeOrderEntity` lifecycle (propose→customer approve/reject→apply); artış P8'e ayrı key `SR-{sr}-OFFER-{offer}-CO-{id}` (yeni snapshot+incremental escrow, kabul snapshot immutable, breach→Rejected), azalış yeni P10 reduction endpoint; effective total türetilir; müşteri-approve API/bus (owner UI ertelendi); S11c ertelendi. SR 138+Payment 82 test. Rapor `REPORT_S11.md`.
- **🎉 SR roadmap S1–S13 backend TAM** (S10=P8). Kalan: S9/S12 admin FE + S11/S13 owner-app FE (bloke). Not: S9/S11/S13/S12+N2 vs. **commit'siz** kısımlar var.
- **BE-S6 ✅ TAMAM** (2026-07-28): `LineDiscountEligibility` (default-by-role, exempt→indirim yok) + `OfferCustomerDiscountAllocator` (pure, deterministik pro-rata eligible pre-tax base, remainder en büyük line → `Σ==requested` tam, clamp, funding Platform/Provider[consent honoured, yoksa düşer]/Shared/Supplier=0) + calc pre-tax uygulama + tax recompute (taban+KDV+`CommissionBaseAmount` düşer; narrow-core byte-identical) + preview `GetOfferCustomerDiscountPreviewQuery` → yeni `IPaymentModuleRemoteCall.ResolveCustomerDiscountAsync` [Authorize] → `ResolveCustomerDiscountForOfferQuery` (mevcut P6 `ResolveCustomerDiscountQuery` ayrı, dokunulmadı) compute-on-demand + migration `AddOfferLineCustomerDiscount` (7 kolon) + backfill. Sonraki: **P8-wiring**.
- **BE-S6 promptu (arşiv)** (`BE_S6_LINE_DISCOUNT_FUNDING.md`) — P8 fast-follow'un 1. yarısı (S6 → P8-wiring): per-line
  `LineDiscountEligibility` (default-by-role, exempt→indirim yok §20.10) + **deterministik allocation** (eligible line
  pre-tax base'e pro-rata, remainder en büyük line'da → `Σ == requested` tam, funding Platform/Provider[consent yoksa
  düşer]/Shared) + **pre-tax uygulama + tax recompute** (indirim taban+KDV+`CommissionBase`'i düşürür; indirimin KDV bazı
  S6'da karara bağlandı, **platform-fee base KDV sorusu P8-wiring/YMM'ye bırakıldı**) + `OfferCalculationService` genişletme
  + **P6-resolving preview** (S7 pattern, `ResolveCustomerDiscount` remote-call, persistence/budget/snapshot YOK).
  Budget reserve/consume + P7 effective rate + DiscountAllocationSnapshot doldurma + non-zero 8-eşitlik = **P8-wiring**.
- **BE-S8 promptu (arşiv)** (`BE_S8_LINE_SNAPSHOTS_AGGREGATE.md`): BE-P1'in immutable/validating-factory pattern'i satır
  granülaritesine → immutable `OfferLineEconomicsSnapshot`/`CommissionAllocationSnapshot`/`DiscountAllocationSnapshot`
  (FK→`PaymentEconomicsSnapshot`, insert-only) + `CreateFromLines` factory (aggregate **yalnız satır toplamlarından türetilir**)
  + **8 eşitlik 0-tolerans** (§20.15) + §20.15 aggregate decomposition alanları. **BE-P1 uzlaştırma:** aggregate
  `CommissionRate` reporting-only effective rate'e (`amount/base`), bağlayıcı invariant line-sum (`Σ line commission`) —
  BE-P1'in `Round(base×rate)` kuralıyla çelişki yaratmadan, dokümante edilmiş dar değişiklik. Travel snapshot=S4,
  attribute snapshot=S2 (**reserve, S8'de yapılmaz**); discount allocation modellenir ama narrow-core'da 0 (S6 doldurur).
  Pure/structural — acceptance/resolver/checkout/MarkApplied YOK (hepsi P8). Sonraki: **BE-P8**.
- **BE-S7 promptu (arşiv)** (`BE_S7_LINE_COMMISSION_RESOLUTION.md`): **Payment'ta** line-set komisyon resolver (BE-P2
  `CommissionRuleResolver`'ı satır kümesine genişletir; per-line commissionable/base/resolvedRate/commissionAmount/
  providerNet, **Σ line commission = transaction commission** §20.15, per-line yuvarlama korunur → P8/S8 mutabakatı) +
  `Payment.Abstraction` internal remote-call `ResolveLineCommissions` (mevcut `IPaymentModuleRemoteCall` escrow pattern'i,
  typed DTO, service-to-service `[Authorize]`) + **SR compute-on-demand önizleme** (offer builder şeffaflığı; authoritative
  persistence YOK, SR migration YOK). Exempt/pass-through (Travel/MarinaFee) → rate 0, providerNet = tam satır. Pure, no
  MarkApplied (P8). Tax pre-tax, KDV=YMM açık. SR yalnız `Payment.Abstraction` referanslar (boundary korunur).

## FE — Provider portalı (`inktavia-marine-provider-web`) → `FE_PROVIDER_*.md`
- **Teklif oluşturma ekranı (en büyük FE işi):** kalem ekleme (tür + pricing method + attribute), **price-book otomatik
  doldurma**, **travel otomatik hesap**, net/KDV/brüt **canlı**, per-line komisyon/net şeffaf, OfferType seçimi ·
  **change-order** akışı · recurring/next-due görünümü · dispute durumu.

## FE — Admin panel (`react-admin-panel-foundation`) → `FE_ADMIN_*.md`
- `PricingAttributeDefinition` · Provider price book · `TravelPricingRule` · `PartCommercialTerm` · recurring policy ·
  **Dispute Case inceleme + item-level release/refund/partial + audit** · change-order inceleme.

## Customer surface (açık nokta)
İşi onayla / itiraz et aksiyonu + tamamlama kanıtı görünümü + checkout kırılımı → **müşteri app'i bağlı repolarda yok**
(§21.10/§21.12). Karar bekliyor.

## Açık kararlar (ServiceRequest)
km oranı/tek-yön/ücretsiz-km · örnek fiyatlar (seed değil) · %10–20 parça indirimi · %30 ticari alan %20/%10 · komisyonun
parça/ulaşım kalemlerine uygulanması · zehirli boya 24 ay tüm müşteriler mi · teklif net mi brüt mü duyurulacak (§20.20).
