// s7 — Mobil owner servis talebi: OLUŞTUR (marinadan şehir otomatik doldurma, foto YOK) → DETAY oku → İPTAL et.
// Sunucu tarafında publish + şehir fan-out'unu (SR → provider bölge bildirimleri) yükler.
// Şehir otomatik doldurma: SR create gövdesi marinaId taşımaz; şehri, TEKNENİN seçili konumundan (marina)
// sunucu türetir (SrCityDerivation). Bu yüzden her VU önce marinası seçili bir tekne hazırlar (bir kez),
// sonra ölçülen sıcak yol = SR create (şehir gövdede boş → türetilir) → detay → iptal.
// Kayıtlar LOADTEST- önekli başlıkla → cleanup.sql siler.
//
// PROFILE=smoke (varsayılan): 1 VU × 3 iterasyon.
// PROFILE=dev-baseline: 5 VU sabit × 2 dk, yalnız hata kapısı — LT-5 kalibrasyon.
// PROFILE=baseline: 5 VU sabit × 2 dk + p95 eşikleri (TODO-CALIBRATE). (Görev profili: 5 VU sabit 2 dk.)
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_MOBILE, LOADTEST_PREFIX, jsonHeaders } from './lib/config.js';
import { getToken, authed, vuMobileUser } from './lib/auth.js';
import { randomCoastal } from './lib/geo.js';

const PROFILE = __ENV.PROFILE || 'smoke';

// DEV kapilari (dev-baseline): 2026-09-07 ilk temiz kosu p95 x 1,5 — 5 VU, 158 istek, %0 hata
// (sr-create 8,44s! / sr-cancel 5,46s / sr-detail 2,30s; Prometheus flush-serisi). sr-create'in
// 8+ sn'si dikkat cekici (publish + sehir fan-out) — optimizasyon adayi, kapi simdilik gercegi soyluyor.
export const options = PROFILE === 'dev-baseline' ? {
  vus: 5, duration: '2m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:sr-create}': ['p(95)<12700'],
    'http_req_duration{name:sr-detail}': ['p(95)<3500'],
    'http_req_duration{name:sr-cancel}': ['p(95)<8200'],
  },
} : PROFILE === 'baseline' ? {
  vus: 5, duration: '2m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:sr-create}': ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:sr-detail}': ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:sr-cancel}': ['p(95)<3000'], // TODO-CALIBRATE
  },
} : {
  vus: 1, iterations: 3,
  thresholds: { http_req_failed: ['rate==0'] },
};

let vuVesselId = null; // VU-yerel: marinası seçili tekne bir kez hazırlanır, iterasyonlar tekrar kullanır.

function ensureVessel(user) {
  if (vuVesselId) return vuVesselId;
  const name = `${LOADTEST_PREFIX}Tekne s7 ${__VU}-${Math.floor(Math.random() * 1e6)}`;
  const created = authed((t) => http.post(`${BASE_MOBILE}/api/v1/mobile/vessels`,
    JSON.stringify({ core: { name, vesselTypeCode: __ENV.VESSEL_TYPE_CODE || 'MOTOR_YACHT' } }),
    { headers: jsonHeaders(t), tags: { name: 'sr-setup-vessel-create' } }), user);
  if (!created || created.status !== 200) return null;
  const id = created.json('body.id') || created.json('body.vesselId');
  if (!id) return null;

  // Marinayı seç (şehir türetiminin kaynağı). Marina yoksa yine de tekne kullanılabilir (şehir null kalır).
  const c = randomCoastal();
  const near = authed((t) => http.get(
    `${BASE_MOBILE}/api/v1/mobile/marinas/nearby?lat=${c.lat}&lng=${c.lng}&limit=5`,
    { headers: jsonHeaders(t), tags: { name: 'sr-setup-nearby' } }), user);
  if (near && near.status === 200) {
    const items = near.json('body') || [];
    const marinaId = Array.isArray(items) && items.length ? (items[0].id || items[0].marinaId) : null;
    if (marinaId) {
      authed((t) => http.put(`${BASE_MOBILE}/api/v1/mobile/vessels/${id}/location/selected`,
        JSON.stringify({ marinaId }),
        { headers: jsonHeaders(t), tags: { name: 'sr-setup-set-location' } }), user);
    }
  }
  vuVesselId = id;
  return id;
}

export default function () {
  const user = vuMobileUser();
  if (!getToken(user)) { sleep(1); return; }
  const vesselId = ensureVessel(user);
  if (!vesselId) { sleep(1); return; }

  group('mobile-sr-create', () => {
    // OLUŞTUR — location* alanları BİLEREK boş: şehir teknenin marina konumundan türetilsin (auto-fill yolu).
    const body = JSON.stringify({
      vesselId,
      serviceCategoryCode: __ENV.SR_CATEGORY_CODE || 'MOTOR_MAINTENANCE',
      title: `${LOADTEST_PREFIX}SR ${__VU}-${__ITER}-${Math.floor(Math.random() * 1e6)}`,
      description: 'k6 s7 yük testi talebi — cleanup.sql siler',
      priority: 'Normal',
      publish: true,
    });
    const created = authed((t) => http.post(`${BASE_MOBILE}/api/v1/mobile/service-requests`, body,
      { headers: jsonHeaders(t), tags: { name: 'sr-create' } }), user);
    const ok = check(created, { 'sr-create 200': (r) => r && r.status === 200 });
    if (!ok) { sleep(1); return; }

    const srId = created.json('body.id') || created.json('body.serviceRequestId');
    if (!srId) { sleep(1); return; }

    // DETAY
    const detail = authed((t) => http.get(`${BASE_MOBILE}/api/v1/mobile/service-requests/${srId}`,
      { headers: jsonHeaders(t), tags: { name: 'sr-detail' } }), user);
    check(detail, { 'sr-detail 200': (r) => r && r.status === 200 });

    // İPTAL (temizlik + iptal yolu ölçümü)
    const cancel = authed((t) => http.post(`${BASE_MOBILE}/api/v1/mobile/service-requests/${srId}/cancel`,
      JSON.stringify({ reasonCode: 'Duplicate', note: 'k6 s7 cleanup' }),
      { headers: jsonHeaders(t), tags: { name: 'sr-cancel' } }), user);
    check(cancel, { 'sr-cancel 200': (r) => r && r.status === 200 });
  });
  sleep(1);
}
