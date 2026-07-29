# iyzico API Alignment — Resmi Doküman ↔ Mevcut Servis Karşılaştırması + Düzeltme Notları

> **Amaç:** iyzico resmi dokümanından (docs.iyzico.com, çekim tarihi 2026-07-28) çıkarılan **kesin request/response
> modelleri, imzalama ve endpoint'ler** ile mevcut `Aizen.Modules.Payment` iyzico entegrasyonunun karşılaştırması.
> Anahtar olmadan runtime'da yakalanamayacak hatalar burada tespit edilmiştir. **Her madde bir düzeltme notudur.**
> Anahtar geldiğinde bu doküman doğrudan bir BE düzeltme promptuna dönüşür (P9-gate öncesi zorunlu).
> **Kaynak sayfalar:** on-hazirliklar (checklist/hmacsha256/sandbox/eslestirme/limitler/postman), api-reference-beta
> (pazaryeri: alt-uye-isyeri/odeme/onay · odeme-formu-cf · iptal-ve-iade), on-provizyon (checkoutform preauth/postauth),
> ek-servisler (webhook/imza-dogrulama), ek-bilgiler (test-kartlari). Sürümler: submerchant 1.3.3, CF 1.0.2, refund 1.0.1.

## 0. Severity özeti (düzeltme sırası)
1. **🔴 BLOCKER — IYZWSv2 imzalama YANLIŞ (3 hata).** Mevcut haliyle **her istek 401** döner. (§1)
2. **🔴 BLOCKER — Approval endpoint YANLIŞ.** `ApproveMarketplacePaymentAsync` → `/payment/marketplace/approval` **yok**; doğru = `/payment/iyzipos/item/approve` (paymentTransactionId ile). ReleaseEscrow bugün 404 alır. (§4)
3. **🔴 Webhook V3 doğrulama YANLIŞ.** Header adı + algoritma + input dizisi hepsi yanlış (SHA256(secret+token) ≠ HMACSHA256 V3). (§2)
4. **🟠 Sub-merchant request tip-bazlı değil + hardcoded TCKN `"11111111111"`.** PERSONAL/PRIVATE_COMPANY/LIMITED farklı zorunlu alanlar ister. (§5)
5. **🟠 Response `signature` doğrulaması yok.** Man-in-the-middle / sahte callback riski. (§3)
6. **🟡 transactionStatus semantiği** (1=onay bekliyor/blokede, 2=onaylandı) escrow modeline bağlanmalı; approve **paymentTransactionId** (item) ile yapılır, payment-level id ile değil. (§4/§6)
7. **🟡 PreAuth auth-mode** init endpoint + PostAuth (`/payment/postauth`) ayrı; 25 gün BKM tavanı doğrulandı. (§7)
8. **🟡 Refund item-level** `paymentTransactionId`+`price`; cancel payment-level `paymentId`. (§8)

---

## 1. 🔴 IYZWSv2 Kimlik Doğrulama (HMACSHA256) — mevcut kod 3 yerde yanlış
**Resmi algoritma** (`on-hazirliklar/kimlik-dogrulama/hmacsha256`):
```
encryptedData = HMACSHA256( randomKey + uriPath + requestBody , secretKey )   // sonuç HEX (lowercase)
authorizationString = "apiKey:" + apiKey + "&randomKey:" + randomKey + "&signature:" + encryptedData
base64EncodedAuthorization = base64( authorizationString )
Authorization: "IYZWSv2 " + base64EncodedAuthorization      // IYZWSv2 ile base64 arasında 1 boşluk
x-iyzi-rnd: randomKey                                        // header'da da gönderilir
```
- `uriPath` = endpoint yolu, ör. `/payment/iyzipos/item/approve` (query'siz). `randomKey` = benzersiz (ör. epoch+rastgele).
- Örnek doğrulama: body `{"locale":"tr","binNumber":"535805","conversationId":"docsTest-v1"}`, path `/payment/bin/check` →
  encryptedData `079df4b2426fc7f4208d8f22fbc0349794019f8ce2b0711de7808b4874f4e796` (HEX).

**Mevcut kod (`IyzicoHttpClient.BuildAuthHeader`) — HATALAR:**
| # | Mevcut | Olması gereken |
|---|---|---|
| a | `payload = apiKey + randomKey + bodyJson` (**uriPath YOK, apiKey öneki YANLIŞ**) | `payload = randomKey + uriPath + bodyJson` |
| b | `hmacB64 = base64(hmac)` (**base64**) | `signature = hex(hmac)` (**HEX lowercase**) |
| c | `authValue = "{apiKey}:{randomKey}:{hmacB64}"` (**yanlış format**) | `"apiKey:"+apiKey+"&randomKey:"+randomKey+"&signature:"+hexSig` |
| d | `x-iyzi-rnd` header **gönderiliyor** (randomKey ile eşleşiyor) ✅ | aynı randomKey kullanılmalı ✅ |

→ **`SendJsonAsync` `uriPath`'i `BuildAuthHeader`'a geçmeli** (şu an geçmiyor). Path, PUT/POST hepsinde imzaya girer.
**Düzeltme:** BuildAuthHeader(path, randomKey, body): hmac = HMACSHA256(secret, randomKey+path+body) → `Convert.ToHexString(hmac).ToLowerInvariant()`; authString `apiKey:{apiKey}&randomKey:{rnd}&signature:{hexSig}` → base64 → `IYZWSv2 {b64}`. Bir unit test: yukarıdaki örnek payload → beklenen encryptedData'yı üretmeli (kanıt).

---

## 2. 🔴 Webhook doğrulama — `X-IYZ-SIGNATURE-V3` (V1/V2 kaldırılıyor)
**Resmi (`ek-servisler/webhook`):** Header **`X-IYZ-SIGNATURE-V3`**. Hesap panelinde webhook signature özelliği aktif olmalı.
İlk bildirim ödeme sonrası 10–15 sn; sunucu **2xx** dönene kadar 15 dk'da bir, 3 kez tekrarlar.
- **HPP format (bizim CheckoutForm senaryomuz):** payload `{paymentConversationId, merchantId, token, status, iyziReferenceCode, iyziEventType, iyziEventTime, iyziPaymentId}`. Doğrulama:
  ```
  key = secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status
  sig = HEX( HMACSHA256(key, secretKey) )   // header X-IYZ-SIGNATURE-V3 ile karşılaştır (case-insensitive)
  ```
  `iyziEventType` = `CHECKOUT_FORM_AUTH` (CF), `status` ∈ {SUCCESS, FAILURE, INIT_THREEDS, ...}.
- **Direct format (NON-3DS/3DS, ileride kullanırsak):** `key = secretKey + iyziEventType + paymentId + paymentConversationId + status`.

**Mevcut kod:** `ValidateWebhookSignature = base64( SHA256(WebhookSecret + token) )` → **algoritma (plain SHA256, HMAC değil), input dizisi, HEX/base64, header adı hepsi yanlış.** WebhookSecret boşsa `true` dönüyor (dev-mode bypass — prod'da kapatılmalı).
**Düzeltme:** `X-IYZ-SIGNATURE-V3` header'ını oku; HPP formülü ile HMACSHA256→HEX; secretKey ile (WebhookSecret ayrı bir kavram değil — imza `secretKey` ile üretiliyor, panelde özellik açılır). status alanları enum'a alınmalı; SUCCESS dışı işlemler capture etmemeli. Idempotency: `iyziReferenceCode`/`iyziPaymentId` ile ProcessedGatewayEvent.

---

## 3. 🟠 Response `signature` doğrulaması — mevcutta YOK
**Resmi (`ek-servisler/imza-yanitinin-dogrulanmasi`):** yanıtların `signature` alanı, `secretKey` + belirli parametre dizisi (`:` separator) → HMACSHA256 → HEX ile doğrulanır. **Fiyat parametreleri trailing-zero atılmış** olmalı (`10.50`→`10.5`, `10.0`→`10`).
- **CheckoutForm sorgulama** (`/payment/iyzipos/checkoutform/auth/ecom/detail`): sıra `paymentStatus, paymentId, currency, basketId, conversationId, paidPrice, price, token`.
- **CF/PWI başlatma**: `conversationId, token`.
- **Refund** (`/payment/refund`): `paymentId, price, currency, conversationId`.
- **Non-3DS `/payment/auth`**: `paymentId, currency, basketId, conversationId, paidPrice, price`.
**Düzeltme:** CF-retrieve yanıtında `signature`'ı yukarıdaki dizi + trailing-zero-trim + `:` join + HMACSHA256-HEX ile doğrula; uymazsa escrow/capture'ı **kabul etme**. (Pre-send split guard'a ek bir güvenlik katmanı — yanıt bütünlüğü.)

---

## 4. 🔴 Pazaryeri Onay (Approve/Disapprove) + escrow release
**Resmi (`pazaryeri/onay`):**
- **Onay:** `POST /payment/iyzipos/item/approve` · body `{ paymentTransactionId, (locale, conversationId) }` · 200 `{status, paymentTransactionId, ...}`. **Onay her zaman kırılım (item) bazında** = paymentTransactionId ile.
- **Onay Kaldırma:** `POST /payment/iyzipos/item/disapprove` · aynı body.
- Limit: approve 1000/dk, disapprove 100/dk.
**Mevcut kod:** `ApproveMarketplacePaymentAsync` → **`/payment/marketplace/approval` (BÖYLE BİR ENDPOINT YOK)**. `ReleaseEscrowAsync` bunu çağırıyor → prod'da 404. P9'da eklenen `ApproveItemAsync` (`/payment/iyzipos/item/approve`) **doğru**.
**Düzeltme:** `ReleaseEscrowAsync` → `ApproveItemAsync(paymentTransactionId)` kullanmalı. `paymentTransactionId`, **checkout retrieve yanıtındaki `itemTransactions[].paymentTransactionId`'dir** (payment-level `paymentId` DEĞİL) — bu id capture anında saklanmalı. `ApproveMarketplacePaymentAsync` + `/payment/marketplace/approval` kaldırılmalı/yönlendirilmeli.

---

## 5. 🟠 Alt Üye İşyeri — tip-bazlı model + hardcoded TCKN
**Resmi (`pazaryeri/alt-uye-isyeri`, submerchant 1.3.3):** `POST /onboarding/submerchant`, `discriminator: subMerchantType`:
- **PERSONAL** zorunlu: `subMerchantType, email, gsmNumber, address, contactName, contactSurname, subMerchantExternalId, identityNumber(TCKN)`. (taxOffice/legalCompanyTitle **yok**.) `iban` create'te opsiyonel ama **ürün onayı öncesi zorunlu**.
- **PRIVATE_COMPANY** (şahıs şirketi) zorunlu: `subMerchantType, email, gsmNumber, address, taxOffice, legalCompanyTitle, subMerchantExternalId`. (taxNumber opsiyonel.)
- **LIMITED_OR_JOINT_STOCK_COMPANY** zorunlu: `+ taxNumber` (taxOffice + taxNumber + legalCompanyTitle).
- Ortak: `currency`(TRY default; TRY/USD/EUR/GBP/RUB/CHF/NOK), `locale`, `conversationId`, `name`.
- **200:** `{status, subMerchantKey, conversationId, systemTime, locale}` (sadece key döner).
- **Güncelleme:** `PUT /onboarding/submerchant` — **subMerchantType gönderilmez**, `subMerchantKey` + `iban` zorunlu (tip varyantına göre).
- **Sorgulama:** `POST /onboarding/submerchant/detail` · `{subMerchantExternalId}` → tüm alanlar + `subMerchantKey`.
- **Hak Ediş Güncelleme:** `PUT /payment/item` · `{paymentTransactionId, subMerchantKey, subMerchantPrice}` → detaylı payout yanıtı (blockage/convertedPayout).
- Limit: create/update/detail 100/dk.

**Mevcut kod (`RegisterSubMerchantCommandHandler` + `IyzicoSubMerchantRequest`):** **tek model**, tüm alanları gönderiyor + **`IdentityNumber` default `"11111111111"`** (tehlikeli — gerçek TCKN/VKN yerine sahte gider). Update/detail iyzico'ya bağlı **değil** (BE-I1 lifecycle sadece key saklıyor, iyzico update/detail çağırmıyor).
**Düzeltme:** `subMerchantType`'a göre **3 ayrı request gövdesi** (zorunlu alan doğrulaması); hardcoded TCKN kaldırılmalı — onboarding'de gerçek `identityNumber`/`taxNumber` toplanmalı (BE-I1 KYC akışına bağlanır); `PUT /onboarding/submerchant` (update) + `POST /onboarding/submerchant/detail` (retrieve) client'a eklenmeli ve BE-I1 lifecycle'ına bağlanmalı; IBAN "ürün onayı öncesi zorunlu" kuralı split-eligibility gate'e eklenmeli (IBAN yoksa split-eligible değil).

---

## 6. 🟡 Marketplace Ödeme / CheckoutForm basket + transactionStatus
**Resmi (`pazaryeri/odeme` NON3D `/payment/auth` + `odeme-metotlari/iyzico-odeme-formu-cf`):**
- **Sepet toplamı kuralı:** `price` = sepet; **Σ basketItem.price == price**; her basketItem `price != 0`. `paidPrice` = taksit/vade sonrası çekilen (≥/≤/= price). Pazaryeri için her kırılımda `subMerchantKey` + `subMerchantPrice` **zorunlu**.
- **CF basket:** `subMerchantKey`/`subMerchantPrice` **string** tipinde; standart modelde gönderilmez, sadece pazaryerinde. `itemType` PHYSICAL/VIRTUAL — hepsi VIRTUAL ise shippingAddress opsiyonel (ama CF `shippingAddress`/`billingAddress` yine de required listede; boş adres gönderiyoruz).
- **`itemTransactions[].transactionStatus`:** `0` fraud incelemede · `-1` reddedildi · **`1` Onaylandı = pazaryerinde "Üye İşyeri Onayı Bekliyor" (para blokede/havuzda)** · **`2` Onaylandı (pazaryeri onayı verilmiş = release edilmiş)**. `fraudStatus` 1=onay/0=inceleme/-1=ret.
- Retrieve yanıtı kırılım başına `paymentTransactionId, price, paidPrice, blockage*, subMerchantPayoutAmount, merchantPayoutAmount` verir — **bunlar saklanmalı** (approve/refund/mutabakat için).
**Mevcut kod:** tek-basket CF, `subMerchantPrice` string ✅, `Price=PaidPrice=grossStr` ✅. **Ama:** capture sonrası `itemTransactions[].paymentTransactionId` saklanmıyor gibi (ReleaseEscrow yanlış endpoint kullandığı için de belli); transactionStatus 1↔2 escrow durumuna map edilmeli.
**Düzeltme:** CF-retrieve'de her kırılımın `paymentTransactionId` + `subMerchantPayoutAmount` + `blockage*` saklanmalı; escrow "held" = status 1, "released" = status 2; approve `paymentTransactionId` ile (§4). Pre-send guard'daki `Σ subMerchantPrice == ProviderNet` kuralı iyzico'nun `Σ basketItem.price == price` kuralıyla birlikte doğrulanmalı (bizim `Σ Price == CustomerTotal` zaten uyumlu).

---

## 7. 🟡 PreAuth / PostAuth (auth-mode) — BE-P9 PaymentAuthMode
**Resmi (`on-provizyon`):**
- **CF PreAuth başlatma:** `POST /payment/iyzipos/checkoutform/initialize/preauth/ecom` (auth ile aynı gövde, farklı endpoint).
- **PostAuth (provizyon kapama):** `POST /payment/postauth` · `{paymentId, paidPrice, (currency, ip, conversationId)}` → satışa çevirir; kısmi kapama mümkün (`paidPrice ≤ preauth tutarı`).
- **BKM: 25 gün içinde PostAuth zorunlu**; aksi halde blokaj otomatik kalkar (işlem tamamlanmaz). Süreler bankaya göre değişir.
**Mevcut kod:** BE-P9 `PaymentAuthMode {Capture, PreAuth}` + resolver var; **ama** PreAuth için init endpoint switch'i (`/preauth/ecom`) ve PostAuth client metodu **henüz yok** (MVP Capture only).
**Düzeltme (P9 PreAuth tamamlama, MVP sonrası):** auth-mode=PreAuth → CF init endpoint `/preauth/ecom`; `PostAuthAsync(paymentId, paidPrice)` client metodu + 25-gün-içinde-kapama job/guard. Capture default doğru (marine 7g + escrow > 25g tavanı → PreAuth riskli).

---

## 8. 🟡 İade / İptal (P10 girdisi)
**Resmi (`iptal-ve-iade`):**
- **İade:** `POST /payment/refund` · `{paymentTransactionId, price, (currency, ip, reason∈[OTHER,FRAUD,BUYER_REQUEST,DOUBLE_PAYMENT], description, conversationId)}` → **kırılım (item) bazında**, tam/kısmi (`price ≤ kırılım tutarı`). Limit 400/dk.
- **İade V2:** `POST /v2/payment/refund` · `{paymentId, price}` → kırılım otomatik seçilir. Limit 150/dk.
- **İptal:** `POST /payment/cancel` · `{paymentId, (reason, description, ip)}` → **tüm ödeme**, genelde gün sonu/settlement öncesi. Limit 150/dk.
- Yanıtlarda `retryable` + `signature`.
**Mevcut kod:** `RefundAsync` → `/payment/refund` (path ✅) ama `RefundInput.GatewayReference` **paymentTransactionId olmalı** (payment-level `paymentId` değil). Cancel yok.
**Düzeltme (P10):** refund `paymentTransactionId` (item) ile; `reason` enum eklenmeli; settlement öncesi tam iptal için `/payment/cancel` (paymentId); `retryable` true ise idempotent retry; response signature doğrula (§3). Marketplace'te iade edilen kırılım için sub-merchant hakedişi geri alınır (clawback — §19.15 ile eşleştir).

---

## 9. Sandbox / test / operasyon notları
- **Base URL:** sandbox `https://sandbox-api.iyzipay.com` (config ✅), prod `https://api.iyzipay.com`.
- **Sandbox hesabı:** `sandbox-merchant.iyzipay.com/auth/register` → merchantId + API Key + Secret Key (Ayarlar > Firma Ayarları > API Anahtarları). **Sandbox SMS şifresi her zaman `123456`.**
- **Test kartları:** başarılı ör. `5528790000000008` (Halkbank MC credit), `5890040000000016` (Akbank MC debit). Hata kartları: `4111111111111129` (yetersiz bakiye), `5406670000000009` (başarılı ama iptal/iade/postauth **yapılamaz** — negatif senaryo), `4131111111111117` (mdStatus 0). SKT ileri tarih + doğru format olmalı, CVV rastgele olabilir. **Gerçek kart sandbox'ta çalışmaz.**
- **Limitler:** CF init 8000/dk, CF detail 1000/dk, item/approve 1000/dk, disapprove 100/dk, submerchant create/update/detail 100/dk, refund 400/dk. Aşımda `{errorCode:50000,"Request Limit Exceeded"}`.
- **Idempotency/eşleştirme:** `conversationId`/`basketId` = üye işyeri üretir, response'da geri döner (bizim `IdempotencyKey`/`SR-{sr}-OFFER-{offer}` buna map). `token` (CF) + `paymentId` (iptal/iade için) iyzico üretir — **saklanmalı**.
- **Postman:** iyzico resmi collection (postman.com/iyzico) + GitHub `iyzico/iyzipay-dotnet` örnekleri imzalama/CF/postauth için referans.
- **Entegrasyon kontrol listesi:** sandbox al → Postman/örnek incele → ürün+ödeme metodu kararı → test ödemeleri → **webhook testi (herhangi bildirim tipini SUCCESS/2xx ile kabul et)** → canlı hesap başvurusu (hesap yöneticisi).

---

## 10. Düzeltme checklist (anahtar geldiğinde BE-P9-fix promptu)
- [ ] **§1** IYZWSv2 imzalama: `randomKey+uriPath+body` → HMACSHA256 → **HEX** → `apiKey:{}&randomKey:{}&signature:{}` → base64. `uriPath` SendJsonAsync'ten geçir. **Örnek-payload unit testi.**
- [ ] **§4** `ReleaseEscrowAsync` → `/payment/iyzipos/item/approve` (`paymentTransactionId`); `/payment/marketplace/approval` kaldır.
- [ ] **§6** CF-retrieve'de `itemTransactions[].paymentTransactionId` + payout/blockage sakla; transactionStatus 1/2 → escrow held/released.
- [ ] **§2** Webhook `X-IYZ-SIGNATURE-V3` HPP HMACSHA256-HEX; dev-bypass prod'da kapalı; status enum.
- [ ] **§3** Response `signature` doğrulama (CF-retrieve + refund) + trailing-zero.
- [ ] **§5** Sub-merchant 3 tip-bazlı gövde; hardcoded TCKN kaldır; PUT update + POST detail; IBAN→split-eligibility.
- [ ] **§8** Refund `paymentTransactionId` + reason enum; cancel; retryable (P10).
- [ ] **§7** PreAuth init `/preauth/ecom` + PostAuth `/payment/postauth` + 25g guard (P9 PreAuth, MVP sonrası).
- [ ] Değişmez pre-send split guard (P9) + bunların hepsi geçtikten sonra **canlı sandbox gate**.

> **Not:** §1 ve §4 düzeltilmeden hiçbir canlı çağrı çalışmaz — bunlar P9-gate'in ön koşuludur. Diğerleri sırayla.
