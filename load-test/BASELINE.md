# Mobil baseline (s5–s9) — eşik kalibrasyon tablosu

İlk `dev-baseline` koşularından (3–5 VU, doygunluk altı) doldurulur. Değerler **ms** (k6 summary
SANİYE üretir → ×1000 çevir). `TODO` = henüz koşulmadı. Doldurulunca her senaryonun
`options.thresholds` `TODO-CALIBRATE` p95'lerini **p95 × ~1.5** ile değiştir (regresyon kapısı).

Koşum: `README.md → Baseline prosedürü`. Tarih damgalı özetler: `results/<YYYYMMDD>-sX-dev-baseline.json`.

| senaryo | endpoint tag | p95 (ms) | p99 (ms) | http_req_failed | önerilen eşik p95<(×1.5) |
|---|---|---:|---:|---:|---:|
| s5 | vessels-list        | 2105 | — | 0% | 3200 |
| s5 | sr-list             | 1812 | — | 0% | 2800 |
| s5 | notifications       | 2190 | — | 0% | 3300 |
| s5 | cargodry-kits       | 1875 | — | 0% | 2900 |
| s6 | vessel-create       | 3508 | — | 0% | 5300 |
| s6 | vessel-set-location | 2625 | — | 0% | 3900 |
| s6 | vessel-archive      | 2689 | — | 0% | 4000 |
| s7 | sr-create           | 8442 | — | 0% | 12700 |
| s7 | sr-detail           | 2304 | — | 0% | 3500 |
| s7 | sr-cancel           | 5456 | — | 0% | 8200 |
| s8 | marinas-nearby      | 2500 | — | 0% | 3800 |
| s8 | discovery-list      | SKIP¹ (2026-09-07 koşusunda atlandı) | — | — | — |
| s9 | trip-start          | TODO² | TODO² | TODO² | TODO² |
| s9 | trip-ping           | TODO² | TODO² | TODO² | TODO² |
| s9 | trip-arrive         | TODO² | TODO² | TODO² | TODO² |

¹ `discovery-list` yalnız `PROVIDER_USER` verildiğinde ölçülür (onboarded provider). Aksi halde leg atlanır.
² `s9` yalnız `PROVIDER_USER` + `TRIP_SR_ID` (accepted-assignment fixture) verildiğinde koşar; s9 ayrıca
  `s9: 10/10 ping hatasız yutuldu` check'ini raporlar (3sn throttle absorbe doğrulaması).

## Koşum kaydı

| tarih | senaryo | profil | VU | ortam | results dosyası | not |
|---|---|---|---|---|---|---|
| _TODO_ | s5 | dev-baseline | 5 | dev | `results/…` | ilk kalibrasyon |

## s1 (admin BFF) — dev-baseline kalibrasyonu TAMAM (2026-09-07)

5 VU × 5 dk, 1346 istek, %0 hata; genel p95 2,21 sn / med 841 ms. Uç bazlı p95'ler Prometheus
flush-serisinden (seyrek orneklemde yukari yanli — esikli ilk kosu kesinlestirir). Esikler s1'in
dev-baseline profiline yazildi (x1,5):

| endpoint tag | p95 (ms) | eşik p95< |
|---|---:|---:|
| dashboard-overview | 6254 | 9500 |
| dashboard-charts | 4401 | 6600 |
| users-kpi | 2938 | 4400 |
| cargodry-analytics | 2188 | 3300 |
| users-list | 2133 | 3200 |
| cargodry-kits | 1727 | 2600 |
| vessels-list | 1658 | 2500 |
| cargodry-stats | 1531 | 2300 |
| providers-list | 1445 | 2200 |
| kc-token | 148 | 300 |

Bağlam: aynı gün 25 VU'luk ilk koşu doygunluk göstermişti (p95 17-32 sn) → CPU limit gevşetmesi
(PR ile) + dev-cap 10→14 sonrası bu tablo alındı. İzleme notu: identity 750m'de bile ~%77 CFS
throttle yedi (p95'e yansımadı; 25 VU tekrarında ilk şüpheli).

## 20 VU baseline (LT-7, 2026-09-09 — BFF CPU/thread-pool fix sonrası)

Koşullar: mobil+provider BFF 1000m, adminpanel 2000m, DOTNET_ThreadPool_MinThreads=32 (4 BFF+identity).

| Senaryo | Uç | p95 (ölçülen) | Kapı (×1,5) |
|---|---|---|---|
| s5 (20 VU, 3,5dk, 1512 istek, %0 hata) | vessels-list | 2,48s | 3700ms |
| s5 | sr-list | 2,15s | 3200ms |
| s5 | notifications | 3,18s | 4800ms |
| s5 | cargodry-kits | 2,47s | 3700ms |
| s8 (20 VU, 3dk, 1779 istek, %0 hata) | marinas-nearby | 2,28s | 3400ms |
| s8 | discovery-list | — (PROVIDER_USER yok, atlandı) | TODO (s9 fixture) |

Not: s5 vessels p95 5 VU'da 2,11s idi — 4x yükte 2,48s: mobil BFF fix'i doygunluğu kırdı.
s8 marinas-nearby 20 VU'da (2,28s) 5 VU ölçümünden (2,50s) İYİ.
