# Inktavia Marine OS — CI/CD + Self-Hosted Kubernetes Go-Live Roadmap

**Sürüm:** 1.1 · **Tarih:** 2026-08-16 · **Sahip:** Cihan Çakır
*(v1.1: A1 frontend repoları ve A2 donanım cevaplandı → §1.3 frontend envanteri, §1.4 donanım kararı, Faz 10 detaylandırıldı, domain haritası koddaki gerçeğe hizalandı)*
**Kapsam:** `inktavia-platform` monorepo (11 modül + 5 BFF + Gateway) + 3 frontend (Admin Panel, Provider Portal, Inktavia Web) → kendi kasanda çalışan k3s cluster'ı, GHCR üzerinden versiyonlanmış image'lar, GitOps ile kontrollü deploy, `inktavia.com` altında yayın.

---

## 0. Kilitlenen Kararlar

| # | Karar | Seçim | Gerekçe |
|---|---|---|---|
| D1 | K8s tabanı | **Windows 10 Pro + Hyper-V → Ubuntu Server VM → k3s** | Windows kurulumu korunur, VM snapshot'ı ile geri dönüş kolay, gerçek Linux CNI (NetworkPolicy uygulanır) |
| D2 | Container registry | **GHCR** (`ghcr.io/cihancakir/inktavia/*`) | Repo zaten GitHub'da, işletme maliyeti sıfır, kasa çökse bile image'lar ayakta |
| D3 | "Private/Public" anlamı | **Ağ erişimi (ingress)** | Modüller ClusterIP + NetworkPolicy ile kapalı; sadece BFF'ler internete açık. Image'ların tamamı GHCR'da **private** kalır (tek görünürlük politikası = daha az hata yüzeyi) |
| D4 | Dış erişim | **Cloudflare Tunnel** | Statik IP / port forward / firewall açma yok; kasanın gerçek IP'si gizli; ücretsiz TLS + WAF + DDoS. Ev/ofis hattında tek makul yol |
| D5 | CD modeli | **Argo CD (pull-based GitOps)** | Cluster'a dışarıdan kimlik/kubeconfig vermeden deploy; her deploy git'te izlenebilir; rollback = git revert |
| D6 | Secret yönetimi | **Sealed Secrets** | Şifreli secret git'e commit edilebilir, GitOps ile uyumlu, tek bileşen |

---

## 1. Mevcut Durum Envanteri (kod tabanından doğrulandı)

### Elimizde olan

| Varlık | Durum |
|---|---|
| Modüller (11) | `Modules/{CargoDry,Content,FileStorage,Identity,Messaging,Notification,Payment,Profile,ReferenceData,ServiceRequest,Vessel}` — **her birinde `build/Dockerfile` + `deploy/aizen-<modül>` Helm chart'ı var** |
| BFF'ler (5) | `Bff/src/{AdminPanel,Aizen.Bff,Marine.Participant.Mobile,Marine.Web,MarineProvider}` — `Bff/build/Dockerfile.*` + `Bff/deploy/aizen-bff-*` chart'ları var |
| Gateway | `Gateway/build/Dockerfile` + `Gateway/deploy/aizen-gateway` |
| CI | `.github/workflows/ci.yml` — sadece `dotnet restore/build/test`. **Image build/push YOK** |
| K8s güvenlik | `infrastructure/k8s/`: `networkpolicy-internal-modules.yaml`, `ingress-bff-marineprovider.yaml`, `configmap-ingress-nginx-log.yaml` + iyi yazılmış README |
| Lokal çalıştırma | `docker-compose.yaml` — postgres, mongo, redis, rabbitmq, minio, keycloak (+init), 11 modül API, 4 BFF, prometheus, grafana, k6 |
| Keycloak | `infrastructure/keycloak/` realm export + init.sh + custom OTP SPI (jar) |

### Kapatılması gereken boşluklar (bu roadmap'in işi)

| # | Boşluk | Etki | Faz |
|---|---|---|---|
| G1 | Chart'ların tamamı `nexus.local:8083/aizen/*` registry'sine bakıyor — böyle bir registry ayakta değil | Hiçbir şey deploy olamaz | F6 |
| G2 | `values-prod.yaml` içinde `tag: "latest"` + `pullPolicy: Always` | Prod'da hangi kodun koştuğu bilinmez, rollback imkânsız | F4 + F6 |
| G3 | **Modül chart'larında `ingress.enabled: true`** (örn. `Modules/Identity/.../values-prod.yaml:43`) | `infrastructure/k8s/README.md`'nin tarif ettiği güvenlik modelini birebir kırar: modüller internete açılır, BFF assertion mekanizması istismar edilebilir hale gelir | **F6 — kritik** |
| G4 | CI image üretmiyor, CD hiç yok | Her deploy elle | F5, F7 |
| G5 | `resources: {}` (limit/request yok) tüm chart'larda | Tek node'da bir pod tüm RAM'i yiyip cluster'ı düşürebilir | F6 |
| G6 | Secret'lar (`BffAssertion__SharedSecret`, DB parolaları, iyzico anahtarları) `.env` ve compose'da | Prod'a taşınamaz | F8 |
| G7 | ~~Frontend repolarının yeri bilinmiyor~~ → **çözüldü**, bkz. §1.3 | — | — |
| G8 | Dockerfile'lar `COPY . .` + ayrı `build` **ve** `publish` adımı | Her image ~2× uzun sürüyor, cache neredeyse hiç tutmuyor | F5 |
| G9 | **`inktavia-marine-provider-web` ve `inktavia-marine-web`'in git remote'u YOK** — sadece lokal repo | GitHub'da olmayan koda CI kurulamaz. Aynı zamanda: kasa/disk arızasında bu iki proje **tamamen kaybolur** | **F0 — acil** |
| G10 | Üç frontend'de de Dockerfile yok, workflow yok | Faz 10 sıfırdan yazılacak | F10 |
| G11 | Commit edilmemiş çalışma: admin 49, marine-web 108, provider 10 dosya | CI ne build ederse etsin, lokalindeki kod değil | F0 |
| G12 | Frontend `.env.production.example` dosyalarındaki host'lar (`marine-os-admin-api.inktavia.com`, `provider-api.inktavia.com`, `auth.inktavia.com`) §4'teki haritayla uyuşmuyordu | Domain haritası koda hizalandı, bkz. §4 | F9 |

---

## 1.3 Frontend Envanteri (yeni bağlanan repolardan doğrulandı)

| Repo | Teknoloji | GitHub remote | Son commit | Kirli dosya | Olgunluk |
|---|---|---|---|---|---|
| `inktavia-marine-admin-web` | Vite + React 19 + TS, TanStack Query, i18next, Recharts, keycloak-js, Zustand | ✅ `CihanCakir/inktavia-marine-admin-web` | 2026-08-07 | **49** | Olgun — canlıya en yakın |
| `inktavia-marine-provider-web` | Vite + React 19 + TS, SignalR, MapLibre GL, keycloak-js, PWA | ❌ **YOK (lokal)** | 2026-08-07 | 10 | Olgun — canlıya en yakın |
| `inktavia-marine-web` | Next.js 16.3 (SSR) + next-intl + Tailwind v4 | ❌ **YOK (lokal)** | 2026-08-15 | **108** | **Phase 1/13** — v1 içeriği hazır değil |

### Bundan çıkan üç sonuç

1. **G9 acil.** İki repo yalnızca senin diskinde. Bu bir CI/CD sorunundan önce bir **veri kaybı riski**; kasayı söküp yeniden kurma işine girmeden önce ikisini de GitHub'a private repo olarak push et.
2. **`inktavia.com` ana sitesi go-live'ın kritik yolunda değil.** Marine Web 13 fazlık kendi roadmap'inin 1. fazında ve BFF'i (`Aizen.Bff.MarineWeb`) henüz gerçek veri döndürmüyor. Yayın sırası bu yüzden: **önce `partner.` ve `admin.`**, ana site sonra. Bu aslında iyi haber — altyapıyı daha az riskli iki uygulamayla prova edersin.
3. **Auth mimarisi zaten Bearer-token tabanlı** (`keycloak-js` tarayıcıda, `axios` ile `Authorization` başlığı). Yani BFF burada cookie tutan bir oturum katmanı değil, kimliği modüle *iddia eden* bir proxy. §4'teki domain kararı buna göre revize edildi.

---

## 1.4 Donanım Değerlendirmesi (DESKTOP-HE78P5B)

| Bileşen | Değer | Yeterli mi? |
|---|---|---|
| CPU | AMD Ryzen 5 1600 — 6 çekirdek / 12 iş parçacığı, 3.2 GHz | ⚠️ Yeterli ama dar. VM'e 8 vCPU ver, host'a 4 kalsın |
| RAM | **32 GB takılı / 15,9 GB kullanılabilir** | 🔴 **BLOKER — önce bu çözülmeli** |
| SSD | 233 GB Samsung 860 EVO (Windows burada) | 🔴 Yetersiz — VM + veritabanları sığmaz |
| HDD | 932 GB Seagate ST1000VX005 (SkyHawk, gözetim diski) | 🟡 Yedek/MinIO için uygun, **veritabanı için değil** |
| GPU | GTX 1060 3GB | ⚪ İlgisiz (ileride ML/görsel işleme düşünülürse not) |

### 🔴 R1a — "32 GB takılı, 15,9 GB kullanılabilir"

Bu normal değil. Ryzen 1600'de tümleşik GPU yok, yani 16 GB'ın donanıma ayrılması için bir sebep yok. En olası üç neden, kontrol sırasıyla:

```powershell
# 1) msconfig maksimum bellek sınırı (en sık neden)
#    Win+R → msconfig → Boot → Advanced options → "Maximum memory" İŞARETLİ mi?
#    İşaretliyse kaldır, yeniden başlat. Veya komutla:
bcdedit /deletevalue {current} truncatememory
bcdedit /deletevalue {current} removememory
bcdedit /enum {current}          # truncatememory/removememory satırı KALMAMALI

# 2) Modüller gerçekten görünüyor mu?
wmic memorychip get BankLabel,DeviceLocator,Capacity,Speed,Manufacturer
#    4×8GB mi, 2×16GB mı? Kaç satır dönüyor?

# 3) Görev Yöneticisi → Performans → Bellek → "Donanım ayrılmış"
#    16 GB civarıysa neden BIOS/donanım tarafında.
```

(1) ve (2) temizse: kasayı aç, modülleri yeniden otur, BIOS'ta XMP/DOCP'yi kapat (Zen 1 bellek uyumluluğu meşhur şekilde hassastır), tek tek test et. `mdsched.exe` ile Windows Bellek Tanılama çalıştır.

**Neden bloker:** Tam yığın için gerçekçi bütçe —

| Katman | RAM |
|---|---|
| 11 modül API (.NET 9, ~180 MB) | ~2,0 GB |
| 4 BFF | ~0,9 GB |
| 3 frontend (2 nginx + 1 Next.js) | ~0,3 GB |
| k3s control plane + ingress + Argo CD | ~1,8 GB |
| Prometheus + Grafana + Loki | ~2,0 GB |
| **Cluster toplam** | **~7 GB** (rahat çalışma için 10 GB) |
| PostgreSQL + Mongo + Keycloak + RabbitMQ + Redis + MinIO | ~5,5 GB |
| Windows host | ~3 GB |
| **GENEL TOPLAM** | **~19–21 GB** |

32 GB ile rahat. **15,9 GB ile tam yığın çalışmaz.**

**15,9 GB'da kalırsan Plan B:** tek namespace (prod veya dev, ikisi birden değil), observability yığını kapalı (metrics-server + `kubectl logs` ile idare), her modül `limits.memory: 384Mi` ve `DOTNET_GCHeapHardLimit` ile sıkılaştırılmış, Loki yok. Bu bir *demo* ortamı olur, prod değil.

### 🔴 R1b — Disk

233 GB SSD'de Windows varken 400 GB'lık bir VM diski sığmaz ve HDD'ye kurulan bir k3s + PostgreSQL kabul edilemez derecede yavaş olur (etcd fsync gecikmesine çok duyarlıdır).

**Yapılacak sıralama:**
1. **1 TB SATA SSD ekle.** Bu bütçedeki en yüksek getirili tek harcama — anakart AM4 ise muhtemelen boş SATA portu var, hatta M.2 NVMe slotu olabilir (B350/X370 çoğunda var).
2. Yeni SSD'yi VM'e ver: sistem + k3s + PostgreSQL/Mongo/Redis burada.
3. Mevcut 932 GB HDD: **yedekler + MinIO nesne deposu** (sıralı okuma/yazma, IOPS gerektirmez).
4. Mevcut 233 GB SSD: Windows host'ta kalsın.

SSD alınana kadar: VM diskini 120 GB olarak SSD'de tut, MinIO'yu HDD'ye bind-mount et, observability'yi kapalı başlat.

---

## 2. Hedef Mimari

```
                        İNTERNET
                            │
                 ┌──────────▼───────────┐
                 │  Cloudflare (DNS/TLS)│  inktavia.com + alt alanlar
                 │  WAF · DDoS · Access │
                 └──────────┬───────────┘
                            │ Cloudflare Tunnel (outbound-only)
╔═══════════════════════════▼════════════════════════════════════╗
║  WINDOWS 10 PRO KASA                                           ║
║  ┌──────────────────────────────────────────────────────────┐  ║
║  │ Hyper-V: VM "inktavia-k3s"  (Ubuntu Server 24.04 LTS)     │  ║
║  │                                                          │  ║
║  │  ┌── k3s cluster ─────────────────────────────────────┐  │  ║
║  │  │  cloudflared ─► ingress-nginx                      │  │  ║
║  │  │                      │                             │  │  ║
║  │  │        ┌─────────────┼─────────────┐               │  │  ║
║  │  │        ▼             ▼             ▼               │  │  ║
║  │  │   bff-adminpanel  bff-provider  bff-marine-web ...  │  │  ║
║  │  │        │             │             │   [tier: bff] │  │  ║
║  │  │        └─────────────┼─────────────┘               │  │  ║
║  │  │                      ▼   NetworkPolicy: sadece BFF │  │  ║
║  │  │   identity · payment · vessel · cargodry · ...      │  │  ║
║  │  │                 [tier: module — INGRESS YOK]        │  │  ║
║  │  │                                                    │  │  ║
║  │  │  Argo CD · Sealed Secrets · Prometheus · Grafana    │  │  ║
║  │  └────────────────────────┬───────────────────────────┘  │  ║
║  │                           │ cluster dışı (host network)   │  ║
║  │  PostgreSQL · MongoDB · Redis · RabbitMQ · MinIO · Keycloak│  ║
║  └──────────────────────────────────────────────────────────┘  ║
╚════════════════════════════════════════════════════════════════╝
            ▲                                    ▲
            │ image pull (private)               │ git sync
    ghcr.io/cihancakir/inktavia/*        github.com/CihanCakir/inktavia-platform
            ▲
            │ push
    GitHub Actions (build · test · scan · sign)
```

**Neden stateful bileşenler cluster dışında?** `infrastructure/k8s/README.md` bunu zaten şart koşuyor. Tek node'lu bir k3s'te PostgreSQL'i pod olarak koşturmak, node yeniden başladığında veri riski + kurtarma karmaşıklığı demek. VM içinde systemd/docker ile çalışan Postgres'i yedeklemek ve geri yüklemek çok daha basit.

---

## 3. Versiyonlama Sözleşmesi (F4'ün çıktısı — önce bunu kabul et)

### Image adlandırma

```
ghcr.io/cihancakir/inktavia/<component>:<tag>
```

`<component>` listesi (chart adlarıyla birebir hizalı):

| Katman | Bileşenler |
|---|---|
| Modül (private ağ) | `identity-api` · `referencedata-api` · `vessel-api` · `filestorage-api` · `servicerequest-api` · `cargodry-api` · `messaging-api` · `notification-api` · `payment-api` · `profile-api` · `content-api` |
| BFF (public ağ) | `bff-adminpanel` · `bff-marineprovider` · `bff-marine-web` · `bff-marine-mobile` |
| Diğer | `gateway` |
| Frontend (ayrı repolar) | `web-admin` · `web-provider` · `web-marine` |

### Tag stratejisi

| Tag | Ne zaman üretilir | Kullanım | Mutable? |
|---|---|---|---|
| `sha-<7hex>` | **her** build | Kaynak gerçek. Her ortam bunu referanslar | ❌ asla |
| `dev` | `develop` push | Sadece `inktavia-dev` namespace | ✅ |
| `1.4.2` / `1.4` / `1` | `v1.4.2` git tag'i | Prod release | `1.4.2` ❌, diğerleri ✅ |
| `latest` | **hiç** | — | — |

**Prod kuralı:** `values-prod.yaml` içinde tag **her zaman** `sha-<hex>` veya digest (`@sha256:...`). Argo CD bu değeri git'ten okur → "prod'da ne koşuyor?" sorusunun cevabı tek bir git commit'i.

### Release treni

Monorepo + tek geliştirici gerçeği: **platform-wide semver**. `v1.4.2` tag'i atıldığında tüm bileşenler aynı sürüm numarasıyla yayınlanır. Bileşen başına bağımsız versiyonlama (`payment-v1.4.2`) teorik olarak daha temiz ama 17 ayrı sürüm hattını tek başına yönetmek pratikte sürdürülemez. İleride ekip büyürse bileşen bazına geçilir.

- `main` = prod'da olan. Sadece release tag'i buradan atılır.
- `develop` = entegrasyon. Her push `dev` namespace'ine otomatik iner.
- `feature/*` → PR → `develop`.

### Chart versiyonlama

| Alan | Anlamı | Ne zaman artar |
|---|---|---|
| `Chart.version` | Helm chart'ın kendi sürümü | Template/values yapısı değişince |
| `Chart.appVersion` | İçindeki uygulamanın sürümü | Release trenine göre (`1.4.2`) |

Şu an tüm chart'larda `version: 0.1.0`, `appVersion: "1.16.0"` (Helm scaffold artığı) — F6'da düzeltilecek.

---

## 4. Domain Haritası (`inktavia.com`) — *koddaki gerçeğe hizalandı*

> **Düzeltme (v1.1):** v1.0'da SPA ve BFF'i aynı host altında (`admin.inktavia.com/bff/*`) toplamayı önermiştim; gerekçem `HttpOnly` cookie oturumuydu. Frontend kodunu okuyunca bu gerekçenin geçersiz olduğu ortaya çıktı: üç uygulama da `keycloak-js` + `axios` ile **Bearer token** taşıyor, cookie oturumu yok. Cookie yoksa aynı-origin zorunluluğu da yok. Bu yüzden **koddaki ayrı-API-host modeli korunuyor** — sıfır refactor, standart SPA+API ayrımı. (İleride HttpOnly cookie oturumuna geçilirse bu karar yeniden açılmalı.)

| Host | Arkasında | Erişim | Kodda geçtiği yer |
|---|---|---|---|
| `inktavia.com`, `www.` | Marine Web (Next.js SSR) | 🌍 Public | `SITE_URL` |
| `admin.inktavia.com` | Admin Panel SPA (statik) | 🔒 Cloudflare Access | — |
| `admin-api.inktavia.com` | `bff-adminpanel` | 🌍 Public | `VITE_ADMIN_PANEL_BFF_BASE_URL` ⚠️ şu an `marine-os-admin-api.` |
| `partner.inktavia.com` | Provider Portal SPA (PWA) | 🌍 Public | — |
| `provider-api.inktavia.com` | `bff-marineprovider` (+ WebSocket) | 🌍 Public | `VITE_PROVIDER_BFF_BASE_URL` ✅ |
| `mapi.inktavia.com` | `bff-marine-mobile` | 🌍 Public | mobil uygulama |
| `web-api.inktavia.com` | `bff-marine-web` | 🌍 Public | `MARINE_WEB_BFF_BASE_URL` |
| `auth.inktavia.com` | Keycloak | 🌍 Public | `VITE_KEYCLOAK_URL` ✅ |
| `files.inktavia.com` | MinIO (presigned URL) | 🌍 Public | — |
| `argocd.` · `grafana.` | Argo CD · Grafana | 🔒 Cloudflare Access | — |
| `*.dev.inktavia.com` | dev namespace | 🔒 Cloudflare Access | — |
| modüller (`identity-api` vb.) | — | 🚫 **DNS yok, Ingress yok, NetworkPolicy kapalı** | — |

**Tek isim değişikliği önerisi:** `marine-os-admin-api.inktavia.com` → `admin-api.inktavia.com`. Diğerleriyle tutarlı, kısa, tek bir `.env.production` satırı. Mevcut adı korumak istersen sorun değil — yalnızca tutarlılık kaygısı.

**CORS:** Ayrı host modeli seçildiği için her BFF'in `AllowedOrigins`'i doğru olmalı (`admin-api` → yalnız `https://admin.inktavia.com`, wildcard **yok**). Bu bir F9 kontrol maddesi.

**Neden `admin.` Cloudflare Access arkasında?** Admin paneli sınırlı sayıda kişinin kullandığı bir yüzey; internete tamamen açık bırakmanın hiçbir kazancı yok, kimlik doğrulama denemelerine karşı bedava bir katman kazanırsın.

---

## FAZ 0 — Acil: Kaynak Kodu Emniyete Al

**Süre:** ~1 saat · **Neden ilk sırada:** `inktavia-marine-provider-web` ve `inktavia-marine-web`'in git remote'u yok. Bu iki proje şu anda **tek bir diskte** duruyor ve o disk, yeniden yapılandırmayı planladığın makinede. Kasayı kurcalamadan önce bu bitmeli.

```bash
# GitHub'da iki private repo aç (inktavia-marine-provider-web, inktavia-marine-web), sonra:
cd ~/Desktop/Mine/DEV/inktavia-marine-provider-web
git status                                    # 10 dosya — neyi commit'leyeceğine bak
git remote add origin https://github.com/CihanCakir/inktavia-marine-provider-web.git
git push -u origin main

cd ~/Desktop/Mine/DEV/inktavia-marine-web
git status                                    # 108 dosya — .next/ ve node_modules gitignore'da mı?
git remote add origin https://github.com/CihanCakir/inktavia-marine-web.git
git push -u origin main
```

Push etmeden önce her iki repoda kontrol et: `.env.local` ve `.env.test` **gitignore'da mı?** (`inktavia-marine-provider-web/.env.test` ve üç repodaki `.env.local` dosyaları gerçek kimlik bilgisi içeriyor olabilir. Bir kez push edilirse git geçmişinden temizlemek acı verici.)

```bash
git check-ignore -v .env.local .env.test      # her satır bir kural göstermeli; göstermiyorsa DUR
```

### ✅ Faz 0 tamamlanma kriteri
- [ ] Üç frontend + backend, dördü de GitHub'da ve `git status` temiz
- [ ] Hiçbir repoda `.env.local` / `.env.test` / `.env` takip edilmiyor
- [ ] `inktavia-platform`'daki `refactor/adminpanel-bff-part3` dalı ya merge edildi ya push edildi

---

## FAZ 1 — Sunucu Kasanın Hazırlanması

**Süre:** ~1 gün (+ donanım tedarik süresi) · **Çıktı:** SSH ile erişilebilen, sabit IP'li Ubuntu VM

### 1.1 Donanım engellerini kaldır — *bunlar bitmeden Faz 2'ye geçme*

§1.4'teki iki bloker:

- [ ] **RAM 32 GB olarak görünüyor** (`msconfig` sınırı kaldırıldı / modüller yeniden oturtuldu / arızalı modül değişti)
- [ ] **1 TB SSD takıldı** (veya: SSD alınana kadar küçültülmüş plan bilinçli olarak kabul edildi)
- [ ] BIOS'ta **SVM (AMD-V) etkin** — Hyper-V bunsuz VM başlatamaz
- [ ] BIOS'ta **IOMMU** etkin (opsiyonel, ileride cihaz geçişi için)

```powershell
# Sanallaştırma açık mı?
systeminfo | findstr /C:"Hyper-V"
```

### 1.2 Hyper-V + VM
```powershell
# Yönetici PowerShell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All   # yeniden başlatma ister
New-VMSwitch -Name "InktaviaExternal" -NetAdapterName "Ethernet" -AllowManagementOS $true
New-VM -Name "inktavia-k3s" -Generation 2 -MemoryStartupBytes 24GB `
       -NewVHDPath "E:\VMs\inktavia-k3s.vhdx" -NewVHDSizeBytes 400GB -SwitchName "InktaviaExternal"
Set-VM -Name "inktavia-k3s" -ProcessorCount 8 -StaticMemory
Set-VMFirmware -VMName "inktavia-k3s" -EnableSecureBoot Off   # Ubuntu için
Set-VM -Name "inktavia-k3s" -AutomaticStartAction Start -AutomaticStartDelay 60
```
`E:\` = yeni 1 TB SSD. Boyutlandırma tablosu:

| Senaryo | VM RAM | vCPU | VHDX | Not |
|---|---|---|---|---|
| RAM düzeldi + yeni SSD var | 24 GB | 8 | 400 GB (yeni SSD) | Hedef yapılandırma |
| RAM düzeldi, SSD yok | 24 GB | 8 | 120 GB (mevcut SSD) + MinIO HDD'ye bind | Sıkışık ama çalışır |
| RAM 15,9'da kaldı | 11 GB | 8 | 120 GB | Yalnız tek namespace, observability kapalı — **demo, prod değil** |

Ubuntu Server 24.04 LTS ISO ile kur. Kurulumda: OpenSSH server ✅, LVM ✅, swap kapalı (k8s isteği).

### 1.3 VM içinde temel hazırlık
```bash
sudo apt update && sudo apt -y upgrade
sudo swapoff -a && sudo sed -i '/ swap / s/^/#/' /etc/fstab
sudo timedatectl set-timezone Europe/Istanbul
# Router'da MAC rezervasyonu ile sabit IP ver (örn. 192.168.1.50)
sudo ufw allow 22/tcp && sudo ufw allow 6443/tcp && sudo ufw enable
```

### ✅ Faz 1 tamamlanma kriteri
- [ ] Windows'tan `ssh cihan@192.168.1.50` çalışıyor
- [ ] VM Windows açılışında otomatik başlıyor (`AutomaticStartAction`)
- [ ] `free -h` en az 24 GB gösteriyor, swap 0
- [ ] Hyper-V checkpoint alındı: `Checkpoint-VM -Name inktavia-k3s -SnapshotName "clean-ubuntu"`

---

## FAZ 2 — k3s Cluster + Platform Katmanı

**Süre:** ~1 gün · **Çıktı:** `kubectl get nodes` → Ready, ingress-nginx ayakta

### 2.1 k3s kurulumu
```bash
curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC="\
  --disable traefik \
  --disable servicelb \
  --write-kubeconfig-mode 644 \
  --node-name inktavia-node-1" sh -
```
- `--disable traefik`: chart'lar `className: "nginx"` kullanıyor, iki ingress controller çakışır.
- k3s **NetworkPolicy'yi kutudan uygular** (kube-router tabanlı controller). Yine de F2.4'te *doğrula*, varsayma — `infrastructure/k8s/README.md`'nin ısrarla söylediği şey bu.

Mac'ine kubeconfig al:
```bash
scp cihan@192.168.1.50:/etc/rancher/k3s/k3s.yaml ~/.kube/inktavia.yaml
sed -i '' 's/127.0.0.1/192.168.1.50/' ~/.kube/inktavia.yaml
export KUBECONFIG=~/.kube/inktavia.yaml && kubectl get nodes
```

### 2.2 Namespace'ler ve etiketler
```bash
kubectl create namespace inktavia-prod
kubectl create namespace inktavia-dev
kubectl create namespace platform      # argocd, monitoring, cloudflared
```

### 2.3 Platform bileşenleri
```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo add metrics-server https://kubernetes-sigs.github.io/metrics-server/
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx -n ingress-nginx --create-namespace \
  --set controller.service.type=LoadBalancer \
  --set controller.metrics.enabled=true

helm install metrics-server metrics-server/metrics-server -n kube-system \
  --set args={--kubelet-insecure-tls}
```
Ardından `infrastructure/k8s/configmap-ingress-nginx-log.yaml` uygulanmalı — **atlanmamalı**: SignalR token'ı `?access_token=` ile geliyor, nginx varsayılanı her kullanıcının canlı token'ını erişim log'una yazar.

### 2.4 NetworkPolicy doğrulaması (kritik)
```bash
kubectl apply -f infrastructure/k8s/networkpolicy-internal-modules.yaml -n inktavia-prod
# Rastgele bir pod'dan modüle ulaşılamamalı:
kubectl -n inktavia-prod run probe --rm -it --image=curlimages/curl --restart=Never -- \
  curl -sS -m 5 http://identity-api:8080/health
# BEKLENEN: timeout / connection refused.  200 dönerse F6'ya geçme.
```

### ✅ Faz 2 tamamlanma kriteri
- [ ] `kubectl get nodes` → `Ready`
- [ ] ingress-nginx pod'u Running, `EXTERNAL-IP` atanmış
- [ ] NetworkPolicy testi **timeout** veriyor
- [ ] Erişim log'unda `access_token=` sayısı 0

---

## FAZ 3 — Stateful Altyapı (Cluster Dışı)

**Süre:** ~1 gün · **Çıktı:** VM host'unda çalışan, yedeklenen veri katmanı

Aynı VM'in host tarafında `docker compose` ile: PostgreSQL 16, MongoDB, Redis, RabbitMQ, MinIO, Keycloak. Mevcut `docker-compose.yaml`'dan **yalnızca infra servisleri** ayıklanarak `infrastructure/host-stack/docker-compose.prod.yaml` üretilecek (uygulama servisleri artık k8s'te).

Kritik noktalar:
- Bind mount'lar `/srv/inktavia/{pgdata,mongo,minio,rabbitmq}` altında — VM disk'inde, yedeklenebilir.
- Servisler **yalnızca** VM'in özel IP'sine bind edilir (`127.0.0.1` değil, `192.168.1.50`), çünkü k8s pod'ları bu adrese bağlanacak.
- Keycloak `KC_HOSTNAME=id.inktavia.com`, `KC_PROXY=edge`, realm import + OTP SPI jar'ı mount edilir (`infrastructure/keycloak/providers/`).
- Pod'lardan erişim için k8s'te `ExternalName`/headless Service tanımları: `postgres.inktavia-prod.svc` → `192.168.1.50:5432`.
- **Günlük yedek:** `pg_dump` + `mongodump` + MinIO `mc mirror` → ikinci fiziksel disk **ve** haftalık dış kopya (Backblaze B2 / harici disk). Tek kasadasın; RAID yedek değildir.

### ✅ Faz 3 tamamlanma kriteri
- [ ] Cluster içindeki bir test pod'u Postgres'e bağlanabiliyor
- [ ] Keycloak `id.inktavia.com` üzerinden açılıyor, realm yüklü, OTP SPI aktif
- [ ] `restore` provası **yapıldı** (yedek alınan bir dump boş bir DB'ye geri yüklendi)

---

## FAZ 4 — GHCR + Versiyonlama Sözleşmesinin Yürürlüğe Girmesi

**Süre:** ~0.5 gün · **Çıktı:** Elle push edilmiş ilk image, cluster'dan pull edilebiliyor

```bash
# GitHub'da: Settings → Developer settings → PAT (classic) → scope: write:packages, read:packages
echo $CR_PAT | docker login ghcr.io -u CihanCakir --password-stdin

docker build -f Modules/Identity/build/Dockerfile -t ghcr.io/cihancakir/inktavia/identity-api:sha-abc1234 .
docker push ghcr.io/cihancakir/inktavia/identity-api:sha-abc1234
```

Cluster'a pull secret:
```bash
kubectl create secret docker-registry ghcr-pull \
  --docker-server=ghcr.io --docker-username=CihanCakir --docker-password=$CR_PAT \
  -n inktavia-prod
```
Tüm chart'ların `values-prod.yaml`'ında: `imagePullSecrets: [{name: ghcr-pull}]` (F6).

### ✅ Faz 4 tamamlanma kriteri
- [ ] GHCR'da `identity-api` paketi görünüyor ve **private**
- [ ] Cluster'da elle bir `kubectl run` ile bu image pull edilebiliyor
- [ ] §3'teki versiyonlama sözleşmesi `docs/DevOps/VERSIONING.md` olarak repoya yazıldı

---

## FAZ 5 — CI Pipeline (Build · Test · Image · Scan)

**Süre:** ~2 gün · **Çıktı:** Her push'ta 17 image otomatik üretiliyor

### 5.1 Önce Dockerfile'ları düzelt (G8)
Mevcut kalıp her image'ı iki kez derliyor:
```dockerfile
RUN dotnet build   ...   # ← gereksiz, publish zaten derliyor
RUN dotnet publish ...
```
Hedef kalıp (tek modül örneği, cache dostu):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.sln ./
COPY Core/ Core/
COPY Modules/Identity/ Modules/Identity/
RUN dotnet restore "Modules/Identity/src/.../Aizen.Modules.Identity.Api.csproj"
COPY . .
RUN dotnet publish "Modules/Identity/src/.../Aizen.Modules.Identity.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" --uid 64198 app && chown -R app /app
USER app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Aizen.Modules.Identity.Api.dll"]
```
Ayrıca kök dizine `.dockerignore` (bin/obj/.git/node_modules/docs) — build context'i onlarca MB'tan MB'lara indirir.

### 5.2 Workflow yapısı

| Workflow | Tetik | İş |
|---|---|---|
| `ci.yml` (mevcut, genişletilecek) | PR + `develop` push | restore · build · test · `dotnet format --verify` |
| `images-dev.yml` (yeni) | `develop` push | Değişen bileşenleri tespit et → build → `sha-<hex>` + `dev` push |
| `images-release.yml` (yeni) | `v*` tag | **Tüm** bileşenleri build → `sha-<hex>` + `1.4.2` + `1.4` + `1` push → Trivy tara → cosign imzala |
| `gitops-bump.yml` (yeni) | image push sonrası | GitOps repo'sundaki `values`'a yeni tag'i yaz + PR aç |

Matrix iskeleti:
```yaml
strategy:
  fail-fast: false
  matrix:
    include:
      - name: identity-api    ; dockerfile: Modules/Identity/build/Dockerfile
      - name: payment-api     ; dockerfile: Modules/Payment/build/Dockerfile
      # ... 11 modül + 4 BFF + gateway
steps:
  - uses: docker/setup-buildx-action@v3
  - uses: docker/login-action@v3
    with: { registry: ghcr.io, username: ${{ github.actor }}, password: ${{ secrets.GITHUB_TOKEN }} }
  - uses: docker/build-push-action@v6
    with:
      context: .
      file: ${{ matrix.dockerfile }}
      tags: ghcr.io/cihancakir/inktavia/${{ matrix.name }}:sha-${{ steps.vars.outputs.sha7 }}
      cache-from: type=gha
      cache-to: type=gha,mode=max
      provenance: true
```

### 5.3 Runner kararı
Başlangıç: **GitHub-hosted runner**. Private repoda ücretsiz Actions dakikası sınırlıdır ve 17 image'lık matrix onu hızlı tüketir. Tüketmeye başladığında: VM'de ikinci bir hafif VM veya container olarak **self-hosted runner** kurulur — o noktada dakika maliyeti sıfırlanır ve build cache lokal kalır. Bunu şimdi yapma; ihtiyaç doğunca yap.

### ✅ Faz 5 tamamlanma kriteri
- [ ] `develop`'a push → 17 image GHCR'da `sha-<hex>` ile
- [ ] Build süresi cache ile < 8 dk
- [ ] Trivy `HIGH/CRITICAL` bulgusu build'i kırıyor (allowlist ile)
- [ ] Aynı commit iki kez build edilince aynı digest çıkıyor (tekrarlanabilirlik)

---

## FAZ 6 — Helm Chart Standardizasyonu ⚠️ *En kritik faz*

**Süre:** ~1–2 gün · **Çıktı:** Deploy edilebilir, güvenli chart seti

### 6.1 G3 — Modül ingress'lerini kapat (güvenlik açığı)
`Modules/*/deploy/*/values-{dev,test,prod}.yaml` içinde:
```yaml
ingress:
  enabled: false     # modüller ASLA dışarı açılmaz
```
Bu şu an `true`. `infrastructure/k8s/README.md`'nin dediği gibi: modüle ulaşabilen + paylaşılan sırrı bilen biri `X-Aizen-User-Id` başlığını değiştirerek **herhangi bir kullanıcı gibi davranabilir**. Ingress'i açık bırakmak, NetworkPolicy'nin koruduğu şeyi kapının önüne koymak demek. Bunu bir CI kontrolüyle kalıcılaştır:
```bash
# scripts/ci/assert-no-module-ingress.sh
! grep -rn -A1 "^ingress:" Modules/*/deploy/*/values-*.yaml | grep -q "enabled: true"
```

### 6.2 Registry ve tag
```yaml
image:
  repository: ghcr.io/cihancakir/inktavia/identity-api
  pullPolicy: IfNotPresent          # 'Always' + sabit tag anlamsız
  tag: "sha-abc1234"                # Argo CD/CI tarafından yazılır, 'latest' YOK
imagePullSecrets:
  - name: ghcr-pull
```

### 6.3 Kaynak limitleri (G5)
Tek node'da limitsiz pod = tüm cluster'ı düşürme yetkisi. Başlangıç değerleri:
```yaml
resources:
  requests: { cpu: 100m, memory: 256Mi }
  limits:   { cpu: 1000m, memory: 768Mi }   # BFF'ler ve Identity için 1Gi
```
Ayrıca namespace başına `ResourceQuota` + `LimitRange`.

### 6.4 Etiketler, probe'lar, güvenlik bağlamı
```yaml
podLabels:
  aizen.io/tier: module      # BFF chart'larında: bff  → NetworkPolicy bunlara dayanıyor
securityContext:
  runAsNonRoot: true
  runAsUser: 64198
  readOnlyRootFilesystem: true
  allowPrivilegeEscalation: false
  capabilities: { drop: [ALL] }
```
Probe'lar `/info`'ya bakıyor — `startupProbe` ekle (EF Core migration'lı ilk açılış `/info`'yu geciktirebilir, aksi halde liveness pod'u sonsuz döngüde öldürür).

### 6.5 Chart sürümleri
Tüm chart'larda `version: 0.1.0` / `appVersion: "1.16.0"` scaffold artığı → `version: 1.0.0`, `appVersion` release treninden.

### ✅ Faz 6 tamamlanma kriteri
- [ ] `helm template` ile render edilen hiçbir modül chart'ında `kind: Ingress` yok
- [ ] `assert-no-module-ingress.sh` CI'da koşuyor ve engelleyici
- [ ] Tüm chart'larda GHCR repository + `sha-` tag + pull secret
- [ ] `kubectl top pods` tüm pod'larda limit gösteriyor
- [ ] Bir modül elle `helm upgrade --install` ile prod namespace'e inip Running oluyor

---

## FAZ 7 — CD: Argo CD + GitOps

**Süre:** ~2 gün · **Çıktı:** Git'e yazılan her değişiklik cluster'a otomatik iniyor

### 7.1 Argo CD kurulumu
```bash
helm repo add argo https://argoproj.github.io/argo-helm
helm install argocd argo/argo-cd -n platform \
  --set configs.params."server\.insecure"=true    # TLS'i ingress/Cloudflare sonlandırıyor
```

### 7.2 GitOps dizin yapısı (aynı repo, `gitops/` altında)
```
gitops/
├── apps/
│   ├── prod/
│   │   ├── identity.yaml          # Argo Application → chart yolu + values-prod + tag
│   │   ├── payment.yaml
│   │   └── ...
│   └── dev/
├── values/
│   ├── prod/identity.yaml         # ortam-özel override (tag burada)
│   └── dev/identity.yaml
└── root-app.yaml                  # app-of-apps
```

`app-of-apps` deseni: tek bir kök Application tüm alt Application'ları yönetir → yeni modül eklemek = bir YAML dosyası.

### 7.3 Deploy akışı
```
kod → PR → develop merge
   → CI: test + image build → ghcr sha-abc1234
   → gitops-bump: gitops/values/dev/identity.yaml içindeki tag → sha-abc1234 (otomatik commit)
   → Argo CD fark görür → inktavia-dev namespace'ine uygular
   → doğrulandı → v1.4.2 tag → release workflow → gitops/values/prod/* PR
   → PR'ı SEN onaylarsın (prod'daki tek manuel kapı)
   → Argo CD prod'a uygular
```

Prod'a giden PR onayı bilerek manuel: tek node'lu, tek operatörlü bir kurulumda tam otomatik prod deploy, hatayı fark etmeden yayına almak demek.

### 7.4 Rollback
```bash
git revert <gitops-commit>   # tek doğru yol; Argo CD önceki tag'e döner
```
`kubectl rollout undo` **kullanma** — Argo CD bir sonraki sync'te geri alır (git kaynak gerçek).

### ✅ Faz 7 tamamlanma kriteri
- [ ] `argocd.inktavia.com` açılıyor, tüm Application'lar `Synced/Healthy`
- [ ] `develop`'a bir commit → 10 dk içinde dev namespace'te yeni pod
- [ ] Bilerek bozuk bir tag push edilip `git revert` ile geri dönüş **prova edildi**

---

## FAZ 8 — Secret Yönetimi

**Süre:** ~0.5 gün

```bash
helm repo add sealed-secrets https://bitnami-labs.github.io/sealed-secrets
helm install sealed-secrets sealed-secrets/sealed-secrets -n kube-system
brew install kubeseal
```
```bash
kubectl create secret generic bff-assertion \
  --from-literal=SharedSecret="$(openssl rand -base64 48)" \
  --dry-run=client -o yaml | kubeseal --format yaml > gitops/secrets/prod/bff-assertion.yaml
# ↑ bu dosya git'e commit edilebilir; sadece cluster'daki private key açabilir
```

Taşınacak sırlar: `BffAssertion__SharedSecret`, Postgres/Mongo/Redis/RabbitMQ kimlikleri, MinIO anahtarları, Keycloak client secret'ları, iyzico API anahtarları, VAPID web-push anahtarları, GHCR pull secret.

⚠️ **Sealed Secrets private key'i mutlaka yedekle** — kaybedersen commit edilmiş hiçbir secret bir daha açılmaz:
```bash
kubectl -n kube-system get secret -l sealedsecrets.bitnami.com/sealed-secrets-key -o yaml > ~/sealed-secrets-master.key   # OFFLINE sakla
```

### ✅ Faz 8 tamamlanma kriteri
- [ ] `.env` içindeki hiçbir prod sırrı chart/ConfigMap'te düz metin değil
- [ ] Master key iki ayrı offline konumda
- [ ] `BffAssertion__AllowedClientIds` her modülde dolu (boşsa servis açılmamalı — README bunu şart koşuyor)

---

## FAZ 9 — Domain, TLS, Cloudflare Tunnel

**Süre:** ~1 gün

1. `inktavia.com` nameserver'larını Cloudflare'a taşı (registrar panelinden).
2. Tunnel oluştur → token'ı Sealed Secret olarak sakla → `cloudflared` deployment'ı `platform` namespace'inde.
3. Tunnel ingress kuralı: tüm host'lar → `http://ingress-nginx-controller.ingress-nginx.svc:80`.
4. Cloudflare DNS: §4'teki her host için CNAME → tunnel.
5. SSL/TLS modu **Full (strict)** değil, tunnel kullanıldığı için origin sertifikası gerekmez; Cloudflare ↔ cloudflared zaten şifreli.
6. `argocd`, `grafana`, `*.dev` için **Cloudflare Access** politikası (yalnızca senin e-postan).
7. WebSocket açık olmalı (Provider Portal SignalR) — Cloudflare varsayılan olarak destekler, doğrula.
8. Rate limiting: `id.inktavia.com/realms/*/protocol/openid-connect/token` ve OTP uçları için.

### ✅ Faz 9 tamamlanma kriteri
- [ ] `https://inktavia.com` geçerli sertifikayla açılıyor
- [ ] Router'da **hiçbir** port forward tanımı yok
- [ ] `admin.inktavia.com` Access ile korunuyor
- [ ] Provider Portal'da canlı mesajlaşma (WebSocket) çalışıyor
- [ ] Modül host'ları dışarıdan denenince NXDOMAIN

---

## FAZ 10 — Frontend Pipeline'ları

**Süre:** ~2 gün · **Ön koşul:** Faz 0 (repolar GitHub'da)

**Sıralama önemli:** `partner.` → `admin.` → `inktavia.com`. Provider Portal ve Admin olgun; Marine Web kendi 13 fazlık roadmap'inin 1. fazında, içeriği v1 için hazır değil. Ana siteyi en sona bırakmak hem riski düşürür hem de altyapıyı iki çalışan uygulamayla prova etmiş olursun.

| # | Uygulama | Repo | Image | Host |
|---|---|---|---|---|
| 10a | Provider Portal (Vite PWA) | `inktavia-marine-provider-web` | `web-provider` | `partner.inktavia.com` |
| 10b | Admin Panel (Vite SPA) | `inktavia-marine-admin-web` | `web-admin` | `admin.inktavia.com` |
| 10c | Inktavia Web (Next.js SSR) | `inktavia-marine-web` | `web-marine` | `inktavia.com` |

### 10a/10b — Vite SPA Dockerfile şablonu

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Vite env değişkenleri BUILD-TIME gömülür — ortam başına ayrı image demektir
ARG VITE_PROVIDER_BFF_BASE_URL
ARG VITE_KEYCLOAK_URL
ARG VITE_KEYCLOAK_REALM
ARG VITE_KEYCLOAK_CLIENT_ID
ARG VITE_APP_ENV=production
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf   # SPA fallback: try_files $uri /index.html
EXPOSE 8080
```

### ⚠️ Vite'ın build-time env sorunu

`VITE_*` değişkenleri derleme anında bundle'a **gömülür**. Bu şu anlama gelir: aynı image dev ve prod'da kullanılamaz — `sha-abc1234` etiketli image yalnızca derlendiği ortamın URL'lerini bilir. Üç seçenek:

| Seçenek | Nasıl | Değerlendirme |
|---|---|---|
| **A. Ortam başına image** | `web-provider:sha-abc1234-prod`, `...-dev` | Basit ama "dev'de test ettiğim tam olarak prod'a gitmiyor" — image değişmezliğini bozar |
| **B. Runtime config.js** ✅ | `index.html` içinde `<script src="/config.js">`; nginx bunu ConfigMap'ten servis eder; kod `window.__ENV__`'den okur | Tek image her ortamda. ~20 satırlık değişiklik. **Önerilen** |
| **C. Aynı origin path-based** | `/api/v1/...` göreli yol | En temiz ama §4'te ayrı-host modeli seçildi |

**B için gereken:** `src/config/env.ts` içinde `import.meta.env.VITE_X` yerine `window.__ENV__.X ?? import.meta.env.VITE_X` (fallback ile dev bozulmaz) + chart'ta bir ConfigMap. Bunu Faz 10a'da bir kez yap, ikinci uygulamada kopyala.

### 10a özel — PWA service worker

Provider Portal bir PWA (`dev-dist/` klasörü var). Yeni sürüm yayınlandığında kullanıcılar eski bundle'da kalabilir. Gerekli:
- `index.html` ve `sw.js` için `Cache-Control: no-cache` (nginx), asset'ler için uzun cache (Vite zaten hash'li isim üretir)
- Uygulama içinde "yeni sürüm var, yenile" bildirimi (`workbox` update prompt)
- SignalR + MapLibre: ingress'te WebSocket upgrade açık olmalı (F9)

### 10c — Next.js SSR

`next.config.ts`'e `output: 'standalone'` eklenmeli (şu an yok):
```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ENV SITE_ENV=production
ARG SITE_URL                      # D-05: prod build'de zorunlu, boşsa build KIRILMALI
RUN npm run build                 # typecheck + i18n parity kontrolünü de koşturur

FROM node:22-alpine AS final
WORKDIR /app
ENV NODE_ENV=production PORT=8080
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
EXPOSE 8080
CMD ["node", "server.js"]
```
`SITE_URL` ve `MARINE_WEB_BFF_BASE_URL` **server-side** okunuyor → runtime env, build ARG değil (`SITE_URL` hariç; D-05 gereği build'de doğrulanmalı). Bu, Vite'daki sorunu yaşamaz.

### Frontend CI (her repoda ayrı workflow)

```
PR       → npm ci · typecheck · lint · test · build          (image yok)
develop  → yukarıdakiler + image build → ghcr sha-<hex> + dev
v*.*.* tag → image + semver tag → gitops PR
```
Backend'in `gitops-bump` akışıyla aynı GitOps repo'suna yazarlar — böylece "prod'da hangi frontend sürümü var?" sorusu da tek git commit'inden okunur.

### ✅ Faz 10 tamamlanma kriteri
- [ ] Üç repoda da CI yeşil, image GHCR'da
- [ ] `partner.inktavia.com` üzerinden giriş + canlı mesajlaşma (WebSocket) çalışıyor
- [ ] Aynı image dev ve prod namespace'inde farklı ConfigMap ile çalışıyor (Seçenek B doğrulaması)
- [ ] Yeni sürüm yayınlandığında PWA kullanıcısı güncelleme uyarısı alıyor
- [ ] BFF'lerin `AllowedOrigins`'inde wildcard yok

---

## FAZ 11 — Gözlemlenebilirlik, Yedek, DR

**Süre:** ~1–2 gün

- **Metrik:** kube-prometheus-stack (Prometheus + Grafana + Alertmanager). Mevcut `observability/` klasöründeki dashboard'lar taşınır.
- **Log:** Loki + Promtail (tek node'da ELK ağırdır).
- **Alarm:** pod CrashLoop, node disk > %80, sertifika < 14 gün, RabbitMQ kuyruk birikmesi, Postgres bağlantı doygunluğu → e-posta/Telegram.
- **Yedek:** Faz 3'teki günlük dump + haftalık dış kopya + Hyper-V checkpoint (aylık, upgrade öncesi).
- **DR provası:** VM'i sıfırdan kurup yedekten dönme senaryosunu **bir kez uçtan uca yap**. Denenmemiş yedek yedek değildir.

---

## FAZ 12 — Go-Live

- [ ] Yük testi (`load-test/` klasöründeki k6 senaryoları) prod benzeri ortamda koşturuldu
- [ ] `payment-smoke-test.md` senaryosu prod'da (test modunda) geçti
- [ ] iyzico IYZWSv2 imzalama + approval endpoint blocker'ı çözüldü *(P9 kapısı — bkz. `docs/V1.0.1/Payment/IYZICO_API_ALIGNMENT.md`)*
- [ ] Rollback provası yapıldı
- [ ] Runbook yazıldı: "X çöktü → ne yapılır" (`docs/DevOps/RUNBOOK.md`)
- [ ] KVKK/veri saklama: log'larda kişisel veri, yedeklerin şifrelenmesi

---

## 5. Özet Zaman Çizelgesi

| Faz | İş | Süre | Bağımlılık |
|---|---|---|---|
| **0** | **Repoları GitHub'a al (acil)** | **1 s** | — |
| 1 | Kasa + Hyper-V + Ubuntu VM | 1 g | **RAM + SSD** |
| 2 | k3s + ingress + NetworkPolicy | 1 g | F1 |
| 3 | Stateful infra + yedek | 1 g | F1 |
| 4 | GHCR + versiyonlama sözleşmesi | 0.5 g | — *(paralel yapılabilir)* |
| 5 | CI: Dockerfile + image pipeline | 2 g | F4 |
| 6 | **Helm standardizasyonu (güvenlik)** | 1–2 g | F4 |
| 7 | Argo CD + GitOps | 2 g | F2, F5, F6 |
| 8 | Sealed Secrets | 0.5 g | F2 |
| 9 | Domain + Cloudflare Tunnel | 1 g | F2 |
| 10 | Frontend pipeline'ları | 2 g | F0, F5, F9 |
| 11 | Observability + DR | 1–2 g | F7 |
| 12 | Go-live | 1 g | hepsi |

**Toplam:** ~14–17 çalışma günü. Takvim süresi, günde ayırdığın saate bağlı.

**Paralelleştirme:** F4+F5+F6 (yazılım tarafı, Mac'te) ile F1+F2+F3 (donanım tarafı, kasada) aynı hafta ilerleyebilir.

---

## 6. Kritik Yol

```
F1 → F2 → F6 → F7 → F9 → F12
        ↘ F5 ↗
```
F6 kritik yolda ve aynı zamanda tek gerçek **güvenlik borcu**. Modül ingress'leri kapatılmadan hiçbir şey internete açılmamalı.

---

## 7. Riskler

| # | Risk | Etki | Önlem |
|---|---|---|---|
| **R0** | **İki frontend repo yalnızca lokal diskte** (G9) | Disk arızası = iki proje tamamen kayıp | **Faz 0 — bugün** |
| **R1a** | **RAM 15,9 GB kullanılabilir** (32 GB takılı) | Tam yığın çalışmaz | §1.4'teki teşhis sırası; çözülmezse Plan B (demo ortamı) |
| **R1b** | **SSD 233 GB, HDD veritabanı için uygun değil** | k3s/etcd + Postgres kabul edilemez yavaş | 1 TB SSD ekle (en yüksek getirili tek harcama) |
| R1c | Ryzen 1600 = 6 çekirdek, 2017 | Build ve yük altında dar | VM'e 8 vCPU; CI'ı GitHub-hosted'da tut (kasada build etme) |
| R2 | **Windows 10 destek dışı** (Ekim 2025'te bitti) | Güvenlik yaması yok | Cloudflare Tunnel sayesinde host internete açık değil; yine de orta vadede Windows 11 veya bare-metal Linux planla |
| R3 | **Tek kasa = tek arıza noktası** | Toplam kesinti | UPS + test edilmiş yedek + "kaç saat kesinti kabul edilebilir?" sorusunu şimdi cevapla |
| R4 | **Ev/ofis interneti** | Upload hızı ve kesintiler | Yedek hat (mobil) veya kritik anda VPS'e failover planı |
| R5 | **Modül ingress'i açık** (G3) | Kimlik taklidi | F6 + CI kontrolü |
| R6 | **Statik BFF assertion sırrı** | Sızarsa süresiz geçerli | README'de kayıtlı: kısa ömürlü imzalı JWT'ye geçiş — F12 sonrası ilk teknik borç |
| R7 | **Sealed Secrets key kaybı** | Tüm secret'lar geri getirilemez | İki offline kopya (F8) |
| R8 | GHCR Actions dakika limiti | CI durur | Self-hosted runner'a geç (F5.3) |

---

## 8. Bu Roadmap'in Yaratacağı Yeni Dosyalar

```
.github/workflows/images-dev.yml
.github/workflows/images-release.yml
.github/workflows/gitops-bump.yml
.dockerignore
scripts/ci/assert-no-module-ingress.sh
infrastructure/host-stack/docker-compose.prod.yaml
infrastructure/cloudflared/
gitops/root-app.yaml
gitops/apps/{prod,dev}/*.yaml
gitops/values/{prod,dev}/*.yaml
gitops/secrets/prod/*.yaml            (sealed)
docs/DevOps/VERSIONING.md
docs/DevOps/RUNBOOK.md
```

---

## 9. Açık Kararlar (senden cevap bekleyen)

| # | Soru | Durum |
|---|---|---|
| ~~A1~~ | Frontend repoları nerede | ✅ **Cevaplandı** — §1.3 |
| ~~A2~~ | Kasanın donanımı | ✅ **Cevaplandı** — §1.4 (iki bloker çıktı) |
| **A3** | **Gateway'in rolü ne?** `Gateway/` var ama `docker-compose.yaml`'da servisi yok. Kullanımda mı, ölü kod mu? | Public mi internal mi olacağı ingress haritasını değiştirir; ölüyse bir image eksilir |
| **A4** | **Kabul edilebilir kesinti?** Saatler mi, günler mi? | Yedek sıklığı ve failover yatırımını belirler |
| **A5** | **`nexus.local` gerçekten öldü mü?** | Ölü değilse geçiş planına eklenir |
| **A6** | **`aizen-bff` (`Bff/src/Aizen.Bff`) hâlâ canlı mı?** | 5. bir image üretilecek mi, yoksa devre dışı mı |
| **A7** | **Admin API host adı** `marine-os-admin-api.` mı kalsın, `admin-api.` mı olsun? | Tek satırlık değişiklik, sadece tutarlılık |
| **A8** | **Vite runtime-config (Seçenek B) kabul mü?** ~20 satır frontend değişikliği | Kabul edilmezse ortam başına ayrı image üretmek zorundayız |

---

## 10. Bir Sonraki Adım

Sıra artık net, çünkü iki cevabın planı değiştirdi:

**1. BUGÜN — Faz 0 (1 saat, senin işin).** İki frontend repo yalnızca senin diskinde duruyor ve o disk, birazdan söküp yeniden yapılandıracağın makineyle aynı ekosistemde. GitHub'a push et. Bundan önce `.env.local` / `.env.test` gitignore kontrolünü atlama.

**2. BU HAFTA — Donanım (senin işin).** `msconfig` bellek sınırını kontrol et; 32 GB göründüğünde haber ver. 1 TB SSD siparişini de bu arada ver — Faz 2'ye kadar elinde olsun.

**3. PARALELDE — F6.1 + F4 (birlikte yapacağımız iş, kasa beklemeden).**
- **F6.1:** 11 modül chart'ında `ingress.enabled: false` + `scripts/ci/assert-no-module-ingress.sh`. Bu açık duran güvenlik borcunu kapatır ve donanımdan bağımsız.
- **F4:** GHCR'a elle bir `identity-api` image'ı push — tüm zinciri 20 dakikada doğrular.

F6.1 ile başlamayı öneriyorum: en kısa, en yüksek değerli ve hiçbir şeye bağlı değil. "Başla" dediğinde 11 chart'ın düzenlemesini ve CI kontrol script'ini yazarım.
