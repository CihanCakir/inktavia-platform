# Provider × ServiceRequest — Roadmap

**Amaç:** Sağlayıcı portalının ServiceRequest modülüyle ilişkisini baştan sona, kontrollü ve doğrulanabilir şekilde
kurmak. Bugüne kadar parça parça ilerledik; bundan sonrası bu haritaya göre gidecek.

Bu doküman **modülün gerçekten sunduğu yüzeye** dayanıyor (uydurma yok). Aşağıdaki her şey kodda mevcut.

---

## 0. Modülün gerçek yüzeyi (envanter)

**Teklif kalem tipleri** (`ServiceRequestOfferItemType`):
`Service=1, Product=2, Installation=3, Delivery=4, Labor=5, Inspection=6, EmergencyFee=7, Discount=8, Other=99`

**Talep kalem tipleri** (`ServiceRequestItemType`):
`Service, Product, Installation, Inspection, Repair, Emergency, Other` — müşteri talebinde ne istediğini kalem
kalem belirtebiliyor. Sağlayıcı teklifi bu kalemlere **karşılık** üretmeli.

**İş kaydı tipleri** (`ServiceRequestWorkLogType`):
`GeneralNote, ArrivedAtVessel, InspectionStarted, InspectionCompleted, WorkStarted, MaterialRequired,
AdditionalIssueFound, WaitingForOwnerApproval…` — ayrıca `LocationLatitude/Longitude` ve `AttachmentFileId`.

**Yaşam döngüsü uçları:**
- Atama: `POST /assignment`, `PATCH /{id}/accept`, `/reject`, `/start`
- İş kaydı: `POST /work-logs/assignment/{assignmentId}`, `GET …`, faz güncelleme (`PATCH /phases/{n}`)
- Tamamlama: `POST /completion/{assignmentId}` (`CompletionNotes`, `EvidenceFileId`), sahibi `approve`/`reject`
- İhtilaf: `POST /dispute`, durum değişimi, çözüm
- Ekler: `POST /service-requests/{id}/attachments` (`FileId` + tip)

**Durum makinesi** (`ServiceRequestStatus`): `Draft → Open/WaitingForOffer → OfferReceived → OfferAccepted →
WaitingForAssignment → Assigned → Scheduled → InProgress → (WaitingForOwnerApproval | WaitingForMaterial | Paused)
→ CompletionSubmitted → Completed`, yan dallar: `DisputeOpened → UnderDisputeReview → DisputeResolved`,
`Cancelled/Expired/Closed`.

**Realtime** (`ServiceRequestRealtimeEventType`): 23 olay tanımlı, `ServiceRequestRealtimePublisher` bunları
yayınlıyor — **ama sadece kendi process'inin SignalR hub'ına.** Modülün 8 bus mesajı tanımlı ve (bu turdaki
düzeltmeler öncesinde) **hiçbiri publish edilmiyordu**; dolayısıyla Notification'ın SR consumer'ları da ölüydü.

---

## Faz P0 — Temel (bitti / doğrulanıyor)

- Açık talep listesi (`GET provider/open`) — kategori/şehir eşitliği, teklif verilmişler hariç.
- Talep detayı, **erişim kontrolüyle** (teklif verilebilir | teklifim var | bana atanmış — aksi halde "not found").
- Basit teklif (tek kalem), teklif listesi, geri çekme.
- Sağlayıcı kimliği **her yerde assertion'dan**; body/query'den asla.

Bilinen açık: teklif oluşturma EF hatası (`temporary value … Modified`) — düzeltildi, derleme bekliyor.

---

## Faz P1 — Gerçek teklif kurgusu (mobildeki gibi)

Bugünkü ekran "fiyat + not" alıyor. Bu, modülün modelini de mobildeki akışı da karşılamıyor.

**Sağlayıcı teklifi kalem kalem kurar:**

1. Talebin kalemleri (`request.items`) gösterilir — müşteri ne istemiş.
2. Sağlayıcı her kalem için **hizmet/ürün seçer ve fiyatlandırır**: `ItemType` (Service/Product/Labor/
   Installation/Inspection/Delivery), başlık, açıklama, **adet × birim fiyat**.
3. Ek kalem ekleyebilir (talepte olmayan bir iş: "ek arıza bulundu" senaryosunun teklif karşılığı).
4. `EmergencyFee` ve `Discount` ayrı kalem tipleri — indirim negatif değil, **tip** olarak modellenmiş; UI bunu
   doğru göstermeli.
5. Toplam **sunucuda** hesaplanır (`Items.Sum(Quantity × UnitPrice)`); istemcinin gönderdiği toplama güvenilmez.
6. Süre tahmini (`EstimatedDurationMinutes`), planlanan başlangıç/bitiş, teklif geçerlilik süresi (`ExpiresAt`).
7. Teklif düzenleme (`PUT`) aynı kurguyla; `Submitted` teklif düzenlenebilir, `Accepted` düzenlenemez.

**Kabul kriteri:** çok kalemli bir teklif verilir, listede toplamı ve kalemleri görünür, düzenlenir, geri çekilir.

---

## Faz P2 — İş yaşam döngüsü (sağlayıcının asıl işi)

Teklif kabul edildikten sonra bugün sağlayıcı **hiçbir şey yapamıyor**. Ekranlar yok, BFF uçları yok.

- **Atama:** teklif kabul → atama oluşur. Sağlayıcı `accept` / `reject` eder. Reddederse talep tekrar havuza döner.
- **İşe başlama:** `start` → durum `InProgress`.
- **İş kaydı (work log):** tipli kayıtlar (`ArrivedAtVessel`, `InspectionStarted`, `MaterialRequired`,
  `AdditionalIssueFound`…), **konum** (tekneye gerçekten gidildiğinin kanıtı) ve **fotoğraf** — fotoğraf
  onboarding'de kanıtlanmış imzalı-URL akışıyla yüklenir; bayt BFF'ten geçmez, bucket/objectKey tarayıcıya asla
  ulaşmaz.
- **Ek iş / malzeme:** `MaterialRequired` / `AdditionalIssueFound` kaydı, sahibinin onayını bekleyen bir duruma
  (`WaitingForOwnerApproval`) götürebilmeli. Bu, ek teklif kalemi doğuran gerçek bir iş senaryosu.
- **Tamamlama:** `CompletionNotes` + **kanıt dosyası** (`EvidenceFileId`) ile gönderilir; sahibi onaylar/reddeder.
  Onay → ödeme escrow'u serbest bırakılır (Payment modülü zaten bağlı).

**Kabul kriteri:** atama kabulünden tamamlama onayına kadar tüm zincir tarayıcıda sürülür; iş kaydına konum ve
fotoğraf eklenir, tamamlama kanıtı yüklenir.

---

## Faz P3 — Realtime (BFF hub'ı)

Karar verildi: **hub BFF'te.** Tarayıcı yalnızca BFF ile konuşur; grup üyeliğine sunucu karar verir
(`provider:{profileId}`, `city:{cityCode}`), istemcinin "şu gruba abone ol" demesi mümkün değil.

Köprü: modül olayı **bus'a** basar → BFF tüketir → kendi hub'ından ilgili gruba iter.

Sağlayıcıya iletilecek olaylar:
`ServiceRequestPublished` (kendi şehrinde yeni iş), `OfferAccepted` (kazandı), `OfferRejected`,
`AssignmentCreated`, `CompletionApproved` / `CompletionRejected`, `DisputeOpened`, `MessageSent`.

**Kural:** realtime bir **ipucu**dur, kayıt değil. UI olayı alır, cache'i invalidate eder, gerçeği API'den çeker.
Bağlantı koparsa uygulama REST üzerinden çalışmaya devam eder.

---

## Faz P4 — Mesajlaşma (SR bağlamında)

İki mesajlaşma implementasyonu var (`Modules/Messaging` ve `ServiceRequest`'in kendi `Message` controller'ı).
**Önce hangisinin otoriter olduğuna karar verilecek, kaybeden adıyla yazılacak.** Sonra sağlayıcı iş/talep
ekranından konuşmaya girer; serbest sohbet yok, konuşma daima bir talebe/işe bağlı.

---

## Faz P5 — İhtilaf

Sağlayıcı ihtilaf açabilir/yanıtlayabilir; admin çözer. Ödeme escrow'uyla doğrudan ilişkili — bu yüzden Payment
tarafının davranışı netleşmeden UI'ı tamamlamıyoruz.

---

## Faz P6 — Admin SR operasyon merkezi

Türkiye haritası (talep koordinatları **zaten var**: `LocationLatitude/Longitude`), şehir/marina bazında
toplulaştırma, canlı olay akışı (`admin:operations`), gerçek KPI'lar: teklifsiz kalan talepler, ilk-teklif süresi,
atanmış ama başlamamış işler. Ayrı faz; sağlayıcı döngüsü oturduktan sonra.

---

## Dağıtım gerçeği: Kubernetes, çok replika

Tüm servisler (modüller ve BFF'ler) **Kubernetes'te ve yük altında çok replikayla** çalışacak. Stateful altyapı —
PostgreSQL, RabbitMQ, Redis, MinIO/S3, Keycloak — **küme dışında** duracak.

Bunun realtime'a doğrudan sonucu var ve pazarlığa açık değil: **her SignalR hub'ı için Redis backplane zorunlu.**
Aksi halde N replikada, sağlayıcının soketi A pod'unda durur, RabbitMQ mesajı B pod'una düşer, B kendi
bağlantılarını bilmediği için olayı **sessizce düşürür**. Hata yok, retry yok — sağlayıcı sadece duymaz. Tek pod'la
testte çalışır, canlıda olayların çoğunu kaybeder. Bu, realtime'ın hiç olmamasından kötüdür.

Bunun yanında: süreç belleğinde kullanıcı durumu tutulmaz (bağlantı haritası, "kim online" sözlüğü yok);
durum-değiştiren consumer'lar **idempotent** olmak zorunda (at-least-once teslim norm); ingress WebSocket
upgrade'ine izin vermeli (yoksa long-polling ve sticky session gerekir).

## Değişmez kurallar (her fazda)

- Sağlayıcı kimliği **BFF assertion'ından**; body/query'den asla. Eksik `UserId` = red, `0` değil.
- 200 zarfı içindeki `success:false` bir hatadır — gövde kontrol edilir, sadece HTTP durumu değil.
- Wire sözleşmelerinde `System.Text.Json.JsonElement` **yasak** (MVC Newtonsoft ile bind ediyor; veri sessizce
  kaybolur).
- İmzalı URL'ler tıklama başına üretilir, hemen kullanılır, **saklanmaz**; bucket/objectKey/URL domain'e ve
  tarayıcıya sızmaz.
- Toplamlar ve fiyat sunucuda hesaplanır.
- Geo/yarıçap **GeoDiscovery'nin işi** — ServiceRequest'e sokulmaz. Şehir eşitliği yeterli.
- Sessiz no-op yok: uygulanmamış yol istisna fırlatır, "başarılı" dönmez.
