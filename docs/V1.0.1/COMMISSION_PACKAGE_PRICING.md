# Inktavia — Komisyon, Paket ve Ödeme Ekonomisi — Implementation-Ready Nihai Spesifikasyon

> **Durum:** IMPLEMENTATION-READY · +§ 19 Kâr Koruma Motoru (Profit Protection Engine) entegre   · +§ 20 Itemized Deniz-Servisi Fiyatlama & Ekonomi entegre · +§ 21 Escrow/İtiraz/Admin İnceleme (capture+escrow, chargeback)
> **Tarih:** 2026-07-27  
> **Dayanak:** `PAYMENT_MODEL_DECISION.md` ve önceki nihai ekonomik spesifikasyon  
> **Kanonik karar:** iyzico Marketplace + sub-merchant + escrow approval + split korunur. Inktavia müşteri fonlarını serbest banka hesabında tutmaz.  
> **Mühendislik ilkesi:** Hazır altyapıyı yeniden yazma; mevcut gateway, webhook, komisyon motoru, abonelik ve transaction yapılarını kontrollü biçimde genişlet.  
> **Üretim kapısı:** Vergi/fatura konfigürasyonu YMM tarafından onaylanmadan ve iyzico sandbox split/refund kanıtları tamamlanmadan production aktivasyonu yapılmaz.

---

## 1. Kesin iş kararları

- **Komisyon oranları:** FREE `%15`, STANDARD `%12`, PREMIUM_PARTNER `%9`.
- Komisyon oranı, komisyon matrahı ve tüm parasal sonuçlar teklif **kabul edildiği anda immutable snapshot** olarak sabitlenir.
- **Provider komisyonu müşteri platform ücretinden hesaplanmaz.** Komisyon matrahı ayrı `CommissionBaseAmountSnapshot` alanıdır.
- Plan fiyatları tarih aralıklı ve versiyonlu tutulur:
  - FREE: `0 ₺`
  - STANDARD lansman: `499 ₺`; liste: `1.490 ₺`
  - PREMIUM_PARTNER lansman: `999 ₺`; liste: `3.490 ₺`
- Lansman fiyatı, platformun ticari go-live tarihinden başlayan **global altı aylık kampanyadır**. Her provider için ayrı başlayan altı aylık dönem değildir.
- Müşteri platform ücreti yapılandırılabilir hibrit modeldir:
  - oran `%2,5`
  - minimum `99 ₺`
  - maksimum `1.500 ₺`
- Komisyon çözümlemesi override yaklaşımıyla çalışır; kural kombinasyonları için bağlayıcı spesifiklik sırası uygulanır.
- Premium MVP yalnızca `OFFER_BOOST_7D` ürünüdür.
- Brüt platform payı ile iyzico kesintileri sonrası banka hesabına geçen net platform settlement tutarı ayrı tutulur.

---

## 2. Terminoloji ve KDV dahil/hariç para tanımları

### 2.1 Hizmet tutarı

`ServiceAmount` kullanıcıya gösterilen tek bir belirsiz alan olarak bırakılmaz. Teknik model aşağıdaki ayrımı taşır:

- `ServiceNetAmountSnapshot`: Provider hizmetinin vergi hariç bedeli.
- `ServiceVatAmountSnapshot`: Provider hizmetine ilişkin KDV/vergi tutarı.
- `ServiceGrossAmountSnapshot`: Müşterinin provider hizmeti için ödeyeceği KDV dahil toplam.

Bu dokümandaki `ServiceAmount` kısa adı, ödeme ve checkout bağlamında **`ServiceGrossAmountSnapshot`** anlamına gelir.

### 2.2 Komisyon matrahı

- `CommissionBaseAmountSnapshot` bağımsız ve zorunlu olarak saklanır.
- Bu alan `ServiceGrossAmountSnapshot` veya `ServiceNetAmountSnapshot` değerlerinden hangisinin sözleşmesel komisyon matrahı olduğuna göre doldurulur.
- **Launch varsayılanı:** mevcut business kararı korunarak `CommissionBaseAmountSnapshot = ServiceGrossAmountSnapshot`.
- YMM ve provider sözleşmesi farklı bir vergi matrahı gerektirirse yalnızca versiyonlu `CommissionBasePolicy` değiştirilir; geçmiş snapshot’lar değiştirilmez.

### 2.3 Platform hizmet bedeli

Platform ücreti üç ayrı snapshot alanıyla tutulur:

- `PlatformFeeNetAmountSnapshot`
- `PlatformFeeVatAmountSnapshot`
- `PlatformFeeGrossAmountSnapshot`

`PlatformFeeRule` yüzde/minimum/maksimum hesabını **net tutar** üzerinde üretir. İlgili KDV oranı `PlatformFeeTaxPolicy` üzerinden çözülür ve müşteriye gösterilecek brüt tutar hesaplanır.

### 2.4 Müşteri toplamı

`CustomerTotalAmountSnapshot`, müşterinin kartından çekilecek **nihai KDV dahil tutardır**:

```text
CustomerTotalAmountSnapshot
  = ServiceGrossAmountSnapshot
  + PlatformFeeGrossAmountSnapshot
```

Checkout, iyzico `price`, ödeme kaydı ve müşteri dekontunda aynı tutar kullanılmalıdır.

### 2.5 YMM doğrulama kapısı

Aşağıdaki konular YMM ve hukuk görüşüyle doğrulanır:

- provider hizmet faturasının tarafları ve KDV yapısı,
- Inktavia komisyon faturasının matrahı,
- platform hizmet bedelinin KDV oranı,
- minimum/maksimum platform ücretlerinin net mi brüt mü ticari olarak duyurulacağı,
- iade halinde KDV düzeltme belgeleri.

Teknik model hem net hem vergi hem brüt tutarı taşıdığı için YMM kararı migration gerektirmeden policy konfigürasyonuyla uygulanabilir.

---

## 3. Bağlayıcı formüller ve yuvarlama standardı

### 3.1 Yuvarlama

- Tüm parasal hesaplamalar .NET `decimal` ile yapılır; `double`/`float` kullanılmaz.
- TRY tutarları 2 ondalık basamakta saklanır.
- Bağlayıcı yöntem: `MidpointRounding.AwayFromZero`.
- Her parasal bileşen yalnızca tanımlanan hesaplama noktasında yuvarlanır.
- Provider neti tekrar yüzde hesabıyla üretilmez; kesin çıkarma ile türetilir.

### 3.2 Formüller

```text
CommissionAmountSnapshot
  = Round(CommissionBaseAmountSnapshot × CommissionRateSnapshot, 2)

ProviderNetAmountSnapshot
  = ServiceGrossAmountSnapshot − CommissionAmountSnapshot

PlatformFeeNetAmountSnapshot
  = Round(ApplyPlatformFeeRule(ServiceGrossAmountSnapshot), 2)

PlatformFeeVatAmountSnapshot
  = Round(PlatformFeeNetAmountSnapshot × PlatformFeeVatRateSnapshot, 2)

PlatformFeeGrossAmountSnapshot
  = PlatformFeeNetAmountSnapshot + PlatformFeeVatAmountSnapshot

CustomerTotalAmountSnapshot
  = ServiceGrossAmountSnapshot + PlatformFeeGrossAmountSnapshot

PlatformGrossShareSnapshot
  = CustomerTotalAmountSnapshot − ProviderNetAmountSnapshot
```

### 3.3 Finansal invariant’lar

```text
ProviderNetAmountSnapshot + PlatformGrossShareSnapshot
  == CustomerTotalAmountSnapshot

PlatformFeeNetAmountSnapshot + PlatformFeeVatAmountSnapshot
  == PlatformFeeGrossAmountSnapshot

ServiceNetAmountSnapshot + ServiceVatAmountSnapshot
  == ServiceGrossAmountSnapshot
```

Hiçbir finansal eşitlikte `0,01 ₺` dahil tolerans uygulanmaz. Eşitlik sağlanmıyorsa ödeme başlatılmaz ve domain hatası üretilir.

---

## 4. ProviderPlanPrice çözümleme algoritması

### 4.1 Veri modeli

`ProviderPlanPrice`:

- `ProviderPlanId`
- `CurrencyCode`
- `BillingPeriod` (`Monthly`, ileride `Annual`)
- `PriceAmount`
- `PricePurpose` (`Launch`, `List`, `Campaign` — raporlama metadata’sı)
- `EffectiveFrom`
- `EffectiveTo` nullable
- `Status`

### 4.2 Tarih aralığı kuralı

Tüm fiyat aralıkları yarı açık aralık olarak değerlendirilir:

```text
[EffectiveFrom, EffectiveTo)
```

- `EffectiveFrom` dahildir.
- `EffectiveTo` dahil değildir.
- `EffectiveTo = null`, süresiz gelecek anlamına gelir.

### 4.3 Lansman ve liste fiyat aralıkları

Örnek:

```text
GoLiveAt = 2026-10-01T00:00:00+03:00
LaunchEndAt = GoLiveAt.AddMonths(6)

Launch: [2026-10-01, 2027-04-01)
List:   [2027-04-01, ∞)
```

Launch ve List aynı anda aktif kayıtlar olarak yarıştırılmaz. Seçim yalnız tarih aralığına göre yapılır.

### 4.4 Çözümleme

`ResolveProviderPlanPrice(planId, currency, billingPeriod, at)`:

1. Status = Active olan kayıtları filtrele.
2. Plan, currency ve billing period eşleşmesini uygula.
3. `EffectiveFrom <= at` ve (`EffectiveTo == null` veya `at < EffectiveTo`) şartını uygula.
4. Sonuç tam olarak bir kayıt olmalıdır.
5. Sonuç yoksa `ProviderPlanPriceNotConfigured` hatası üret.
6. Birden fazla sonuç varsa `ProviderPlanPriceConfigurationConflict` üret; sessiz seçim yapma.

### 4.5 Çakışma koruması

Aynı `ProviderPlanId + CurrencyCode + BillingPeriod` için iki aktif kayıt tarihsel olarak örtüşemez. Bu kural:

- command validator,
- repository conflict query,
- mümkünse PostgreSQL exclusion constraint veya eşdeğer DB koruması

ile savunulur.

### 4.6 Renewal politikası

- Provider mevcut dönemi satın alma anındaki `SubscriptionPriceSnapshot` ile tamamlar.
- Yenileme tarihinde fiyat yeniden çözülür.
- Eski aboneliğin snapshot fiyatı geriye dönük değişmez.
- Fiyat değişecekse provider’a varsayılan olarak yenilemeden **7 takvim günü önce** bildirim gönderilir.
- `PriceChangeNotificationLeadDays` yapılandırılabilir system parameter’dır.
- Bildirim gönderilmemesi finansal snapshot’ı değiştirmez; fakat renewal compliance alarmı üretir.

---

## 5. Immutable PaymentEconomicsSnapshot

### 5.1 Tek kanonik ekonomik kayıt

Teklif kabulünde yalnız bir kez `PaymentEconomicsSnapshot` oluşturulur. Bu kayıt immutable’dır ve aşağıdakiler tarafından referans edilir:

- `PaymentTransaction`
- `MarketplaceSettlement`
- `RefundAllocation`
- `InvoiceCorrelation`
- finansal raporlama ledger kayıtları

Transaction ve settlement ekonomik tutarları bağımsız biçimde yeniden hesaplamaz.

### 5.2 Zorunlu alanlar

- `ServiceNetAmountSnapshot`
- `ServiceVatAmountSnapshot`
- `ServiceGrossAmountSnapshot`
- `CommissionBasePolicyIdSnapshot`
- `CommissionBaseAmountSnapshot`
- `CommissionRuleIdSnapshot`
- `CommissionRateSnapshot`
- `CommissionAmountSnapshot`
- `ProviderNetAmountSnapshot`
- `PlatformFeeRuleIdSnapshot`
- `PlatformFeeRateSnapshot`
- `PlatformFeeMinimumSnapshot`
- `PlatformFeeMaximumSnapshot`
- `PlatformFeeNetAmountSnapshot`
- `PlatformFeeVatRateSnapshot`
- `PlatformFeeVatAmountSnapshot`
- `PlatformFeeGrossAmountSnapshot`
- `CustomerTotalAmountSnapshot`
- `PlatformGrossShareSnapshot`
- `CurrencyCodeSnapshot`
- `RoundingModeSnapshot`
- `CreatedAt`

### 5.3 Settlement’ın sorumluluğu

`MarketplaceSettlement` yalnız gerçekleşen sonuçları tutar:

- `ProviderSettledAmount`
- `PlatformGrossSettledAmount`
- `PaymentProcessingExpenseActual`
- `GatewayOtherExpenseActual`
- `RefundProcessingExpenseActual`
- `PlatformSettlementNetAmount`
- `GatewaySettlementReference`
- `SettlementStatus`
- `SettledAt`

```text
PlatformSettlementNetAmount
  = PlatformGrossSettledAmount
  − PaymentProcessingExpenseActual
  − GatewayOtherExpenseActual
  − RefundProcessingExpenseActual
```

`PlatformGrossShareSnapshot`, teorik kabul-anı payıdır. `PlatformSettlementNetAmount`, gateway kesintileri sonrası fiilen Inktavia’ya kalan net tutardır. Aynı alan değildir.

---

## 6. CommissionRule spesifiklik ve conflict algoritması

### 6.1 Bağlayıcı sıra

1. `Provider + Plan + Category`
2. `Provider + Category`
3. `Provider + Plan`
4. `Provider`
5. `Plan + Category`
6. `Plan`
7. `Category`
8. `Global`

### 6.2 Çözümleme sırası

1. İşlem tarihi ve status filtresi uygulanır.
2. Context, product, sales channel, commercial model ve currency gibi diğer boyutlar eşleştirilir.
3. Yukarıdaki kombinasyona göre `SpecificityScore` hesaplanır.
4. `SpecificityScore DESC` sıralanır.
5. Aynı score içindeyse `Priority DESC` uygulanır.
6. En üst score ve priority’de birden fazla kayıt kalırsa `CommissionRuleConfigurationConflict` üretilir.
7. Tek kuralın nihai oranı snapshot’a yazılır; alt kurallar toplanmaz.

### 6.3 Çakışma denetimi

Aynı scope, aynı specificity, aynı priority ve örtüşen effective range için iki aktif kural oluşturulamaz. Admin UI kayıt öncesi uyarı verir; backend her durumda reddeder.

---

## 7. RefundAllocationPolicy

### 7.1 Temel modeller

`RefundCause`:

- `ProviderCancelled`
- `CustomerCancelledBeforeWork`
- `CustomerCancelledAfterWorkStarted`
- `DisputeCustomerFavoured`
- `DisputeProviderFavoured`
- `TechnicalFailure`
- `DuplicatePayment`
- `AdministrativeCorrection`

`ReleaseState`:

- `BeforeProviderRelease`
- `AfterProviderRelease`

`PlatformFeeRefundMode`:

- `Full`
- `ProRata`
- `None`
- `FixedAmount`
- `RuleBased`

### 7.2 Release öncesi refund

Provider payı henüz serbest bırakılmadıysa:

- provider settlement oluşturulmaz,
- iade edilen service payına karşılık gelen provider net iptal edilir,
- komisyon geliri aynı oranda ters kayıtla düzeltilir,
- platform fee iadesi `RefundAllocationPolicy` ile belirlenir,
- gateway refund expense ayrı gider kaydıdır.

Varsayılan MVP politikası:

| Sebep | Service refund | Platform fee |
|---|---:|---:|
| ProviderCancelled | Tam | Tam |
| TechnicalFailure | Tam | Tam |
| DuplicatePayment | Tam | Tam |
| CustomerCancelledBeforeWork | İptal politikasına göre | Varsayılan tam |
| CustomerCancelledAfterWorkStarted | Onaylanan tutar | RuleBased |
| DisputeCustomerFavoured | Karar tutarı | RuleBased/Tam |
| DisputeProviderFavoured | Yok veya karar tutarı | None |

### 7.3 Release sonrası refund

Provider’a ödeme yapıldıktan sonra refund gerekiyorsa geri kazanım sırası:

1. iyzico/gateway clawback veya sub-merchant refund kabiliyeti,
2. provider’ın henüz settle edilmemiş kullanılabilir bakiyesi,
3. sonraki provider hakedişlerinden mahsup,
4. `ProviderNegativeBalance` oluşturma,
5. manuel tahsilat/borç takibi.

Müşteriye hukuken/operasyonel olarak iade hemen yapılmak zorundaysa ve provider’dan para henüz geri alınamadıysa:

- `PlatformAdvancedRefundAmount` kaydedilir,
- bu tutar Inktavia’nın provider’dan alacağıdır,
- sonraki settlement’lardan otomatik mahsup edilir.

### 7.4 Provider negative balance

- Provider bakiyesi eksiye düşebilir fakat bu bakiye ayrı ledger’da tutulur.
- Negatif bakiye varken provider’a yeni payout yapılmadan önce mahsup uygulanır.
- Belirlenen `NegativeBalanceLimit` aşılırsa payout ve yeni teklif kabulü risk policy’sine göre durdurulabilir.
- Admin manuel override işlemleri audit log gerektirir.

### 7.5 Refund allocation kaydı

Her iade için aşağıdaki kırılım saklanır:

- `ServiceRefundAmount`
- `ProviderNetReversalAmount`
- `CommissionRevenueReversalAmount`
- `PlatformFeeNetRefundAmount`
- `PlatformFeeVatRefundAmount`
- `PlatformFeeGrossRefundAmount`
- `GatewayRefundExpenseAmount`
- `ProviderRecoveryAmount`
- `PlatformAdvancedRefundAmount`
- `RemainingProviderNegativeBalance`

Toplam iade, gateway’e gönderilen iade tutarıyla birebir eşleşmelidir.

---

## 8. Payment/webhook idempotency ve concurrency

### 8.1 Inbox ve benzersizlik

`GatewayEventInbox` tablosu:

- `GatewayProvider`
- `GatewayEventId`
- `EventType`
- `PayloadHash`
- `ReceivedAt`
- `ProcessedAt`
- `ProcessingStatus`
- `FailureReason`

Unique constraint:

```text
(GatewayProvider, GatewayEventId)
```

Gateway event ID sağlanmıyorsa provider’a özgü deterministik idempotency key üretilir.

### 8.2 İşlem garantileri

Aynı gateway event:

- ikinci `PaymentTransaction` oluşturamaz,
- escrow’u ikinci kez approve edemez,
- ikinci settlement oluşturamaz,
- aynı refund allocation’ı ikinci kez oluşturamaz,
- aynı purchase için ikinci entitlement oluşturamaz.

### 8.3 Concurrency koruması

- Kritik aggregate’larda optimistic concurrency token (`rowversion`/xmin veya explicit version) kullanılır.
- State transition ve ledger yazımı tek DB transaction içinde yapılır.
- Dış gateway çağrısı için idempotency key saklanır.
- Outbox pattern ile webhook sonrası entitlement ve bildirim olayları güvenilir yayımlanır.
- Unique constraint domain servisinin son savunmasıdır; yalnız uygulama seviyesindeki `if exists` kontrolüne güvenilmez.

### 8.4 Geçerli state transition’lar

Payment:

```text
Pending → Captured → AwaitingRelease → Released
Pending/Captured/AwaitingRelease → Failed/Cancelled
Captured/AwaitingRelease/Released → PartiallyRefunded/Refunded
```

Entitlement:

```text
PendingPayment → Active
Active → Expired
Active → Revoked
PendingPayment → Cancelled
```

Geçersiz transition domain exception üretir ve audit edilir.

---

## 9. Premium ürün, fiyat ve entitlement modeli

### 9.1 Entity’ler

`PremiumProduct`:

- `Code`
- `Name`
- `EntitlementType`
- `Status`

`PremiumProductPrice`:

- `PremiumProductId`
- `CurrencyCode`
- `PriceAmount`
- `EffectiveFrom`
- `EffectiveTo`
- `Status`

`PremiumPurchase`:

- `ProviderProfileId`
- `PremiumProductId`
- `PremiumProductPriceIdSnapshot`
- `UnitPriceSnapshot`
- `CurrencyCodeSnapshot`
- `DurationDaysSnapshot`
- `PaymentTransactionId`
- `Status`

`PremiumEntitlement`:

- `PremiumPurchaseId`
- `ProviderProfileId`
- `ProductCodeSnapshot`
- `Status`
- `StartsAt`
- `ExpiresAt`
- `RevokedAt`
- `RevocationReason`

### 9.2 MVP ürünü

`OFFER_BOOST_7D`:

- tek seferlik non-marketplace ödeme,
- belirli bir offer için 7 günlük entitlement,
- ödeme başarılı webhook’u işlenmeden Active olamaz,
- refund tamamlanırsa Revoked olur,
- aynı purchase için en fazla bir entitlement bulunur.

Premium ürün fiyat aralıkları da plan fiyatları gibi çakışamaz.

---

## 10. iyzico ekonomik ayrımı ve sandbox doğrulaması

### 10.1 Gönderilecek değerler

```text
iyzico.price = CustomerTotalAmountSnapshot
Σ basketItem.price = CustomerTotalAmountSnapshot
subMerchantPrice = ProviderNetAmountSnapshot
price − subMerchantPrice = PlatformGrossShareSnapshot
```

Platform ücretinin ayrı sub-merchant’sız basket item olarak gönderilebildiği varsayılmaz. Sandbox ve iyzico teknik/sözleşmesel onayı sonucu:

- destekleniyorsa ayrık basket item,
- desteklenmiyorsa tek item + `subMerchantPrice` yaklaşımı

kullanılır.

### 10.2 Gerçek settlement

Gateway dönüşü ve settlement raporundan aşağıdakiler kaydedilir:

- provider’a fiilen aktarılan tutar,
- Inktavia brüt settlement payı,
- payment processing kesintisi,
- diğer gateway kesintileri,
- refund processing kesintisi,
- net platform settlement.

Beklenen ve gerçekleşen tutarlar eşleşmezse otomatik reconciliation exception açılır.

### 10.3 Production kapısı

Aşağıdaki sandbox senaryoları kanıtlanmadan production tamamlanmış sayılmaz:

1. split matematiği,
2. approve sonrası provider ve ana merchant dağılımı,
3. ayrı platform fee basket item desteği,
4. release öncesi tam/kısmi refund,
5. release sonrası tam/kısmi refund veya clawback davranışı,
6. gateway fee’nin hangi paydan kesildiği,
7. non-marketplace boost payment webhook’u,
8. duplicate webhook/idempotency.

---

## 11. Impact analysis

| Alan | Mevcut | Genişletme |
|---|---|---|
| iyzico gateway | Marketplace checkout, approve, refund mevcut | CustomerTotal/ProviderNet ayrımı, idempotency key, settlement detayları |
| Webhook | `ProcessIyzicoWebhook` mevcut | Inbox, duplicate guard, premium event routing, outbox |
| Komisyon motoru | 4 katman mevcut | Spesifiklik sırası, conflict exception, snapshot referansı |
| Plan fiyatı | `MonthlyPriceTRY` | `ProviderPlanPrice` tarih aralıkları ve renewal çözümleme |
| Subscription | PaidAmount snapshot mevcut | Price version ID ve renewal notification policy |
| Payment transaction | Gross/commission/net alanları | `PaymentEconomicsSnapshotId` referansı |
| Settlement | Ekonomik alanlar tekrar tutulabilir | Yalnız actual settled/expense/net sonuçları |
| Platform fee | Yok | Rule + tax policy + net/vat/gross snapshot |
| Refund | Tutar bazlı | Allocation, release state, negative balance, recovery |
| Premium | Yok | Product, price version, purchase, entitlement |
| Raporlama | Kısmi | Gross share, actual expense, net contribution ayrımı |

---

## 12. Migration planı

1. `provider_plan_prices` oluştur.
2. `payment_economics_snapshots` oluştur.
3. `platform_fee_rules` ve `platform_fee_tax_policies` oluştur.
4. `marketplace_settlements` actual settlement alanlarını ekle/güncelle.
5. `refund_allocation_policies`, `refund_allocations`, `provider_negative_balances` oluştur.
6. `gateway_event_inbox` ve outbox tablosu yoksa `integration_outbox` oluştur.
7. `premium_products`, `premium_product_prices`, `premium_purchases`, `premium_entitlements` oluştur.
8. `payment_transactions`, settlement ve refund kayıtlarına `PaymentEconomicsSnapshotId` ekle.
9. `ProviderPlanSubscription` üzerine `ProviderPlanPriceIdSnapshot`, `PriceAmountSnapshot`, `CurrencyCodeSnapshot` ekle.
10. Legacy transaction backfill:
   - `ServiceGross = GrossAmount`
   - `ServiceNet = GrossAmount`, `ServiceVat = 0` yalnız legacy marker ile
   - `PlatformFee* = 0`
   - `CustomerTotal = GrossAmount`
   - `ProviderNet = mevcut NetAmount`
   - `PlatformGrossShare = CustomerTotal − ProviderNet`
11. Seed:
   - go-live config üzerinden launch ve list tarih aralıkları,
   - plan komisyon kuralları,
   - platform fee default rule,
   - tax policy placeholder/approved config,
   - `OFFER_BOOST_7D` ve fiyatı.
12. Migration’lar append-only, idempotent ve duplicate-seed korumalı olmalıdır.

---

## 13. Yeni kararlar için uygulama sözleşmesi

### 13.1 ProviderPlanPrice tarihsel çözümleme

**Domain impact**
- `ProviderPlanPrice` aggregate/repository/resolve query.
- Overlap domain rule ve configuration conflict exception.

**Migration impact**
- Yeni tablo ve plan subscription price snapshot kolonları.
- Launch/List seed’leri birbirini izleyen aralıklarla oluşturulur.

**Acceptance criteria**
- Her plan/currency/billing period/tarih için tam bir fiyat çözülür.
- Aynı anda iki fiyat çözülemez.
- Renewal eski snapshot’ı değiştirmez.

**Test cases**
- Launch başlangıç anı.
- Launch bitişinden 1 saniye önce.
- List başlangıç anı.
- Overlap kayıt reddi.
- Fiyat bulunamaması hatası.

### 13.2 Global launch ve renewal bildirimi

**Domain impact**
- Go-live campaign config.
- Renewal price resolve ve notification policy.

**Migration impact**
- System parameter/notification log alanları.

**Acceptance criteria**
- Provider kayıt tarihi kampanya süresini değiştirmez.
- Mevcut dönem snapshot fiyatıyla tamamlanır.
- Yeni fiyat renewal tarihinde uygulanır.
- Fiyat değişikliği 7 gün önce bildirilir veya compliance alarmı oluşur.

**Test cases**
- Go-live ilk gün kayıt olan provider.
- Go-live’dan 5 ay sonra kayıt olan provider.
- Kampanya bittikten sonra yeni abonelik.
- Kampanya içinde başlayıp sonrasında yenilenen abonelik.

### 13.3 KDV net/vergi/brüt ayrımı

**Domain impact**
- Service ve platform fee için net/vat/gross money fields.
- `PlatformFeeTaxPolicy` ve version snapshot.

**Migration impact**
- Snapshot kolonları ve legacy tax marker.

**Acceptance criteria**
- CustomerTotal karttan çekilen nihai brüt tutardır.
- Checkout, payment ve invoice correlation aynı tutarı kullanır.
- Vergi policy değişikliği geçmiş işlemleri değiştirmez.

**Test cases**
- `%0`, standart ve farklı KDV oranları.
- Min/maks platform fee + KDV.
- Legacy kaydın backfill’i.

### 13.4 Immutable PaymentEconomicsSnapshot

**Domain impact**
- Yeni immutable aggregate/value object.
- Transaction/settlement/refund referansları.

**Migration impact**
- Snapshot tablosu ve foreign key’ler.

**Acceptance criteria**
- Teklif kabulünden sonra snapshot update edilemez.
- Settlement ekonomik formülü tekrar çalıştırmaz.
- Refund aynı snapshot’tan allocation üretir.

**Test cases**
- Sonradan plan/rule/fiyat değişikliği.
- Snapshot update denemesi.
- Aynı snapshot’a bağlı transaction ve refund.

### 13.5 Refund ve provider recovery

**Domain impact**
- Refund cause/release state/policy/allocation/negative balance.

**Migration impact**
- Refund policy, allocation ve negative balance tabloları.

**Acceptance criteria**
- Release öncesi ve sonrası refund farklı yollarla işlenir.
- Provider’dan geri alınamayan tutar görünür alacak olur.
- Sonraki payout negatif bakiyeyi önce kapatır.
- Refund toplamı allocation toplamıyla eşleşir.

**Test cases**
- Provider cancellation tam refund.
- Customer partial refund before release.
- Full refund after release.
- Insufficient provider balance.
- Future payout offset.
- Dispute provider/customer lehine.

### 13.6 Yuvarlama ve invariant

**Domain impact**
- Merkezi `MoneyRoundingPolicy`.
- Payment economics invariant validation.

**Migration impact**
- `RoundingModeSnapshot` veya policy version alanı.

**Acceptance criteria**
- Tüm katmanlarda aynı sonuç üretilir.
- 0,01 ₺ farkla ödeme başlatılmaz.
- Provider net çıkarma ile hesaplanır.

**Test cases**
- 3+ ondalık üreten oranlar.
- Min/maks sınırları.
- Midpoint değerler.
- Büyük tutarlar.

### 13.7 CommissionRule spesifikliği

**Domain impact**
- `SpecificityScore`, deterministic resolver, conflict exception.

**Migration impact**
- Gerekirse computed metadata/index; mevcut rule verisi conflict audit’inden geçirilir.

**Acceptance criteria**
- Bağlayıcı sekiz seviyeli sıra uygulanır.
- Alt kurallar toplanmaz.
- Aynı üst score/priority sonucu error üretir.

**Test cases**
- Her specificity seviyesi.
- Provider+Plan vs Provider+Category.
- Priority tie.
- Effective range conflict.

### 13.8 Idempotency ve concurrency

**Domain impact**
- Gateway inbox, state machine, optimistic concurrency, outbox.

**Migration impact**
- Inbox/outbox tabloları, unique index’ler, version kolonu.

**Acceptance criteria**
- Aynı event bir kez finansal etki üretir.
- Aynı escrow/refund/entitlement ikinci kez oluşamaz.
- Concurrent işlemlerden yalnız biri state geçişi yapar.

**Test cases**
- Aynı webhook’un 2/10 kez gönderilmesi.
- Eşzamanlı approve.
- Eşzamanlı refund.
- Webhook işlenirken process crash ve retry.

### 13.9 PremiumProductPrice snapshot

**Domain impact**
- Product/price/purchase/entitlement ayrımı.

**Migration impact**
- Dört yeni tablo ve unique purchase-entitlement ilişkisi.

**Acceptance criteria**
- Geçmiş purchase fiyat değişikliğinden etkilenmez.
- Başarılı ödeme öncesi entitlement aktif olmaz.
- Refund entitlement’ı revoke eder.

**Test cases**
- Fiyat değişikliği öncesi/sonrası satın alma.
- Duplicate webhook.
- Refund.
- Expiration.

### 13.10 PlatformGrossShare ve PlatformSettlementNetAmount

**Domain impact**
- Expected economics ile actual settlement ayrımı.
- Reconciliation service.

**Migration impact**
- Settlement expense ve net kolonları.

**Acceptance criteria**
- Brüt pay kabul anında sabittir.
- Net settlement gateway kesintilerinden türetilir.
- Beklenen/gerçekleşen fark raporlanır.

**Test cases**
- Gateway fee normal kesinti.
- Ek gateway expense.
- Refund processing fee.
- Settlement mismatch alarmı.

---

## 14. Frontend işleri

### Müşteri checkout

- Hizmet bedeli (KDV dahil)
- Platform hizmet bedeli (KDV dahil)
- Nihai toplam
- İade/iptal politikasına bağlantı

### Provider

- Hizmet brüt tutarı
- Uygulanan komisyon oranı ve komisyon tutarı
- Beklenen provider neti
- Settlement sonrası gerçekleşen ödeme
- Negatif bakiye/mahsup bilgisi varsa açık gösterim
- Abonelikte mevcut dönem snapshot fiyatı ve sonraki renewal fiyatı

### Admin

- Plan fiyat zaman çizelgesi ve overlap uyarısı
- CommissionRule specificity önizlemesi/conflict uyarısı
- PlatformFeeRule ve tax policy yönetimi
- Refund allocation ve provider negative balance ekranı
- Gateway inbox/idempotency gözlem ekranı
- Premium product/price/entitlement ekranı
- Expected vs actual settlement reconciliation ekranı

---

## 15. Finansal raporlama

Ayrı ledger kalemleri:

- `ProviderCommissionRevenue`
- `CustomerPlatformFeeNetRevenue`
- `CustomerPlatformFeeVatLiability`
- `SubscriptionRevenue`
- `PremiumProductRevenue`
- `PaymentProcessingExpense`
- `GatewayOtherExpense`
- `RefundProcessingExpense`
- `ChargebackExpense`
- `DiscountExpense`
- `ProviderRecoveryReceivable`
- `PlatformAdvancedRefundAmount`
- `PlatformGrossShare`
- `PlatformSettlementNetAmount`
- `NetMarketplaceContribution`

```text
NetMarketplaceContribution
  = Revenue accounts
  − Payment/Refund/Chargeback/Discount expenses
```

Vergi yükümlülüğü gelir olarak sayılmaz.

---

## 16. Bağımlılık sıralı faz planı

```text
a → b → c → d → e → f → g → h → i → j → k
```

- **a.** ProviderPlanPrice + renewal policy
- **b.** CommissionRule specificity/conflict
- **c.** PlatformFeeRule + tax policy
- **d.** Immutable PaymentEconomicsSnapshot + rounding
- **e.** Kabul-anı hesaplama ve checkout contract
- **f.** Gateway inbox/idempotency/concurrency
- **g.** iyzico sandbox split/settlement doğrulaması
- **h.** RefundAllocation + provider negative balance/recovery
- **i.** Checkout/provider/admin frontend
- **j.** PremiumProductPrice + OFFER_BOOST_7D
- **k.** Financial ledger, reconciliation ve raporlama

### Üretim kapıları

- `(g)` tamamlanmadan marketplace ödeme production’a alınmaz.
- YMM vergi/fatura policy onayı olmadan platform fee production’da aktif edilmez.
- Release sonrası refund/clawback davranışı kanıtlanmadan otomatik provider release açılmaz.
- Idempotency ve concurrency testleri geçmeden webhook production endpoint’i aktive edilmez.

---

## 17. Yeniden yazılmayacak mevcut altyapı

Önce incelenecek ve genişletilecek:

- `IPaymentGatewayProvider`
- `IyzicoMarketplacePaymentGatewayProvider`
- `ProcessIyzicoWebhook`
- `CommissionRule`
- `ResolveCommissionRate`
- `ProviderPlan`
- `ProviderPlanSubscription`
- `PaymentTransactionEntity`
- mevcut iyzico basket/checkout modelleri
- mevcut fatura motoru ve `VatOnCommission`

Yeni geliştirme, bu sınıfların sorumluluklarını bozacak paralel gateway veya ikinci komisyon motoru oluşturamaz.

---

## 18. Definition of Done

Doküman teknik olarak **implementation-ready** kabul edilir. Bir fazın tamamlanmış sayılması için:

1. Domain modeli ve invariant’lar uygulanmış olmalı.
2. Migration ve backfill başarılı olmalı.
3. Acceptance criteria testlerle kanıtlanmalı.
4. Duplicate/concurrency testleri geçmeli.
5. Audit log ve finansal correlation mevcut olmalı.
6. İlgili sandbox kanıtı dokümante edilmeli.
7. Frontend ve backend aynı snapshot değerlerini göstermeli.
8. Hazır altyapı yeniden yazılmamış, kontrollü genişletilmiş olmalı.



---

## 19. ServiceRequest — Kâr Koruma Motoru (Profit Protection Engine)
> **Kapsam:** yalnızca ServiceRequest Marketplace ödeme ekonomisi (müşteri üyelik/kategori indirimleri, provider plan
> komisyonları, provider komisyon avantajları, müşteri/provider tarafı minimum kârlılık, işlem bazlı toplam kâr koruması).
> **Dahil DEĞİL:** CargoDry satış/komisyon/renewal/inventory/consignment/muhasebe. Hazır altyapı (§17) yeniden yazılmaz;
> kontrollü genişletilir. Mevcut bölümler tekrar edilmez, referans verilir.

### 19.0 Bu revizyonda ne değişti? (teknik olmayan özet)
- Eski yapı yalnızca **paranın doğru dağıtıldığını** kontrol ediyordu (§3.3 invariant).
- Yeni yapı işlemin **Inktavia için kârlı olup olmadığını** da kontrol ediyor.
- **Müşteri indirimi** ile **provider komisyon avantajı** artık birbirinden **ayrıldı**.
- Her indirimin maliyetini **kim karşılıyor** açıkça tutuluyor (PlatformFunded / ProviderFunded / Shared).
- Müşteri paketleri **sınırsız indirim** sağlamıyor; **benefit budget** ile sınırlandırılıyor.
- Provider **boost** ürünü (§9.2) komisyon oranını **düşürmüyor**.
- Komisyon avantajları **minimum oran, GMV ve kullanım kotasıyla** sınırlanıyor.
- **Üç ayrı minimum katkı** kontrolü yapılıyor (müşteri tarafı / provider tarafı / işlem toplamı).
- Kârlılığı bozan avantaj **azaltılıyor veya uygulanmıyor**.
- Kâr koruma başarısızsa **iyzico checkout başlatılmıyor**.
- CargoDry bu revizyonun dışında.

### 19.1 Gerekçe (finansal invariant kârlılığı göstermez)
§3.3 invariant'ı (`ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount`) paranın matematiksel doğru
dağıtıldığını gösterir; **kârlılığı göstermez**. Zarar örneği:
```
Provider hizmet tutarı:            10.000 ₺
Provider komisyon oranı:                %9
Provider net alacağı:               9.100 ₺
Müşteri platform ücreti:              250 ₺
Platform-funded müşteri indirimi:   1.000 ₺
Müşteri toplamı:                    9.250 ₺
Inktavia brüt payı:                   150 ₺
Beklenen ödeme maliyeti:              400 ₺
Inktavia işlem sonucu:               -250 ₺   ← invariant kapanır ama ZARAR
```
Bu nedenle §3.3 invariant'ının **üstüne** `ProfitProtectionEngine` eklenir.
**Bağlayıcı kural:** Hiçbir müşteri indirimi, provider komisyon avantajı, kategori kampanyası veya üyelik hakkı;
Inktavia'nın müşteri tarafı, provider tarafı veya işlem toplamı için belirlediği **minimum katkı sınırlarını ihlal
ederek** uygulanamaz.

### 19.2 Üç ayrı ekonomik kontrol
```
CustomerSideContributionExpected =
    CustomerPlatformFeeNetRevenue + CustomerPlanRevenueAllocation + CustomerPremiumRevenueAllocation
  − PlatformFundedCustomerDiscount − CustomerSideVariableCostAllocation
      ≥ RequiredCustomerSideContribution

ProviderSideContributionExpected =
    ProviderCommissionNetRevenue + ProviderPlanRevenueAllocation + ProviderAddOnRevenueAllocation
  − ProviderCommissionBenefitCost − ProviderSideVariableCostAllocation
      ≥ RequiredProviderSideContribution

TotalTransactionContributionExpected =
    ProviderCommissionNetRevenue + CustomerPlatformFeeNetRevenue
  + ApprovedSubscriptionRevenueAllocations + ApprovedAddOnRevenueAllocations
  − PlatformFundedCustomerDiscount − ExpectedPaymentProcessingExpense
  − ExpectedRefundRiskReserve − ExpectedOtherVariableExpenses
      ≥ RequiredTransactionContribution
```
Üç kontrolden **herhangi biri** başarısızsa işlem mevcut avantajlarla ödeme aşamasına **geçemez**.

### 19.3 Minimum katkı politikası — `ProfitProtectionPolicy`
Sabit tutar **ve** oran birlikte desteklenir:
```
RequiredContribution = Max(MinimumContributionAmount, ContributionBaseAmount × MinimumContributionRate)
```
Ayrı policy değerleri: `MinimumCustomerSideContribution{Amount,Rate}`, `MinimumProviderSideContribution{Amount,Rate}`,
`MinimumTransactionContribution{Amount,Rate}`. Değerler **koda gömülmez**; **versiyonlu + effective-date** destekli
`ProfitProtectionPolicy` üzerinden yönetilir (§4/§13.1 tarih-aralığı deseni). Kesin rakamlar sonra admin'den; teknik
model belirli bir oran/tutara bağımlı olmamalı.

### 19.4 Provider boost ile komisyon avantajının ayrımı
**Bağlayıcı:** `OFFER_BOOST_7D` (§9.2) komisyon oranını **değiştirmez** — yalnız görünürlük/sıralama/vitrin/süreli
entitlement sağlar. Provider'a komisyon avantajı verilecekse bu **boost'tan bağımsız** bir kural/entitlement olur:
`COMMISSION_BENEFIT_1PP` → `1PP = 1 yüzde puan` (indirim değil). Örn. Base %9 + (−1 puan) = Effective %8.

### 19.5 Provider komisyon avantajı çözümleme + kontroller
İki aşamalı: `BaseCommissionRate` (§6 specificity) → `CommissionAdjustment` → `EffectiveCommissionRate`.
```
EffectiveCommissionRate = Max(BaseCommissionRate + AllowedCommissionAdjustment, MinimumCommissionRate)
```
(negatif adjustment = indirim). `ProviderCommissionBenefitRule` alanları: `Stackable, Exclusive,
AdjustmentPercentagePoints, MinimumCommissionRate, MaximumDiscountAmount, MaximumEligibleGMV, UsageLimit,
ApplicableCategoryCodes, EffectiveFrom, EffectiveTo, Status`.
**Bağlayıcı kurallar:** (1) `OFFER_BOOST_7D` komisyon avantajı üretemez. (2) Birden fazla avantaj yalnız hepsi
`Stackable=true` ise birleşir. (3) `Exclusive=true` başka avantajla birleşemez. (4) Nihai oran `MinimumCommissionRate`
altına düşemez. (5) `MaximumEligibleGMV` aşan hacme uygulanamaz. (6) Parasal etki `MaximumDiscountAmount`'ı aşamaz.
(7) Provider-side minimum katkısı bozulursa avantaj tam/kısmi uygulanmaz. (8) MVP'de PREMIUM_PARTNER otomatik oranı
**%9 altına inemez**. (9) %9 altı ticari oran yalnız açık admin `ProviderOverride` (§6) veya sonraki fazdaki kontrollü
komisyon entitlement'ı ile verilir.

### 19.6 `CustomerDiscountRule` + funding modeli
Alanlar: `CustomerPlanId, CategoryCode, DiscountType, DiscountRate, FixedDiscountAmount, MinimumPurchaseAmount,
MaximumDiscountAmount, EffectiveFrom, EffectiveTo, Priority, FundingMode, Status`.
`FundingMode ∈ {PlatformFunded, ProviderFunded, Shared}`; `Shared` ise `PlatformFundingRate + ProviderFundingRate = %100`.
**Bağlayıcı:** funding kaynağı belirlenmemiş indirim uygulanamaz.
**Funding davranışı:**
- **PlatformFunded:** `ProviderEconomicServiceAmount = OriginalServiceGrossAmount` (provider net korunur; maliyet
  platform katkısından).
- **ProviderFunded:** provider kampanyaya **önceden ve açık** katılmış olmalı; `ProviderEconomicServiceAmount =
  OriginalServiceGrossAmount − ProviderFundedDiscountAmount`; **komisyon matrahı indirim sonrası ekonomik tutar**.
- **Shared:** `TotalCustomerDiscount = PlatformFundedDiscountAmount + ProviderFundedDiscountAmount` açık oranlarla.
- **Kural:** platform indirimi provider'a, provider indirimi platforma **otomatik aktarılamaz**; provider onayı olmadan
  provider-funded yok; bir kaynakta bütçe yetersizse diğer taraf **sessizce finansör** yapılamaz → avantaj **azaltılır/
  uygulanmaz**.

### 19.7 `CustomerBenefitBudget` + reservation/consumption
ELITE paket **sınırsız/kontrolsüz** indirim hakkı doğurmaz; paket gelirinin yalnız **yapılandırılmış bir kısmı** avantaj
bütçesine ayrılır. Alanlar: `CustomerSubscriptionId, CustomerPlanId, PeriodStart, PeriodEnd, FundedAmount,
ReservedAmount, ConsumedAmount, RemainingAmount, CurrencyCode, Status, Version`.
**Bağlayıcı:** (1) indirim öncesi **reserve**; (2) ödeme başarılıysa **consume**; (3) başarısız/iptal → **release**;
(4) refund policy'ye göre kullanılan bütçe geri yüklenebilir/tüketilmiş kalır (§7); (5) indirim `RemainingAmount`'ı
aşamaz; (6) paket bedelinin **tamamı** bütçe olamaz; (7) oran plan bazlı konfigüre; (8) **optimistic concurrency**;
(9) aynı bütçe iki eşzamanlı checkout'ta iki kez kullanılamaz. → Gerçek cüzdan/ödeme hesabı **değil**; ticari avantaj
kontrol bütçesi.

### 19.8 Resolver ayrımı (bağımsız hesaplar, tek birleştirici)
`ResolveCustomerDiscount`, `ResolveCustomerDiscountFunding`, `ResolveBaseProviderCommission` (=§6),
`ResolveProviderCommissionBenefits`, `ResolvePlatformFee` (=§2.3), `EvaluateProfitProtection` → tek domain servisi
`CalculateServiceRequestPaymentEconomics` içinde birleşir. **Müşteri indirimi resolver'ı komisyon oranını,
komisyon resolver'ı indirim tutarını değiştiremez.**

### 19.9 Revize edilmiş bağlayıcı hesap sırası (12 adım)
1. Provider teklif tutarı → `OriginalServiceGrossAmount` kesinleşir.
2. Uygulanabilir `CustomerDiscountRule` kayıtları çözülür (üyelik/kategori/kampanya).
3. Talep edilen müşteri indirimi hesaplanır (oran/sabit/min harcama/maks indirim/kullanım limiti).
4. İndirim **funding dağılımı** (Platform/Provider/Shared) kesinleşir.
5. **Platform-funded indirim güvenli üst sınıra** tabi (§19.10): benefit budget + customer-side/total minimum katkı +
   iyzico split uygulanabilirliği (§10) + beklenen ödeme/refund giderleri.
6. `ProviderEconomicServiceAmount = OriginalServiceGrossAmount − ProviderFundedDiscountAmount`.
7. `BaseCommissionRate` çözülür (§6 specificity sırası korunur).
8. İzin verilen provider komisyon avantajları çözülür (§19.5: stackable/exclusive/category/GMV/kota/tarih).
9. `EffectiveCommissionRate ≥ MinimumCommissionRate`; provider-side katkı minimumun altına düşemez.
10. Hesaplar (§3.1 yuvarlama):
```
CommissionAmount            = Round(CommissionBaseAmount × EffectiveCommissionRate, 2)
ProviderNetAmount           = ProviderEconomicServiceAmount − CommissionAmount
CustomerPayableServiceAmount= OriginalServiceGrossAmount − TotalCustomerDiscount
PlatformFeeBaseAmount       = CustomerPayableServiceAmount          // §19.13
CustomerTotalAmount         = CustomerPayableServiceAmount + PlatformFeeGrossAmount
```
11. Beklenen giderler + üç katkı değeri hesaplanır (`ExpectedPaymentProcessingExpense`, `ExpectedRefundRiskReserve`,
    `ExpectedOtherVariableExpenses`; §19.2). Gerekirse müşteri indirimi veya komisyon avantajı **güvenli seviyeye
    düşürülür** ve ekonomi yeniden hesaplanır.
12. §3.3 invariant'ları **+ üç kâr koruma kapısı** doğrulanır:
```
ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount
CustomerTotalAmount >= ProviderNetAmount
CustomerSideContributionExpected     >= RequiredCustomerSideContribution
ProviderSideContributionExpected     >= RequiredProviderSideContribution
TotalTransactionContributionExpected >= RequiredTransactionContribution
```
Tümü geçerse **immutable `PaymentEconomicsSnapshot` (§5) oluşturulur ve ödeme başlatılır**; biri başarısızsa ödeme
mevcut avantajlarla **başlatılmaz**.

### 19.10 Güvenli maksimum platform indirimi
```
MaximumSafePlatformFundedDiscount =
    Max(0, PreDiscountExpectedContribution − RequiredTransactionContribution − ExpectedVariableExpenses)
MaximumSafePlatformFundedDiscount <= CustomerBenefitBudget.RemainingAmount
CustomerTotalAmount >= ProviderNetAmount
```
Müşterinin teorik indirim hakkı güvenli maksimumdan büyükse **yalnız güvenli maksimum** uygulanır.

### 19.11 `ProfitProtectionDecision` sonuç modeli
Durumlar: **`Approved`** (tüm avantajlar uygulanabilir) · **`ApprovedWithAdjustment`** (izin verilen maksimum uygulanır;
kullanılmayan hak policy'ye göre benefit budget'ta kalır) · **`Rejected`** (hiçbir güvenli kombinasyon minimum katkıyı
sağlamıyor → ödeme başlatılmaz) · **`ConfigurationError`** (eksik policy / çakışan kural / hesaplanamaz funding →
avantaj uygulanmaz + operasyon alarmı).
**Bağlayıcı:** sistem müşteriye/provider'a gösterilmiş tutarı checkout **sonrasında sessizce değiştiremez**; adjustment
ödeme ekranı açılmadan **önce** tamamlanır ve nihai tutar açıkça gösterilir.

### 19.12 `PaymentEconomicsSnapshot` genişletmesi
Mevcut immutable snapshot (§5) **korunur** ve şu alanlarla genişletilir (yalnız tüm invariant + kâr koruma kapıları
geçtikten sonra oluşur; başarısız değerlendirmeler `ProfitProtectionEvaluationLog`'da audit edilir, **snapshot
oluşmaz**):
- **Hizmet & müşteri indirimi:** `OriginalServiceNet/Vat/GrossAmountSnapshot`, `CustomerDiscountRuleIdsSnapshot`,
  `CustomerDiscountGrossAmountSnapshot`, `PlatformFundedDiscountAmountSnapshot`, `ProviderFundedDiscountAmountSnapshot`,
  `CustomerPayableServiceAmountSnapshot`, `ProviderEconomicServiceAmountSnapshot`.
- **Provider komisyonu:** `BaseCommissionRuleIdSnapshot`, `BaseCommissionRateSnapshot`, `CommissionBenefitIdsSnapshot`,
  `CommissionAdjustmentPercentagePointsSnapshot`, `CommissionBenefitAmountSnapshot`, `MinimumCommissionRateSnapshot`,
  `EffectiveCommissionRateSnapshot`, `CommissionBaseAmountSnapshot`, `CommissionAmountSnapshot`,
  `ProviderNetAmountSnapshot`.
- **Platform ücreti:** `PlatformFeeBaseAmountSnapshot`, `PlatformFeeRuleIdSnapshot`, `PlatformFeeNet/Vat/GrossAmountSnapshot`.
- **Beklenen giderler:** `ExpectedPaymentProcessingExpenseSnapshot`, `ExpectedRefundRiskReserveSnapshot`,
  `ExpectedOtherVariableExpenseSnapshot`.
- **Kâr koruma sonuçları:** `Customer/Provider/TotalTransactionContributionExpectedSnapshot`,
  `RequiredCustomer/Provider/TransactionContributionSnapshot`, `ProfitProtectionPolicyIdSnapshot`,
  `ProfitProtectionDecisionSnapshot`, `ProfitProtectionAdjustmentReasonSnapshot`.
- **Nihai ödeme:** `CustomerTotalAmountSnapshot`, `PlatformGrossShareSnapshot`, `CurrencyCodeSnapshot`,
  `RoundingModeSnapshot`, `CreatedAt`.

### 19.13 Platform fee davranışı (bu revizyondaki bağlayıcı matrah)
`PlatformFeeBaseAmount = CustomerPayableServiceAmount` (indirim **sonrası** müşterinin ödeyeceği hizmet bedeli).
Min/maks + oran politikası (§1/§2.3) korunur (%2,5 · 99 ₺ · 1.500 ₺); KDV net/vergi/brüt ayrımı §2.5/§13.3 gibi.

### 19.14 Yeni domain modelleri ve value object'ler
**Entity:** `CustomerDiscountRule`, `CustomerBenefitBudget`, `CustomerBenefitReservation`, `ProviderCommissionBenefitRule`,
`ProviderCommissionBenefitEntitlement`, `ProfitProtectionPolicy`, `ProfitProtectionEvaluationLog`.
**VO/Result:** `CustomerDiscountResolution`, `CustomerDiscountFundingAllocation`, `ProviderCommissionResolution`,
`ProviderCommissionBenefitResolution`, `ExpectedVariableCostCalculation`, `ProfitProtectionEvaluation`,
`ServiceRequestPaymentEconomicsResult`. Mevcut `CommissionRule`, `ResolveCommissionRate`, gateway, webhook, subscription,
`PaymentTransactionEntity` **yeniden yazılmaz** (§17).

### 19.15 Concurrency & idempotency (§8 genişletmesi)
Benefit budget ve komisyon entitlement kullanımları §8 güvenliğine dahildir: (1) aynı budget eşzamanlı iki checkout'ta
tüketilemez; (2) indirim checkout başlarken **reserve**; (3) başarılı ödeme → **consumed**; (4) başarısız/timeout →
**release**; (5) entitlement kullanım limitini aşamaz; (6) **duplicate webhook** ikinci consumption üretemez; (7) refund
benefit geri-yükleme policy'si **yalnız bir kez** çalışır; (8) optimistic concurrency + unique constraint birlikte.

### 19.16 Domain / Application-CQRS / DB-Migration / FE etkileri
- **Domain:** §19.14 yeni entity/VO; funding & benefit davranışı; profit protection invariant'ları; snapshot genişletmesi.
- **Application/CQRS:** yeni resolver query'leri (§19.8) + `CalculateServiceRequestPaymentEconomics` domain servisi;
  benefit reserve/consume/release command'leri; profit protection değerlendirme; admin CRUD command/query'leri.
  Teklif kabul akışı bu servisi çağırıp snapshot üretir, **sonra** checkout başlatır (§10).
- **DB/Migration:** §19.18 (append-only tablolar + snapshot kolonları + constraint'ler + policy seed).
- **Admin FE:** `CustomerDiscountRule`, `CustomerBenefitBudget` policy, `ProviderCommissionBenefit`,
  `ProfitProtectionPolicy` yönetim ekranları; güvensiz kombinasyonda **uyarı** + backend'de bağlayıcı doğrulama.
- **Customer checkout:** ServiceAmount + uygulanan indirim (kaynağıyla değil, net etkiyle) + platform ücreti +
  `CustomerTotalAmount`; adjustment varsa nihai tutar **checkout öncesi** açıkça gösterilir (§19.11).
- **Provider ekranı:** ekonomik hizmet tutarı, base vs effective komisyon oranı, komisyon avantajı (varsa), net alacak
  şeffaf; provider-funded indirim provider onayına bağlı gösterilir.

### 19.17 Finansal raporlama genişletmesi (§15 üzerine)
Yeni ayrımlar: `CustomerPlanRevenue`, `ProviderPlanRevenue`, `ProviderAddOnRevenue`,
`PlatformFundedCustomerDiscountExpense`, `ProviderFundedCustomerDiscount`, `ProviderCommissionBenefitCost`,
`ExpectedPaymentProcessingExpense`, `ActualPaymentProcessingExpense`, `RefundRiskReserve`, `ActualRefundExpense`,
`CustomerSideContribution`, `ProviderSideContribution`, `TotalTransactionContribution`.
**Kurallar:** müşteri indirimi + komisyon avantajı + normal fiyat indirimi **tek `DiscountAmount`'ta birleştirilmez**;
**provider-funded indirim Inktavia gideri gibi raporlanmaz**; **platform-funded indirim** açıkça Inktavia kampanya/
üyelik avantaj maliyeti olarak gösterilir.

### 19.18 Migration impact (append-only)
1. `customer_discount_rules` · 2. `customer_benefit_budgets` · 3. `customer_benefit_reservations` ·
4. `provider_commission_benefit_rules` · 5. `provider_commission_benefit_entitlements` · 6. `profit_protection_policies` ·
7. `profit_protection_evaluation_logs` · 8. `payment_economics_snapshots`'a yeni indirim/benefit/contribution kolonları ·
9. unique constraint + effective-date overlap koruması · 10. concurrency/version kolonları · 11. default
`ProfitProtectionPolicy` seed · 12. **Legacy backfill:** müşteri indirimi 0, commission adjustment 0, base=effective,
contribution alanları nullable/legacy marker. Migration'lar **idempotent + geriye dönük uyumlu**.

### 19.19 Acceptance criteria
(1) Müşteri indirimi ile provider komisyonu **ayrı resolver'lardan** gelir. (2) Funding'siz indirim uygulanamaz.
(3) Provider onayı olmadan provider-funded yok. (4) Boost komisyon oranını değiştirmez. (5) Effective komisyon min oran
altına inemez. (6) Komisyon avantajı maks GMV/kullanım limitini aşamaz. (7) Benefit budget kalanı aşılamaz.
(8) Customer-side, (9) Provider-side, (10) Total contribution minimumun altına inemez. (11) `CustomerTotalAmount ≥
ProviderNetAmount`. (12) Invariant'larda **0,01 ₺ tolerans yok**. (13) Güvensiz avantaj tam uygulanmaz. (14) Adjustment
checkout öncesi müşteriye yansır. (15) Aynı benefit/entitlement eşzamanlı iki kez tüketilemez. (16) Başarısız ödeme
rezervasyonları serbest bırakır. (17) Refund benefit restoration idempotent. (18) Başarılı sonuçlar tek immutable
snapshot ile audit edilir. (19) Kâr koruma başarısızsa **iyzico checkout başlatılmaz**. (20) CargoDry dahil değil.

### 19.20 Test matrisi
- **Müşteri indirimi:** platform-funded bütçe içinde / bütçeyi aşıyor; provider-funded onaylı / onaysız; shared oran
  toplamı %100; ELITE kategori indirimi; maks tutar clamp; eşzamanlı checkout aynı budget; başarısız ödeme→release;
  refund→restoration.
- **Provider komisyonu:** FREE %15 / STANDARD %12 / PREMIUM %9; boost sonrası oran değişmez; izinli entitlement; min
  floor; maks GMV; maks benefit; stackable iki avantaj; exclusive çakışması; aynı entitlement iki kez tüketimi.
- **Kâr koruma:** üç kapı geçer; customer-side fail; provider-side fail; total fail; ApprovedWithAdjustment; güvenli maks
  müşteri indirimi; güvenli maks komisyon benefit; hiç güvenli kombinasyon yok→Rejected; ConfigurationError; yüksek
  tutarda min oran; düşük tutarda min sabit tutar.
- **Invariant:** net+gross share = total; total ≥ net; yuvarlama sonrası kuruş farkı yok; KDV net/vergi/brüt; snapshot
  değişmezliği.

### 19.21 Uygulama fazları (bağımlılık sıralı — §16 genişletmesi)
`ProfitProtectionPolicy → CustomerDiscountRule → CustomerBenefitBudget+reservation → provider commission benefit modeli
→ snapshot genişletmesi → CalculateServiceRequestPaymentEconomics → üç contribution hesabı → safe adjustment → acceptance/
invariant kontrolleri → checkout entegrasyonu → refund/benefit restoration → admin yönetimi → finansal raporlama → test
matrisi → iyzico sandbox regression.`
> **Kapı:** kâr koruma motoru tamamlanmadan müşteri ELITE indirimleri veya provider komisyon avantajları production'a
> **açılmaz**; §10.3 sandbox kapısı korunur.

### 19.22 Riskler ve açık kararlar
- **KDV/faturalama:** §2.5/§13.3 YMM kapısı; platform-funded indirim ve komisyon avantajının KDV/gider muhasebesi YMM
  onayına bağlı.
- **Kesin policy rakamları:** min katkı tutar/oranları admin'den; teknik model orana bağımsız.
- **iyzico funding uygulanabilirliği:** platform-funded indirim sonrası split'in §10.1 eşitliklerini bozmadığı **sandbox'ta
  doğrulanmalı** (indirim müşteri toplamını düşürür; subMerchantPrice=ProviderNet korunur).
- **Refund ↔ benefit restoration ↔ provider clawback** etkileşimi (§7 + §19.15) çok senaryolu; policy netleştirmesi.
- **Shared funding** provider onay akışı (ne zaman/nasıl toplanır) ürün kararı.

### 19.23 Değiştirilmeyecek mevcut sınıflar (§17 ile aynı)
`IPaymentGatewayProvider`, `IyzicoMarketplacePaymentGatewayProvider`, `ProcessIyzicoWebhook`, `CommissionRule`,
`ResolveCommissionRate`, `ProviderPlan`, `ProviderPlanSubscription`, `PaymentTransactionEntity` ve mevcut iyzico
checkout/escrow/refund akışları **yeniden yazılmaz**; yalnız kontrollü genişletilir.


---

## 20. ServiceRequest — Kalem Bazlı (Itemized) Deniz Servisi Fiyatlama & Ekonomi
> **Kapsam:** ServiceRequest teklif ekonomisini **aggregate ServiceAmount**'tan **line-item** seviyesine taşımak ve
> deniz servisine özgü dinamik fiyatlamayı eklemek. §19 Kâr Koruma Motoru **korunur**; line-level → transaction-level
> olarak genişletilir. **Dahil DEĞİL:** CargoDry satış/inventory/consignment/renewal/muhasebe. Hazır offer/gateway/
> snapshot/commission resolver/PPE **yeniden yazılmaz** (§17), line-item ve marine-pricing katmanlarıyla genişletilir.

### 20.0 Saha notlarının rafine business özeti
- Deniz servis fiyatı **kategoriden fazlasına** bağlı: motor tipi (içten/kıçtan takma), motor sınıfı/gücü/yaşı/**adedi**,
  yağ litresi, filtre adedi, değişecek parça sayısı, işçilik zorluğu.
- Teklif **kalemlerden** oluşur: işçilik, parça, sarf malzeme, yağ, filtre, ulaşım, dış hizmet, marina/lift/vinç.
- Bazı hizmetler **periyodik** (ör. zehirli boya ~24 ay); periyot ve tekne boyu/eni/yüzey/boya türü/kat/hazırlık/lift
  fiyatı etkiler.
- Provider başlangıç lokasyonu ↔ tekne/marina arası **mesafeye dayalı ulaşım** ücreti.
- İndirimler **kalem bazında** (toplam teklife kontrolsüz değil); parçalarda **tedarikçi/marka/provider** bazlı ticari
  terimler olabilir.
- Teklif/mutabakatta **net + KDV + brüt** açık gösterilir.
- **Uyarı:** saha rakamları (≈300–500 EUR parça/işçilik, %10–20 parça indirimi, %30 ticari alan %20/%10 dağılımı,
  km başı 0,50–1 USD, 24 ay periyot) **yalnızca örnektir** → seed/bağlayıcı değil; §20.20 açık kararlar listesinde.

### 20.1 Gap analysis (mevcut doküman/kod vs saha)
| Kavram | Mevcut | Gap |
|---|---|---|
| Itemized offer | **VAR** — `ServiceRequestOfferItemEntity` (ItemType, Qty, UnitPrice, TaxRate, per-line discount, LineSubtotal/Tax/Total) + `OfferEntity` per-type totals | **Ekonomi alanları yok** (commission eligibility/base/amount, funding allocation, provider net, platform contribution per line) |
| Kalem türleri | **Kısmi** — Service/Product/Installation/Delivery/Labor/Inspection/EmergencyFee/Discount/Other | **Consumable / Travel / ExternalService / EquipmentRental / MarinaOrLiftFee / OtherApprovedExpense** yeni (Part≈Product) |
| Pricing method | **YOK** | Fixed/PerUnit/PerHour/PerDay/PerKm/PerLiter/PerSquareMeter/EstimateRange/AfterInspection/TimeAndMaterials |
| Dinamik pricing attribute | **YOK** | motor/tekne/boya/işçilik değişkenleri — category/template bazlı tanımlanabilir |
| Provider price book | **Kısmi** — `ProviderOfferTemplate/Item` (şablon) | pricing method, min/max estimate, service area, min call-out, effective dates, commission eligibility, discountable |
| Per-line commission | **YOK** (komisyon aggregate, Payment §6) | line eligibility/base/rate/amount/provider-net; toplam = line toplamı |
| Per-line discount funding | **Kısmi** (line `DiscountAmount`) | funding (Platform/Provider/Shared/**Supplier**) + allocation snapshot |
| Parça ticari terimleri | **YOK** | supplier/list price, dealer margin, funded amounts, min provider receivable, scope |
| Travel/mobilizasyon | **YOK** (Delivery tipi var, mesafe yok) | `TravelPricingRule` (server-side mesafe + snapshot) |
| Fixed/Estimate/T&M/Inspection | **Kısmi** (Inspection tipi) | `PricingMethod` + `OfferType` |
| Change-order/ek iş | **YOK** | ServiceChangeOrder/OfferRevision/ExtraWorkApproval |
| Recurring interval | **YOK** | RecommendedIntervalMonths/NextDueAt (CargoDry'den ayrı) |
| Line VAT | **VAR** ✓ | — |
| Line refund allocation | **YOK** | line-level refund (§7 genişletme) |
| Line + total profit protection | **Kısmi** (transaction §19) | **line-level** kontroller yeni |
| Currency | offer default USD; settlement TL kabulde sabit | **FX snapshot köprüsü** (EUR/USD referans → settlement currency; kabul sonrası re-valuation yok) |

### 20.2 Existing entity reuse (paralel model KURMA — genişlet)
- `ServiceRequestOfferEntity` / `ServiceRequestOfferItemEntity` → **genişlet** (ekonomi + pricing alanları).
- `ServiceRequestOfferItemType` → **enum genişlet** (§20.3); mevcut değerler geriye-uyumlu korunur.
- `OfferDiscountType` (None/Amount/Percent) → korunur.
- `OfferCalculationService` → **line ekonomisi + aggregation** eklenir; aggregate **yalnız line toplamı** (§20.15).
- `ProviderOfferTemplate/ProviderOfferTemplateItem` → **provider price book** temeli; genişlet.
- Payment `CommissionRule`/`ResolveCommissionRate` (§6) → `LineType`/`ProductCode`/`CommissionEligibility` boyutlarıyla
  **kullan**, yeniden yazma.
- `PaymentEconomicsSnapshot` (§5) + line snapshot'lar (§20.15). PPE (§19) line-level'a genişletilir (§20.12).

### 20.3 Teklif kalem türleri (enum genişletmesi)
Yeni/haritalı: `Labor`(mevcut), `Part`(≈Product), `Consumable`, `Travel`, `ExternalService`, `EquipmentRental`,
`MarinaOrLiftFee`, `OtherApprovedExpense`. Mevcut Service/Product/Installation/Delivery/Inspection/EmergencyFee/Discount
korunur; migration'da mapping notu.

### 20.4 Fiyatlama yöntemleri (`PricingMethod`)
`Fixed, PerUnit, PerHour, PerDay, PerKm, PerLiter, PerSquareMeter, EstimateRange, AfterInspection, TimeAndMaterials`.
Her kalemde `PricingMethod` + yöntemin gerektirdiği parametreler (ör. PerKm→BillableKm, PerLiter→litre, EstimateRange→
min/max) tutulur.

### 20.5 Kalem asgari alanları (ekonomi genişletmesi)
`description/product/serviceRef, quantity, unitOfMeasure, unitNetPrice, lineNetAmount, vatRate, vatAmount,
lineGrossAmount, pricingMethod, discountEligibility, commissionEligibility, commissionBaseAmount,
customerDiscountAllocation, fundingAllocation, providerNetAmount, platformContribution, snapshotRefs`.

### 20.6 Dinamik fiyatlandırma değişkenleri (attribute modeli)
**Sabit kolona dönüştürme.** Category/service-template bazlı tanımlanabilir `PricingAttributeDefinition` (kod, tip,
birim, zorunlu/opsiyonel, validation) + offer/line'da `PricingAttributeValue`. Örnek attribute'lar:
`EngineInstallationType, EngineType/Class, EnginePower, EngineAge, EngineCount, OilCapacityLitres, FilterCount, PartCount,
VesselLength, VesselBeam, CalculatedSurfaceArea, PaintType, CoatCount, WorkDifficulty, EstimatedLaborHours`. Değerler
kabulde snapshot (§20.15 PricingAttributeSnapshot).

### 20.7 Provider fiyat kataloğu (price book)
`ProviderPriceBookEntry` (veya `ProviderOfferTemplate` genişletmesi): `service/category, pricingMethod, basePrice,
unitPrice, minEstimate, maxEstimate, currencyCode, effectiveFrom/To, serviceArea, minimumCallOutFee, discountableAmount,
commissionEligibility`.
**FX kuralı (settlement currency kararı korunur):** provider EUR/USD referans fiyat tutabilir; **teklif oluşturulurken**
settlement currency'ye (TL) çevrilir; kullanılan `sourcePrice` + `exchangeRateSnapshot` **audit için** saklanır; **kabul
sonrası yeniden değerleme yapılmaz** (yurt içi kabul-anı sabitleme, §payment).

### 20.8 Mesafe/mobilizasyon fiyatlaması (`TravelPricingRule`)
Alanlar: `ProviderOriginLocation, VesselOrMarinaDestination, RouteDistanceKm, DistanceSource, IncludedKm, BillableKm,
RatePerKm, OneWayOrRoundTrip, MinimumTravelFee, MaximumServiceRadius, Toll/Ferry/Parking/MarinaEntry, TravelFeeAmount,
distanceAndRuleSnapshot`. **Mesafe istemciden güvenilir finansal veri olarak alınmaz; server-side doğrulanır** ve
kabulde snapshot (§20.15 TravelPricingSnapshot). Travel bir `Travel` kalemi olarak teklife girer.

### 20.9 Parça ticari terimleri (commercial discount pool)
`PartCommercialTerm`: `supplierListPrice, providerDealerMargin, maxCustomerDiscount, supplierFundedAmount,
providerFundedAmount, platformFundedAmount, minimumProviderReceivable, maximumDiscountableAmount, effectiveFrom/To,
scope(brand/product/provider/category)`. **Provider'ın gerçek maliyeti müşteriye veya diğer provider'lara GÖSTERİLMEZ;**
kâr koruması için yalnız `MinimumProviderReceivable`/`MaximumProviderFundedDiscount` kullanılır. Parça indirimi normal
müşteri indiriminden **ayrı** modellenir.

### 20.10 Kalem bazlı indirim (varsayılan davranış)
- İndirim **line-item** seviyesinde uygulanır; **discount-eligibility=false** kaleme indirim uygulanamaz.
- Tüm teklife **isimsiz tek** indirim uygulanamaz.
- Order-level voucher/kampanya desteklenecekse tutar **eligible line'lara deterministik** allocate edilir; allocation
  KDV/commission/refund/funding hesaplarında **snapshot** edilir.
- Funding türleri: `PlatformFunded, ProviderFunded, Shared, (geleceğe açık) SupplierFunded` (§19.6 funding kuralları
  line-level uygulanır).

### 20.11 Kalem bazlı komisyon
İşçilik/parça/ulaşım/pass-through için **aynı oran zorunlu değil.** `CommissionRule` (§6) `LineType`/`ProductCode`/
`CommissionEligibility` boyutlarıyla **genişletilir** (yeniden yazılmaz). Her kalemde snapshot:
`commissionable/non-commissionable, commissionBase, resolvedRate, commissionAmount, providerNet`. **Toplam provider
komisyonu = line commission toplamı** (§20.15 eşitlik).

### 20.12 Kâr Koruma Motoru genişletmesi (§19 → line-level)
§19 **korunur**; önce **line-level** sonra **transaction-level** çalışır.
**Line-level kontroller (her kalem):** `providerMinimumReceivable`, `allowedProviderFundedDiscount`,
`allowedPlatformFundedDiscount`, `commissionFloor`, `linePlatformContribution`, **negatif katkı yasağı** (veya açık,
versiyonlu, limitli exception policy). **Sonra transaction-level:** §19.2 üç kapı (Customer/Provider/Total). **Bir
kalemin zararı başka kalemin kârında sessizce kaybolamaz;** zararına izin verilen stratejik kampanya **ayrı, versiyonlu,
limitli** policy gerektirir.

### 20.13 Teklif türü + ek iş akışı
`OfferType: FixedPrice, EstimateRange, RequiresInspection, TimeAndMaterials`. **Kabul sonrası immutable snapshot
değiştirilmez.** Yeni parça/işçilik için `ServiceChangeOrder` / `OfferRevision` / `ExtraWorkApproval`. Ek iş **müşteri
onaylamadan** provider netine, müşteri toplamına ve iyzico tahsilatına **eklenemez** (yeni onaylı iş → yeni/ek economics
snapshot + yeni approve/split, §10/§13.4 idempotency).

### 20.14 Periyodik bakım (ServiceRequest kapsamında)
`RecommendedIntervalMonths, LastPerformedAt, NextDueAt, ReminderLeadDays` — **konfigüre, sabit değil.** CargoDry renewal
ile **birleştirilmez**; ayrı reminder mekanizması (Notification).

### 20.15 Snapshot mimarisi (line → aggregate; §5 korunur)
Tek immutable `PaymentEconomicsSnapshot` (§5) korunur; ona bağlı **immutable line snapshot'lar:**
`OfferLineEconomicsSnapshot, DiscountAllocationSnapshot, CommissionAllocationSnapshot, TravelPricingSnapshot,
PricingAttributeSnapshot`. **Aggregate tutarlar yalnız line snapshot toplamlarından türetilir.** Garanti edilen
eşitlikler (**0,01 ₺ tolerans yok**):
```
Σ(line gross before discounts)      = OriginalServiceGrossAmount
Σ(line customer discounts)          = TotalCustomerDiscount
Σ(line provider-funded discounts)   = TotalProviderFundedDiscount
Σ(line platform-funded discounts)   = TotalPlatformFundedDiscount
Σ(line commission amounts)          = TotalProviderCommission
Σ(line provider net amounts)        = ProviderNetTotal
Σ(line VAT amounts)                 = ServiceVatTotal
Σ(line totals) + PlatformFeeGross   = CustomerTotalAmount
```

### 20.16 Revize edilmiş uygulama akışı (16 adım)
1. Tekne/motor/hizmet **pricing attribute**'ları çözülür (§20.6).
2. **Provider price book** + pricing method çözülür (§20.7; FX→settlement currency).
3. İşçilik/parça/sarf/ulaşım **kalemleri** oluşturulur (§20.3–20.4).
4. **Mesafe & mobilizasyon** bedeli hesaplanır (§20.8, server-side).
5. Her kalemin **net/KDV/brüt** hesaplanır.
6. Her kalemin **discount eligibility**'si çözülür (§20.10).
7. Kalem bazlı **müşteri indirimi + funding** dağılımı (§20.9–20.10, §19.6).
8. **Provider min receivable + commercial discount limitleri** kontrol (§20.9).
9. Her kalemin **commission eligibility/base/rate** çözülür (§20.11, §6).
10. Her kalemin **provider net + platform contribution**.
11. **Line-level profit protection** (§20.12).
12. Line sonuçlarından **aggregate ServiceRequest ekonomisi** türetilir (§20.15).
13. Mevcut **customer/provider/transaction contribution** kapıları (§19.2).
14. **Finansal invariant'lar** doğrulanır (§3.3 + §20.15 eşitlikleri).
15. **Line snapshots + tek aggregate `PaymentEconomicsSnapshot`** oluşturulur (§5/§19.12).
16. Tüm kontroller geçerse **iyzico checkout** başlatılır (§10).

### 20.17 Impact
- **Domain:** OfferItem/Offer genişletme; PricingAttributeDefinition/Value; ProviderPriceBookEntry; TravelPricingRule;
  PartCommercialTerm; line commission/discount/funding VO'ları; line profit protection; ServiceChangeOrder/OfferRevision/
  ExtraWorkApproval; recurring alanları; line snapshot entity'leri.
- **Application/CQRS:** `OfferCalculationService` → line ekonomisi + aggregation; yeni resolver'lar (pricing attribute,
  price book, travel, part commercial term, line commission, line discount funding); `CalculateServiceRequestPaymentEconomics`
  (§19.8) line-loop + line PPE + aggregate; change-order command/query'leri; price book/attribute/travel/part-term admin CRUD.
- **Migration:** yeni tablolar (pricing_attribute_definitions/values, provider_price_book_entries, travel_pricing_rules,
  part_commercial_terms, service_change_orders, offer_line_economics_snapshots, discount/commission/travel/attribute
  allocation snapshots) + OfferItem/Offer'a ekonomi/pricing kolonları + recurring kolonları; append-only, idempotent,
  legacy backfill (line discount 0, commission eligibility default, funding default, attribute yok).
- **Admin FE:** pricing attribute tanımları, provider price book, travel pricing rule, part commercial term, recurring
  policy yönetimi; güvensiz kombinasyon uyarısı + backend bağlayıcı doğrulama.
- **Provider teklif ekranı:** kalem ekleme (tür + pricing method + attribute), otomatik price-book doldurma, mesafe/travel
  otomatik hesap, net/KDV/brüt canlı, per-line komisyon/net şeffaf; FixedPrice/Estimate/Inspection/T&M seçimi.
- **Customer checkout kırılımı:** kalem kalem net + KDV + brüt, uygulanan indirimler (kaynak gösterilmeden net etki),
  platform ücreti, `CustomerTotalAmount`; estimate/inspection ise koşullu tutar açık.
- **Change-order akışı:** ek iş → OfferRevision/ChangeOrder → müşteri onayı → ek economics snapshot → ek approve/split;
  onaysız tahsilat yok.

### 20.18 Acceptance criteria
1. Teklif ekonomisi **line-level**; aggregate = line toplamı; §20.15 eşitlikleri **0,01 toleranssız** tutar.
2. Discount-ineligible kaleme indirim uygulanamaz; isimsiz toplam indirim yok.
3. Line funding kaynağı belirli; provider-funded provider onaylı; provider maliyeti müşteriye gösterilmez.
4. Line komisyonu eligibility/base/rate ile çözülür; toplam komisyon = line toplamı; pass-through kalem non-commissionable
   olabilir.
5. Travel bedeli server-side mesafeyle; kabulde snapshot; client değeri finansal kaynak değil.
6. Provider price book FX referansı kabulde settlement currency'ye sabitlenir; source+rate snapshot; re-valuation yok.
7. Line-level profit protection çalışır; bir kalemin zararı diğerinde saklanamaz; transaction kapıları da geçer.
8. Kabul sonrası snapshot değişmez; ek iş yalnız müşteri onayı + yeni snapshot ile.
9. Recurring interval konfigüre; CargoDry ile birleşmez.
10. CargoDry bu revizyona dahil değil; hazır gateway/iyzico/snapshot/commission/PPE yeniden yazılmadı.

### 20.19 Test matrisi
- **Itemized ekonomi:** çok kalemli teklif (labor+part+consumable+travel); §20.15 sekiz eşitliği; karışık KDV oranları;
  yuvarlama kuruş farkı yok.
- **Pricing method:** PerHour/PerLiter/PerSquareMeter/PerKm/EstimateRange/AfterInspection/T&M hesapları.
- **Attribute:** V12 ~40L yağ + 12 filtre örneği (rakamlar test-fixture, seed değil); zorunlu attribute eksikse validation.
- **Price book + FX:** EUR referans → TL sabitleme; kabul sonrası kur değişse tutar sabit.
- **Travel:** included/billable km, one-way/round-trip, min fee, max radius dışı reddi; client-manipüle mesafe reddi.
- **Part commercial term:** supplier/provider/platform funded dağılımı; min provider receivable koruması; maliyet gizliliği.
- **Line discount funding:** platform/provider/shared/(supplier) allocation; ineligible line; voucher deterministik allocation.
- **Line commission:** farklı oran per line; non-commissionable pass-through; toplam = line toplamı.
- **Line profit protection:** zararlı kalem reddi/azaltımı; kalemler arası sessiz sübvansiyon yok; exception policy limitli.
- **Change-order:** kabul sonrası ek iş onaylı/onaysız; onaysız tahsilat yok; ek snapshot immutability.
- **Recurring:** NextDueAt hesabı; reminder lead; CargoDry ayrımı.
- **Concurrency/idempotency:** change-order webhook duplicate; line benefit/commission entitlement iki kez tüketilemez (§8/§19.15).

### 20.20 Açık business kararları (sistem kararı gibi VARSAYMA)
- km bedelinin kesin oranı · tek yön/gidiş-dönüş · ücretsiz km · 300–500 EUR örnek fiyatlar · parça fiyat aralıkları ·
  %10–20 parça indirimi · %30 ticari alanın %20/%10 dağılımı · komisyonun parça/ulaşım kalemlerine uygulanıp
  uygulanmayacağı · zehirli boya periyodunun tüm müşteriler için 24 ay olup olmadığı · teklif fiyatlarının net mi brüt
  mü duyurulacağı · kategori bazlı KDV oranları. → Hepsi **admin-konfigüre + versiyonlu**; teknik model bu değerlere
  bağımlı olmamalı; §2.5/§13.3 YMM kapısı geçerli.

### 20.21 Bağımlılık sıralı uygulama fazları
1. OfferItem/Offer ekonomi + pricing-method genişletmesi (+`OfferCalculationService` line ekonomisi).
2. PricingAttributeDefinition/Value + validation.
3. ProviderPriceBookEntry (+ FX snapshot köprüsü).
4. TravelPricingRule (server-side mesafe + snapshot).
5. PartCommercialTerm.
6. Line-level discount + funding allocation.
7. Line-level commission (CommissionRule boyut genişletmesi).
8. Line snapshots + aggregate türetme + §20.15 invariant'ları.
9. Line-level Profit Protection (§19 genişletmesi) + transaction kapıları.
10. OfferType + ServiceChangeOrder/ExtraWorkApproval.
11. Recurring interval + reminder.
12. Admin CRUD ekranları · 13. Provider teklif ekranı · 14. Customer checkout kırılımı.
15. Test matrisi + **iyzico sandbox regression** (§10.3 kapısı korunur).
> **Kapı:** line-item ekonomi + line/total profit protection tamamlanmadan itemized deniz-servisi fiyatlaması production'a
> açılmaz.

### 20.22 Değiştirilmeyecek mevcut altyapı (§17 ile aynı + ServiceRequest offer)
Payment: `IPaymentGatewayProvider`, `IyzicoMarketplacePaymentGatewayProvider`, `ProcessIyzicoWebhook`, `CommissionRule`,
`ResolveCommissionRate`, `PaymentEconomicsSnapshot`, PPE (§19). ServiceRequest offer: mevcut `ServiceRequestOfferEntity`/
`ServiceRequestOfferItemEntity`/`OfferCalculationService`/`ProviderOfferTemplate` **yeniden yazılmaz**; kontrollü
genişletilir.


---

## 21. İş Tamamlama, Escrow Serbest Bırakma ve İtiraz / Admin İnceleme Akışı
> **Kapsam:** ServiceRequest ödemesinin **tahsilat → korumalı havuz → tamamlama → onay/oto-onay/itiraz → dağıtım**
> yaşam döngüsü. Mevcut escrow/completion/timeout altyapısı **korunur**; yalnız **itiraz → admin inceleme** katmanı
> ve auth-mode policy'si eklenir. CargoDry hariç. §17/§20.22 do-not-touch geçerli.

### 21.0 Bağlayıcı kararlar (kilitlendi, 2026-07-27)
1. **Auth modu:** **Varsayılan = Capture (`Payment/Auth`) + korumalı havuz + `item/approve` (release).** **PreAuth**,
   teklif/kategori bazlı **opsiyonel policy** modu (§21.3).
2. **Otomatik onay süresi:** tamamlama bildiriminden **7 gün** (admin-konfigüre, kategori-override); 3. ve 6. günde
   hatırlatma. (Capture modunda 25-gün tavanı yok; PreAuth modunda §21.3 kısıtı geçerli.)
3. **Admin karar granülaritesi:** domain **item-level approve**'a göre kurulur (partial native); **MVP tam-release /
   tam-iade**, kısmi hızlı ikinci faz.
4. **İtiraz penceresi:** **tek sayaç** = onay penceresi (7 gün). Süre içinde onay yoksa → **oto-onay + release**; itiraz
   açılırsa oto-release **durur** → admin. Release/iade sonrası **terminal**.
5. **Kanıt/inceleme yüzeyi:** mevcut kanıt yeniden toplanmaz; tek **admin Dispute Case** ekranı toplar (§21.6).

### 21.1 Neden Capture + escrow (offer-acceptance modeli)
```
Teklif kabul → müşteriden GERÇEK tahsilat → tutar KORUMALI HAVUZDA → provider işi yapar
    → müşteri onayı / oto-onay → provider payı + Inktavia komisyonu DAĞITILIR
```
Bu model: (a) kart limitinin haftalar sonra serbest kalması riskini **kaldırır**; (b) provider'a müşterinin **ödeme
gücünün doğrulandığını** gösterir; (c) platform komisyonu + provider hakedişini **işlem anında** tanımlar (kabul-anı
snapshot, §5/§19/§20); (d) **iş tamamlanmadan** provider'a para aktarılmasını engeller; (e) iptal / kısmi iade /
uyuşmazlık yönetimine **daha uygun temel** sağlar.

### 21.2 ⚠️ "Provider tamamen korunur" DENMEZ — chargeback riski
Capture yapılmış olsa bile müşteri **chargeback / harcama itirazı** başlatabilir. iyzico satıcı sözleşmesine göre yetkisiz
veya hatalı işlemler için **işlem tarihinden itibaren 13 aya kadar** chargeback bildirimi gelebilir. **Aktarım yapılmışsa
tutar provider'dan veya sonraki hakedişlerinden geri alınır.** Sonuç: escrow **onay-öncesi** riski azaltır ama
**onay-sonrası** riski sıfırlamaz. Modelleme:
- `ChargebackExpense` (§19 raporlama) ayrı gider kalemi.
- Provider **clawback** + **negatif bakiye** + **sonraki hakedişten mahsup** (§7 / §19.15 `ProviderBalance`).
- Chargeback penceresi **13 ay** olduğundan, dağıtılmış tutarlar için **geri-alma yükümlülüğü** provider sözleşmesinde
  açık olmalı; rezerv/teminat politikası (opsiyonel) açık karar.

### 21.3 Auth-mode policy (Capture varsayılan, PreAuth opsiyonel)
Soyutlama (`IPaymentGatewayProvider`) **iki modu da** destekler; mod teklif/kategori bazlı `PaymentAuthModePolicy`.
- **Capture (varsayılan):** `POST /payment/auth` → havuz → `POST /payment/iyzipos/item/approve`. Zaman tavanı marketplace
  şartlarına tabi (25-gün PostAuth kuralı **uygulanmaz**).
- **PreAuth (opsiyonel):** kart **bloke** → **≤25 gün** içinde PostAuth (BKM); banka operasyonel limiti daha kısa
  olabilir → **expiry-öncesi otomatik PostAuth job'ı + tampon** zorunlu. Uygun: kısa & FixedPrice & ~2 hafta altı işler;
  **kısmi kapama (marjlı PostAuth)** T&M/EstimateRange/change-order'da nihai tutarı çekmeye uygun. PreAuth de marketplace
  split destekler (`subMerchantKey`+`subMerchantPrice`).
- **Kısıt:** PreAuth modunda `onay penceresi + PostAuth ≤ ~20–25 gün` (banka tamponuyla).

### 21.4 iyzico resmi op ↔ domain abstraction eşlemesi
| Domain (bizim) | iyzico resmi | Not |
|---|---|---|
| CapturePayment | `POST /payment/auth` | tahsilat + `subMerchantKey`/`subMerchantPrice` split bilgisi |
| ReleaseEscrow / Approve | `POST /payment/iyzipos/item/approve` | **kalem bazlı** onay → dağıtım |
| Provider payını değiştir | `PUT /payment/item` | change-order / kısmi karar öncesi pay ayarı |
| Onayı geri çek / itiraz | disapprove | dispute/iade yolu |
| PreAuth / PostAuth | PreAuth + PostAuth | opsiyonel mod (§21.3) |
> `CapturePayment`/`ReleaseEscrow` **domain abstraction**'dır; resmi API adları değildir (gateway katmanı eşler).

### 21.5 State machine (mevcut / yeni)
**PaymentTransaction:** `PendingIntent → Captured(escrow) → Released(split)` | `Refunded`/`PartiallyRefunded` | `Disputed
→ (admin) → Released|Refunded|PartiallyRefunded` | `Cancelled`. (Alanlar `DisputedAt`/`DisputeResolution`/`Status=Disputed`
**var**; oto-onay `PaymentEscrowTimeoutJob` **var**.)
**ServiceRequest:** `... → CompletionSubmitted → (müşteri onay) Completed/Released | (müşteri itiraz) Disputed → (admin)
Resolved`. (`SubmitServiceRequestCompletion` + WorkLogs + evidence **var**; `ReleasePayment` **var**.)
- **Built (yeniden yazma):** capture (`CapturePayment`), release (`ReleasePaymentEscrow`), oto-onay
  (`PaymentEscrowTimeoutJob`/`StaleEscrowCleanupJob`), iptal (`ServiceRequestCancelledConsumer`), tamamlama
  (`SubmitServiceRequestCompletion`), work logs/evidence, offer-gated messaging.
- **NEW:** itiraz → admin inceleme workflow (§21.6); auth-mode policy (§21.3); item-level partial approve/refund (§21.7);
  chargeback/clawback yönetimi (§21.2).

### 21.6 İtiraz / Admin inceleme workflow (YENİ)
- **`ServiceRequestDispute`** (case) entity: `ServiceRequestId, OfferId, PaymentTransactionId, OpenedByCustomerId, Reason,
  Status(Opened|UnderReview|ResolvedForProvider|ResolvedForCustomer|PartiallyResolved), OpenedAt, ResolvedAt,
  ResolvedByAdminId, ResolutionNote, EvidenceSnapshotRef`.
- **`OpenDispute`** (müşteri, onay penceresi içinde) → oto-release **durur**, escrow **freeze**.
- **`ResolveDispute`** (admin) → item-level karar: `ReleasePaymentEscrow` (provider haklı) veya `Refund`/refund-allocation
  (§7) veya **kısmi** (bazı kalem approve + bazı kalem disapprove/refund). Karar **audit** (kim/ne zaman/neden + incelenen
  kanıt referansı).
- **Dispute Case ekranı (admin):** teklif + itemized snapshot, provider completion + **work logs + evidence** (signed
  read-url), **mesaj thread'i**, müşteri itiraz gerekçesi, ödeme/escrow durumu — hepsi **mevcut kaynaklardan agregasyon**;
  yeni kanıt yakalama yok.

### 21.7 Kısmi çözüm (item-level; iyzico native)
`item/approve` kalem bazlı olduğundan partial **doğal**: onaylanan kalemler approve→split; itirazlı kalemler
disapprove/refund; gerekirse `PUT /payment/item` ile provider payı düzeltilir. MVP tam/tam çıkabilir ama domain
**item-level**'e göre kurulur; §20.15 line snapshot eşitlikleri partial'da da tutar (0,01 tolerans yok).

### 21.8 Change-order ile ilişki (§20.13)
Kabul sonrası ek iş → `ServiceChangeOrder`/`ExtraWorkApproval` → **müşteri onayı** → ek economics snapshot → ek
approve/split veya (PreAuth modunda) PostAuth ile nihai tutar. Onaysız hiçbir ek tutar provider netine / müşteri
toplamına / iyzico tahsilatına eklenmez.

### 21.9 Idempotency, concurrency, bildirim
- Dispute resolution **idempotent + state-machine korumalı** (§8): çözüldükten sonra terminal; çifte release/refund yok;
  `ProcessedGatewayEvent` + optimistic concurrency.
- **Bildirim** (Notification, mevcut): tamamlama→müşteri; oto-onay yaklaşıyor→müşteri; itiraz açıldı→admin+provider;
  çözüldü→ikisi; chargeback→admin+provider.

### 21.10 Impact
- **Domain:** `ServiceRequestDispute`, `PaymentAuthModePolicy`, chargeback/clawback + `ProviderBalance` (§7); item-level
  approve/refund; escrow freeze state.
- **Application/CQRS:** `OpenDispute`, `ResolveDispute` (item-level), auth-mode çözümleme; `PaymentEscrowTimeoutJob`
  parametreleştirme (7g, kategori-override); chargeback consumer/clawback.
- **Migration:** `service_request_disputes`, `payment_auth_mode_policies`, provider balance/clawback kolonları, dispute
  evidence snapshot ref; append-only.
- **Admin FE:** **Dispute Case** inceleme + karar ekranı (agregasyon + item-level release/refund + audit); auth-mode
  policy yönetimi; chargeback kuyruğu.
- **Provider ekranı (mevcut):** işlem "İtirazlı" durumu + sonuç; chargeback/clawback → negatif bakiye/mahsup şeffaf
  (Finans ekranları).
- **Müşteri yüzeyi (repolarda YOK — açık nokta):** işi onayla / itiraz et aksiyonu + tamamlama kanıtı görünümü bir
  müşteri uygulaması gerektirir (bağlı repolarda yok → netleştirilecek).

### 21.11 Acceptance criteria
1. Para kabul anında tahsil edilir, havuzda tutulur; provider'a **onay/oto-onay öncesi** aktarılmaz.
2. Oto-onay 7 günde (konfigüre) release eder; hatırlatmalar gider.
3. İtiraz onay penceresi içinde açılır; oto-release durur; escrow freeze.
4. Admin kararı item-level release/refund/partial uygular; audit tutulur; §20.15 eşitlikleri korunur.
5. Release/iade sonrası durum terminal; dispute idempotent; çifte işlem yok.
6. Chargeback (13 ay) modellenir; dağıtılmış tutar provider'dan/sonraki hakedişten geri alınır (`ProviderBalance`).
7. PreAuth modunda onay+PostAuth ≤ banka/BKM limiti; expiry-öncesi oto-PostAuth job'ı çalışır.
8. CargoDry dahil değil; hazır capture/release/timeout/completion altyapısı yeniden yazılmadı.

### 21.12 Açık kararlar (hâlâ)
- Capture modunda **korumalı havuz max tutma süresi** → iyzico kurumsal sözleşme + sandbox.
- Chargeback için **rezerv/teminat** politikası (opsiyonel).
- **Müşteri yüzeyi** (onay/itiraz + kanıt görünümü) nerede.
- PreAuth policy'sinin hangi kategori/teklif tiplerine açılacağı.
- Otomatik onay 7 gün: kategori override değerleri.

### 21.13 Değiştirilmeyecek mevcut altyapı
`CapturePayment`, `ReleasePaymentEscrow`, `PaymentEscrowTimeoutJob`, `StaleEscrowCleanupJob`,
`ServiceRequestCancelledConsumer`, `SubmitServiceRequestCompletion`, `ReleasePayment` (SR), WorkLogs/evidence,
`IyzicoMarketplacePaymentGatewayProvider`, `ProcessIyzicoWebhook` — **yeniden yazılmaz**, kontrollü genişletilir.
