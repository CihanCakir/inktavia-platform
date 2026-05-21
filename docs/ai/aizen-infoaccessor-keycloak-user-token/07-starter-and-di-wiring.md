# 07 - Starter and DI Wiring

Wire the new InfoAccessor components into the existing framework registration.

## Target

Inspect the registration points for:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
InfoAccessorServiceCollectionExtensions
InfoAccessorApplicationBuilderExtensions
AizenUserInfoMiddleware
```

## Requirements

Register the new services using the existing lifetime patterns.

Likely lifetimes:

- Accessors storing request context: Scoped
- Token readers without state: Scoped or Singleton depending on dependencies
- Options: Options pattern

Do not guess if existing lifetimes are clear.

## Configuration

Bind options from configuration if the package already supports configuration.

Example config path:

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

Use existing config section naming if different.

## Pipeline

Ensure `AizenUserInfoMiddleware` is still registered in the existing pipeline.

Recommended relative order depends on existing architecture. If it reads `HttpContext.User`, it should run after authentication. If it must be available before authorization handlers, document why.

## Required Output

Produce:

```md
# Starter and DI Wiring Result

## DI Registrations
- ...

## Options Binding
- ...

## Middleware Registration
- ...

## Middleware Order
- ...

## Files Changed
- ...
```
