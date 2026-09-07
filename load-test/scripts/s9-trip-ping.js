// s9 — Sefer (trip) sıcak yolu + 3sn ping throttle probu: START → 10× konum PING (1sn arayla, yani 3sn
// sunucu throttle'ı da tetiklenir) → ARRIVE. Amaç: hızlı ping'lerin HATASIZ yutulduğunu (throttle absorbe
// eder, 4xx/5xx üretmez) doğrulamak + sefer uçlarının gecikmesini ölçmek.
//
// KAPSAM / KİMLİK: trip start/location/arrive uçları PROVIDER BFF'te ve PROVIDER-kapsamlı
// (aud=provider-portal-bff + Identity'de Approved+Active provider). Ayrıca ACCEPTED-ASSIGNMENT fixture'ı şart:
// bu senaryo bir SR'yi kendisi kuramaz (owner+provider iki ayrı kimlik + ekonomi/escrow zinciri). Bu yüzden
// fixture env ile verilir; verilmezse senaryo TEMİZ ATLANIR (aşağıdaki seed adımları elle bir kez yapılır).
//
// GEREKLİ ENV:
//   PROVIDER_USER   — onboarded (Approved+Active) provider loadtest kullanıcısı (token aud=provider-portal-bff).
//   TRIP_SR_ID      — o provider'a ATANMIŞ (accepted offer) bir service request id.
//
// FIXTURE SEED (elle, bir kez — script API'den kuramaz):
//   1) Provider teklif verir:  POST {BASE_PROVIDER}/api/v1/provider/service-requests/{srId}/offers  (+ submit)
//   2) Owner teklifi kabul eder: POST {BASE_MOBILE}/api/v1/mobile/service-requests/{srId}/offers/{offerId}/accept
//      (ekonomi/escrow çalışır, provider ATANIR, SR in-progress/assigned olur)
//   3) srId'yi TRIP_SR_ID, provider kullanıcısını PROVIDER_USER olarak ver.
// Ayrıntı ve uç satır no.: bkz. bu PR raporu + Modules/ServiceRequest TripsController.
import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_PROVIDER, jsonHeaders } from './lib/config.js';
import { getToken, authed } from './lib/auth.js';
import { randomCoastal } from './lib/geo.js';

const PROVIDER_USER = __ENV.PROVIDER_USER;
const TRIP_SR_ID = __ENV.TRIP_SR_ID;

// Tek akış / tek SR → paralel VU anlamsız; fonksiyonel throttle probu olarak 1 VU × 1 iterasyon.
export const options = {
  vus: 1, iterations: 1,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:trip-start}':  ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:trip-ping}':   ['p(95)<3000'], // TODO-CALIBRATE
    'http_req_duration{name:trip-arrive}': ['p(95)<3000'], // TODO-CALIBRATE
  },
};

export default function () {
  if (!PROVIDER_USER || !TRIP_SR_ID) {
    console.warn('s9 ATLANDI: PROVIDER_USER ve TRIP_SR_ID gerekli (accepted-assignment fixture — başlıktaki seed adımları).');
    return;
  }
  if (!getToken(PROVIDER_USER)) { console.warn('s9 ATLANDI: provider token alınamadı.'); return; }

  const base = `${BASE_PROVIDER}/api/v1/provider/trips/${TRIP_SR_ID}`;

  group('trip-flow', () => {
    // START — tekrar koşuda "zaten aktif" (4xx) olabilir; probu bloklamasın diye <500 kabul, sonra ping'e devam.
    const start = authed((t) => http.post(`${base}/start`, null,
      { headers: jsonHeaders(t), tags: { name: 'trip-start' } }), PROVIDER_USER);
    check(start, { 'trip-start <500': (r) => r && r.status < 500 });

    // 10× PING, 1sn arayla → 3sn throttle penceresine düşenler SUNUCUDA YUTULUR (hatasız). Hepsi <400 beklenir.
    let absorbedOk = 0;
    for (let i = 0; i < 10; i++) {
      const c = randomCoastal();
      const ping = authed((t) => http.post(`${base}/location`,
        JSON.stringify({ latitude: c.lat, longitude: c.lng, heading: (i * 36) % 360 }),
        { headers: jsonHeaders(t), tags: { name: 'trip-ping' } }), PROVIDER_USER);
      // "absorbe edildi, hata değil" = istemci/sunucu hatası YOK (2xx/3xx). Throttle penceresi bunu 4xx yapmamalı.
      if (ping && ping.status < 400) absorbedOk++;
      check(ping, { 'trip-ping absorbe (hata yok, <400)': (r) => r && r.status < 400 });
      sleep(1); // 1sn arayla → 3sn throttle'ı bilerek tetikle
    }
    // 10 ping 1sn arayla gönderildi; hızlı olanların throttle'a takılıp yine de HATASIZ dönmesi beklenir.
    check(absorbedOk, { 's9: 10/10 ping hatasız yutuldu': (n) => n === 10 });

    // ARRIVE — sefer terminali.
    const arrive = authed((t) => http.post(`${base}/arrive`, null,
      { headers: jsonHeaders(t), tags: { name: 'trip-arrive' } }), PROVIDER_USER);
    check(arrive, { 'trip-arrive <500': (r) => r && r.status < 500 });
  });
}
