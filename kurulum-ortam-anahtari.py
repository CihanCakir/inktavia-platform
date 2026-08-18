#!/usr/bin/env python3
"""TEK SEFERLİK kurulum: ortam açma/kapama anahtarını ApplicationSet'e taşır.

    python3 kurulum-ortam-anahtari.py            # kuru koşu
    python3 kurulum-ortam-anahtari.py --uygula

Sorun:
    Prod'u kapatıp dev'i açmak (ve tersi) tekrar tekrar yapılacak bir iş. Bunu
    values dosyalarında yapmak her geçişte 30 dosyalık diff ve dallar arasında
    sürekli merge çakışması demek.

Çözüm:
    replicaCount de image.tag gibi ApplicationSet parametresi olsun. Geçiş
    2 satır: prod-appset 0/1, dev-appset 1/0.

Bu script:
  1. 30 values dosyasında replicaCount'u 1'e normalize eder + açıklama koyar
  2. render-appsets.py'ye ortam bazlı `replicas` ekler
  3. ApplicationSet'leri yeniden üretir (prod=0 kapalı, dev=1 açık)
"""
import pathlib, sys, re, subprocess

UYGULA = "--uygula" in sys.argv
KOK = pathlib.Path(".")
if not (KOK / "gitops/render-appsets.py").is_file():
    sys.exit("HATA: repo kökünde çalıştır")

rapor = []
def yaz(p, s):
    if UYGULA:
        p.write_text(s, encoding="utf-8")

# ── 1) values dosyalarını normalize et ───────────────────────────────────────
NOT = """# ⚠️ replicaCount'u BURADAN yönetme. Ortamın açık/kapalı olması
# gitops/apps/{prod,dev}-appset.yaml içindeki `replicaCount` parametresinden
# geliyor ve Helm parametresi bu değerin ÜZERİNE yazıyor. Buradaki 1, chart tek
# başına çalıştırıldığında geçerli olan doğal varsayılan.
# Ortam değiştirmek için:  python3 scripts/ortam-degistir.py dev|prod
"""

rapor.append("── 1) values dosyaları normalize ediliyor ──")
dosyalar = sorted(KOK.glob("Modules/*/deploy/*/values-prod.yaml")) + \
           sorted(KOK.glob("Modules/*/deploy/*/values-dev.yaml")) + \
           sorted(KOK.glob("Bff/deploy/*/values-prod.yaml")) + \
           sorted(KOK.glob("Bff/deploy/*/values-dev.yaml"))

sayac = 0
for p in dosyalar:
    s = p.read_text(encoding="utf-8")
    satirlar = s.splitlines(keepends=True)
    idx = [k for k, l in enumerate(satirlar) if re.match(r"^replicaCount:\s*\d", l)]
    if len(idx) != 1:
        rapor.append(f"  ⚠️ ATLANDI {p} — replicaCount {len(idx)} kez")
        continue
    i = idx[0]

    # replicaCount'un hemen üstündeki "# replicaCount:" ile başlayan açıklama
    # bloğunu temizle (varsa) — artık yanlış bilgi veriyor.
    j = i - 1
    while j >= 0 and satirlar[j].strip() == "":
        j -= 1
    bas = None
    k = j
    while k >= 0 and satirlar[k].lstrip().startswith("#"):
        if satirlar[k].lstrip().startswith("# replicaCount:"):
            bas = k
            break
        k -= 1
    if bas is not None:
        del satirlar[bas:j + 1]
        i -= (j + 1 - bas)

    if "gitops/apps/{prod,dev}-appset.yaml" not in s:
        satirlar.insert(i, NOT)
        i += 1
    satirlar[i] = "replicaCount: 1\n"
    yaz(p, "".join(satirlar))
    sayac += 1
rapor.append(f"  {sayac}/{len(dosyalar)} dosya normalize edildi")

# ── 2) render-appsets.py ─────────────────────────────────────────────────────
rapor.append("── 2) render-appsets.py ──")
rp = KOK / "gitops/render-appsets.py"
s = rp.read_text(encoding="utf-8")

# ⚠️ "replicas=" ile kontrol ETME — `ignore_replicas=True` satırı da bu dizgeyi
# içeriyor ve yanlış pozitif veriyor. Benzersiz bir işarete bak.
if "ORTAM ANAHTARI" in s:
    rapor.append("  zaten eklenmiş, atlandı")
else:
    # ENVS'e replicas ekle
    s = s.replace(
        '''    "prod": dict(ns="inktavia-prod", values="values-prod.yaml", tag="sha-146071d",''',
        '''    # ── ORTAM ANAHTARI ──────────────────────────────────────────────────────────
    # replicas: hangi ortamın AÇIK olduğunu belirler. Tek node'da ikisini birden
    # çalıştırmanın anlamı yok — biri 1 ise diğeri 0 olmalı.
    # Değiştirmek için:  python3 scripts/ortam-degistir.py dev|prod
    "prod": dict(ns="inktavia-prod", values="values-prod.yaml", tag="sha-146071d",
                 replicas=0,''', 1)
    s = s.replace(
        '''    "dev": dict(ns="inktavia-dev", values="values-dev.yaml", tag="sha-146071d",''',
        '''    "dev": dict(ns="inktavia-dev", values="values-dev.yaml", tag="sha-146071d",
                replicas=1,''', 1)

    # Şablona parametreyi ekle
    eski = """            # ▲▲▲ TEK SÜRÜM İŞARETÇİSİ ▲▲▲"""
    yeni = """            # ▲▲▲ TEK SÜRÜM İŞARETÇİSİ ▲▲▲
            # ▼▼▼ ORTAM ANAHTARI — 0 kapalı, 1 açık ▼▼▼
            - name: replicaCount
              value: "{c["replicas"]}"
            # ▲▲▲ ORTAM ANAHTARI ▲▲▲"""
    assert eski in s, "sürüm işaretçisi çapası bulunamadı"
    s = s.replace(eski, yeni, 1)
    yaz(rp, s)
    rapor.append("  replicas parametresi eklendi (prod=0, dev=1)")

print("\n".join(rapor))
print()
if UYGULA:
    print("── 3) ApplicationSet'ler yeniden üretiliyor ──")
    subprocess.run([sys.executable, "gitops/render-appsets.py"], check=True)
    print("\nUYGULANDI.")
else:
    print("KURU KOŞU — hiçbir dosya değişmedi. Uygulamak için: --uygula")
