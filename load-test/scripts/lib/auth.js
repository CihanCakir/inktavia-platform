// LT-0 token yardımcısı — loadtest-runner public client, direct access grant (password).
// Token VU başına BİR kez alınır ve süresi dolana dek yeniden kullanılır: her istekte token
// almak Keycloak'ı yapay yere döver ve BFF ölçümünü kirletir (Keycloak'ın kendi yükü LT-4'te ayrı ölçülür).
import http from 'k6/http';
import { check } from 'k6';
import { TOKEN_URL } from './config.js';

const CLIENT_ID = __ENV.LOADTEST_CLIENT_ID || 'loadtest-runner';
const PASSWORD = __ENV.LOADTEST_USER_PASSWORD; // .env'den compose aktarır; commit edilmez.

let cached = null; // { token, expiresAt } — VU başına ayrı JS sandbox'ı olduğundan VU-yerel cache.

export function getToken(username) {
  const now = Date.now();
  if (cached && cached.expiresAt - 15000 > now) return cached.token;
  const res = http.post(TOKEN_URL, {
    grant_type: 'password',
    client_id: CLIENT_ID,
    username: username,
    password: PASSWORD,
  }, { tags: { name: 'kc-token' } });
  check(res, { 'token 200': (r) => r.status === 200 });
  if (res.status !== 200) return null;
  const body = res.json();
  cached = { token: body.access_token, expiresAt: now + body.expires_in * 1000 };
  return cached.token;
}

// VU numarasından deterministik test kullanıcısı: loadtest-user-01 .. loadtest-user-25
export function vuUser() {
  const n = ((__VU - 1) % 25) + 1;
  return `loadtest-user-${String(n).padStart(2, '0')}@example.test`;
}
