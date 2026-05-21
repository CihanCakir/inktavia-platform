# 08 - Protect All Controllers

This step verifies that all `*Controller` classes are protected by default.

## Target

Search all controller classes:

```text
**/*Controller.cs
```

Especially under:

```text
Modules/**/src/**/Controller/V1/**/*Controller.cs
Modules/**/src/**/Controllers/V1/**/*Controller.cs
Core/**/src/**/*Controller.cs
```

## Goal

No controller action should be reachable without a Keycloak token unless it is explicitly marked as public.

## Preferred Protection Mechanism

The preferred protection mechanism is the central fallback policy in:

```text
Core/Auth/src/Aizen.Core.Auth
```

This means you should not need to add `[Authorize]` to every controller.

However, you must verify that the fallback policy actually applies to MVC controllers.

## Verification Work

For representative controllers:

- Check whether requests without tokens now receive `401` or `403`.
- Check whether valid Keycloak tokens allow access.
- Check whether `[AllowAnonymous]` is respected.

## Attribute Strategy

Do not add `[Authorize]` to every controller unless fallback policy does not apply in this architecture.

If fallback policy is not enough because of the endpoint mapping style, consider:

```csharp
app.MapControllers().RequireAuthorization();
```

from the central starter mapping location.

## Required Output

Produce:

```md
# Controller Protection Result

## Protection Strategy
- FallbackPolicy / RequireAuthorization / Controller attributes

## Controller Scan Summary
- Total controllers found:
- Controllers already using Authorize:
- Controllers/actions using AllowAnonymous:

## Gaps Found
- ...

## Changes Made
- ...
```
