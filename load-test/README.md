# Load test suite (k6)

k6 senaryoları + dev cluster koşum düzeni. Metrikler Prometheus remote-write ile akar, Grafana
"Yük Testi" panosunu doldurur.

## Senaryolar

| script | ne ölçer | hedef (baseline) | kimlik |
|---|---|---|---|
| `s0-smoke.js` | boru temizliği: Keycloak + admin/mobil BFF kökü erişilebilir mi | 1 VU 30s | yok |
| `s1-token-read.js` | admin BFF okuma | 5→25 VU | admin (Admin rolü) |
| `s2-write-flow.js` | admin CargoDry ürün yazma | 3 rps | admin |
| `s3-otp-request.js` | OTP zinciri (throttle yolu) | 2→10 VU | yok (identifier env) |
| `s4-kc-token.js` | Keycloak token ucu | 5 VU | 25 kullanıcı |
| **`s5-mobile-read.js`** | **mobil ana-ekran fan-out** (vessels/SR/notifications/cargodry-kits) | 1→20 VU 3dk | katılımcı |
| **`s6-mobile-vessel-write.js`** | **tekne yaz → marina seç → arşivle** | 5 VU | katılımcı |
| **`s7-mobile-sr-create.js`** | **SR oluştur (marinadan şehir) → detay → iptal** | 5 VU 2dk | katılımcı |
| **`s8-discovery-nearby.js`** | **nearby marinas + radiusless provider discovery** (cache adayı) | 20 VU | katılımcı (+provider opsiyonel) |
| **`s9-trip-ping.js`** | **sefer start → 10 ping (3sn throttle) → arrive** | 1 VU | provider (fixture) |

Her mobil senaryo `PROFILE` env ile üç profil taşır: `smoke` (varsayılan; 1 VU, yalnız hata kapısı),
`dev-baseline` (3–5 VU doygunluk-altı kalibrasyon), `baseline` (20-VU/hedef profil + p95 eşikleri).
**p95 eşikleri şu an `TODO-CALIBRATE` placeholder** — ilk `dev-baseline` koşusunun `BASELINE.md`
değerlerinden doldurulur.

> **Dev kapasitesi (ölçüm 2026-09-07):** dev ~25 eşzamanlı VU'da doyuyor (p95 17–32 sn, CPU-limit
> throttle). Bir limits-raise PR'ı yolda. O merge+rollout OLANA DEK kalibrasyonu `dev-baseline`
> (3–5 VU) ile koşun; `baseline` (20 VU) profilleri limits fix SONRASI içindir.

## Hedef adresler (BASE_MOBILE)

`dev-mapi.inktavia.com` **HEDEF ALINMAZ** — yolda Cloudflare Tunnel var (ölçümü kirletir, WAF throttle
riski). Bunun yerine `BASE_MOBILE` doğrudan servise gider (`lib/config.js`):
- Lokal compose: `http://bff-marine-mobile:8080` (k6 compose ağında koşar).
- Dev cluster: `http://aizen-bff-marine-mobile.inktavia-dev.svc.cluster.local` (svc port 80; K8s Job geçer).
- Lokal→dev port-forward: `kubectl -n inktavia-dev port-forward svc/aizen-bff-marine-mobile 18080:80` →
  `BASE_MOBILE=http://localhost:18080`.

`BASE_ADMIN` / `BASE_PROVIDER` aynı desende (compose vs ClusterIP).

## Ön koşul: Keycloak hazırlığı

`setup-keycloak-loadtest.sh` (idempotent, DEV-only) `loadtest-runner` public client + `loadtest-user-01..25`
kullanıcılarını kurar. Mobil BFF için EKLENENLER (bu PR):
- **Audience mapper `marine-mobile-bff`** loadtest-runner token'ına (mobil BFF `ValidAudience=marine-mobile-bff`
  bekler — olmadan her mobil istek 401).
- **Realm rolü `mobile_user`** 25 kullanıcıya (mobil `ParticipantActive` politikası `RequireRole("mobile_user")`;
  owner CRUD uçları bugün yalnız `ParticipantAuthenticated` istese de rol ileriye dönük eklendi).

```bash
 LOADTEST_USER_PASSWORD='<sifre>' ./load-test/setup-keycloak-loadtest.sh   # komut başında BOŞLUK: history'e düşmesin
```

## Lokal koşum (dev'e karşı, Grafana panelleri dolar)

k6 remote-write süreleri **SANİYE** cinsindendir (panolar `unit=s`'e göre düzeltildi). `K6_PROMETHEUS_RW_TREND_STATS`
**şart** — yoksa k6 yalnız p99 üretir, p95 panelleri boş kalır.

```bash
# 1) Prometheus'u port-forward et (remote-write alıcısı)
kubectl -n monitoring port-forward svc/prometheus 19090:9090 &

# 2) Mobil BFF'i port-forward et (dev-mapi yerine — Cloudflare Tunnel'i atla)
kubectl -n inktavia-dev port-forward svc/aizen-bff-marine-mobile 18080:80 &
kubectl -n inktavia-dev port-forward svc/keycloak 18081:8080 &

# 3) Koştur (grafana/k6 0.52.x → çıktı bayrağı 'experimental-prometheus-rw';
#    k6 ≥ v0.55'te bu 'prometheus-rw' olarak yeniden adlandırıldı — kurulu sürümü teyit et: `k6 version`)
K6_PROMETHEUS_RW_SERVER_URL=http://localhost:19090/api/v1/write \
K6_PROMETHEUS_RW_TREND_STATS="p(95),p(99)" \
k6 run -o experimental-prometheus-rw \
  -e PROFILE=dev-baseline \
  -e BASE_MOBILE=http://localhost:18080 \
  -e BASE_PROVIDER=http://localhost:18082 \
  -e KEYCLOAK_BASE=http://localhost:18081 \
  -e LOADTEST_USER_PASSWORD="$LT_PW" \
  load-test/scripts/s5-mobile-read.js
```

## Cluster içi koşum (K8s Job — alıcı Service'e doğrudan)

Job'lar Prometheus alıcı Service'ine cluster içinden yazar (port-forward gerekmez) ve `TREND_STATS`'ı
manifest'te taşır. Önce script/lib ConfigMap'lerini repo'nun güncel klonundan oluştur/yenile:

```bash
cd /srv/deploy/inktavia-platform    # dev sunucudaki güncel klon
kubectl -n inktavia-dev create configmap k6-scripts \
  --from-file=load-test/scripts/s0-smoke.js \
  --from-file=load-test/scripts/s1-token-read.js \
  --from-file=load-test/scripts/s2-write-flow.js \
  --from-file=load-test/scripts/s3-otp-request.js \
  --from-file=load-test/scripts/s4-kc-token.js \
  --from-file=load-test/scripts/s5-mobile-read.js \
  --from-file=load-test/scripts/s6-mobile-vessel-write.js \
  --from-file=load-test/scripts/s7-mobile-sr-create.js \
  --from-file=load-test/scripts/s8-discovery-nearby.js \
  --from-file=load-test/scripts/s9-trip-ping.js \
  --dry-run=client -o yaml | kubectl apply -f -
kubectl -n inktavia-dev create configmap k6-lib \
  --from-file=load-test/scripts/lib/config.js \
  --from-file=load-test/scripts/lib/auth.js \
  --from-file=load-test/scripts/lib/geo.js \
  --dry-run=client -o yaml | kubectl apply -f -

# şifre secret'i (bir kez): read -s LT_PW; kubectl -n inktavia-dev create secret generic loadtest-credentials --from-literal=LOADTEST_USER_PASSWORD="$LT_PW"; unset LT_PW

kubectl apply -f load-test/k8s/job-s5-dev-baseline.yaml
kubectl -n inktavia-dev logs -f job/k6-s5-dev-baseline
kubectl -n inktavia-dev delete job k6-s5-dev-baseline    # tekrar koşmadan önce (Job değişmez)
```

`baseline` (20-VU) koşusu için aynı Job'da `PROFILE` env'ini `baseline` yap (limits fix sonrası).

## Baseline prosedürü (eşik kalibrasyonu)

1. `s5→s9`'u SIRAYLA koş (`dev-baseline`; s9 fixture ister — aşağı bak). Her koşuda özeti kaydet:
   ```bash
   k6 run --summary-export=load-test/results/$(date +%Y%m%d)-s5-dev-baseline.json \
     -e PROFILE=dev-baseline -e BASE_MOBILE=... load-test/scripts/s5-mobile-read.js
   ```
   (Cluster Job koşularında özet log'dadır; `kubectl logs job/... > results/<tarih>-sX.json` ile al ya da
   remote-write'tan Grafana'da oku.)
2. Özet çıktısındaki **saniye** p95/p99'ları **ms'ye çevirip** `BASELINE.md` tablosuna yaz (Prometheus'tan
   değil summary'den — panolar saniye, tablo ms).
3. Her senaryonun `options.thresholds` içindeki `TODO-CALIBRATE` p95'lerini tablodaki p95 × ~1.5 ile değiştir
   (regresyon kapısı), s1/s2 desenindeki gibi.

## Temizlik

Her senaryo ürettiği kaydı `LOADTEST-` önekiyle damgalar; s6 tekneyi arşivler, s7 SR'yi iptal eder (yani
aktif veriyi kirletmezler). Sert silme (isteğe bağlı, önce SAYAR):
```bash
docker exec -i postgres psql -U aizen -d inktavia_store -f - < load-test/cleanup.sql
```

## Kimlik doğrulanamayan uçlar (bilinen kısıt)

- **provider discovery (s8)** ve **trip start/ping/arrive (s9)**: PROVIDER-kapsamlı (aud `provider-portal-bff`
  + Identity'de Approved+Active provider profili). loadtest KATILIMCI kullanıcıları provider-onboarded DEĞİL,
  bu yüzden bu legler VARSAYILAN OLARAK ATLANIR. Onboarded provider + (s9 için) accepted-assignment fixture
  verilirse `PROVIDER_USER` / `TRIP_SR_ID` env'leriyle açılır (script başlıklarındaki seed adımları).
