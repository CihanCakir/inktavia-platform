# 07 — Controller Route and Frontend Proxy Validation

Validate that the final backend route matches the frontend call.

Expected from browser/dev server:

```http
http://localhost:3000/api/v1/admin-panel/vessels/register
```

If Vite/React dev server proxies `/api` to the AdminPanel BFF, the BFF route must be:

```http
/api/v1/admin-panel/vessels/register
```

Check frontend proxy config:

```bash
find . -name "vite.config.*" -o -name "proxy*.ts" -o -name "*.env*" | sort
grep -r "localhost.*admin\|VITE_.*API\|proxy\|/api" . --include="*.ts" --include="*.tsx" --include="*.env*" --include="vite.config.*" | head -200
```

Do not change frontend unless the backend endpoint is correct and frontend is provably pointing to the wrong URL.

Add route test:

```bash
curl -i -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

Expected: `200`, not `404`.
