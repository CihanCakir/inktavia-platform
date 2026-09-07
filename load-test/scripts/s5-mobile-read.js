// s5 — Mobil ana-ekran (Home) fan-out: owner uygulaması açılışta 4 ucu paralel çeker. Burada tek VU
// döngüsünde sıralı okur; ölçülen GERÇEK = mobil BFF'in kompozisyon maliyeti (BFF → vessel/SR/notification/
// cargodry modülleri). Kimlik token'dan çözülür; loadtest kullanıcısının hiç kaydı olmasa da uçlar 200 +
// boş liste dönmeli (BFF birleştirme maliyeti yine ölçülür).
//
// PROFILE=smoke (varsayılan): 1 VU × 1 dk, yalnız hata kapısı.
// PROFILE=dev-baseline: 5 VU sabit × 2 dk, yalnız hata kapısı — LT-5 kalibrasyon (dev 25 VU'da doyuyor,
//   limits-raise PR'ı beklerken 3-5 VU ile ölç). Çıkan p95'ler baseline eşiklerinin taban verisi.
// PROFILE=baseline: 1→20 VU rampa × 3 dk — LT hedef profili (limits fix SONRASI). p95 eşikleri
//   TODO-CALIBRATE: ilk dev-baseline koşusunun BASELINE.md değerlerinden doldurulacak.
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_MOBILE, jsonHeaders } from './lib/config.js';
import { getToken, authed, vuMobileUser } from './lib/auth.js';

const PROFILE = __ENV.PROFILE || 'smoke';

// DEV kapilari (dev-baseline): 2026-09-07 ilk temiz kosu p95 x 1,5 — 5 VU, 365 istek, %0 hata
// (vessels 2,11s / kits 1,88s / sr-list 1,81s; Prometheus flush-serisinden, yukari yanli olabilir).
// notifications kapisiz: SKIP_NOTIFICATIONS=1 ile atlaniyor (borc #109 participant fixture).
export const options = PROFILE === 'dev-baseline' ? {
  vus: 5, duration: '2m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:kc-token}':       ['p(95)<300'],
    'http_req_duration{name:vessels-list}':   ['p(95)<3200'],
    'http_req_duration{name:sr-list}':        ['p(95)<2800'],
    'http_req_duration{name:cargodry-kits}':  ['p(95)<2900'],
    'http_req_duration{name:notifications}':  ['p(95)<3300'], // 2026-09-07 fixture sonrasi kosu: p95 2,19s x 1,5
  },
} : PROFILE === 'baseline' ? {
  stages: [
    { duration: '3m', target: 20 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // TODO-CALIBRATE: 20 VU degerleri limits-fix sonrasi 20 VU kosusundan kalibre edilecek (BASELINE.md).
    'http_req_duration{name:vessels-list}':   ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:sr-list}':        ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:notifications}':  ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:cargodry-kits}':  ['p(95)<3000'], // TODO-CALIBRATE
  },
} : {
  vus: 1, duration: '1m',
  thresholds: { http_req_failed: ['rate==0'] },
};

const READS = [
  ['vessels-list',  '/api/v1/mobile/vessels'],
  ['sr-list',       '/api/v1/mobile/service-requests?pageIndex=0&pageSize=20'],
  ['notifications', '/api/v1/mobile/notifications?skip=0&take=20'],
  ['cargodry-kits', '/api/v1/mobile/cargodry/kits'],
].filter(([name]) =>
  // notifications, cagiran hesabin Identity participant kaydini ister; loadtest kullanicilari yalniz
  // Keycloak'ta var -> 400 "No participant profile is linked" (2026-09-07'de canlida dogrulandi, zarif
  // hata). Participant fixture kurulana dek SKIP_NOTIFICATIONS=1 ile atlanir (borc defteri: fixture).
  !(name === 'notifications' && __ENV.SKIP_NOTIFICATIONS === '1'));

export default function () {
  const user = vuMobileUser();
  if (!getToken(user)) { sleep(1); return; }
  group('mobile-home-read', () => {
    for (const [name, path] of READS) {
      const res = authed((t) => http.get(`${BASE_MOBILE}${path}`,
        { headers: jsonHeaders(t), tags: { name } }), user);
      check(res, { [`${name} 200`]: (r) => r && r.status === 200 });
    }
  });
  sleep(1);
}
