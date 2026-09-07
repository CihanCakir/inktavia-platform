// s6 — Mobil owner yazma yolu: tekne OLUŞTUR → seçili konumu (marina) GÜNCELLE → ARŞİVLE.
// Ölçülen: yazma zinciri + marina çözümü (BFF selected-location, marinaId'den ad/koordinat çözer).
// Her tekne LOADTEST- önekli adla açılır → cleanup.sql siler (arşiv = soft-delete; sert silme SQL'de).
//
// PROFILE=smoke (varsayılan): 1 VU × 5 iterasyon.
// PROFILE=dev-baseline: 2 VU sabit × 2 dk — LT-5 kalibrasyon (düşük hız yazma).
// PROFILE=baseline: 5 VU sabit × 3 dk — düşük hız (yazma yolu; 20 VU profili okuma senaryolarına özgü).
// p95 eşikleri TODO-CALIBRATE.
//
// VERİ ÖN KOŞULU: loadtest KC kullanıcısı için owner id token'dan çözülür; tekne OwnerUserId ile damgalanır,
// önceden Identity katılımcı kaydı GEREKTİRMEZ. Marina adımı için ref.marinas seed'i (osm-marina-seed) yüklü olmalı.
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_MOBILE, LOADTEST_PREFIX, jsonHeaders } from './lib/config.js';
import { getToken, authed, vuUser } from './lib/auth.js';
import { randomCoastal } from './lib/geo.js';

const PROFILE = __ENV.PROFILE || 'smoke';

export const options = PROFILE === 'dev-baseline' ? {
  vus: 2, duration: '2m',
  thresholds: { http_req_failed: ['rate<0.01'] },
} : PROFILE === 'baseline' ? {
  vus: 5, duration: '3m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:vessel-create}':       ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:vessel-set-location}': ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:vessel-archive}':      ['p(95)<3000'], // TODO-CALIBRATE
  },
} : {
  vus: 1, iterations: 5,
  thresholds: { http_req_failed: ['rate==0'] },
};

// nearby'den ilk marina id'sini çek (selected-location adımı için). Bulunamazsa null → adım atlanır.
function pickMarinaId(user) {
  const c = randomCoastal();
  const res = authed((t) => http.get(
    `${BASE_MOBILE}/api/v1/mobile/marinas/nearby?lat=${c.lat}&lng=${c.lng}&limit=5`,
    { headers: jsonHeaders(t), tags: { name: 'marinas-nearby' } }), user);
  if (!res || res.status !== 200) return null;
  const items = (res.json('body') || []);
  if (!Array.isArray(items) || items.length === 0) return null;
  const m = items[0];
  return m.id || m.marinaId || null;
}

export default function () {
  const user = vuUser();
  if (!getToken(user)) { sleep(1); return; }

  group('mobile-vessel-write', () => {
    // 1) OLUŞTUR — yalnız zorunlu core alanları (motor opsiyonel, sade tutuldu).
    const name = `${LOADTEST_PREFIX}Tekne ${__VU}-${__ITER}-${Math.floor(Math.random() * 1e6)}`;
    const createBody = JSON.stringify({
      core: { name, vesselTypeCode: __ENV.VESSEL_TYPE_CODE || 'MOTOR_YACHT' },
    });
    const created = authed((t) => http.post(`${BASE_MOBILE}/api/v1/mobile/vessels`, createBody,
      { headers: jsonHeaders(t), tags: { name: 'vessel-create' } }), user);
    const ok = check(created, { 'vessel-create 200': (r) => r && r.status === 200 });
    if (!ok) { sleep(1); return; }

    const vesselId = created.json('body.id') || created.json('body.vesselId');
    if (!vesselId) { sleep(1); return; }

    // 2) SEÇİLİ KONUM (marina) — marina bulunduysa güncelle; yoksa adımı atla (ölçüm yine create+archive).
    const marinaId = pickMarinaId(user);
    if (marinaId) {
      const setLoc = authed((t) => http.put(
        `${BASE_MOBILE}/api/v1/mobile/vessels/${vesselId}/location/selected`,
        JSON.stringify({ marinaId }),
        { headers: jsonHeaders(t), tags: { name: 'vessel-set-location' } }), user);
      check(setLoc, { 'vessel-set-location 200': (r) => r && r.status === 200 });
    }

    // 3) ARŞİVLE — soft-delete; LOADTEST- kayıtlarının sert silinmesi cleanup.sql'de.
    const archived = authed((t) => http.post(
      `${BASE_MOBILE}/api/v1/mobile/vessels/${vesselId}/archive`,
      JSON.stringify({ reason: 'Other', notes: 'k6 s6 cleanup' }),
      { headers: jsonHeaders(t), tags: { name: 'vessel-archive' } }), user);
    check(archived, { 'vessel-archive 200': (r) => r && r.status === 200 });
  });
  sleep(1);
}
