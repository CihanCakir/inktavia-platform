// s2 — yazma akışı: admin BFF üzerinden CargoDry ürün oluşturma (BFF → cargodry-api → Postgres).
// Provider tarafı yazma (ServiceRequest) BİLİNÇLİ kapsam dışı: loadtest kullanıcıları Identity'de
// provider-onboarded değil; o zincir QA-2'nin E2E konsinye senaryosunda GERÇEK veriyle koşulacak.
// Her kayıt LOADTEST- önekli → koşu sonrası load-test/cleanup.sql ile silinir.
// PROFILE=smoke: 1 VU × 5 iterasyon. PROFILE=baseline (LT-3): sabit 3 rps × 3 dk.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_ADMIN, LOADTEST_PREFIX, jsonHeaders } from './lib/config.js';
import { getToken, vuUser } from './lib/auth.js';

const PROFILE = __ENV.PROFILE || 'smoke';
export const options = PROFILE === 'baseline' ? {
  scenarios: {
    writes: { executor: 'constant-arrival-rate', rate: 3, timeUnit: '1s', duration: '3m',
              preAllocatedVUs: 10, maxVUs: 20 },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // Regresyon kapısı = LT-3 baseline p95 (55 ms) × 1,5 (2026-09-03, 3 rps).
    'http_req_duration{name:cargodry-product-create}': ['p(95)<85'],
  },
} : {
  vus: 1, iterations: 5,
  thresholds: { http_req_failed: ['rate==0'] },
};

export default function () {
  const token = getToken(vuUser());
  if (!token) { sleep(1); return; }
  // ProductCode benzersiz olmalı: VU + iterasyon + rastgele ek.
  const code = `${LOADTEST_PREFIX}P${__VU}-${__ITER}-${Math.floor(Math.random() * 1e6)}`;
  const payload = JSON.stringify({
    productCode: code,
    name: `${LOADTEST_PREFIX}Yük Testi Ürünü`,
    description: 'k6 yük testi kaydı — cleanup.sql siler',
    validityDays: 365,
    retailPrice: 100.0,
    currencyCode: 'TRY',
    hasSmartDevice: false,
    deviceType: null,
  });
  const res = http.post(`${BASE_ADMIN}/api/v1/admin-panel/cargodry/products`, payload,
    { headers: jsonHeaders(token), tags: { name: 'cargodry-product-create' } });
  check(res, { 'create 200': (r) => r.status === 200 });
  sleep(1);
}
