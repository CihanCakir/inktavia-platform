# Inktavia V1.0.1 — Ödeme Ekonomisi Büyük Revizyonu — Roadmap Index

Bu dizin, `COMMISSION_PACKAGE_PRICING.md` (kanonik, 21 bölüm) spesifikasyonunu **modül bazlı roadmap'lere** böler.
Her modül klasöründe önce `ROADMAP.md`, sonra faz bazında **backend prompt**'ları ve **frontend değişiklik spesifikasyonları**
(Provider portalı + Admin panel) ayrı dosyalar olarak eklenecektir.

## Kanonik kaynak
- `COMMISSION_PACKAGE_PRICING.md` — tek doğru kaynak. Roadmap'ler bu dosyanın **§ numaralarına** referans verir; kural/formül
  tekrarı yapılmaz.

## Klasör ve dosya konvansiyonu
```
docs/V1.0.1/
  COMMISSION_PACKAGE_PRICING.md        ← kanonik spesifikasyon
  ROADMAP_INDEX.md                     ← bu dosya
  Payment/         ROADMAP.md          → BE_P<n>_*.md · FE_ADMIN_*.md
  ServiceRequest/  ROADMAP.md          → BE_S<n>_*.md · FE_PROVIDER_*.md · FE_ADMIN_*.md
  ReferenceData/   ROADMAP.md          → BE_R<n>_*.md · FE_ADMIN_*.md
  Identity/        ROADMAP.md          → BE_I<n>_*.md · FE_PROVIDER_*.md · FE_ADMIN_*.md
  Notification/    ROADMAP.md          → BE_N<n>_*.md · (FE bildirim tipleri)
```
- **BE_ = Backend prompt** (Claude Code için, İngilizce, faz bazında).
- **FE_PROVIDER_ = ** `inktavia-marine-provider-web` değişiklikleri.
- **FE_ADMIN_ = ** admin panel (`react-admin-panel-foundation`) değişiklikleri.
- **Müşteri yüzeyi:** onay/itiraz + checkout kırılımı için ayrı müşteri app'i **bağlı repolarda yok** → açık karar
  (bkz. §21.10/§21.12). Roadmap'lerde "customer surface" olarak işaretli.

## Modüllerin rolü (özet)
| Modül | Ana sorumluluk | Kanonik referans |
|---|---|---|
| **Payment** | Ekonomi çekirdeği: snapshot, komisyon, platform fee, plan fiyatı, profit protection, indirim/benefit, refund/chargeback, iyzico gateway, raporlama | §1–19, §21 (para resolution) |
| **ServiceRequest** | Itemized teklif ekonomisi, pricing method/attribute, price book, travel, part terms, line-level discount/commission/snapshot, change-order, recurring, dispute case | §20, §21 (case) |
| **ReferenceData** | FX/kur, kategori KDV, ölçü birimi, marine lookup'ları | §20.7, §2.5/§13.3, §20.4/§20.6 |
| **Identity** | Provider iyzico sub-merchant onboarding, provider origin/hizmet alanı, participant (müşteri) plan bağı | Payment Model Decision, §20.8, §19.6 |
| **Notification** | Renewal fiyat bildirimi, recurring hatırlatma, dispute/chargeback bildirimleri | §13.2, §20.14, §21.9 |

## Global bağımlılık sırası (kritik yol)
1. **Payment P1–P5** (ekonomi çekirdeği: snapshot + komisyon + platform fee + plan fiyatı + profit protection policy)
2. **ReferenceData R1–R3** (FX + KDV + ölçü birimi — Payment/SR hesaplarının girdisi)
3. **ServiceRequest S1–S8** (itemized offer + line economics/snapshot) — Payment komisyon + ReferenceData'ya bağlı
4. **Payment P6–P8** (indirim/benefit + `CalculateServiceRequestPaymentEconomics` + kabul-anı hesaplama)
5. **ServiceRequest S9–S10** (line+transaction profit protection + kabul akışı)
6. **Payment P9** (iyzico gateway auth-mode + item-approve + **sandbox split testi = ÜRETİM KAPISI**)
7. **Payment P10–P12** (refund/chargeback + premium + raporlama) · **ServiceRequest S11–S13** (change-order + recurring + dispute)
8. **Identity I1–I3** (sub-merchant onboarding + location) — P9 öncesi hazır olmalı (sub-merchant yoksa split yok)
9. **Notification N1–N4** (paralel; ilgili event'ler devreye girince)

> **Üretim kapıları:** (a) iyzico **sandbox split testi** (§10.3/§21) geçmeden production yok. (b) KDV/fatura **YMM onayı**
> (§2.5/§13.3). (c) Kâr Koruma Motoru tamamlanmadan müşteri indirimi/komisyon avantajı production'a açılmaz (§19.21).

## Çalışma yöntemi
Her fazı sırayla: (1) ilgili modül `ROADMAP.md`'den fazı seç → (2) `BE_*` backend promptu üret (Claude Code) →
(3) uygula + doğrula → (4) gerekiyorsa `FE_PROVIDER_*` / `FE_ADMIN_*` spesifikasyonu + uygulama. CargoDry bu revizyonun
dışında; hazır gateway/webhook/commission resolver/snapshot/escrow/completion altyapısı yeniden yazılmaz (§17/§20.22/§21.13).
