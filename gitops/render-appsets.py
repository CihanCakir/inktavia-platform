#!/usr/bin/env python3
"""build/components.json'dan Argo CD ApplicationSet'lerini üretir.

    python3 gitops/render-appsets.py

NEDEN ApplicationSet, 15 ayrı Application dosyası değil:
Sürüm işaretçisi TEK SATIRDA duruyor. 15 dosyayı güncellemek hem gürültülü diff
üretir hem de birinin atlanması "modüllerin yarısı eski sürümde" demektir.

NEDEN TEK ETİKET (tüm bileşenler aynı sha):
Seçici derleme yalnız değişen bileşeni derliyor, ama CI derleme sonrası
değişmeyenlerin `develop` etiketini de `sha-<yeni>` olarak işaretliyor (registry
tarafında manifest kopyası, yeniden derleme yok). Böylece her commit 15 bileşen için
EKSİKSİZ bir etiket seti üretiyor ve tek işaretçi yeterli oluyor.
Sonuç: atomik deploy ve atomik geri alma — `git revert` tüm platformu birlikte alır.
Bileşen başına ayrı tag olsaydı geri alma "hangi modül hangi sha'daydı?" sorusuna dönerdi.

releaseName KRİTİK: mevcut Helm sürüm adlarıyla (aizen-identity vb.) birebir aynı olmalı,
yoksa Argo CD kaynakları devralmaz, YENİLERİNİ yaratır ve iki kopya yan yana çalışır.
"""
import json, os, io

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = "https://github.com/CihanCakir/inktavia-platform.git"
BRANCH = "develop"

comps = json.load(open(os.path.join(ROOT, "build/components.json"), encoding="utf-8"))["components"]

# Başlangıç sürümü: imajları GERÇEKTEN var olan son tam derleme.
# Güncel develop sha'sı kullanılamazdı — o commit yalnız infrastructure/ değiştirdi
# ve seçici derleme sıfır imaj üretti, yani o etiketle imaj yok.
ENVS = {
    # ── ORTAM ANAHTARI ──────────────────────────────────────────────────────────
    # replicas: hangi ortamın AÇIK olduğunu belirler. Tek node'da ikisini birden
    # çalıştırmanın anlamı yok — biri 1 ise diğeri 0 olmalı.
    # Değiştirmek için:  python3 scripts/ortam-degistir.py dev|prod
    "prod": dict(ns="inktavia-prod", values="values-prod.yaml", tag="sha-146071d",
                 replicas=0,
                 ignore_replicas=False,
                 # Otomatik sync 2026-08-17'de AÇILDI. Kapalı başlamıştı: 15 canlı servis
                 # helm CLI ile yönetiliyordu ve devrin çakışmasız olduğu doğrulanmadan
                 # prune/selfHeal açmak riskliydi. Devir kanıtlandıktan sonra açıldı
                 # (tracking-id eklendi, kaynak sayısı artmadı, hepsi Synced/Healthy).
                 #
                 # Bundan sonra `helm upgrade` ile elle müdahale ETMEYİN — selfHeal geri alır.
                 # Prod'a çıkışın tek yolu bu dosyadaki image.tag satırını değiştiren PR'dır.
                 automated=True,
                 note="Prod sürümü ELLE terfi ettirilir. Bu satırı değiştiren PR, prod'a\n"
                      "#     çıkışın tek kapısıdır — tek node, tek operatör, otomatik prod deploy\n"
                      "#     hatayı fark etmeden yayına almak demek."),
    "dev": dict(ns="inktavia-dev", values="values-dev.yaml", tag="sha-146071d",
                replicas=1,
                automated=True,
                # Dev'de replika sayısı GIT'İN DEĞİL operatörün kararı: her şey 0 replika
                # ile duruyor, sınanacak servis elle kaldırılıyor. ignoreDifferences
                # olmadan selfHeal bunu ~20 saniyede geri alır (2026-08-18'de ölçüldü).
                # Prod'da BU YOK: orada replika sürüklenmesi geri alınmalı.
                ignore_replicas=True,
                note="Dev sürümünü CI her develop merge'inde OTOMATİK günceller."),
}

for env, c in ENVS.items():
    elements = "\n".join(
        f"          - id: {x['id']}\n            chart: {x['chart']}" for x in comps
    )
    sync = """  syncPolicy:
    # Yeni Application'lar oluşturulur, silinenler temizlenir.
    preserveResourcesOnDeletion: false""" 
    ignore = ("""      # Replika sayısı dev'de operatörün kararı — bkz. ENVS["dev"] yorumu.
      ignoreDifferences:
        - group: apps
          kind: Deployment
          jsonPointers:
            - /spec/replicas
""" if c.get("ignore_replicas") else "")

    auto = """      syncPolicy:
        automated:
          prune: true
          selfHeal: true
        syncOptions:
          - CreateNamespace=false""" if c["automated"] else """      # syncPolicy YOK — senkronizasyon ELLE tetiklenir.
      # Devir doğrulandıktan sonra automated/prune/selfHeal açılacak."""

    body = f"""# ÜRETİLMİŞ DOSYA — elle düzenleme, tek istisna aşağıdaki image.tag satırı.
# Kaynak: build/components.json · Yeniden üret: python3 gitops/render-appsets.py
#
# {c["note"]}
apiVersion: argoproj.io/v1alpha1
kind: ApplicationSet
metadata:
  name: inktavia-{env}
  namespace: platform
spec:
  goTemplate: true
  goTemplateOptions: ["missingkey=error"]
  generators:
    - list:
        elements:
{elements}
  template:
    metadata:
      name: 'aizen-{{{{.id}}}}-{env}'
    spec:
      project: default
      source:
        repoURL: {REPO}
        targetRevision: {BRANCH}
        path: '{{{{.chart}}}}'
        helm:
          # Mevcut Helm sürüm adlarıyla BİREBİR aynı olmalı — yoksa devir olmaz,
          # Argo CD ikinci bir kopya yaratır.
          releaseName: 'aizen-{{{{.id}}}}'
          valueFiles:
            - {c["values"]}
          parameters:
            # ▼▼▼ TEK SÜRÜM İŞARETÇİSİ ▼▼▼
            - name: image.tag
              value: {c["tag"]}
            # ▲▲▲ TEK SÜRÜM İŞARETÇİSİ ▲▲▲
            # ▼▼▼ ORTAM ANAHTARI — 0 kapalı, 1 açık ▼▼▼
            - name: replicaCount
              value: "{c["replicas"]}"
            # ▲▲▲ ORTAM ANAHTARI ▲▲▲
      destination:
        server: https://kubernetes.default.svc
        namespace: {c["ns"]}
{ignore}{auto}
"""
    out = os.path.join(ROOT, f"gitops/apps/{env}-appset.yaml")
    io.open(out, "w", encoding="utf-8").write(body)
    print(f"  yazıldı: gitops/apps/{env}-appset.yaml  ({len(comps)} bileşen, ns={c['ns']}, otomatik={c['automated']})")
