#!/usr/bin/env bash
# ══════════════════════════════════════════════════════════════════════════════
# FAZ20 (#55) — Frontend surum isaretci denetimi.
#
# SORUN: gitops/apps/web-*-app.yaml icindeki image.tag ELLE guncelleniyor.
# Kusur, elle guncelleme degil; UNUTMANIN SESSIZ olmasi: merge edersin, frontend CI
# yeni image yayinlar, ama bu isaretci eski sha'da kalir ve cluster sessizce eski
# image'i servis etmeye devam eder — hicbir yerde sinyal yok.
#
# COZUM (secenek b — "once yuksek sesle, sonra otomatik"): SALT-OKUR bir CI denetimi.
# TUM web-*-app.yaml'lar icin, sabitlenmis image.tag'in digest'ini GHCR'daki en yeni
# ':dev' image'inin digest'i ile kiyaslar. Tutmuyorsa YUKSEK SESLE patlar.
# Cross-repo YAZMA yetkisi GEREKMEZ (public image → anonim pull token). Sadece okur.
#
# Neden ':dev' ile kiyas: frontend CI her develop push'unda AYNI image'i hem
# sha-<7hex> hem ':dev' etiketiyle yayinliyor → ':dev' daima en yeni sha'ya esittir.
# Sabitlenen sha'nin manifest digest'i ':dev'inkiyle ayniysa isaretci gunceldir.
#
# Kullanim:  bash build/ci/gitops-frontend-isaretci-denetimi.sh
# Gerekli:   jq, curl (CI runner'inda var).
# ══════════════════════════════════════════════════════════════════════════════
set -uo pipefail

cd "$(dirname "$0")/../.." || exit 2   # repo koku

for tool in jq curl awk; do
  command -v "$tool" >/dev/null 2>&1 || { echo "❌ '$tool' yok — denetim calisamaz."; exit 2; }
done

# Verilen GHCR repo (or. cihancakir/inktavia/web-provider) ve etiket icin manifest digest'i (bos = yok).
digest_of() {
  local repo="$1" tag="$2" token
  token=$(curl -fsSL "https://ghcr.io/token?scope=repository:${repo}:pull&service=ghcr.io" 2>/dev/null | jq -r '.token // empty')
  [ -n "$token" ] || return 0
  curl -fsSL -o /dev/null -D - \
    -H "Authorization: Bearer ${token}" \
    -H "Accept: application/vnd.oci.image.index.v1+json" \
    -H "Accept: application/vnd.docker.distribution.manifest.list.v2+json" \
    -H "Accept: application/vnd.oci.image.manifest.v1+json" \
    -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
    "https://ghcr.io/v2/${repo}/manifests/${tag}" 2>/dev/null \
    | awk 'tolower($1)=="docker-content-digest:"{print $2}' | tr -d '\r'
}

APPS=(gitops/apps/web-*-app.yaml)
[ -e "${APPS[0]}" ] || { echo "web-*-app.yaml bulunamadi (gitops/apps/) — denetlenecek isaretci yok."; exit 0; }

FAIL=0
echo "── Frontend surum isaretci denetimi (image.tag ↔ GHCR ':dev') ──"
for app in "${APPS[@]}"; do
  name=$(basename "$app")

  # Chart yolu (source.path) ve sabitlenmis image.tag (name: image.tag'i izleyen ilk value:).
  chart_path=$(awk '/^[[:space:]]*path:[[:space:]]/{print $2; exit}' "$app")
  pinned=$(awk '
    /-[[:space:]]*name:[[:space:]]*image\.tag/{f=1; next}
    f && /value:[[:space:]]/{v=$2; gsub(/"/,"",v); print v; exit}
  ' "$app")

  if [ -z "$chart_path" ] || [ -z "$pinned" ]; then
    echo "⚠️  ${name}: path/image.tag okunamadi — atlandi."
    continue
  fi

  # image.repository chart'in values-dev.yaml'indan.
  repo_full=$(awk '/^[[:space:]]*repository:[[:space:]]/{print $2; exit}' "${chart_path}/values-dev.yaml" 2>/dev/null)
  repo="${repo_full#ghcr.io/}"
  if [ -z "$repo" ] || [ "$repo" = "$repo_full" ]; then
    echo "⚠️  ${name}: ghcr.io repository ${chart_path}/values-dev.yaml'dan cozulemedi — atlandi."
    continue
  fi

  dev_digest=$(digest_of "$repo" "dev")
  if [ -z "$dev_digest" ]; then
    # Henuz hic image yayinlanmamis (or. yeni eklenen app). Sessiz gecme — ama HATA da degil.
    echo "⚠️  ${name}: ${repo}:dev GHCR'da yok (henuz image uretilmemis) — kiyas atlandi."
    continue
  fi

  pin_digest=$(digest_of "$repo" "$pinned")
  if [ -z "$pin_digest" ]; then
    echo "🔴 ${name}: sabitlenen etiket ${repo}:${pinned} GHCR'da YOK — yanlis/eski sha. (ImagePullBackOff riski)"
    FAIL=1
    continue
  fi

  if [ "$pin_digest" = "$dev_digest" ]; then
    echo "✅ ${name}: ${repo}:${pinned} guncel (':dev' ile ayni digest)."
  else
    echo "🔴 ${name}: ISARETCI BAYAT — ${repo}:${pinned}, GHCR'daki en yeni ':dev'den FARKLI."
    echo "     pinned=${pin_digest}"
    echo "     dev   =${dev_digest}"
    echo "     → ${app} icindeki image.tag'i en yeni sha ile guncelle."
    FAIL=1
  fi
done

if [ "$FAIL" -ne 0 ]; then
  echo ""
  echo "🔴 En az bir surum isaretcisi bayat/yanlis. Yukaridaki app.yaml'lari guncelle."
  exit 1
fi
echo "✅ Tum frontend surum isaretcileri guncel."
