// LT-5 ortak coğrafya yardımcısı — s6/s7/s8 rastgele TR kıyı koordinatı üretir (nearby marina + discovery
// merkez noktası). Deterministik değil (VU/iter'e göre çeşitlensin diye Math.random) ama hep gerçek marina
// yoğunluğu olan kıyı kutularında kalır; ölçüm anlamlı satırlar döndürür.
export const COASTAL_POINTS = [
  [41.0200, 29.0030], // İstanbul Boğazı
  [40.7550, 29.9200], // Gölcük / İzmit körfezi
  [38.4200, 26.9300], // İzmir / Çeşme
  [37.0320, 27.4300], // Bodrum
  [36.8380, 28.2660], // Marmaris
  [36.7530, 28.9400], // Göcek
  [36.6560, 29.1150], // Fethiye
  [39.3100, 26.6900], // Ayvalık
  [36.8000, 34.6330], // Mersin
  [36.5900, 32.0000], // Alanya
];

// Bir kıyı noktası seç + ~±0.03° (~3 km) rastgele kayma; her istek biraz farklı merkez → cache/agirlik testi gerçekçi.
export function randomCoastal() {
  const [lat, lng] = COASTAL_POINTS[Math.floor(Math.random() * COASTAL_POINTS.length)];
  return {
    lat: lat + (Math.random() - 0.5) * 0.06,
    lng: lng + (Math.random() - 0.5) * 0.06,
  };
}
