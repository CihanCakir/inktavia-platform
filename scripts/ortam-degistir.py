#!/usr/bin/env python3
"""Hangi ortamın açık olduğunu değiştirir. Repo kökünde çalıştır.

    python3 scripts/ortam-degistir.py dev     # dev açık, prod kapalı
    python3 scripts/ortam-degistir.py prod    # prod açık, dev kapalı
    python3 scripts/ortam-degistir.py         # mevcut durumu göster

Tek node'da iki ortamı birden çalıştırmanın anlamı yok: 30 pod, ~9Gi tavan,
15,9GB'lık kutuda host stack de var. Bu script ikisini birlikte çevirir ve
kotayı da açık olan ortama kaydırır.

İki şeyi BİRLİKTE değiştirir — ayrı yapılırsa tuzak var:
  1. gitops/apps/{prod,dev}-appset.yaml → replicaCount parametresi
  2. infrastructure/k8s/namespaces-and-quotas.yaml → kotalar

Kotayı unutursan: açtığın ortam 2Gi'ye sığmaz, pod'lar sessizce Pending'de kalır
ve deployment "0/15 ready" der ama hata vermez.

Çıktı bir COMMIT DEĞİL — dosyaları değiştirir, gözden geçirip kendin commit et.
"""
import pathlib, re, sys

KOK = pathlib.Path(".")
APPSET = {e: KOK / f"gitops/apps/{e}-appset.yaml" for e in ("prod", "dev")}
KOTA = KOK / "infrastructure/k8s/namespaces-and-quotas.yaml"
RENDER = KOK / "gitops/render-appsets.py"

# Açık ortamın kotası / kapalı ortamın kotası
BUYUK = {"mem": "8Gi", "cpu": "10", "pods": "32"}
KUCUK = {"mem": "2Gi", "cpu": "3",  "pods": "4"}

if not APPSET["prod"].is_file():
    sys.exit("HATA: repo kökünde çalıştır")

def oku_replicas(env):
    s = APPSET[env].read_text(encoding="utf-8")
    m = re.search(r'- name: replicaCount\n\s+value: "(\d+)"', s)
    if not m:
        sys.exit(f"HATA: {APPSET[env]} içinde replicaCount parametresi yok.\n"
                 f"      Önce kurulum-ortam-anahtari.py çalıştırılmalı.")
    return int(m.group(1))

durum = {e: oku_replicas(e) for e in APPSET}

if len(sys.argv) < 2:
    print("Mevcut durum:")
    for e, v in durum.items():
        print(f"  {e:5s} {'AÇIK' if v else 'kapalı'}  (replicaCount={v})")
    print("\nDeğiştirmek için: python3 scripts/ortam-degistir.py dev|prod")
    sys.exit(0)

acik = sys.argv[1].lower()
if acik not in APPSET:
    sys.exit(f"HATA: 'dev' ya da 'prod' bekleniyordu, '{acik}' geldi")
kapali = "dev" if acik == "prod" else "prod"

if durum[acik] == 1 and durum[kapali] == 0:
    print(f"Zaten {acik} açık, {kapali} kapalı. Yapılacak bir şey yok.")
    sys.exit(0)

# ── 1) render-appsets.py içindeki kaynak değerler ────────────────────────────
s = RENDER.read_text(encoding="utf-8")
for env, deger in ((acik, 1), (kapali, 0)):
    desen = rf'("{env}": dict\(ns="inktavia-{env}".*?replicas=)\d'
    yeni, n = re.subn(desen, rf"\g<1>{deger}", s, count=1, flags=re.S)
    if n != 1:
        sys.exit(f"HATA: render-appsets.py içinde {env} için replicas bulunamadı")
    s = yeni
RENDER.write_text(s, encoding="utf-8")

# ── 2) ApplicationSet'leri yeniden üret ──────────────────────────────────────
import subprocess
subprocess.run([sys.executable, "gitops/render-appsets.py"], check=True)

# ── 3) Kotalar ───────────────────────────────────────────────────────────────
k = KOTA.read_text(encoding="utf-8")

def kota_yaz(metin, ns, v, aciklama):
    """Belirtilen namespace'in ResourceQuota bloğunu yeniden yazar."""
    desen = re.compile(
        rf"(  name: \w+-cap\n  namespace: {ns}\nspec:\n  hard:\n)(?:.*?\n)*?(?=---|\Z)")
    yeni_govde = (f"{aciklama}"
                  f"    limits.memory: {v['mem']}\n"
                  f"    limits.cpu: \"{v['cpu']}\"\n"
                  f"    pods: \"{v['pods']}\"\n")
    metin, n = desen.subn(lambda m: m.group(1) + yeni_govde, metin, count=1)
    if n != 1:
        sys.exit(f"HATA: {ns} kota bloğu bulunamadı")
    return metin

ACIK_NOT = ("    # AÇIK ORTAM. 15 servisin tavan toplamı ~4,2Gi; rolling update\n"
            "    # sırasında her deployment geçici olarak İKİ pod çalıştırır.\n"
            "    # Bu değerler ortam-degistir.py tarafından yönetiliyor.\n")
KAPALI_NOT = ("    # KAPALI ORTAM — kaynak açık olana verildi.\n"
              "    # Bu değerler ortam-degistir.py tarafından yönetiliyor.\n")

k = kota_yaz(k, f"inktavia-{acik}",  BUYUK, ACIK_NOT)
k = kota_yaz(k, f"inktavia-{kapali}", KUCUK, KAPALI_NOT)
KOTA.write_text(k, encoding="utf-8")

print()
print(f"✅ {acik.upper()} açıldı, {kapali} kapatıldı.")
print()
print("Değişen dosyalar:")
print("  gitops/render-appsets.py")
print("  gitops/apps/prod-appset.yaml")
print("  gitops/apps/dev-appset.yaml")
print("  infrastructure/k8s/namespaces-and-quotas.yaml")
print()
print("⚠️ Kota manifesti Argo CD'de DEĞİL — elle uygulanmalı:")
print("     kubectl apply -f infrastructure/k8s/namespaces-and-quotas.yaml")
print("   Ve kota, replicaCount'tan ÖNCE uygulanmalı; ters sırada pod'lar Pending'de kalır.")
print()
print("git diff ile gözden geçir, sonra commit et.")
