// s3 — OTP isteği: BFF → identity-api → RabbitMQ → notification-api zincirinin tamamını yükler.
// UYARI (LT-4 ön şartı): koşudan ÖNCE lokal Notification'ın SMS/e-posta STUB'da olduğu doğrulanmalı —
// vendor'a gerçek istek çıkıyorsa bu senaryo KOŞULMAZ.
// OTP_IDENTIFIER zorunlu: Identity DB'de GERÇEKTEN var olan bir admin e-postası (loadtest
// kullanıcıları Identity'de yok — Keycloak'ta varlar; OTP zinciri Identity tablosuna bakar).
// ÖLÇÜLEN GERÇEK (kod: AdminOtpLoginDomainService): cooldown'a takılan istek SENTETİK 200 döner
// (enumeration-safe) — 4xx yok, sadece OTP üretilmez. Dolayısıyla PROFILE=baseline bu uçta
// THROTTLE YOLUNU ölçer (brute-force'un çarpacağı sıcak yol): tüm istekler 200 beklenir,
// gerçek OTP üretimi ~1/dk kalmalı (koşu sonrası DB sayımıyla doğrulanır — LOAD-TEST-RAPORU LT-4).
// Zincirin yük altında uçtan uca çalıştığı kanıtı ayrı: e2e-otp-login.sh, s1 baseline koşarken.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_ADMIN } from './lib/config.js';

const IDENTIFIER = __ENV.OTP_IDENTIFIER;
const PROFILE = __ENV.PROFILE || 'smoke';
export const options = PROFILE === 'baseline' ? {
  stages: [
    { duration: '30s', target: 2 },
    { duration: '2m', target: 10 },
    { duration: '30s', target: 0 },
  ],
  thresholds: { http_req_failed: ['rate<0.01'] },
} : {
  vus: 1, iterations: 1,
  thresholds: { http_req_failed: ['rate==0'] },
};

export default function () {
  if (!IDENTIFIER) throw new Error('OTP_IDENTIFIER ver: Identity DB\'de var olan admin e-postasi');
  const res = http.post(`${BASE_ADMIN}/api/v1/admin-panel/auth/otp-login/request`,
    JSON.stringify({ channel: 'email', identifier: IDENTIFIER }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'otp-request' } });
  check(res, { 'otp-request 200': (r) => r.status === 200 });
  sleep(2); // OTP üretimi maliyetli; iterasyonlar arasına nefes.
}
