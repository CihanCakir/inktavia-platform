# AdminPanel BFF Test Runner and Postman Alignment Report

## Scope

Align test runner and Postman collection to the final Identity-only browser auth model.

## Current State (From Local Report)

The test runner currently sends **both** headers on every request:

```http
Authorization: Bearer <identityAccessToken>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

This is inconsistent with the final security model where the browser sends only `X-Aizen-User-Token`.

## Required Updates

### Test Runner (`Bff/src/AdminPanel/docs/reports/` test scripts)

Remove `Authorization` header from React-like BFF test requests. Only send:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Keep `Authorization` header only in:
- Keycloak service token validation tests (separate folder)
- Direct internal module API tests (non-BFF path)

### Postman Collection (`docs/postman/admin-panel-bff/`)

Update BFF collection pre-request scripts:
- Remove `pm.request.headers.add({ key: "Authorization", value: ... })` for BFF requests
- Use `pm.request.headers.add({ key: "X-Aizen-User-Token", value: "Bearer {{identityAccessToken}}" })`

### Negative Auth Test Expectations

Two tests currently expect HTTP 400/401 for bad credentials:
- `POST /auth/login/phone` — bad credentials
- `POST /auth/refresh` — invalid refresh token

**Finding**: The Aizen framework returns HTTP 200 with `header.isSuccess = false` envelope on domain failures. The Identity module does not return HTTP 400/401 for bad credentials — it uses the envelope pattern.

**Recommendation**: Update test assertions to check:
```javascript
pm.expect(response.header.isSuccess).to.be.false;
// NOT: pm.expect(pm.response.code).to.equal(401);
```

### Timeout Test

`POST /auth/login/username` with wrong password times out:
- **Root cause**: The Identity service authentication path does not return quickly for failed credentials locally (possibly bcrypt hash comparison delay).
- **Recommendation**: Set BFF remote call timeout to 10s; ensure Identity module short-circuits on invalid username before bcrypt.

## Status

| Item | Status |
|------|--------|
| Test runner `Authorization` header removal | ⚠️ Manual — requires test runner script update |
| Postman collection update | ⚠️ Manual — requires Postman file edit |
| Negative auth test expectation update | ⚠️ Manual — update assertion to check `isSuccess` |
| Timeout fix | ⚠️ Identity service fix required |
