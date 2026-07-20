# CE-6c Slice-1 FIX-3 — "error deserializing the response" (enum wire-format mismatch) — Backend Prompt

> **Bağlam:** CE-6c Slice-1 provider bildirim listesi. FIX-2 (envelope) sonrası data akıyor ama BFF şimdi
> **911 "An error occured deserializing the response."** dönüyor (`GetNotifications` Refit çağrısında). Yani modül
> yanıtı ile BFF-local mirror DTO şekli uyuşmuyor.

---

## Kök neden (en olası — step 0 ile KESİNLEŞTİR)

BFF Refit ayarları lenient (case-insensitive + `JsonStringEnumConverter`), bu yüzden hata **tip uyuşmazlığı** kaynaklı
sert bir throw. Mirror `ProviderNotificationItemDto` enum alanlarını **`int`** olarak tanımlıyor:
```
public int Type; public int Channel; public int Status;
```
Ama modül yanıtı enum'ları büyük olasılıkla **string** olarak serileştiriyor (platform konvansiyonu — Refit ayarındaki
yorum bunu doğruluyor: *"JsonStringEnumConverter — handles string enums from ... API responses"*). System.Text.Json bir
**string**'i (`"CargoDryProviderFirstSale"`) `int` alana deserialize edemez → throw. (`JsonStringEnumConverter` yalnız
**enum-tipli** hedefe uygulanır, `int`'e değil.)

### Step 0 — wire formatını KANITLA (çıktı yapıştır)
`AizenRequestResponseMiddleware` yanıt gövdesini logluyor. notification-api logunda `/provider` yanıtını gör:
```
docker compose logs --tail=400 notification-api | grep -iE "HttpResponseBody|provider|CargoDryProvider|\"type\"" | tail -20
```
`"type":"CargoDryProviderFirstSale"` (string) mı yoksa `"type":306` (sayı) mı? Buna göre aşağıdaki fix'i uygula
(genelde **string** çıkar → FIX-A).

---

## FIX-A (string enum ise — beklenen) — mirror'ı gerçek enum tiplerine hizala

BFF-local mirror'daki `int` enum alanlarını **gerçek enum tipleriyle** değiştir; Refit'in `JsonStringEnumConverter`'ı
string VEYA sayıyı doğru deserialize eder. Enumlar `Aizen.Modules.Notification.Abstraction.Enum` altında.

1. **Proje referansı:** `Aizen.Bff.MarineProvider.Application.csproj` `Aizen.Modules.Notification.Abstraction`'ı
   referans etmiyorsa ekle (diğer modüllerin Abstraction'ları gibi). (Zaten push request tipleri Abstraction'dan
   geliyorsa referans vardır.)

2. **Mirror item'ı gerçek DTO ile değiştir** — `ProviderNotificationItemDto`'yu silip modülün `NotificationDto`'sunu
   kullan (Abstraction'da, doğru enum tipleriyle):
   ```csharp
   using Aizen.Modules.Notification.Abstraction.Dto;

   public sealed class ProviderNotificationsResponse
   {
       public List<NotificationDto> Items       { get; init; } = [];
       public int                   Total       { get; init; }
       public int                   UnreadCount { get; init; }
   }
   ```
   `NotificationDto` alanları: `Id(long)`, `Type(NotificationType)`, `Channel(NotificationChannel)`,
   `Status(NotificationStatus)`, `Title`, `Body`, `MetadataJson?`, `IsRead(bool)`, `CreatedAt(DateTimeOffset)`,
   `ReadAt(DateTimeOffset?)`. `GetProviderNotificationsBffQueryHandler`/`GetProviderNotificationsBffQuery` dönüş tipi
   `ProviderNotificationsResponse` kalır; yalnız item tipi değişir.

3. **FE kontratı korunur:** BFF yanıtını FE'ye **Newtonsoft** ile serileştiriyor (AddNewtonsoftJson, StringEnumConverter
   YOK) → enum'lar FE'ye **sayı** (306–309) olarak gider. FE mevcut `type: number` + `isMilestone(type>=306)` bozulmaz.
   > BFF Newtonsoft'ta global `StringEnumConverter` VARSA, FE'ye string gider ve FE kırılır — o durumda FE'ye sayı
   > gitmesini sağla (bu endpoint için) ya da FE tipini uyarlamamı iste. Beklenen: sayı (mevcut config'de StringEnum yok).

> **Not (referans eklenemiyorsa):** Abstraction referansı bir sebeple istenmiyorsa, alternatif: mirror'da `int` yerine
> `string Type/Channel/Status` tut ve BFF handler'da int'e parse et (`Enum.Parse`/sayısal string kontrolü). Ama gerçek
> DTO'yu kullanmak daha temiz ve önerilendir.

## FIX-B (sayı enum ise — daha az olası) — başka alan uyuşmazlığını bul
Step 0 sayı gösteriyorsa throw başka bir alandan: `CreatedAt`/`ReadAt` format, veya `Body` içindeki null non-nullable.
Log gövdesini item alan alan mirror ile karşılaştır ve uyumsuz tipi düzelt.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB + HTTP; hepsi geçmeden "done" deme)

provider2 = **100011**. DB: aizen/aizen.

1. **Step 0 log kanıtı** (yukarıda) — enum string mi sayı mı, yapıştır.
2. **Build** — BFF (+ değiştiyse csproj): `0 Error(s)`.
3. **Rebuild + restart** `bff-marineprovider`; boot log temiz.
4. **HTTP smoke — provider2 token (ASIL KABUL):**
   ```
   GET /api/v1/provider/notifications?skip=0&take=30
   ```
   Beklenen: **200**, gövde dolu:
   ```json
   { "header": { "isSuccess": true },
     "body": { "items": [ { "id":.., "type":306, "title":"İlk satışın gerçekleşti! 🎉", "isRead":false }, ...(4) ],
               "total": 4, "unreadCount": 4 } }
   ```
   - `body.items.length == 4`, her item'da `type` **sayısal** (306–309), `unreadCount == 4`.
   - **911 "error deserializing" KALMAMALI.**
5. **mark-read:** `PATCH .../{id}/read` → 200; GET → o kayıt `isRead=true`, `unreadCount=3`.
   `POST .../mark-all-read` → 200; GET → `unreadCount=0`.

---

## Acceptance
- `GET /api/v1/provider/notifications` **200** + dolu gövde (4 milestone item, `type` sayısal, `unreadCount=4`).
- 911 deserialization hatası gitti; FE `type: number` kontratı korunur.
- mark-read / mark-all-read UnreadCount'u doğru günceller.
- Build temiz; boot hatasız; token'lı GET + log kanıtı yapıştırıldı.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1 FIX-3"): BFF mirror enum alanları `int` iken modül string enum döndürüyordu →
deserialization throw. Mirror item gerçek `NotificationDto` (Abstraction, doğru enum tipleri) ile değiştirildi; Refit
JsonStringEnumConverter string/sayıyı çözüyor, BFF→FE sayısal kalıyor. Wire formatı log ile doğrulandı; token'lı GET
4 item + unreadCount=4 döndürdü.
