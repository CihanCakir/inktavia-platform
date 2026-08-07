# Provider Web — Live Walkthrough Findings (2026-08, localhost:3002, logged-in provider "PROVIDER 2 AS")

> On-screen pass via the browser (provider Cihan Çakır). Complements the static audit in `PROVIDER_QA_ROADMAP.md`.
> Screens visited: dashboard, service-requests, offers, finance, cargodry, settings, performance, support (+ nav observed
> throughout). Confirms much of the app is live and working; surfaces new runtime/visual findings.

## 🔴 New cross-cutting finding — X4: currency inconsistency (₺ TRY vs USD) — GO-LIVE CRITICAL
- **Service Requests / Offers show TRY (₺):** e.g. "Teklifiniz: ₺6.780,00", offer amounts "₺3.732,00".
- **Finance + CargoDry show USD:** Finance › Hakedişler = "0 USD / 0 USD / 0 USD" under the header "CargoDry konsinye
  hakedişlerin…"; CargoDry = "Silver kademesine **5.000 USD** kaldı", "Aylık Hedef **0/150 USD**", "Kazanç Potansiyeli
  **60 USD**", "Son 12 Ay Komisyon **0 USD**".
- The marketplace **settles in TRY** (TL fixed at acceptance). A provider seeing earnings/targets in **USD** on Finance +
  CargoDry while offers are in **₺** is inconsistent and likely wrong. **Decide the canonical currency (TRY) and fix the
  Finance + CargoDry displays** (or, if CargoDry commissions are genuinely USD-denominated, make that explicit and
  reconcile with the TRY marketplace). Add this as **P-QA-X4** (high priority) → `Finance/` + `CargoDry/`.

## Per-screen live results
- **Dashboard** (`/app/dashboard`): confirms P-QA1 — **Aktif İşler = 1** (real) + recent job "Servis talebi #9011
  Assigned"; the other 5 tiles show **"Yakında"** (Açık Teklifler / Servis Talepleri / Okunmamış Mesajlar / Finans Özeti /
  Bekleyen Yenilemeler). No console errors. → P-QA1 wiring stands.
- **Service Requests / Discovery** (`/app/service-requests`): **live & working** — "Şehrimdeki Açık İşler 48", real request
  cards (RT2-12/RT2-11, HULL_MAINTENANCE, "Teklifi Yönet", "Teklifiniz ₺…"), Bölünmüş/Harita/Liste toggle, distance filter,
  "Canlı" indicator. **Visual bug:** in **Bölünmüş** view the **map panel is blank** (no tiles render; only zoom + "Bu
  alanda ara") — matches the `DiscoveryMap.tsx` "hardcoded credential" note → map tile provider key/config. → `ServiceRequests/`.
  (Static "0 query hooks" was misleading — queries live in child components; the page IS implemented → nav flag correct.)
- **Offers** (`/app/offers`): **live & working** — 4 offers with status badges (Taslak/Kabul), amounts in ₺, status filter
  tabs, Geri Çek/Detay. **Minor UX:** "Aktif Teklifler 0" + "Bekleyen Değer ₺0,00" while 3 Taslak (draft) offers with value
  exist — metric semantics (draft ≠ active/pending) may confuse; clarify labels. → `Offers/`.
- **Finance** (`/app/finance`): **live** — 6 tabs (Hakedişler/Ödemeler/İşlemler/Faturalar/Abonelik/Ödeme Profili), trend
  chart, filters, "Henüz hakediş yok" empty (expected pre-payment). **Findings:** (a) **USD** amounts (X4); (b) the landing
  **Hakedişler** tab is scoped to **CargoDry consignment** ("CargoDry konsinye hakedişlerin") — where do **service-request
  completion earnings** surface? confirm they appear (Ödemeler/İşlemler) and that the default tab isn't misleading. → `Finance/`.
- **CargoDry** (`/app/cargodry` → inventory): **live & rich** — Bronze tier + progress, Bu Ay Kazancın / Aylık Hedef /
  Kazanç Potansiyeli, komisyon trend, kritik uyarı (kit expiring 13d), tiles (stok 2 / kit 3 / yenileme 1 / uyarı 1 /
  iade 1), Envanter/Yenilemeler/Ürünler tabs. Read-oriented (no activation write) ✓. **USD throughout (X4).** → `CargoDry/`.
- **Settings** (`/app/settings`): **live & real** — Dil selector + **N-B notification preference matrix** (Mesajlar/Servis
  talepleri/Anlaşmazlıklar/Ödemeler/CargoDry/Duyurular × Uygulama-içi[locked]/Anlık/E-posta/SMS) + browser-push enable.
  **Note:** SMS column disabled everywhere (channel unavailable — expected); preferences live here while the Notifications
  page is the inbox (reasonable split — avoid duplicating). Static "no api dir" resolved: it uses the notification hooks. → `Account/`.
- **Performance** (`/app/performance`): confirmed **"Yakında" Coming-Soon stub** (Construction icon). Nav badge present. → P-QA7.
- **Support** (`/app/support`): **live** — "Canlı Destek" form (Konu: Ödeme/Servis talebi/Hesap/Faturalama/Teknik/Diğer +
  Başlık + Mesaj + "Canlı desteğe bağlan") = N-D live-support channel. → `Account/` (verify it reaches an admin).
- **Nav (observed):** only **Performans** + **Belgeler** carry the "YAKINDA" badge now — offers/notifications/service-requests/
  cargodry are un-badged → **P-QA0 nav-flag fix appears already landed** (or was already correct for those). ✓

## Not yet walked (trust static / quick follow-up): jobs (detail/work-logs/complete), messages (realtime), notifications
(inbox), profile, documents (stub like performance). Recommend a short second pass for jobs + messages (realtime best seen
live).

## Method note
Console-error capture starts after the tool attaches, so page-load errors weren't captured on first paint — a second pass
with the console tool attached **before** navigation would catch load-time errors (esp. the discovery map tile failure).

## Priority (live-adjusted)
1. **X4 currency (₺ vs USD)** — Finance + CargoDry — go-live critical.
2. **P-QA1 dashboard** — wire the 5 placeholders.
3. **Discovery map** blank tiles — ServiceRequests.
4. Finance Hakedişler scope (CargoDry-only default) + SR-earnings visibility.
5. Offers metric-label semantics (minor).
6. P-QA7 build/hide performance + documents stubs.
