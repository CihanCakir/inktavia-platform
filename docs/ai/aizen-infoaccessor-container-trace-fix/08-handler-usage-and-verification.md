# 08 - Handler Usage and Verification

After fixing root cause, update unsafe handler access patterns.

## Search

```text
_infoAccessor.UserInfoAccessor.UserInfo
.UserInfoAccessor.UserInfo.UserId
```

## Required Pattern

Handlers should not rely on unsafe nested access without guard.

Preferred:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor.UserInfo;
if (userInfo is null || userInfo.UserId == 0)
    throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());
var userId = Convert.ToInt32(userInfo.UserId);
```

Better if consistent with architecture:

```csharp
var userId = _infoAccessor.UserInfoAccessor.GetRequiredUserId();
```

Only add a helper if it does not break existing abstractions.

## Verify

Headers:

```http
Authorization: Bearer {{keycloakAccessToken}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
Content-Type: application/json
```

Body:

```json
{
  "oldPassword": "123456",
  "newPassword": "NewPassword123!",
  "newPasswordComfirm": "NewPassword123!"
}
```

Run:

```bash
dotnet build
dotnet test
```

## Output Required

```md
# Verification Result

## Unsafe Usages Found
- ...

## Updated Handlers
- ...

## Build
- ...

## Tests
- ...

## Manual Request Result
- ...
```
