# Inktavia Marine OS — İşler (Jobs) — Stitch Design Spec

Provider portalının **İşler** ana sayfası (`/app/jobs`): sağlayıcının kabul edilmiş tekliflerden doğan **işlerini**
(assignment'lar) takip ettiği yer. KPI board + durum segmentleri + iş listesi/tablosu + sayfalama + iş yaşam-döngüsü
(süreçler). KPI ve kolonlar **yalnızca mevcut gerçek veriden** türetilir (aşağıdaki "Veri" notları) — uydurma yok.

---

## 0. Veri Gerçeklik Notları (tasarımın söz vermemesi gerekenler)
Mevcut iş kaydı (`ProviderJobDto`) şunları içerir: `assignmentId`, `serviceRequestId`, `serviceRequestOfferId`,
`status`, `scheduledStartDate/EndDate`, `actualStartDate/EndDate`, `providerNotes`.

- **İş başlığı, tekne adı, tutar YOK** — DTO'da yalnızca id'ler + durum + tarihler var. Tablodaki "başlık / tekne /
  tutar" kolonları **BFF enrichment gerektirir** (detaydaki vessel-enrichment gibi, ayrı bir backend prompt).
  Tasarımda gösterilecek ama "enrichment gerekli" olarak işaretlensin. Enrichment gelene kadar satır **iş kodu +
  durum + tarihlerle** kurulabilir.
- **Toplam kayıt sayısı YOK** — sayfalama `pageIndex/pageSize` ile, `totalReturned` yalnızca o sayfadaki adet. Yani
  "X / Y toplam" gösterme; **"Daha fazla yükle"** ya da İleri/Geri sayfalama tasarla (gerçek toplam istenirse backend
  follow-up gerekir).
- **Gelir/kazanç KPI'sı** teklif tutarına bağlı → tutar enrichment gelmeden gerçek gösterilemez. "Post-MVP" işaretle.
- Müşteri kimliği gösterilmez (gizlilik) — iş, talep kodu/başlığıyla anılır.

Durum değerleri (iş yaşam-döngüsü): `Assigned` (Atandı) · `Scheduled` (Planlandı) · `InProgress` (Devam Ediyor) ·
`WaitingForOwnerApproval` (Onay Bekliyor) · `WaitingForMaterial` (Malzeme Bekliyor) · `Paused` (Duraklatıldı) ·
`CompletionSubmitted` (Tamamlama Gönderildi) · `Completed` (Tamamlandı).

---

## 1. Layout Hiyerarşisi (tek kolon, üstten alta)
1. **Page Header** — "İşler" (EB Garamond) + kısa alt başlık. Sağda görünüm değiştirici (Liste / Pano) — opsiyonel.
2. **KPI Board** — 4 metrik kartı (mobilde 2×2).
3. **Filter / Segment Bar** — durum segmentleri + arama (iş kodu).
4. **İş Listesi** — tablo (masaüstü) veya kart-liste; her satırda durum + tarihler + aksiyon.
5. **Sayfalama** — "Daha fazla yükle" veya İleri/Geri.
6. **(Opsiyonel) Pano görünümü** — duruma göre kolonlu Kanban (süreç görselleştirmesi).
7. **Empty / Error / Loading**.

## 2. KPI Board (4 kart) — *tümü iş listesinden türetilir (durum sayımı)*
| Kart | Değer | Alt-metin | Kaynak |
|------|-------|-----------|--------|
| **Aktif İşler** | Tamamlanmamış tüm işler (Completed hariç) adedi | "devam eden süreçler" | status |
| **Devam Eden** | `InProgress` adedi | "şu an sahada" | status |
| **Planlanan** | `Scheduled` adedi (yaklaşan `scheduledStartDate`) | "yaklaşan işler" | status + scheduledStartDate |
| **Onay/Aksiyon Bekleyen** | `WaitingForOwnerApproval` + `WaitingForMaterial` + `CompletionSubmitted` adedi | "aksiyon gerektiren" | status |

> Gelir/kazanç kartı **koyulacaksa** "Post-MVP" işaretle (tutar enrichment yok). Aksan rengi (gold) yalnızca bir
> pozitif metrikte (ör. Tamamlanan sayısı) kullanılabilir.

## 3. Filter / Segment Bar
- **Durum segmentleri** (pill): Tümü · Atandı · Planlandı · Devam Ediyor · Onay Bekliyor · Malzeme Bekliyor ·
  Duraklatıldı · Tamamlandı. Aktif segment gold.
- **Arama:** iş kodu ("İş kodu ara…"). (Başlık aramasi enrichment sonrası.)

## 4. İş Listesi (masaüstü tablo / mobil kart)
Kolonlar (enrichment-gerektirenler işaretli):
- **İş** — talep başlığı *(enrichment)* + altında **iş kodu** (SR-kodu).
- **Tekne** — tekne adı *(enrichment)* — enrichment yoksa kolon gizlenir/"—".
- **Durum** — renkli rozet (Planlandı nötr, Devam Ediyor gold/amber, Onay/Malzeme Bekliyor amber, Duraklatıldı gri,
  Tamamlandı gold, Atandı navy).
- **Planlanan Tarih** — `scheduledStartDate – scheduledEndDate`.
- **Gerçek Tarih** — `actualStartDate – actualEndDate` (varsa).
- **Aksiyon** — "Detay" → iş detayına (`/app/jobs/:id`).
- Satır sağında (opsiyonel) küçük **süreç ilerleme çubuğu**: Atandı → Planlandı → Devam → Onay → Tamamlandı
  adımlarında işin nerede olduğunu gösteren mini-stepper.

## 5. Sayfalama
- **"Daha fazla yükle"** (önerilen — toplam sayı yok) ya da İleri/Geri. "X/Y toplam" YOK.

## 6. (Opsiyonel) Pano / Süreç Görünümü
Duruma göre kolonlu Kanban: **Planlandı · Devam Ediyor · Onay Bekliyor · Tamamlandı**. Her kart: iş kodu + durum +
planlanan tarih. Süreç akışını görselleştirir; "süreçler" ihtiyacını en iyi bu karşılar. Liste ↔ Pano geçişi header'da.

---

## 3. Teknik Tasarım Kuralları (Nautical Heritage — diğer ekranlarla aynı)
- Renkler: Arka plan `#F9F8F6` · Yüzey `#F2EEE9` · Kart `#FFFFFF` · Navy `#002147` · Gold `#C5A059` · hata `#8B0000`.
- Tipografi: Başlıklar & KPI sayıları `EB Garamond`; UI/veri `Hanken Grotesk`.
- Radius: kart 8px · chip/buton 4px. Durum rozetleri yumuşak dolgu + eşleşen border.
- Ferah kart-liste veya hizalı tablo; yoğun grid'den kaçın. Tarihler `gg Ay yyyy`.

## 4. Durumlar
- **Empty:** "Henüz işiniz yok" + "Kabul edilen teklifleriniz burada işe dönüşür." + "Teklifleri Gör" CTA.
- **Error:** "İşler yüklenemedi" + tekrar dene.
- **Profil bağlı değil** (`hasProfileLink=false`): bilgilendirici not (işler için sağlayıcı profili gerekli).

---

## 5. Claude / Stitch Master Prompt (EN — doğrudan kullanılabilir)

> "Design the 'İşler' (Jobs) page for the Inktavia Marine OS provider portal using its Nautical Heritage design
> system. Layout: a page header ('İşler' + subtitle, optional List/Board view toggle); a 4-card KPI board (Active
> Jobs = all not-completed, In Progress, Scheduled/upcoming, Awaiting Action = approval+material+completion-submitted)
> derived purely from job status; a status segment bar (All · Assigned · Scheduled · In Progress · Awaiting Approval ·
> Awaiting Material · Paused · Completed) with a job-code search; and a roomy jobs table (desktop) / card list
> (mobile). Each row: a Job column (request title + code — title needs backend enrichment), an optional Vessel column
> (also enrichment), a colored status badge, a Scheduled date range, an Actual date range, a 'Detail' action, and an
> optional small lifecycle progress stepper (Assigned → Scheduled → In Progress → Approval → Completed). Pagination is
> a 'Load more' button (there is no total count — do NOT show 'X of Y'). Optionally include a Board (Kanban) view
> grouped by status (Scheduled · In Progress · Awaiting Approval · Completed) as the process visualization. No
> customer name and no amount column (not available yet). Colors: #F9F8F6 background, #F2EEE9 surface, #002147 navy,
> #C5A059 gold; fonts EB Garamond (headings & KPI numbers) and Hanken Grotesk (UI); card radius 8px, control radius
> 4px. Calm and roomy — prefer cards/aligned tables over dense grids."

---

## 6. Uygulama Sonrası (bilgi — tasarım kararı değil)
- KPI'lar + durum segmentleri + tarih kolonları mevcut `GET /jobs` verisiyle **şimdi** bağlanır.
- **Başlık / tekne / tutar** kolonları için bir **BFF enrichment prompt'u** gerekir (detaydaki vessel-enrichment gibi
  bulk çağrı + id eşleştirme). Enrichment gelene kadar satır iş kodu + durum + tarihlerle çalışır.
- Gerçek "toplam kayıt" sayısı ve gelir KPI'sı istenirse ayrı backend follow-up.
- Rotalar: `/app/jobs` (liste) ve `/app/jobs/:assignmentId` (detay).
