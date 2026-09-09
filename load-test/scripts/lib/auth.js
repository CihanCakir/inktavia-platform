// LT-0 token yardımcısı — loadtest-runner public client, direct access grant (password).
// Token VU başına BİR kez alınır ve süresi dolana dek yeniden kullanılır: her istekte token
// almak Keycloak'ı yapay yere döver ve BFF ölçümünü kirletir (Keycloak'ın kendi yükü LT-4'te ayrı ölçülür).
import http from 'k6/http';
import { check } from 'k6';
import { TOKEN_URL } from './config.js';

const CLIENT_ID = __ENV.LOADTEST_CLIENT_ID || 'loadtest-runner';
const PASSWORD = __ENV.LOADTEST_USER_PASSWORD; // .env'den compose aktarır; commit edilmez.
// s9: PROVIDER_USER gercek (loadtest havuzu disi) bir hesap — sifresi ayri env'den gelir.
// PROVIDER_PASSWORD verilmezse eski davranis aynen korunur (tek sifre).
const PROVIDER_PASSWORD = __ENV.PROVIDER_PASSWORD || PASSWORD;

// VU-yerel cache (VU başına ayrı JS sandbox'ı). Kullanıcı-ADINA anahtarlı: bir senaryo aynı VU'da
// birden çok kimlikle konuşabilir (ör. s8/s9 katılımcı + provider) ve tek slot birbirini ezmesin.
const cache = {}; // { [username]: { token, expiresAt } }

// force=true → önbelleği atla ve tazele (401 sonrası yeniden alım için).
export function getToken(username, force) {
  const now = Date.now();
  const hit = cache[username];
  if (!force && hit && hit.expiresAt - 15000 > now) return hit.token;
  const res = http.post(TOKEN_URL, {
    grant_type: 'password',
    client_id: CLIENT_ID,
    username: username,
    password: (__ENV.PROVIDER_USER && username === __ENV.PROVIDER_USER) ? PROVIDER_PASSWORD : PASSWORD,
  }, { tags: { name: 'kc-token' } });
  check(res, { 'token 200': (r) => r.status === 200 });
  if (res.status !== 200) { delete cache[username]; return null; }
  const body = res.json();
  cache[username] = { token: body.access_token, expiresAt: now + body.expires_in * 1000 };
  return cache[username].token;
}

// Token'lı istek + 401'de BİR kez tazeleyip tekrar dene. reqFn: (token) => http.<verb>(...).
// Token süresi koşu ortasında dolarsa (uzun ramp/baseline) tek 401'i şeffaf yutar; ikinci 401 gerçek hatadır.
export function authed(reqFn, username) {
  let token = getToken(username);
  if (!token) return null;
  let res = reqFn(token);
  if (res && res.status === 401) {
    token = getToken(username, true);
    if (token) res = reqFn(token);
  }
  return res;
}

// VU numarasından deterministik test kullanıcısı: loadtest-user-01 .. loadtest-user-25
export function vuUser() {
  const n = ((__VU - 1) % 25) + 1;
  return `loadtest-user-${String(n).padStart(2, '0')}@example.test`;
}

// Mobil senaryolar (s5-s7) icin AYRI havuz: loadtest-mobile-01..25 — bunlar gercek register
// ucundan yaratildigi icin Identity participant kaydi + participant_profile_id claim'i TASIR
// (borc #109'un cozumu; setup-mobile-loadtest.sh kurar). loadtest-user-XX'te bu kayit YOK.
export function vuMobileUser() {
  const n = ((__VU - 1) % 25) + 1;
  return `loadtest-mobile-${String(n).padStart(2, '0')}@example.test`;
}
