# Mobil baseline (s5–s9) — eşik kalibrasyon tablosu

İlk `dev-baseline` koşularından (3–5 VU, doygunluk altı) doldurulur. Değerler **ms** (k6 summary
SANİYE üretir → ×1000 çevir). `TODO` = henüz koşulmadı. Doldurulunca her senaryonun
`options.thresholds` `TODO-CALIBRATE` p95'lerini **p95 × ~1.5** ile değiştir (regresyon kapısı).

Koşum: `README.md → Baseline prosedürü`. Tarih damgalı özetler: `results/<YYYYMMDD>-sX-dev-baseline.json`.

| senaryo | endpoint tag | p95 (ms) | p99 (ms) | http_req_failed | önerilen eşik p95<(×1.5) |
|---|---|---:|---:|---:|---:|
| s5 | vessels-list        | TODO | TODO | TODO | TODO |
| s5 | sr-list             | TODO | TODO | TODO | TODO |
| s5 | notifications       | TODO | TODO | TODO | TODO |
| s5 | cargodry-kits       | TODO | TODO | TODO | TODO |
| s6 | vessel-create       | TODO | TODO | TODO | TODO |
| s6 | vessel-set-location | TODO | TODO | TODO | TODO |
| s6 | vessel-archive      | TODO | TODO | TODO | TODO |
| s7 | sr-create           | TODO | TODO | TODO | TODO |
| s7 | sr-detail           | TODO | TODO | TODO | TODO |
| s7 | sr-cancel           | TODO | TODO | TODO | TODO |
| s8 | marinas-nearby      | TODO | TODO | TODO | TODO |
| s8 | discovery-list      | TODO¹ | TODO¹ | TODO¹ | TODO¹ |
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
