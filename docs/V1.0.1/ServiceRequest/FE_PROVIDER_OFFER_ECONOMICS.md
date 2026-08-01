# FE_PROVIDER_OFFER_ECONOMICS — Teklif builder'ına komisyon/indirim/net önizleme paneli (S7 + S6)

> **Repo:** `inktavia-marine-provider-web`. **Backend:** S7 (`ResolveLineCommissions`) + S6 (`ResolveCustomerDiscountForOffer`),
> **BFF hazır** (BFF-Wave 5, runtime-verified): `POST /api/v1/provider/offers/commission-preview` +
> `POST /api/v1/provider/offers/customer-discount-preview` (compute-on-demand, lines-in, by-subject). Provider-web **canlı**.
> **İlke:** MEVCUT `OfferBuilder`'ı **genişlet**, yeniden yazma. Server-owns-totals kuralı korunur (FE hesap yapmaz, önizler).
> Nautical token, mevcut auto-save/preview pattern'i. Null-safe/degrade (split-eligible değilse veya endpoint hata verirse
> panel gizlenir/uyarı, builder çalışmaya devam eder).

## 0. Doğrulanan mevcut yüzey
- `src/features/service-requests/offer/OfferBuilder.tsx` — itemized builder; `useOfferDraft`; `offerApi` (saveDraft/preview/
  submit; "sends ONLY inputs, server computes totals"). `OfferComputed`: per-line `lineSubtotal/discountAmount/taxAmount` +
  totals `subtotal/discountTotal/taxTotal/grandTotal` — **müşteri-tarafı**, komisyon/provider-net YOK.
- `src/shared/api/endpoints.ts`: `serviceRequests.offerDraft/offerPreview/offerSubmit` var; **yeni provider offers preview
  endpoint'leri YOK** → eklenecek.
- `OfferItemInput`: `itemType, quantity, unitPrice, taxRate, discountType, discountValue` (+ S1 sonrası pricingMethod/
  commissionEligibility eklenebilir — backend hazır; FE input'a additive).

## 1. Endpoints + api adapter (additive)
- `endpoints.ts`'e ekle (provider BFF): `offers.commissionPreview = '/provider/offers/commission-preview'`,
  `offers.customerDiscountPreview = '/provider/offers/customer-discount-preview'` (base `/api/v1` httpClient'ta).
- `offerApi`'ye (veya yeni `offerEconomicsApi`) ekle: `commissionPreview(lines, providerContext)` + `customerDiscountPreview(...)`
  → POST **yalnız input satırları** (mevcut `toRequestBody` line-mapping'i yeniden kullan) → DTO:
  - commission: per-line `{lineRef, commissionable, commissionBase, resolvedRate, commissionAmount, providerNet}` +
    `{transactionCommission, transactionProviderNet, transactionCommissionBase}`.
  - discount: `{ requestedDiscount, funding {platform/provider/shared}, perLine[...] }` (S6 preview DTO — A+B/S6
    REPORT'taki gerçek alanlara hizala).
  `httpClient` (auth interceptor'lı, `Authorization: Bearer`) ile — provider by-subject.

## 2. OfferBuilder genişletme — "Sen ne alırsın" paneli
- Mevcut debounced preview/auto-save yanında **debounced economics preview** çağrısı (aynı satır input'ları değişince).
- Yeni panel (builder'ın altında/yanında, mevcut müşteri-tarafı total kartını bozmadan):
  - **Provider net (take-home):** `transactionProviderNet` — büyük, gold vurgulu.
  - **Komisyon:** `transactionCommission` + efektif oran; per-line açılır kırılım (satır → base × rate → commission → net).
  - **Müşteri indirimi (varsa):** requested discount + funding (platform/provider/shared) — provider-funded olan provider'ın
    payını düşürür, bunu şeffaf göster.
  - **Müşteri öder (net/KDV/brüt):** mevcut `OfferComputed` totalleri (zaten var) ile birlikte tek bakışta: "Müşteri öder X →
    komisyon Y → sen alırsın Z".
- **Exempt/pass-through satırlar** (Travel/MarinaFee): komisyon 0, providerNet=tam satır — panelde ayrışsın.
- **Null-safe:** endpoint hata/boş dönerse panel "önizleme alınamadı" ile gizlenir; builder + müşteri totalleri etkilenmez.
  Split-eligible değilse (FE_PROVIDER_I1 banner'ı) opsiyonel bir "ödeme kurulumun tamamlanınca net görünür" notu.

## 3. Bozmama / QA
- `OfferComputed`/server-owns-totals kuralı korunur; FE komisyon/net'i **hesaplamaz**, BFF'ten okur.
- Mevcut builder akışı (draft/preview/submit) değişmez; panel additive.
- i18n (tr+en): panel etiketleri (provider net, komisyon, efektif oran, müşteri indirimi/funding, exempt).
- Test: satır değişiminde debounced preview; per-line kırılım; exempt satır; funding split; endpoint-hata degrade; mevcut
  builder regresyonu; typecheck/build 0.

## 4. Ön koşul + not
- BFF endpoint'leri **hazır ve runtime-verified** (BFF-Wave 5). DTO adlarını S6/S7 + BFF-Wave 5 REPORT'larındaki gerçek
  alanlara hizala (varsayım değil).
- Provider'ın **aktif planı** BFF preview'da doğru orana yansır (P8'de plan çözümü; BFF by-subject provider'ı biliyor).
- **En yüksek değerli provider FE işi** — teklif ekonomisini provider'a şeffaf yapar (komisyondan sonra eline ne geçer).
