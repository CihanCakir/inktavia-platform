# 07 - Options and DI Wiring

Wire new options and services without breaking existing architecture.

## Target

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
ServiceCollectionExtensions
ApplicationBuilderExtensions
Options
Configure
IOptions
AizenUserInfoMiddleware
```

## Requirements

### Options

If an options class exists, extend it.

If not, create one only if consistent with architecture.

Recommended config section:

```json
{
  "Aizen": {
    "InfoAccessor": {
      "UserTokenHeaderName": "X-Aizen-User-Token",
      "ThrowOnMissingUserToken": false,
      "ThrowOnInvalidUserToken": false
    }
  }
}
```

Use the existing section name if different.

### DI

If middleware requires options/accessors, register them using existing patterns.

Do not duplicate registrations.

### Pipeline

Do not move middleware unless required.

If order matters, ensure it runs after authentication/authorization only if the project design expects API authorization before user context.

If other components need `AizenUserInfo` before authorization, document it clearly.

## Required Output

Produce:

```md
# Options and DI Wiring Result

## Options Added/Extended
- ...

## Config Section
- ...

## DI Changes
- ...

## Middleware Order
- ...

## Files Changed
- ...
```
