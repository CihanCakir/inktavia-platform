# Inktavia Marine OS — Mesajlar (Messages) — Stitch Design Spec

Provider portalının **Mesajlar** tam sayfası. Amaç: sağlayıcının, teklif verdiği taleplerdeki müşteri
konuşmalarını tek ekranda görüp yanıtlaması. İki panelli **inbox** düzeni: solda konuşma listesi, sağda seçili
konuşmanın thread'i + composer.

> **Anti-taciz kuralı (zorunlu, #18):** Sağlayıcı soğuk mesaj atamaz. Composer yalnızca **müşteri en az bir mesaj
> attıysa** (`channelOpen`) etkindir. Kilitliyken composer yerine bilgi notu görünür. Teklif, müşteriye otomatik
> "teklif mesajı" olarak düşer (sistem üretimi), sağlayıcının serbest metni değildir.

---

## 0. Veri Gerçeklik Notları (tasarımın fazla söz vermemesi için)
- **Thread + composer** (sağ panel): backend **HAZIR** (#18 — `GET/POST …/messages`, `channelOpen`, realtime
  `MessageAdded`). Mesaj alanları: gönderen tipi (Owner/Provider/System), mesaj tipi (Text/Offer/System), içerik,
  tarih, okundu.
- **Konuşma listesi + okunmamış rozeti + son-mesaj önizlemesi** (sol panel): backend **YOK** — yeni bir
  provider-scoped "konuşmalarım" endpoint'i gerekiyor (bir sonraki backend prompt). Tasarımda çizilecek ama
  "backend gerekli" kabul edilecek.
- **Müşteri adı gösterilmez** (gizlilik) — konuşmalar **talep başlığı + iş kodu** ile anılır, kişi adıyla değil.
- Ekli dosya/görsel gönderimi Post-MVP (composer'da ikon durabilir, pasif).

---

## 1. Layout Hiyerarşisi
İki panelli, tam yükseklik (üst bar altında). Masaüstü: sol liste sabit genişlik (~360px) + sağ thread esner.
Mobil: tek panel — liste; bir konuşmaya girince thread'e geçilir (geri butonuyla listeye dönülür).

1. **Sol Panel — Konuşmalar (Inbox)**
   - Üstte: başlık "Mesajlar" + arama ("Konuşmalarda ara…").
   - Liste: her satır bir konuşma.
2. **Sağ Panel — Thread**
   - Üstte: konuşma başlığı (talep başlığı + iş kodu) + "Talebi Gör" linki.
   - Orta: mesaj balonları (kaydırılabilir, en altta son mesaj).
   - Altta: composer (veya kilitli notu).
3. **Boş durumlar:** konuşma yoksa; konuşma seçili değilse (sağ panelde "Bir konuşma seçin").

---

## 2. Bileşen Detayları

### A. Konuşma satırı (sol liste)
- Sol: küçük yuvarlak avatar/ikon (tekne/gemi ikonu — müşteri fotoğrafı YOK).
- Orta: **talep başlığı** (navy, tek satır kırpma) + altında **son mesaj önizlemesi** (gri, tek satır) veya
  "Teklif gönderildi" (teklif mesajıysa).
- Sağ: **saat/tarih** (küçük) + **okunmamış rozeti** (gold dolu daire, sayı) varsa.
- Seçili satır: sol kenarda gold şerit + hafif surface dolgu.
- (Opsiyonel) küçük durum chip'i: "Kilitli" (müşteri henüz yanıtlamadı) — nötr gri.

### B. Thread başlığı (sağ panel üst)
- Talep başlığı (EB Garamond) + iş kodu (küçük gri).
- Sağda: "Talebi Gör" (outline buton/link → talep detayına).

### C. Mesaj balonları
- **Sağlayıcı (ben):** sağa hizalı, navy dolgu + açık metin.
- **Müşteri (owner):** sola hizalı, surface dolgu + navy metin.
- **Teklif mesajı (Offer):** ortalanmış/özel **teklif kartı** — "Teklif gönderildi · ₺{tutar}" + "Teklifi Gör"
  linki (talep teklif drawer'ına). Serbest metin balonu gibi değil, kart görünümlü.
- **Sistem mesajı:** ortalanmış küçük gri metin.
- Her balonun altında: gönderen etiketi ("Siz"/"Müşteri") + saat. Okundu göstergesi opsiyonel.

### D. Composer (alt)
- `channelOpen === true`: metin girişi + gönder butonu (gold). (Ekli dosya ikonu pasif/Post-MVP.)
- `channelOpen === false`: composer YERİNE bilgi kutusu → "Müşteri teklifinize yanıt verdiğinde mesajlaşma
  açılır." (gri, surface kutu).
- Gönderim sırasında buton loading; başarısızsa hata bandı.

### E. Realtime
- Yeni mesaj (`MessageAdded`) gelince: aktif thread otomatik tazelenir + o konuşma sol listede en üste taşınır /
  okunmamış rozeti güncellenir. Frame'de içerik yok — liste/thread yeniden çekilir.

---

## 3. Teknik Tasarım Kuralları (Nautical Heritage — diğer ekranlarla aynı)
- Renkler: Arka plan `#F9F8F6` · Yüzey `#F2EEE9` · Kart `#FFFFFF` · Navy `#002147` · Gold `#C5A059` · hata
  `#8B0000`. Border 1px navy-%12.
- Tipografi: Başlıklar EB Garamond; UI/mesaj Hanken Grotesk.
- Radius: kart 8px · balon 8px · input/buton 4px.
- Sağlayıcı balonu navy, müşteri balonu surface; gold yalnızca vurgu (gönder butonu, seçili konuşma şeridi,
  okunmamış rozeti, teklif kartı vurgusu).
- Para: `₺` decimal, binlik ayraç.

---

## 4. Durumlar
- **Boş inbox:** "Henüz konuşmanız yok" + kısa açıklama ("Teklifleriniz kabul veya yanıt aldığında konuşmalar
  burada açılır.").
- **Konuşma seçili değil (masaüstü sağ panel):** nötr placeholder "Soldan bir konuşma seçin."
- **Kilitli thread:** mesaj geçmişi (varsa teklif mesajı) + composer yerine kilitli notu.
- **Yükleniyor / hata:** iskeleti / tekrar dene.

---

## 5. Claude / Stitch Master Prompt (EN — doğrudan kullanılabilir)

> "Design a two-pane 'Messages' inbox for the Inktavia Marine OS provider portal using its Nautical Heritage design
> system. **Left pane — Conversations:** a 'Messages' title, a search field, and a scrollable list of conversation
> rows; each row shows a small vessel icon (NO customer photo), the service-request title (one line), a one-line
> last-message preview (or 'Offer sent' for an offer message), a timestamp, and an unread count badge in gold when
> present; the selected row has a gold left-edge stripe and a soft surface fill; an optional 'Locked' chip when the
> customer hasn't replied yet. **Right pane — Thread:** a header with the request title + code and a 'View Request'
> outline link; a scrollable message area with bubbles — provider (me) right-aligned navy fill, customer
> left-aligned surface fill, an OFFER message rendered as a centered card ('Offer sent · ₺{amount}' + 'View Offer'),
> and system messages as small centered gray text; each bubble labeled 'You'/'Customer' with a time. **Composer:**
> at the bottom, a text input + gold send button when the channel is open; when locked, replace the composer with a
> muted box: 'Messaging opens once the customer replies to your offer.' Include empty states: no conversations, and
> 'select a conversation' on the right. Colors: #F9F8F6 background, #F2EEE9 surface, #002147 navy, #C5A059 gold;
> fonts EB Garamond (headings) and Hanken Grotesk (UI/messages); radius 8px cards/bubbles, 4px controls. Customer
> identity is never shown — conversations are labeled by request title + code. Calm, roomy, no dense tables."

---

## 6. Uygulama Sonrası (bilgi — tasarım kararı değil)
- Sağ panel (thread + composer) mevcut #18 endpoint'leriyle bağlanır.
- Sol panel (inbox listesi + okunmamış + son mesaj) için **yeni provider-scoped "konuşmalarım" endpoint'i** gerekir
  (bir sonraki backend prompt: talep başlığı/kodu, son mesaj önizlemesi + zamanı, okunmamış sayısı, kilitli/açık).
- Rotalar: `/app/messages` (liste; masaüstünde thread seçilebilir) ve `/app/messages/:serviceRequestId` (derin
  link).
