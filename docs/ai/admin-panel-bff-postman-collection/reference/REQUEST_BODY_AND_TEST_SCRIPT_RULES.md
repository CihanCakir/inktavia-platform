# Request Body and Test Script Rules

## Body rules

Use real DTOs where available. Otherwise create minimal placeholders.

Known minimal bodies:

```json
{ "username": "admin.user@inktavia.com", "password": "Password123!" }
```

```json
{ "phone": "+905555555555", "password": "Password123!" }
```

```json
{ "phone": "+905555555555", "otpCode": "123456" }
```

```json
{ "refreshToken": "{{identityRefreshToken}}" }
```

```json
{ "reason": "Rejected by admin during validation." }
```

```json
{ "expiresInMinutes": 60 }
```

```json
{ "fileIds": [{{sampleFileId}}], "expiresInMinutes": 60 }
```

```json
{ "visibility": "Private" }
```

If a DTO is complex, add a TODO and report it.

## Required tests

Every request:

```javascript
pm.test('Status code is not 500', function () {
  pm.expect(pm.response.code).to.not.equal(500);
});

pm.test('Response is JSON when body exists', function () {
  if (pm.response.text()) {
    pm.expect(() => pm.response.json()).to.not.throw();
  }
});
```

Protected request check:

```javascript
pm.test('Identity user token variable exists', function () {
  pm.expect(pm.environment.get('X_Aizen_User_Token')).to.not.be.empty;
});
```
