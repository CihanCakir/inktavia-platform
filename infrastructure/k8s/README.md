# Kubernetes — security-critical deployment configuration

These manifests are not scaffolding. They are the controls that make the Provider auth design safe.

## The one thing to understand

**The Provider BFF is the authorization trust boundary.** The end-user's Keycloak token stops at the BFF.
Modules never see it — they trust the identity the BFF *asserts*:

```
Authorization: Bearer <service-account token>
X-Aizen-Bff-Assertion:        <shared secret>
X-Aizen-User-Id:              <Identity UserId>
X-Aizen-Provider-Profile-Id:  <ProviderProfileId>
```

`AizenUserInfoMiddleware.TryAcceptBffAssertion` accepts that assertion when the secret matches (constant-time)
**and** the calling service-account client is in `BffAssertion:AllowedClientIds`.

So: **anyone who can reach a module, holds the shared secret and any valid service-account token can claim to
be any user** by changing `X-Aizen-User-Id`. Two things prevent that, and both live here:

1. Modules are unreachable from outside the cluster → `networkpolicy-internal-modules.yaml`
2. The allowlist is non-empty → enforced in code (the service refuses to start otherwise) and configured per
   module (`BffAssertion__AllowedClientIds__0=provider-portal-bff`)

## Files

| File | What it does |
|---|---|
| `networkpolicy-internal-modules.yaml` | Default-deny ingress; modules reachable **only** from BFF pods (plus a narrow Prometheus rule). BFFs reachable only from the ingress controller. |
| `ingress-bff-marineprovider.yaml` | The **only** public entry point. WebSocket upgrade allowed; no sticky sessions (the Redis backplane carries frames across pods). Internal modules get **no Ingress object at all**. |
| `configmap-ingress-nginx-log.yaml` | Access log records the **path, not the query string**. The SignalR token travels as `?access_token=`; nginx's default `$request_uri` would write every user's live token into the access log. |

## Preconditions

- A CNI that actually enforces NetworkPolicy (Calico, Cilium…). On a CNI that ignores it, the policy file is
  decoration — **verify, do not assume**.
- Pods labelled `aizen.io/tier: bff` / `aizen.io/tier: module`.
- `BffAssertion__SharedSecret` from a Kubernetes Secret, never from a ConfigMap, never in git.
- Stateful infrastructure (PostgreSQL, RabbitMQ, Redis, MinIO, Keycloak) stays **outside** the cluster.

## Verify (observations, not assertions)

```bash
# 1. A module must NOT be reachable from a random pod.
kubectl -n inktavia run probe --rm -it --image=curlimages/curl --restart=Never -- \
  curl -sS -m 5 http://identity-api:8080/health ; # expect: timeout / connection refused

# 2. No tokens in the ingress log.
kubectl -n ingress-nginx logs deploy/ingress-nginx-controller | grep -c "access_token=" ; # expect: 0

# 3. A module refuses to start with a secret but an empty allowlist.
#    (BffAssertion__SharedSecret set, BffAssertion__AllowedClientIds unset → the pod must CrashLoop with the
#    validation message, not come up quietly.)
```

If (1) returns a 200, the network control is not in place and the assertion mechanism is exposed. Fix that
before anything else — nothing else on this list matters while a module answers the world.

## Known interim risk (tracked, not solved here)

The assertion secret is **static**: no `exp`, no `jti`, no binding to the asserted user. If it leaks, it is
valid forever. Migration path: a short-lived **signed** BFF assertion JWT (`iss`, `aud`, `exp`, `iat`, `jti`,
`user_id`, `provider_profile_id`, calling client id) with replay protection. Written down so "interim" does not
become permanent by silence.
