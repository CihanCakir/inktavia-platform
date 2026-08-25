#!/usr/bin/env bash
# ══════════════════════════════════════════════════════════════════════════════
# FAZ20 (#55) — Frontend surum isaretci denetimi.
#
# SORUN: gitops/apps/web-*-app.yaml icindeki image.tag ELLE guncelleniyor.
# Kusur, elle guncelleme degil; UNUTMANIN SESSIZ olmasi: merge edersin, frontend CI
# yeni image yayinlar, ama bu isaretci eski sha'da kalir ve cluster sessizce eski
# image'i servis etmeye devam eder — hicbir yerde sinyal yok.
#
# COZUM: SALT-OKUR bir CI denetimi. Her web-*-app.yaml icin, sabitlenmis image.tag'in
# manifest digest'ini GHCR'daki en yeni ':dev' digest'i ile kiyaslar; tutmuyorsa patlar.
#
# ──────────────────────────────────────────────────────────────────────────────
# 🔴 2026-08-22 DERSI — bu betigin ILK surumu YESIL kosuyordu ama HICBIR SEY
#    kiyaslamiyordu. Kosu ciktisi aynen soyleydi:
#        ⚠️  web-admin-app.yaml:    ...:dev GHCR'da yok — kiyas atlandi.
#        ⚠️  web-provider-app.yaml: ...:dev GHCR'da yok — kiyas atlandi.
#        ✅ Tum frontend surum isaretcileri guncel.
#    Iki ayri kusur vardi:
#
#    (1) YANLIS VARSAYIM: "public GHCR image → anonim pull token yeter". Image'lar
#        ozel — chart values'lari `imagePullSecrets: ghcr-pull` istiyor. Anonim
#        token bu paketleri goremez, dolayisiyla denetim HER ZAMAN atlardi.
#        → Artik GITHUB_TOKEN ile kimlik dogruluyor (`permissions: packages: read`).
#
#    (2) SESSIZ BASARI YALANI: hicbir sey kiyaslanmadigi halde "hepsi guncel"
#        yaziyordu. Bu, bu projede kapatmaya calistigimiz desenin (#26/#34/#67)
#        bizzat onu onlemek icin yazilan bekcinin icinde tekrar etmesiydi.
#        → Artik: 0 kiyas = HATA. Ozet daima "N kiyaslandi, M atlandi" der.
#
#    Ayrica eski surumde token hatasi, 401 ve 404 AYNI mesaji basiyordu — #62'deki
#    `|| echo "skipped (already present or roles absent)"` hatasinin aynisi.
#    → Artik uc durum ayri: TOKEN YOK / YETKISIZ (401,403) / YOK (404).
# ──────────────────────────────────────────────────────────────────────────────
#
# Kullanim:  bash build/ci/gitops-frontend-isaretci-denetimi.sh
# Gerekli:   jq, curl, awk. GITHUB_TOKEN (CI'da otomatik) — yoksa anonim dener.
# ══════════════════════════════════════════════════════════════════════════════
set -uo pipefail

cd "$(dirname "$0")/../.." || exit 2   # repo koku

for tool in jq curl awk; do
  command -v "$tool" >/dev/null 2>&1 || { echo "❌ '$tool' yok — denetim calisamaz."; exit 2; }
done

# ── GHCR pull token ────────────────────────────────────────────────────────────
# Ozel paketler icin GITHUB_TOKEN gerekiyor. Token endpoint'ine basic-auth ile
# gidilir; kullanici adi onemsiz, parola GITHUB_TOKEN'dir.
ghcr_token() {
  local repo="$1" url="https://ghcr.io/token?scope=repository:${repo}:pull&service=ghcr.io"
  if [ -n "${GITHUB_TOKEN:-}" ]; then
    curl -fsSL -u "${GITHUB_ACTOR:-x}:${GITHUB_TOKEN}" "$url" 2>/dev/null | jq -r '.token // empty'
  else
    curl -fsSL "$url" 2>/dev/null | jq -r '.token // empty'
  fi
}

# "<durum> <digest>" basar. durum: OK | TOKENYOK | YETKISIZ | YOK | HATA:<kod>
manifest_of() {
  local repo="$1" tag="$2" token yanit status digest
  token=$(ghcr_token "$repo")
  [ -n "$token" ] || { echo "TOKENYOK -"; return 0; }

  yanit=$(curl -sSL -o /dev/null -D - \
    -H "Authorization: Bearer ${token}" \
    -H "Accept: application/vnd.oci.image.index.v1+json" \
    -H "Accept: application/vnd.docker.distribution.manifest.list.v2+json" \
    -H "Accept: application/vnd.oci.image.manifest.v1+json" \
    -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
    "https://ghcr.io/v2/${repo}/manifests/${tag}" 2>/dev/null)

  # Yonlendirme olabilir → SON durum satirini al.
  # tr -d '\r' SART: HTTP basliklari CRLF ile biter, "200\r" hicbir case ile eslesmez.
  # (2026-08-22: stub testi tam olarak bunu yakaladi — her durum HATA:<kod>'a dusuyordu.)
  status=$(printf '%s' "$yanit" | awk '/^HTTP\//{s=$2} END{print s}' | tr -d '\r')
  digest=$(printf '%s' "$yanit" | awk 'tolower($1)=="docker-content-digest:"{d=$2} END{print d}' | tr -d '\r')

  case "$status" in
    200) [ -n "$digest" ] && echo "OK $digest" || echo "HATA:200-digestyok -" ;;
    401|403) echo "YETKISIZ -" ;;
    404) echo "YOK -" ;;
    *)   echo "HATA:${status:-bos} -" ;;
  esac
}

APPS=(gitops/apps/web-*-app.yaml)
[ -e "${APPS[0]}" ] || { echo "🔴 gitops/apps/web-*-app.yaml bulunamadi — denetlenecek isaretci yok, bu BEKLENMEYEN bir durum."; exit 1; }

FAIL=0
KIYASLANAN=0
ATLANAN=0
echo "── Frontend surum isaretci denetimi (image.tag ↔ GHCR ':dev') ──"
[ -n "${GITHUB_TOKEN:-}" ] && echo "   kimlik: GITHUB_TOKEN (ozel paketler okunabilir)" \
                           || echo "   kimlik: YOK (anonim) — ozel paketler okunamaz, denetim eksik kalir"

for app in "${APPS[@]}"; do
  name=$(basename "$app")

  chart_path=$(awk '/^[[:space:]]*path:[[:space:]]/{print $2; exit}' "$app")
  pinned=$(awk '
    /-[[:space:]]*name:[[:space:]]*image\.tag/{f=1; next}
    f && /value:[[:space:]]/{v=$2; gsub(/"/,"",v); print v; exit}
  ' "$app")

  if [ -z "$chart_path" ] || [ -z "$pinned" ]; then
    echo "🔴 ${name}: path/image.tag OKUNAMADI — denetim bu dosyayi kapsayamiyor."
    FAIL=1; continue
  fi

  repo_full=$(awk '/^[[:space:]]*repository:[[:space:]]/{print $2; exit}' "${chart_path}/values-dev.yaml" 2>/dev/null)
  repo="${repo_full#ghcr.io/}"
  if [ -z "$repo" ] || [ "$repo" = "$repo_full" ]; then
    echo "🔴 ${name}: ghcr.io repository ${chart_path}/values-dev.yaml'dan COZULEMEDI."
    FAIL=1; continue
  fi

  # FAZ29 (2026-08-25, inktavia.com yayini): '-prod' ekli etiketler AYRI bir build'dir
  # (SITE_ENV=production image'a gomulu) ve digest'i HICBIR ZAMAN ':dev' ile tutmaz.
  # Bu isaretciler ':prod' hareketli etiketiyle kiyaslanir (CI image-prod isi her prod
  # build'inde ':prod'u da basar). Aksi, web-marine-prod-app.yaml'i sonsuza dek kirmizi
  # yapardi — denetim #32/#33'te tam bu yasandi.
  kanal="dev"
  case "$pinned" in *-prod) kanal="prod" ;; esac

  read -r dev_durum dev_digest <<<"$(manifest_of "$repo" "$kanal")"

  case "$dev_durum" in
    YETKISIZ|TOKENYOK|HATA:*)
      # Denetim isini YAPAMADI. Bu bir "temiz" sonuc DEGIL — sessizce gecilmez.
      echo "🔴 ${name}: ${repo}:${kanal} okunamadi (${dev_durum}). Denetim bu isaretciyi DOGRULAYAMADI."
      echo "     → Ozel paket ise workflow'da 'permissions: packages: read' ve GITHUB_TOKEN gerekir."
      FAIL=1; continue ;;
    YOK)
      # Image gercekten hic uretilmemis (or. web-admin, #79 nedeniyle derlenmiyor).
      # Mesru bir atlama — ama SESSIZ degil ve "guncel" SAYILMAZ.
      echo "⚠️  ${name}: ${repo}:${kanal} GHCR'da YOK (henuz image uretilmemis) — kiyas ATLANDI, dogrulanmadi."
      ATLANAN=$((ATLANAN+1)); continue ;;
  esac

  read -r pin_durum pin_digest <<<"$(manifest_of "$repo" "$pinned")"
  if [ "$pin_durum" != "OK" ]; then
    echo "🔴 ${name}: sabitlenen etiket ${repo}:${pinned} okunamadi (${pin_durum}). (ImagePullBackOff riski)"
    FAIL=1; continue
  fi

  KIYASLANAN=$((KIYASLANAN+1))
  if [ "$pin_digest" = "$dev_digest" ]; then
    echo "✅ ${name}: ${repo}:${pinned} guncel (':${kanal}' ile ayni digest)."
  else
    echo "🔴 ${name}: ISARETCI BAYAT — ${repo}:${pinned}, GHCR'daki en yeni ':${kanal}'dan FARKLI."
    echo "     pinned=${pin_digest}"
    echo "     ${kanal}   =${dev_digest}"
    echo "     → ${app} icindeki image.tag'i en yeni sha ile guncelle."
    FAIL=1
  fi
done

echo ""
echo "── Ozet: ${KIYASLANAN} kiyaslandi, ${ATLANAN} atlandi ──"

# 🔴 Bu blok bu betigin VAR OLMA SEBEBI. Hicbir sey kiyaslanmadiysa denetim
# CALISMAMISTIR; "hepsi guncel" demek yalan olur ve yesil kosu bizi uyutur.
if [ "$KIYASLANAN" -eq 0 ] && [ "$FAIL" -eq 0 ]; then
  echo "🔴 HICBIR isaretci kiyaslanamadi — denetim isini YAPMADI."
  echo "   Yesil bir kosu 'isaretciler guncel' anlamina GELMEZ. Yukaridaki atlama"
  echo "   sebeplerini gider (image uret / paket erisimini ac), sonra tekrar kos."
  exit 1
fi

if [ "$FAIL" -ne 0 ]; then
  echo "🔴 En az bir surum isaretcisi bayat/yanlis ya da dogrulanamadi."
  exit 1
fi

echo "✅ Kiyaslanan ${KIYASLANAN} isaretcinin hepsi guncel."
[ "$ATLANAN" -gt 0 ] && echo "⚠️  ${ATLANAN} isaretci DOGRULANMADI (yukarida sebepleri var) — bu bir 'temiz' sonuc degil."
exit 0
