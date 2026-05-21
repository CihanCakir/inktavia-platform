# 02 - Starter Operation Flow

This step focuses on the existing startup framework.

Modify code only if the correct integration points are clear.

## Target Area

Inspect:

```text
Core/Starter/src
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
BuildForOperation
```

## Goal

Understand how the operation service configuration and application configuration are used so the auth protection can be integrated without breaking the architecture.

## Service Configuration Requirements

`AizenOperationServiceConfiguration` should be checked for service registration responsibilities.

Determine whether this is the correct place to call or ensure:

```csharp
services.AddAizenAuth(...)
```

or the existing equivalent.

Do not duplicate registration if `AddAizenAuth` is already called elsewhere.

## Application Configuration Requirements

`AizenOperationApplicationConfiguration` should be checked for middleware pipeline responsibilities.

Determine whether this is the correct place to ensure:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

These must be called before controller mapping.

## Middleware Order Requirement

The expected order should be logically equivalent to:

```csharp
app.UseRouting();

app.UseCors(...); // if used by the existing project

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

If the framework uses custom wrappers, preserve them.

## Do Not

Do not:

- Bypass the starter architecture.
- Add one-off code to every service `Program.cs`.
- Change `BuildForOperation` public behavior unless required.
- Break existing service registration order.
- Break existing pipeline conventions.
- Remove existing middleware.

## Required Output

Produce:

```md
# Starter Operation Flow Result

## Correct Service Registration Point
- ...

## Correct Application Pipeline Point
- ...

## Required Changes
- ...

## Files To Change
- ...

## Risks
- ...
```
