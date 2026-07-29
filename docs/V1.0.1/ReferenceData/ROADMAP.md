# ReferenceData Modülü — V1.0.1 Roadmap
> Fiyatlama ve ekonominin **referans veri** girdileri: FX/kur, kategori KDV, ölçü birimi, marine lookup'ları.
> Kanonik: `../COMMISSION_PACKAGE_PRICING.md`. Mevcut ReferenceData altyapısı (Currency/ExchangeRate/LookupGroup/
> MeasurementUnit) **genişletilir**, yeniden yazılmaz. Her faz → `BE_R<n>_*.md`.

## Backend fazları
| Faz | Kapsam | Kanonik § | Not |
|---|---|---|---|
| **R1** | **Currency + ExchangeRate** — provider price book FX köprüsü için "belirli tarihte kur çöz" erişimi (EUR/USD→TL). Mevcut `ExchangeRate` entity'si var; **teklif oluşturma anında kur snapshot** için resolve API'si netleştirilecek. | §20.7 | Mevcut yapı teyit + eksikse resolve endpoint |
| **R2** | **Kategori bazlı KDV oranı** yapılandırması (versiyonlu, effective-date). ServiceAmount/PlatformFee/line-VAT için. | §2.5, §13.3, §20.20 | **YMM kapısı** — oranlar YMM onayına bağlı |
| **R3** | **Ölçü birimi** (litre, km, m², saat, gün) — `PricingMethod` (PerLiter/PerKm/PerSquareMeter/PerHour/PerDay) için birim tanımları. | §20.4 | Mevcut MeasurementUnit genişletme |
| **R4** | **Marine pricing lookup'ları** — `EngineInstallationType` (içten/kıçtan), `EngineType/Class` (V12/V16…), `PaintType`, `WorkDifficulty` gibi değerler `LookupGroup`/`LookupItem` olarak; `PricingAttributeDefinition` (SR S2) bunlara referans verebilir. | §20.6 | Aday; SR ile koordine |

## FE — Admin panel (`react-admin-panel-foundation`) → `FE_ADMIN_*.md`
- Currency/ExchangeRate yönetimi (çoğunlukla **mevcut**) · **kategori KDV oranı** yönetimi (yeni) · ölçü birimi yönetimi ·
  marine lookup grupları (EngineInstallationType/EngineType/PaintType/WorkDifficulty) yönetimi.

## FE — Provider portalı
- Doğrudan yeni ekran yok; provider teklif ekranı (SR FE) bu lookup/birim/kur verilerini **tüketir**.

## Açık kararlar (ReferenceData)
- Kategori bazlı KDV oranları (YMM) · marine lookup'larının ReferenceData'da mı yoksa ServiceRequest config'inde mi
  tutulacağı (öneri: statik/paylaşımlı olanlar ReferenceData `LookupGroup`; kategoriye özel pricing attribute tanımları
  ServiceRequest) · kur kaynağı ve güncelleme sıklığı (mevcut ExchangeRate akışı).
