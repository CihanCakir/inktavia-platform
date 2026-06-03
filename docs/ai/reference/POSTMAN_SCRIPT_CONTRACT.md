# Postman Script Contract

## Keycloak token script

```javascript
const response = pm.response.json();

pm.test("Token response contains access_token", function () {
  pm.expect(response.access_token).to.be.a("string");
});

pm.environment.set("mobile_access_token", response.access_token);
pm.environment.set("active_access_token", response.access_token);
```

Use the correct variable for each client:

- `mobile_access_token`
- `customer_access_token`
- `admin_access_token`

## Identity login script

```javascript
pm.test("Status is not 500", function () {
  pm.expect(pm.response.code).to.not.eql(500);
});

pm.test("Login response is successful and identity token exists", function () {
  const response = pm.response.json();

  pm.expect(response).to.have.property("header");
  pm.expect(response.header.isSuccess).to.eql(true);

  pm.expect(response).to.have.property("body");
  pm.expect(response.body).to.have.property("token");
  pm.expect(response.body.token).to.have.property("accessToken");

  const identityAccessToken = response.body.token.accessToken;

  if (!identityAccessToken) {
    throw new Error("Identity access token could not be found in body.token.accessToken");
  }

  pm.environment.set("X-Aizen-User-Token", `Bearer ${identityAccessToken}`);
  pm.environment.set("identityAccessToken", identityAccessToken);
  pm.environment.set("identityAccessTokenExpiredDate", response.body.token.accessTokenExpiredDate || "");
  pm.environment.set("identityRefreshToken", response.body.token.refreshToken || "");
  pm.environment.set("identityRefreshTokenExpiredDate", response.body.token.refreshTokenExpiredDate || "");

  if (response.body.profile) {
    pm.environment.set("identityUserId", response.body.profile.userId || "");
    pm.environment.set("identityUserEmail", response.body.profile.email || "");
    pm.environment.set("identityUserName", response.body.profile.name || "");
    pm.environment.set("identityUserSurname", response.body.profile.surname || "");
  }
});
```

## Id extraction helper pattern

```javascript
const response = pm.response.json();
const body = response.body || response;

if (body && body.id) {
  pm.environment.set("serviceRequestId", body.id);
}

if (body && body.serviceRequestId) {
  pm.environment.set("serviceRequestId", body.serviceRequestId);
}
```
