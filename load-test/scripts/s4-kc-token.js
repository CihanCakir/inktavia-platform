// s4 — Keycloak token ucu mini yükü (LT-4c): password grant, 25 FARKLI kullanıcı → throttle yok.
// Soru: Keycloak'ın kendisi darboğaz mı? (s1'de token VU başına 1 kez alındığı için orada görünmez.)
import http from 'k6/http';
import { check, sleep } from 'k6';
import { TOKEN_URL } from './lib/config.js';

export const options = {
  stages: [
    { duration: '15s', target: 5 },
    { duration: '1m', target: 5 },
    { duration: '15s', target: 0 },
  ],
  thresholds: { http_req_failed: ['rate<0.01'] },
};

export default function () {
  const n = ((__VU * 7 + __ITER) % 25) + 1; // kullanıcıları dolaş
  const res = http.post(TOKEN_URL, {
    grant_type: 'password',
    client_id: __ENV.LOADTEST_CLIENT_ID || 'loadtest-runner',
    username: `loadtest-user-${String(n).padStart(2, '0')}@example.test`,
    password: __ENV.LOADTEST_USER_PASSWORD,
  }, { tags: { name: 'kc-token-load' } });
  check(res, { 'token 200': (r) => r.status === 200 });
  sleep(1);
}
