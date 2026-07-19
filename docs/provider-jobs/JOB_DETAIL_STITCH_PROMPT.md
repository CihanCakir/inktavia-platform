# Inktavia Marine OS — İş Detayı (Job Detail) — Stitch Design Spec

Bir işin (assignment) detay ekranı (`/app/jobs/:assignmentId`). Sağlayıcı işi buradan yönetir: durumu görür,
**işi başlatır / tamamlandı bildirir**, iş kapsamını ve kabul edilen teklifi inceler, müşteriyle mesajlaşmaya geçer.
KPI/alanlar **mevcut gerçek veriye** dayandırılmıştır — aşağıdaki "Veri" notları neyin backend gerektirdiğini söyler.

---

## 0. Veri Gerçeklik Notları (tasarımın söz vermemesi gerekenler)
- **İş detay endpoint'i YOK.** Bugün yalnızca liste/summary/workload/action-required var. Detay için **yeni bir
  aggregate endpoint** (`GET /provider/jobs/{assignmentId}`) gerekir: assignment (durum + planlanan/gerçek tarih) +
  SR (başlık, kod, iş kapsamı, konum) + kabul edilen teklif (kalemler + toplam) + yaşam-döngüsü zaman çizelgesi. →
  backend prompt.
- **Aksiyonlar (İşi Başlat / Tamamlandı Bildir)** modülde komut olarak **var** (`StartServiceRequestAssignment`,
  `SubmitServiceRequestCompletion`) ama provider BFF'ine **açık değil** → BFF endpoint'leri gerekir. Onay/Red
  müşteri aksiyonudur (provider'da değil).
- **İş kayıtları / ilerleme fotoğrafları / saat takibi = Post-MVP** (backend yok) — tasarıma koyulursa "yakında"
  işaretlensin.
- **Müşteri kimliği gösterilmez** (gizlilik) — iş, talep başlığı/kodu + tekneyle anılır. Tutar yalnızca **kendi
  teklifinin** toplamıdır (müşteri bütçesi değil).

Durumlar (SR/iş yaşam-döngüsü): Atandı · Planlandı · Devam Ediyor · Onay Bekliyor · Malzeme Bekliyor · Duraklatıldı ·
Tamamlama Gönderildi · Tamamlandı.

---

## 1. Layout Hiyerarşisi
Inktavia Provider kabuğu içinde, üstte tam-genişlik başlık + 12-kolon grid (sol ana içerik col-8, sağ sticky sidebar
col-4).
1. **Page Header** — breadcrumb (İşler / iş kodu), başlık (EB Garamond), durum rozeti + **yaşam-döngüsü stepper**,
   sağda **duruma göre birincil aksiyon**.
2. **Main (col-8):** Zaman çizelgesi kartı · İş Kapsamı · Kabul Edilen Teklif · (Post-MVP) İş Kayıtları/Fotoğraflar.
3. **Sticky Sidebar (col-4):** Hızlı Bilgiler · Aksiyonlar · Mesaj kısayolu.

## 2. Bileşen Detayları

### A. Page Header
- Breadcrumb: `İşler / SR-SEED-EMERGENCY-1`.
- Başlık: talep başlığı ("Acil: Dümen sistemi arızası — Çeşme"). Alt satır: iş kodu + tekne (Aegean Wind).
- **Durum rozeti** (Atandı navy · Devam Ediyor gold · Onay/Malzeme amber · Tamamlandı gold) + yatay **stepper**
  (Atandı → Başlatıldı → Devam → Onay → Tamamlandı; işin bulunduğu adım vurgulu).
- **Birincil aksiyon (duruma göre):**
  - `Assigned`/`Scheduled` → **"İşi Başlat"** (gold) → StartAssignment.
  - `InProgress` → **"Tamamlandı Bildir"** (gold) → SubmitCompletion.
  - `CompletionSubmitted` → "Müşteri onayı bekleniyor" (pasif bilgi).
  - `Completed` → "Tamamlandı" (pasif) + "Finansal Özet".

### B. Zaman Çizelgesi (Timeline) — main, ilk kart
Yaşam-döngüsü olayları dikey liste: Teklif Kabul Edildi · İş Başlatıldı ({tarih}) · Tamamlama Gönderildi · İş
Tamamlandı — her biri ikon + tarih. (Veri: SR status history + lifecycle sistem mesajları.)

### C. İş Kapsamı — main
SR'nin iş kalemleri (ikon + başlık + açıklama). Detay sayfasındaki İş Kapsamı ile birebir (aynı SR verisi).

### D. Kabul Edilen Teklif — main
Kabul edilen teklifin kalem tablosu (Tür/Hizmet-Ürün/Miktar/Birim Fiyat/KDV/Toplam) + **Genel Toplam** (₺). Salt-okunur.

### E. (Post-MVP) İş Kayıtları / Fotoğraflar — main
İlerleme notları + fotoğraf yükleme yer tutucusu — "yakında" işaretli (backend yok).

### F. Sidebar — Hızlı Bilgiler
Tekne · Konum (yaklaşık, "kesin yer atandığında" notu) · Planlanan tarih aralığı · Gerçek başlangıç/bitiş · Müşteri
(isim yok → "iş size atandı" notu).

### G. Sidebar — Aksiyonlar & Mesaj
Duruma göre ikincil aksiyonlar (Talebi Gör → SR detay) + **"Mesajlar"** kısayolu (→ `/app/messages/:serviceRequestId`).

## 3. Teknik Tasarım Kuralları (Nautical Heritage — diğer ekranlarla aynı)
- Renkler: Arka plan `#F9F8F6` · Yüzey `#F2EEE9` · Kart `#FFFFFF` · Navy `#002147` · Gold `#C5A059` · hata `#8B0000`.
- Tipografi: Başlıklar `EB Garamond`; UI/veri `Hanken Grotesk`. Radius: kart 8px · buton/çip 4px. Para ₺ decimal.
- Durum rozetleri yumuşak dolgu; stepper gold-tamamlanmış / gri-bekleyen segmentler.

## 4. Durumlar
- **Loading / Error / Not found** (başka provider'ın işi → "bulunamadı", detay endpoint'i erişim-kontrollü).
- **İşi Başlat / Tamamlandı Bildir** onay diyaloğu (geri alınamaz aksiyon → tek onay).

## 5. Claude / Stitch Master Prompt (EN — doğrudan kullanılabilir)

> "Design the 'Job Detail' page for the Inktavia Marine OS provider portal using its Nautical Heritage design system.
> A full-width header (breadcrumb 'İşler / <job code>', request title, vessel + code subline, a status badge and a
> horizontal lifecycle stepper Assigned→Started→In Progress→Approval→Completed, and a state-dependent primary action:
> 'İşi Başlat' when Assigned, 'Tamamlandı Bildir' when In Progress, a passive 'awaiting approval' when submitted, and
> 'Completed' + 'Financial Summary' when done). Below, a 12-col grid: main (col-8) with a vertical lifecycle timeline
> card (Offer accepted, Job started {date}, Completion submitted, Completed), a Work Scope card (icon + title +
> description items), an Accepted Offer table (type / item / qty / unit price / VAT / line total + grand total in ₺,
> read-only), and a 'coming soon' Work Logs/Photos placeholder. A sticky sidebar (col-4) with Quick Info (vessel,
> approximate location with a 'exact berth shared once assigned' note, scheduled date range, actual start/end, and a
> privacy note instead of a customer name), an Actions block (View Request, and a Messages shortcut), and secondary
> state actions. No customer name, no budget — only the provider's own accepted-offer total. Colors: #F9F8F6
> background, #F2EEE9 surface, #002147 navy, #C5A059 gold; fonts EB Garamond (headings) and Hanken Grotesk (UI); card
> radius 8px, control radius 4px. Calm and roomy."

---

## 6. Uygulama Sonrası (bilgi — tasarım kararı değil)
- **Backend gerekir:** (1) `GET /provider/jobs/{assignmentId}` aggregate (assignment + SR + kabul edilen teklif +
  timeline), erişim-kontrollü (yalnızca kendi işi). (2) provider aksiyon endpoint'leri: `POST .../jobs/{id}/start`
  (StartAssignment) ve `POST .../jobs/{id}/complete` (SubmitCompletion) — komutlar mevcut, BFF'e açılacak.
- Timeline verisi SR status history + yaşam-döngüsü sistem mesajlarından; iş kapsamı + teklif SR/offer'dan.
- İş kayıtları/fotoğrafları Post-MVP. Rotalar: `/app/jobs/:assignmentId`.
