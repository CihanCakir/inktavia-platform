#!/usr/bin/env python3
"""build/components.json'dan modül izolasyon NetworkPolicy'sini üretir.

    python3 infrastructure/k8s/render-networkpolicy.py inktavia-prod > np-prod.yaml

NEDEN ÜRETİLİYOR, ELLE YAZILMIYOR:
Politika modül adlarını tek tek sayıyor. Elle tutulan bir liste yeni modül eklendiğinde
sessizce eksik kalır — ve eksik kalan bir izolasyon kuralı, olmayan bir kuraldan daha
tehlikelidir çünkü var sanılır.

TASARIM KARARLARI (hepsi bilinçli, hepsi test edilebilir):

1. `default-deny-ingress` YOK.
   Namespace geneline uygulanan bir varsayılan-ret, kubelet'in sağlık probe'larını da
   kesebilir ve 15 pod birden Ready olmaktan çıkar. Politikayı yalnız modül pod'larına
   uyguluyoruz; BFF'ler ingress-nginx'ten gelmeye devam ediyor.

2. Modüller BİRBİRİNİ de çağırabilir.
   servicerequest → payment, identity → filestorage, content → filestorage gerçek
   çağrılar. "Yalnız BFF" kuralı bunları kırardı. İzolasyon yine anlamlı: ingress
   controller, başka namespace'ler ve rastgele pod'lar modüle ulaşamaz.

3. Kubelet probe'ları için düğüm IP'si açık.
   Probe pod'dan değil düğümden gelir; podSelector onu kapsamaz.

4. Etiket olarak `app.kubernetes.io/name` kullanılıyor.
   Chart'lar `aizen.io/tier` üretmiyor; mevcut etiketlerle matchExpressions yazmak
   15 şablonu değiştirmekten hem hızlı hem daha az riskli.
"""
import json, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NS = sys.argv[1] if len(sys.argv) > 1 else "inktavia-prod"
NODE_IP = os.environ.get("NODE_IP", "192.168.1.50")

comps = json.load(open(os.path.join(ROOT, "build/components.json"), encoding="utf-8"))["components"]
mods = [f"aizen-{c['id']}" for c in comps if c["kind"] == "module"]
bffs = []
for c in comps:
    if c["kind"] != "bff":
        continue
    bffs.append("aizen-" + c["id"].replace("bff-marineprovider", "bff-marineprovider"))

def lst(items, indent):
    pad = " " * indent
    return "\n".join(f"{pad}- {i}" for i in items)

print(f"""# ÜRETİLMİŞ DOSYA — elle düzenleme.
# Kaynak: build/components.json
# Yeniden üret: python3 infrastructure/k8s/render-networkpolicy.py {NS}
#
# NEDEN BU BİR GÜVENLİK KONTROLÜ, SÜS DEĞİL:
# Modüller son kullanıcı token'ını görmez; BFF'in ilettiği kimliğe güvenirler
# (X-Aizen-User-Id + paylaşılan sır + AllowedClientIds). Bir modüle ULAŞABİLEN ve sırrı
# bilen biri istediği kullanıcıyı taklit edebilir. Ağ izolasyonu bu yüzden kimlik
# doğrulama tasarımının parçasıdır, üzerine eklenen bir katman değil.
#
# ⚠️ NetworkPolicy'yi UYGULAYAN bir CNI şart. Bu cluster'da k3s/kube-router ile
# sentetik test yapıldı ve uygulandığı KANITLANDI (politikasız 200, default-deny sonrası
# bağlantı kurulamıyor). Başka bir ortama taşınırsa yeniden doğrulanmalı.
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: modules-ingress-restricted
  namespace: {NS}
spec:
  podSelector:
    matchExpressions:
      - key: app.kubernetes.io/name
        operator: In
        values:
{lst(mods, 10)}
  policyTypes:
    - Ingress
  ingress:
    # 1) BFF'ler — modüllere çağrı yapan asıl katman.
    - from:
        - podSelector:
            matchExpressions:
              - key: app.kubernetes.io/name
                operator: In
                values:
{lst(bffs, 18)}
      ports:
        - protocol: TCP
          port: 8080

    # 2) Modüller arası çağrılar (servicerequest→payment, identity→filestorage, …).
    #    Modüler monolitte bunlar normal; kapatmak uygulamayı kırar.
    - from:
        - podSelector:
            matchExpressions:
              - key: app.kubernetes.io/name
                operator: In
                values:
{lst(mods, 18)}
      ports:
        - protocol: TCP
          port: 8080

    # 3) Kubelet sağlık probe'ları. Probe pod'dan değil DÜĞÜMDEN gelir; podSelector
    #    onu kapsamaz. Bu kural olmadan startup/liveness probe'ları düşer ve 11 modül
    #    birden yeniden başlatılır.
    - from:
        - ipBlock:
            cidr: {NODE_IP}/32
      ports:
        - protocol: TCP
          port: 8080
""")
