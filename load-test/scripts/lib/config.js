// LT-0 ortak yapılandırma — tüm senaryolar buradan okur.
// Adresler compose ağı İÇİNDEN çözülür (k6 aynı ağda koşar); dev cluster koşusunda (LT-5)
// aynı env isimleri K8s Job manifest'inden ClusterIP adresleriyle verilir.
export const BASE_ADMIN = __ENV.BASE_ADMIN || 'http://bff-adminpanel:8080';
export const BASE_PROVIDER = __ENV.BASE_PROVIDER || 'http://bff-marineprovider:8080';
export const KEYCLOAK_BASE = __ENV.KEYCLOAK_BASE || 'http://keycloak:8080';
export const KC_REALM = __ENV.KC_REALM || 'inktavia-realm';
export const TOKEN_URL = `${KEYCLOAK_BASE}/realms/${KC_REALM}/protocol/openid-connect/token`;

// Yük testinin ürettiği HER kayıt bu önekle başlar — temizlik cleanup.sql bu öneke bakar.
export const LOADTEST_PREFIX = 'LOADTEST-';

// Ortak eşikler: smoke'ta yalnız hata kapısı; baseline sonrası senaryolar kendi p95 eşiğini ekler.
export const smokeThresholds = { http_req_failed: ['rate==0'] };

export function jsonHeaders(token) {
  const h = { 'Content-Type': 'application/json' };
  if (token) h['Authorization'] = `Bearer ${token}`;
  return h;
}
