# Inktavia Marine OS — Teklifler & Teklif Kütüphanesi — Stitch Design Specs

Bu doküman iki provider ekranının Stitch üzerinde yüksek sadakatle tasarlanması için hazırlanmıştır:
**SCREEN_A — Teklifler (My Offers)** ve **SCREEN_B — Teklif Kütüphanesi (Offer Library: Katalog + Şablonlar)**.
KPI ve alanlar **yalnızca mevcut gerçek veriden** türetilmiştir (aşağıdaki "Veri" notlarına bakın) — uydurma metrik
yoktur. Her iki ekran da Inktavia Provider Portal kabuğu içinde, sol sabit sidebar + üst bar ile çalışır.

---

## 0. Ortak Tasarım Sistemi (Nautical Heritage) — her iki ekran için

- **Renkler:** Arka plan (canvas) `#F9F8F6` (Pearl) · Yüzey (surface) `#F2EEE9` (Light Sand) · Kart yüzeyi
  `#FFFFFF` · Yapı/Metin `#002147` (Marine Navy) · İkincil metin `#44474E` · Birincil aksiyon/vurgu `#C5A059`
  (Imperial Gold) · Uyarı/hata `#8B0000`. Border: 1px `rgba(0,33,71,0.12)`.
- **Tipografi:** Başlıklar & büyük sayılar `EB Garamond`; UI, etiket ve veri `Hanken Grotesk`.
- **Radius:** Kartlar 8px · Input/Buton/Chip 4px.
- **Para:** Tümü `decimal`, `₺` (TRY), binlik ayraçlı (₺1.250,00).
- **Gizlilik (zorunlu):** Müşteri kimliği (isim/telefon) HİÇBİR yerde gösterilmez. Teklifler yalnızca **talep kodu +
  başlık** ile anılır.

---

## SCREEN_A — Teklifler (My Offers)

Sağlayıcının tüm tekliflerinin tek ekranda özeti + listesi. Sağlayıcı buradan tekliflerini takip eder, filtreler,
detaya gider, gerekirse geri çeker.

### A.1 Layout Hiyerarşisi (tek kolon, üstten alta)
1. **Page Header** — Başlık "Teklifler" (EB Garamond), kısa alt başlık; sağ üstte ikincil buton **"Teklif
   Kütüphanesi"** (kalem/şablon yönetimine gider).
2. **KPI Board** — 4 metrik kartı, yatay grid (mobilde 2×2).
3. **Filter / Segment Bar** — durum segmentleri + arama.
4. **Offers List** — kart-liste veya tablo.
5. **Empty / Error / Loading** durumları.

### A.2 KPI Board (4 kart) — *tümü teklif listesinden türetilir*
Her kart: küçük ETİKET (uppercase, gri) + büyük DEĞER (EB Garamond, navy) + opsiyonel alt-metin (gri).

| Kart | Değer | Alt-metin | Veri kaynağı |
|------|-------|-----------|--------------|
| **Aktif Teklifler** | `Submitted + UnderReview` adedi | "₺{aktif tutar toplamı} bekliyor" | offerStatus + totalAmount |
| **Kazanılan** | `Accepted` adedi | "%{kazanma oranı}" (Accepted / (Accepted+Rejected)) | offerStatus |
| **Bekleyen Değer** | aktif tekliflerin `totalAmount` toplamı ₺ | "gönderilen tekliflerin toplamı" | totalAmount (Submitted/UnderReview) |
| **Toplam Teklif** | tüm teklif adedi | "tümü" | items sayısı |

> Aksan rengi (gold) yalnızca "Kazanılan" kartının değerinde kullanılabilir; diğerleri navy.

### A.3 Filter / Segment Bar
- **Durum segmentleri** (pill/segment control): Tümü · Gönderildi · İncelemede · Kabul · Red · Geri Çekildi ·
  Süresi Doldu. Aktif segment gold alt-çizgi veya gold-dolgulu chip.
- **Arama:** talep kodu veya başlık ("İş kodu veya başlık ara…").
- (Opsiyonel) tarih aralığı filtresi.

### A.4 Offers List (kart-liste; masaüstünde tablo görünümü de kabul)
Her satır/kart:
- Sol: **Talep başlığı** (navy, kalın) + altında **talep kodu** (küçük, gri).
- Orta: **Durum rozeti** (Gönderildi = navy/açık, İncelemede = amber, Kabul = gold, Red = kırmızı, Geri Çekildi/
  Süresi Doldu = nötr gri).
- Sağ: **Tutar** (₺, EB Garamond) + **tarih** (oluşturma ya da duruma göre kabul/red tarihi) + aksiyonlar.
- **Aksiyonlar:** "Detay" (talebin teklif drawer'ına gider); yalnızca `Submitted`/`Draft` için "Geri Çek".
- **Veri notu:** Listede tekne adı YOKTUR (DTO'da yok) — tasarımda tekne kolonu koymayın ya da "opsiyonel/enrichment
  gerekli" olarak işaretleyin. Alanlar: başlık, kod, tutar, durum, tarih.

### A.5 Empty / Error
- Empty: "Henüz teklif vermediniz" + "Servis taleplerini keşfedin" CTA (discovery'e gider).
- Error: "Teklifler yüklenemedi" + tekrar dene.

---

## SCREEN_B — Teklif Kütüphanesi (Katalog + Şablonlar)

Sağlayıcının yeniden kullanılabilir teklif kalemleri (katalog) ve adlandırılmış çok-kalemli şablonlarını yönettiği
ekran. Teklif drawer'ındaki "Katalogdan Seç / Şablondan Başla" picker'ları bu kütüphaneden beslenir.

### B.1 Layout Hiyerarşisi
1. **Page Header** — "Teklif Kütüphanesi" + alt başlık.
2. **KPI Strip** — küçük, 2 kart (Katalog kalem sayısı · Şablon sayısı). İnce, header altında.
3. **Tabs** — "Katalog" · "Şablonlar" (gold alt-çizgili aktif sekme).
4. **Aksiyon satırı** — sağda birincil buton ("Yeni Kalem" / "Yeni Şablon"); solda (opsiyonel) arama + tür filtresi.
5. **İçerik** — liste (kart-satır) veya kart-grid.
6. **Modal formlar** — oluştur/düzenle.
7. **Empty** durumları.

### B.2 KPI Strip (2 kart) — *gerçek veriden*
| Kart | Değer | Veri |
|------|-------|------|
| **Katalog Kalemleri** | katalog kalem adedi | catalog list |
| **Şablonlar** | şablon adedi | template list |

> "En çok kullanılan kalem/şablon" gibi metrikler kullanım takibi gerektirir — **şu an veri yok**, tasarıma
> koyulacaksa "Post-MVP" olarak işaretleyin, gerçek sayı gibi göstermeyin.

### B.3 Katalog sekmesi
- **Filtre (opsiyonel):** ara + Tür filtresi (Hizmet/Ürün/İşçilik/…) chip'leri.
- **Liste:** her kart-satır → sol: kalem başlığı (navy) + alt satır "Tür · Birim · %KDV" (gri); sağ: **birim
  fiyat** (₺) + düzenle (kalem ikonu) + sil (çöp ikonu).
- (Opsiyonel görünüm) Tür bazında gruplama başlıkları.
- **Yeni/Düzenle modalı:** alanlar → Tür (select), Başlık, Açıklama, Birim (kod, örn. ADET/LITER/HOUR), Birim
  Fiyat, Miktar, KDV %. Alt: Vazgeç / Kaydet.

### B.4 Şablonlar sekmesi
- **Liste:** her kart-satır → sol: şablon adı (navy) + "N kalem" (gri); sağ: (opsiyonel) kalemlerin toplam ₺
  önizlemesi + düzenle + sil.
- **Yeni/Düzenle modalı (geniş):** Şablon Adı + Açıklama; altında **kalem satırları** editörü — her satır: Tür
  (select) · Başlık · Miktar · Birim · Birim Fiyat · KDV % · satır sil; altında "Kalem Ekle". Alt: Vazgeç / Kaydet.
- **Kural:** şablon en az 1 kalem içermeli (0 kalemde Kaydet pasif).

### B.5 Empty
- Katalog boş: "Kataloğunuz boş" + kısa açıklama + "Yeni Kalem".
- Şablon boş: "Kayıtlı şablonunuz yok" + açıklama + "Yeni Şablon".

---

## 3. Teknik Tasarım Kuralları (özet)
- Kartlar 8px radius, 1px navy-%12 border, beyaz yüzey; sayfa arka planı Pearl.
- KPI değerleri EB Garamond; etiketler Hanken Grotesk uppercase, letter-spacing.
- Birincil aksiyon gold dolgulu + navy metin; ikincil aksiyon outline (navy border).
- Durum rozetleri: yumuşak dolgu + eşleşen border (gold/amber/kırmızı/nötr), 4px radius.
- Yoğun tablolar yerine ferah kart-liste tercih edilir; masaüstünde çok sütun gerekiyorsa hizalı tablo.

## 4. Veri Gerçeklik Notları (tasarımın söz vermemesi gerekenler)
- Teklifler: **müşteri adı yok**, **tekne adı yok** (DTO'da yalnızca talep kodu/başlık/tutar/durum/tarihler var).
- Kütüphane: kullanım/popülerlik metriği yok (Post-MVP).
- Tüm sayılar/tutarlar provider'a özeldir (server assertion ile scoped).

---

## 5. Claude / Stitch Master Prompt (EN — doğrudan kullanılabilir)

> "Design two screens for the Inktavia Marine OS provider portal using its Nautical Heritage design system.
> **Screen A — 'Teklifler' (My Offers):** a page header with a secondary 'Offer Library' action, a 4-card KPI board
> (Active Offers with pending ₺ total, Won with win-rate %, Pending Value ₺, Total Offers), a status segment bar
> (All · Submitted · Under Review · Accepted · Rejected · Withdrawn · Expired) with a code/title search, and a roomy
> card-list of offers — each row showing request title + code, a status badge, the amount in ₺, a date, a 'Detail'
> action and a 'Withdraw' action only for submitted/draft. No customer name and no vessel column (not available).
> **Screen B — 'Teklif Kütüphanesi' (Offer Library):** a header, a small 2-card KPI strip (catalog item count,
> template count), tabs 'Katalog' / 'Şablonlar', a right-aligned primary 'New' button, and a card-list. Catalog
> rows show title, 'type · unit · %VAT', unit price and edit/delete; template rows show name, 'N items' and
> edit/delete. Include create/edit modals: a catalog-item form (type, title, description, unit, price, quantity,
> VAT%) and a wide template form (name, description, and repeatable item rows: type, title, quantity, unit, price,
> VAT%, remove; plus 'Add Item'). Colors: #F9F8F6 background, #F2EEE9 surface, #002147 navy text, #C5A059 gold
> accents; fonts EB Garamond (headings & KPI numbers) and Hanken Grotesk (UI); card radius 8px, control radius 4px.
> Money as ₺ with thousands separators. Keep it calm and roomy — prefer cards over dense tables."
