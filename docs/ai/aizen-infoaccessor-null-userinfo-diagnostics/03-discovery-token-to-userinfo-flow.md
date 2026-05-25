# 03 - Discovery: Token to UserInfo Flow

Do not modify code in this step.

Inspect:

```text
AizenUserInfoMiddleware
```

and related token readers/services.

## Required Questions

Answer:

### Token Header

- Does middleware read `X-Aizen-User-Token`?
- Does it support `Bearer` prefix?
- Does it still read from `Authorization` incorrectly?
- What happens if the header is missing?

### Token Parsing

- Which method parses the Identity token?
- Which claims are read?
- Is parsing successful?
- Are claim values logged or visible in debugger?
- Does parsing produce an `AizenUserInfo` instance?

### Assignment

This is critical.

Find exactly where parsed user info is assigned.

Examples:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo = userInfo;
```

or:

```csharp
context.Items["AizenUserInfo"] = userInfo;
```

or:

```csharp
_userInfoAccessor.SetUserInfo(userInfo);
```

Answer:

- Is parsed user info assigned to the DI accessor?
- Or only to `HttpContext.Items`?
- Does command handler read the same place?
- Is `UserInfoAccessor` initialized before assignment?
- Is assignment happening after `await _next(context)` instead of before?
- Is assignment skipped because of a flag/condition?

## Required Output

Produce:

```md
# Token to UserInfo Flow Report

## Header Reading
- ...

## Token Parsing
- ...

## Claims Read
- ...

## AizenUserInfo Created
- Yes / No

## Assignment Target
- DI accessor / HttpContext.Items / Local variable / Other

## Assignment Timing
- Before next middleware / After next middleware

## Why Handler Cannot See UserInfo
- ...
```
