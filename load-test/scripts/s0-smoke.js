// s0 — boru temizliği: k6 koşuyor mu, ağdan Keycloak + BFF'lere erişiliyor mu, çıktılar akıyor mu.
// Henüz token/iş akışı YOK; onlar s1/s2/s3'te (LT-1).
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_ADMIN, BASE_MOBILE, KEYCLOAK_BASE, KC_REALM, smokeThresholds } from './lib/config.js';

export const options = { vus: 1, duration: '30s', thresholds: smokeThresholds };

export default function () {
  const disc = http.get(`${KEYCLOAK_BASE}/realms/${KC_REALM}/.well-known/openid-configuration`,
    { tags: { name: 'kc-discovery' } });
  check(disc, { 'keycloak discovery 200': (r) => r.status === 200 });

  // BFF köküne dokunuş: kök 401 döner (auth middleware her yolu korur — LT1-s0'da ölçüldü; bu İYİ sinyal).
  // responseCallback olmadan k6 404'ü http_req_failed'e sayar ve smoke eşiği yalancı kırmızı verir (LT1-s0 ilk koşusunda görüldü).
  const bff = http.get(`${BASE_ADMIN}/`, {
    tags: { name: 'bff-admin-root' },
    responseCallback: http.expectedStatuses(200, 401, 404),
  });
  check(bff, { 'bff-adminpanel < 500': (r) => r.status < 500 });

  // Mobil BFF kökü de smoke kapısında: kimliksiz istek 401 döner (auth middleware her yolu korur — İYİ sinyal).
  // s5–s9 mobil senaryolarının hedefine ağdan erişilebildiğini erkenden doğrular.
  const mobile = http.get(`${BASE_MOBILE}/`, {
    tags: { name: 'bff-mobile-root' },
    responseCallback: http.expectedStatuses(200, 401, 404),
  });
  check(mobile, { 'bff-marine-mobile < 500': (r) => r.status < 500 });
  sleep(1);
}
