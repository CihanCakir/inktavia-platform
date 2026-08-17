# Image inşası — sözleşme

Bu dizin, 15 .NET bileşeninin (11 modül + 4 BFF) container imajlarını üretir.

| Dosya | İşi |
|---|---|
| `components.json` | **Tek gerçek kaynak.** Bileşen kimliği, csproj yolu, dll adı, Helm chart yolu. |
| `Dockerfile` | Hepsi için tek parametrik Dockerfile. Bileşene özel ne varsa build-arg. |
| `../.dockerignore` | Build context'i yalnız `Core/`, `Modules/`, `Bff/` ile sınırlar. |
| `../.github/workflows/images.yml` | CI: PR'da inşa eder, `develop`'a merge'de GHCR'a iter. |

## Adlandırma ve tag

```
ghcr.io/cihancakir/inktavia/<id>:<tag>
```

| Tag | Ne zaman | Değişebilir mi |
|---|---|---|
| `sha-<7hex>` | her `develop` push'unda | ❌ **Deploy edilen tek tag türü budur.** |
| `develop` | `develop` push'unda | ✅ yalnız insan gözü için |
| `latest` | — | 🚫 **yasak, üretilmiyor** |

`latest` yasak çünkü "hangi kod çalışıyor?" sorusunun cevabını yok eder; rollback ve olay
incelemesi imkânsızlaşır. Chart'lar her zaman `sha-` tag'i alır.

## Yeni bileşen eklemek

`components.json`'a bir satır ekle. Başka hiçbir yere dokunma — CI matrisi, compose ve
deploy hepsi oradan okuyor.

```json
{ "id": "yenimodul", "kind": "module",
  "project": "Modules/YeniModul/src/Aizen.Modules.YeniModul/Aizen.Modules.YeniModul.csproj",
  "dll": "Aizen.Modules.YeniModul.dll",
  "chart": "Modules/YeniModul/deploy/aizen-yenimodul" }
```

## Elle inşa

```bash
docker build -f build/Dockerfile \
  --build-arg PROJECT=Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj \
  --build-arg DLL=Aizen.Modules.Identity.dll \
  -t inktavia/identity:local .
```

> ⚠️ **Apple Silicon Mac'te `--platform linux/amd64` eklemeyi unutma.** Sunucu Ryzen (amd64);
> arm64 imaj orada `exec format error` verir. En güvenlisi imajı CI'da üretmektir.

## Neden 15 ayrı Dockerfile değil

Eskiden her bileşenin kendi Dockerfile'ı vardı ve bu üç somut soruna yol açmıştı:

1. **`Modules/Profile/build/Dockerfile` hiç çalışmıyordu.** Kendi dizinini build context
   alıyordu (`COPY ./src ./src`), ama Profile'ın csproj'u `../../../../Core/...` ve
   `../../../Payment/...` referansı veriyor. Kimse fark etmemişti çünkü Profile
   `docker-compose.yaml`'da da yoktu.
2. **Her Dockerfile iki kez derliyordu** — önce `dotnet build`, sonra `dotnet publish`.
   İkincisi birincinin işini baştan yapar; derleme süresinin yarısı boşaydı.
3. **BFF'ler `COPY . .` yapıyordu** ve `.dockerignore` yoktu; `.git`, `docs`, `bin`, `obj`
   build context'ine gidiyordu.

15 kopyayı senkronda tutmak bu hataların kaynağıydı. Tek dosya + `components.json` bunu
yapısal olarak imkânsız kılıyor.

## Seçici derleme — hangi imaj ne zaman derlenir

Her push'ta 15 imajı birden derlemiyoruz. `scripts/ci/affected-components.py` değişen
dosyalardan yalnızca etkilenen bileşenleri hesaplıyor.

**Dizin adına bakmak yetmez.** `Modules/Payment/src/Aizen.Modules.Payment.Abstraction`
içindeki bir değişiklik `Profile`'ı da etkiler, çünkü Profile ona `ProjectReference` veriyor.
"Payment değişti → payment derle" demek 13 imajı bayat bırakırdı. Bu yüzden script tüm
`.csproj` dosyalarından gerçek referans grafiğini çıkarıyor ve her bileşenin **geçişli**
bağımlılık kümesini hesaplıyor.

| Değişiklik | Derlenen |
|---|---|
| `Modules/CargoDry/src/**` | 1 (cargodry) |
| `Bff/src/AdminPanel/**` | 1 (bff-adminpanel) |
| `Modules/Payment/src/*.Abstraction/**` | 14 (Payment'a bağlı her şey) |
| `Core/**` | 15 |
| `build/Dockerfile`, `build/components.json`, `.dockerignore` | 15 |
| `*/deploy/**`, `docs/**`, `scripts/**`, `*.md` | **0 — iş atlanır** |
| Karşılaştırma noktası yok (yeni dal, force-push) | 15 |

Son satır kasıtlı: neyin değiştiğini bilemediğimizde "hiç derleme" değil "hepsini derle"
diyoruz. Fazladan derleme birkaç dakika, bayat imajla deploy ise sessiz bir hata.

Aynı şekilde, kaynak ağacında bir dosya hiçbir projeye eşlenemezse script yine hepsini
derliyor — tanımadığı bir şeyi görmezden gelmiyor.

### Elle sınamak

```bash
echo "Modules/Payment/src/Aizen.Modules.Payment.Abstraction/X.cs" \
  | python3 scripts/ci/affected-components.py
```

stdout'a CI matrisi, stderr'e hangi bileşenlerin neden seçildiği yazılır.

## Bilinen borçlar (Faz 5/6)

- Katman önbelleği: önce yalnız `.csproj`'ları kopyalayıp restore etmek, sonra kaynağı
  kopyalamak kod değişikliklerinde restore'u atlatır.
- `USER $APP_UID` ile root olmayan çalışma — Faz 6 güvenlik bağlamıyla birlikte.
- Trivy imaj taraması — Faz 5.
- Chart'lardaki `nexus.local:8083` + `tag: latest` hâlâ duruyor — Faz 6'da bu sözleşmeye
  çekilecek, `imagePullSecrets` de `ghcr-pull` ile doldurulacak.
