#!/usr/bin/env bash
#
# Fails the build if any module chart would create an Ingress object.
#
# Why this exists
# ---------------
# Modules do not authenticate end users. They trust the identity the BFF asserts:
#
#   Authorization: Bearer <service-account token>
#   X-Aizen-Bff-Assertion:       <shared secret>
#   X-Aizen-User-Id:             <Identity UserId>
#   X-Aizen-Provider-Profile-Id: <ProviderProfileId>
#
# So anyone who can reach a module, holds the shared secret and any valid
# service-account token can claim to be any user by changing X-Aizen-User-Id.
# Two controls prevent that, and both must hold:
#
#   1. Modules are unreachable from outside the cluster  <- this script
#      (no Ingress object at all)
#   2. NetworkPolicy restricts module ingress to BFF pods
#      (infrastructure/k8s/networkpolicy-internal-modules.yaml)
#
# Publishing a module through an Ingress silently removes control #1.
#
# See infrastructure/k8s/README.md for the full threat model.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

failed=0

# ---------------------------------------------------------------- values check
for f in Modules/*/deploy/*/values*.yaml; do
  [ -e "$f" ] || continue
  value="$(awk '
    /^ingress:[[:space:]]*$/ { inblk=1; next }
    inblk && /^[A-Za-z]/     { inblk=0 }
    inblk && /^[[:space:]][[:space:]]enabled:/ { print $2; exit }
  ' "$f")"
  if [ "${value:-false}" = "true" ]; then
    echo "FAIL  $f  ->  ingress.enabled: true"
    failed=1
  fi
done

# -------------------------------------------------------------- rendered check
# Stronger than the values check: catches a raw Ingress template added to a
# module chart. Skipped when helm is unavailable (the values check still runs).
if command -v helm >/dev/null 2>&1; then
  for chart in Modules/*/deploy/*/; do
    [ -f "${chart}Chart.yaml" ] || continue
    for values in "${chart}"values*.yaml; do
      [ -e "$values" ] || continue
      if helm template test "$chart" -f "$values" 2>/dev/null | grep -qE '^kind:[[:space:]]*Ingress'; then
        echo "FAIL  $chart rendered with $(basename "$values") produces a kind: Ingress"
        failed=1
      fi
    done
  done
else
  echo "note: helm not found, skipped the rendered-manifest check"
fi

if [ "$failed" -ne 0 ]; then
  cat <<'MSG'

-------------------------------------------------------------------------------
A module chart is configured to publish an Ingress.

Modules are an internal trust boundary: they accept the caller-asserted user id
from the BFF. Exposing one through an Ingress makes user impersonation possible
for anyone who can reach it and holds the shared secret.

Set `ingress.enabled: false` in the offending values file. If a module genuinely
needs to be reachable from outside, that is an architecture decision, not a
values change -- discuss it before editing this guard.
-------------------------------------------------------------------------------
MSG
  exit 1
fi

echo "OK  no module chart produces an Ingress object"
