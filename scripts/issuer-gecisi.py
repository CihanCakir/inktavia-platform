#!/usr/bin/env python3
"""
Keycloak issuer geçişi — BFF values dosyalarında Authority'yi public issuer'a çevirir.

Kullanım (repo kökünde):
    python3 issuer-gecisi.py            # KURU KOŞU — hiçbir şeyi değiştirmez, ne yapacağını yazar
    python3 issuer-gecisi.py --uygula   # değişiklikleri yazar

Neden elle sed değil:
    Aynı URL dizgesi dört farklı ayarda geçiyor ve YALNIZ ikisi değişmeli:

      *Keycloak__Authority              = .../realms/inktavia-realm          → DEĞİŞİR
      KeycloakServiceToken__Authority   = .../realms/inktavia-realm          → DEĞİŞİR
      *Keycloak__MetadataAddress        = .../realms/.../.well-known/...     → KALIR (iç adres)
      KeycloakServiceToken__TokenEndpoint = .../realms/.../protocol/...      → KALIR (iç adres)
      *Keycloak__BaseUrl                = http://keycloak:8080               → KALIR (Admin API)

    Ayrım tek kurala dayanıyor: değer TAM OLARAK realm ile bitiyorsa Authority'dir.
    Kör bir sed MetadataAddress'i de çevirir ve her token doğrulaması Cloudflare
    üzerinden dolaşmaya başlar.
"""
import sys
import pathlib

ESKI = "http://keycloak:8080/realms/inktavia-realm"
YENI = "https://auth.inktavia.com/realms/inktavia-realm"

UYGULA = "--uygula" in sys.argv
KOK = pathlib.Path(".")

if not (KOK / "Bff").is_dir():
    sys.exit("HATA: repo kökünde çalıştır (Bff/ dizini görünmüyor)")

dosyalar = sorted(KOK.glob("Bff/**/values-*.yaml"))
if not dosyalar:
    sys.exit("HATA: Bff altında values-*.yaml bulunamadı")

degisen, korunan = 0, 0
dokunulan_dosya = []

for f in dosyalar:
    satirlar = f.read_text(encoding="utf-8").splitlines(keepends=True)
    yeni_satirlar = []
    dosya_degisti = False

    for i, satir in enumerate(satirlar, 1):
        if ESKI not in satir:
            yeni_satirlar.append(satir)
            continue

        kalan = satir.split(ESKI, 1)[1].strip()
        # Değer tam burada bitiyorsa (opsiyonel tırnak dışında) → Authority
        if kalan in ("", '"', "'"):
            yeni = satir.replace(ESKI, YENI)
            yeni_satirlar.append(yeni)
            dosya_degisti = True
            degisen += 1
            print(f"  DEĞİŞİR  {f}:{i}")
            print(f"           - {satir.strip()}")
            print(f"           + {yeni.strip()}")
        else:
            yeni_satirlar.append(satir)
            korunan += 1
            print(f"  KALIR    {f}:{i}  {satir.strip()}")

    if dosya_degisti:
        dokunulan_dosya.append(f)
        if UYGULA:
            f.write_text("".join(yeni_satirlar), encoding="utf-8")

print()
print(f"Değişecek satır : {degisen}")
print(f"Korunacak satır : {korunan}")
print(f"Dosya           : {len(dokunulan_dosya)}")

# Ortam başına beklenti: 4 BFF Authority + 2 ServiceToken Authority = 6
print()
print("Ortam dagilimi (ortam basina 6 bekleniyor):")
for ortam in ("prod", "dev", "test"):
    n = sum(1 for f in dokunulan_dosya if f.name == f"values-{ortam}.yaml")
    print(f"  values-{ortam}.yaml : {n} dosya")
if degisen % 6 != 0:
    print()
    print(f"⚠️  DİKKAT: toplam {degisen} satır 6'nın katı değil. Uygulamadan önce gözden geçir.")

print()
print("UYGULANDI." if UYGULA else "KURU KOŞU — hiçbir dosya değişmedi. Uygulamak için: --uygula")
