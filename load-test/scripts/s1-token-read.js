// s1 — token'lı okuma: admin BFF'in gerçek liste/dashboard uçları.
// PROFILE=smoke (varsayılan): 1 VU × 1 dk, yalnız hata kapısı.
// PROFILE=baseline: 5→25 VU rampa + 5 dk sabit — LT-2. Baseline p95'leri rapora yazıldıktan
// sonra buraya senaryo-bazlı p95 eşikleri eklenecek (regresyon kapısı).
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_ADMIN, jsonHeaders } from './lib/config.js';
import { getToken, vuUser } from './lib/auth.js';

const PROFILE = __ENV.PROFILE || 'smoke';
// dev-baseline: LT-5 kalibrasyon profili — dev 25 VU'da doyuyor (p95 17-32 sn, 2026-09-07),
// esikler LOKAL degerlerden; dev esigi ancak doygunluk ALTI olcumle kalibre edilir. O yuzden
// bu profil 5 VU sabit + yalniz hata kapisi kosar; cikan p95'ler dev esiklerinin taban verisi olur.
export const options = PROFILE === 'dev-baseline' ? {
  vus: 5, duration: '5m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // DEV kapilari = 2026-09-07 ilk dev-baseline kosusu p95 x 1,5 (5 VU, limit gevsetmesi sonrasi;
    // 1346 istek, %0 hata). NOT: p95'ler Prometheus flush-araligi serisinden alindi — seyrek orneklemde
    // yukari yanlidir; esikli ILK kosunun ozeti kesin degerleri basinca gerekirse SIKILASTIR.
    // overview yine acik ara en yavas (dev'de 6,2s) — #107 cache fix'i sonrasi bu kapi da inecek.
    'http_req_duration{name:kc-token}':           ['p(95)<300'],
    'http_req_duration{name:cargodry-analytics}': ['p(95)<3300'],
    'http_req_duration{name:cargodry-kits}':      ['p(95)<2600'],
    'http_req_duration{name:cargodry-stats}':     ['p(95)<2300'],
    'http_req_duration{name:dashboard-charts}':   ['p(95)<6600'],
    'http_req_duration{name:dashboard-overview}': ['p(95)<9500'],
    'http_req_duration{name:providers-list}':     ['p(95)<2200'],
    'http_req_duration{name:users-kpi}':          ['p(95)<4400'],
    'http_req_duration{name:users-list}':         ['p(95)<3200'],
    'http_req_duration{name:vessels-list}':       ['p(95)<2500'],
  },
} : PROFILE === 'baseline' ? {
  stages: [
    { duration: '1m', target: 5 },
    { duration: '1m', target: 25 },
    { duration: '5m', target: 25 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // Regresyon kapıları = LT-2 baseline p95 × 1,5 (2026-09-03, 25 VU; LOAD-TEST-RAPORU.md).
    'http_req_duration{name:cargodry-analytics}': ['p(95)<475'],
    'http_req_duration{name:cargodry-kits}':      ['p(95)<440'],
    'http_req_duration{name:cargodry-stats}':     ['p(95)<465'],
    'http_req_duration{name:dashboard-charts}':   ['p(95)<970'],
    // overview: yük altında varyans devasa (koşular arası p95 1,24s ↔ 5,27s; p99 6,7s+) — bilinen
    // sorun, cache/birleşik özet ucu fix'i bekliyor (LOAD-TEST-RAPORU bulgu #2). Kapı geçici geniş;
    // fix sonrası 1860'a sıkılacak.
    'http_req_duration{name:dashboard-overview}': ['p(95)<3000'],
    'http_req_duration{name:providers-list}':     ['p(95)<650'],
    'http_req_duration{name:users-kpi}':          ['p(95)<1000'],
    'http_req_duration{name:users-list}':         ['p(95)<860'],
    'http_req_duration{name:vessels-list}':       ['p(95)<685'],
  },
} : {
  vus: 1, duration: '1m',
  thresholds: { http_req_failed: ['rate==0'] },
};

const ENDPOINTS = [
  ['dashboard-overview', '/api/v1/admin-panel/dashboard/overview'],
  ['dashboard-charts',   '/api/v1/admin-panel/dashboard/charts'],
  ['users-kpi',          '/api/v1/admin-panel/admin/users/kpi'],
  ['users-list',         '/api/v1/admin-panel/admin/users'],
  ['providers-list',     '/api/v1/admin-panel/providers'],
  ['vessels-list',       '/api/v1/admin-panel/vessels'],
  ['cargodry-stats',     '/api/v1/admin-panel/cargodry/stats'],
  ['cargodry-analytics', '/api/v1/admin-panel/cargodry/analytics'],
  ['cargodry-kits',      '/api/v1/admin-panel/cargodry/kits'],
];

export default function () {
  const token = getToken(vuUser());
  if (!token) { sleep(1); return; }
  group('admin-read', () => {
    for (const [name, path] of ENDPOINTS) {
      const res = http.get(`${BASE_ADMIN}${path}`, { headers: jsonHeaders(token), tags: { name } });
      check(res, { [`${name} 200`]: (r) => r.status === 200 });
    }
  });
  sleep(1);
}
