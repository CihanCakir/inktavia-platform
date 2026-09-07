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
import { getToken, authed, vuUser } from './lib/auth.js';

const PROFILE = __ENV.PROFILE || 'smoke';

export const options = PROFILE === 'dev-baseline' ? {
  vus: 5, duration: '2m',
  thresholds: { http_req_failed: ['rate<0.01'] },
} : PROFILE === 'baseline' ? {
  stages: [
    { duration: '3m', target: 20 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // TODO-CALIBRATE: değerler placeholder; ilk baseline koşusundan p95 × ~1.5 ile sıkılacak (BASELINE.md).
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
];

export default function () {
  const user = vuUser();
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
