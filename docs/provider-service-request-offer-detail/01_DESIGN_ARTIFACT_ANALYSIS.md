# 01 — Design Artifact Analysis

Sources: `screen.png`, `DESIGN.md` (Nautical Heritage — the system the SPA already implements), `code.html`
(Tailwind-CDN mock, sample data only).

## What the screen is

**Service Request Detail + Itemized Offer Builder** at `/app/service-requests/:serviceRequestId`. Two halves:

**Read (the request):** header (code `SR1637FF20`, title, `AÇIK` status chip, `ÖNCELİK: DÜŞÜK`, marina + distance
"Çeşme Marina (12 km)", planned dates), **İş Kapsamı** (structured work scope — Gövde Temizliği, Pasta Cila,
Zehirli Boya, each with a description), **Tekne Bilgileri** (Beneteau Oceanis 45, 2018, 13.85 m, Fiberglas), a
**Konum** map with distance "12.4 KM / 18 DK", **Tekne Durum Fotoğrafları** (photo gallery, "+5 Fotoğraf"),
**Hızlı Bilgiler** (customer "Kaptan Ahmet Yılmaz", vessel registration), **Mesajlar & Aktivite** (activity
timeline "Talep Yayınlandı 12 Nisan 10:24" + a customer message thread).

**Write (the offer builder — the crux):** **Teklif Oluştur** table with columns **TÜR / HİZMET-ÜRÜN / MİKTAR /
BİRİM FİYAT / KDV % / TOPLAM**, rows for a Service ("Gövde Basınçlı Yıkama", 1 Adet, 1.250, KDV 20, 1.500,00 ₺)
and a Product ("Jotun SeaForce", 2 Adet, 4.800, KDV 20, 11.520,00 ₺), **Yeni Kalem Ekle**, **Ticari Koşullar**
(currency TRY, earliest start date, payment plan "%50 Peşin", validity "15 Gün"), and a totals panel:
**Hizmet Toplamı / Ürün Toplamı / İşçilik & Diğer / Ara Toplam / KDV (%20) / GENEL TOPLAM 13.020,00 ₺**. Footer:
**Taslak Kaydet / Önizle / Teklifi Gönder**; header also **Şablondan Başla / Katalogdan Seç**.

## Design system — already ours

Same Nautical Heritage tokens the provider SPA implements (Marine Navy, Imperial Gold, EB Garamond + Hanken
Grotesk). **No new tokens.** Reuse `shared/ui/*` and the discovery scaffolding.

## Where the mock outruns the domain (resolved in doc 05/06)

| Mock element | Domain reality (verified) |
|---|---|
| **KDV % per line, KDV Toplamı** | Offer/offer-item entities have **no tax field at all**. `RecalculateTotal = Σ(Quantity × UnitPrice)` — no tax, no per-line total |
| **Per-line TOPLAM, Ara Toplam, service/product totals** | Not stored — only the offer's single `TotalAmount` |
| MİKTAR "2 Adet" (integer) | `Quantity` is **`int`** on both request and offer items — decimal quantities (labour hours, metres) unsupported |
| **Ödeme planı "%50 Peşin", deposit** | No deposit / payment-terms field on the offer |
| **Geçerlilik "15 Gün", validity** | `ExpiresAt` exists (absolute date) — mock shows a relative window |
| **Şablondan Başla / Katalogdan Seç** | No catalog module active → **manual items only for MVP** |
| Budget "₺18.000–25.000" (discovery card) | No budget field (decision 2026-07-14) — the detail header must not show one either |
| Warranty, version history, "customer viewed" | None modelled |
| Vessel "Beneteau Oceanis 45 / 13.85 m" | Vessel snapshot was **removed** in 09b.1; detail must BFF-enrich from Vessel (bulk), like the card |

## Non-negotiable adaptation rules

- **The server is authoritative for every total.** The mock computes in the browser; the backend must recompute
  and the UI must show server values on save/submit. Never trust a client-supplied line/tax/total.
- **Money is decimal, never float.** Quantity precision is a real decision (doc 06).
- Never render a backend enum (`Service`, `Submitted`, `AÇIK`) — i18n owns the words.
- No budget. No catalog APIs invented. No direct module calls from the browser.
- Reuse the discovery contracts and the canonical auth flow (`docs/provider-service-request-discovery/`), do not
  contradict them.
