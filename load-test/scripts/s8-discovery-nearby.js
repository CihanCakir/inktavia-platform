// s8 — Okuma-ağırlıklı keşif: mobil /marinas/nearby (rastgele kıyı koordinatı) + provider discovery LİSTE
// (yeni düzeltilen uç; radiusKm'siz merkez şekli DAHİL — 500 regresyon kapısı). CACHE ADAYI taban ölçümü:
// bu iki uç tekrar-okumada Redis cache'e alınacak; buradaki p95 cache ÖNCESİ tabandır.
//
// PROFILE=smoke (varsayılan): 1 VU × 1 dk.
// PROFILE=dev-baseline: 5 VU sabit × 2 dk — LT-5 kalibrasyon (dev doygunluk altı).
// PROFILE=baseline: 20 VU sabit × 3 dk — okuma-ağırlıklı hedef profil (limits fix SONRASI). p95 TODO-CALIBRATE.
//
// PROVIDER DISCOVERY KİMLİK: discovery PROVIDER-kapsamlı (aud=provider-portal-bff + Identity'de Approved+Active
// provider profili — çalışma anında uzak çağrı). loadtest KATILIMCI kullanıcıları provider-onboarded DEĞİL, bu
// yüzden leg VARSAYILAN OLARAK ATLANIR. Onboarded bir provider loadtest kullanıcısı varsa PROVIDER_USER env ile
// aç: leg o kimlikle radiusKm'siz discovery'yi yükler. (Raporda "kimlik doğrulanamayan uç" olarak listelenir.)
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_MOBILE, BASE_PROVIDER, jsonHeaders } from './lib/config.js';
import { getToken, authed, vuUser } from './lib/auth.js';
import { randomCoastal } from './lib/geo.js';

const PROFILE = __ENV.PROFILE || 'smoke';
const PROVIDER_USER = __ENV.PROVIDER_USER; // onboarded provider loadtest kullanıcısı; yoksa discovery leg atlanır.

// DEV kapisi (dev-baseline): 2026-09-07 ilk kosu p95 x 1,5 — 5 VU, 281 istek, %0 hata,
// marinas-nearby p95 2,50 sn (rastgele kiyi koordinatlariyla; discovery leg PROVIDER_USER yok diye atlandi).
export const options = PROFILE === 'dev-baseline' ? {
  vus: 5, duration: '2m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:kc-token}':       ['p(95)<300'],
    'http_req_duration{name:marinas-nearby}': ['p(95)<3800'],
  },
} : PROFILE === 'baseline' ? {
  vus: 20, duration: '3m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // LT-7 kalibrasyonu (2026-09-09): 20 VU, 1779 istek, %0 hata; marinas-nearby p95 2,28s x 1,5.
    'http_req_duration{name:marinas-nearby}': ['p(95)<3400'],
    // discovery-list kalibrasyonu (2026-09-09, s9 fixture'i sonrasi ilk tam kosu): 20 VU,
    // 2796 istek, %0 hata; p95 2,88s x 1,5. Not: 20 VU tek provider kimligini paylasir (fixture).
    'http_req_duration{name:discovery-list}': ['p(95)<4300'],
  },
} : {
  vus: 1, duration: '1m',
  thresholds: { http_req_failed: ['rate==0'] },
};

let warnedNoProvider = false;

export default function () {
  const user = vuUser();
  if (!getToken(user)) { sleep(1); return; }
  const c = randomCoastal();

  group('discovery-nearby', () => {
    // 1) Mobil nearby marinas (katılımcı token) — lat/lng ZORUNLU.
    const near = authed((t) => http.get(
      `${BASE_MOBILE}/api/v1/mobile/marinas/nearby?lat=${c.lat}&lng=${c.lng}&limit=10`,
      { headers: jsonHeaders(t), tags: { name: 'marinas-nearby' } }), user);
    check(near, { 'marinas-nearby 200': (r) => r && r.status === 200 });

    // 2) Provider discovery — radiusKm'siz merkez şekli (düzeltilen uç). Yalnız PROVIDER_USER verildiyse.
    if (PROVIDER_USER) {
      const q = `pageSize=20&centerLatitude=${c.lat}&centerLongitude=${c.lng}&sort=PublishedAtDesc`;
      const disc = authed((t) => http.get(
        `${BASE_PROVIDER}/api/v1/provider/service-requests/discovery?${q}`,
        { headers: jsonHeaders(t), tags: { name: 'discovery-list' } }), PROVIDER_USER);
      // Radiusless artık 200 olmalı (eski hata: 400→BFF 500). 401/403 = provider profili Approved+Active değil.
      check(disc, { 'discovery-list 200': (r) => r && r.status === 200 });
    } else if (!warnedNoProvider) {
      warnedNoProvider = true;
      console.warn('s8: PROVIDER_USER verilmedi → provider discovery leg ATLANDI (katılımcı token discovery yetkisi yok).');
    }
  });
  sleep(1);
}
