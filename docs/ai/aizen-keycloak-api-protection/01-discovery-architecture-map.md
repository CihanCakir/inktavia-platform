# 01 - Discovery: Architecture Map

Do not modify code in this step.

Map the current architecture.

## Search Targets

Search and inspect these paths and symbols:

```text
Core/Starter/src
BuildForOperation
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
Core/Auth/src/Aizen.Core.Auth
BuilderExtensions.cs
AddAizenAuth
UseAuthentication
UseAuthorization
AddAuthentication
AddAuthorization
FallbackPolicy
MapControllers
RequireAuthorization
AllowAnonymous
Authorize
```

## Questions to Answer

### 1. Program.cs Flow

Find the relevant `Program.cs` files.

Answer:

- How is `BuildForOperation` called?
- Which service configuration classes are used?
- Which application configuration classes are used?
- Does `Program.cs` directly call auth-related methods?
- Should the fix be added in `Program.cs` or in the framework starter?

### 2. Operation Starter Flow

Inspect:

```text
Aizen.Core.Starter.Operation
```

Answer:

- What does `AizenOperationServiceConfiguration` register?
- What does `AizenOperationApplicationConfiguration` configure?
- Where are services registered?
- Where is the middleware pipeline configured?
- Where are controllers mapped?
- Does this layer already call `AddAizenAuth`?
- Does this layer already call `UseAuthentication` and `UseAuthorization`?

### 3. Auth Package Flow

Inspect:

```text
Core/Auth/src/Aizen.Core.Auth
```

Answer:

- Where is `AddAizenAuth` defined?
- What services does it register?
- Does it add `AddAuthentication`?
- Does it add JWT Bearer?
- Does it add `AddAuthorization`?
- Does it define default or fallback policies?
- Does it expose options for auth behavior?

### 4. Controller Protection

Search all `*Controller.cs` files.

Answer:

- How many controllers exist?
- Which controllers/actions already use `[Authorize]`?
- Which controllers/actions already use `[AllowAnonymous]`?
- Are controllers protected by route mapping or by attributes?
- Is there any global authorization policy?

## Required Output

Produce this report before changing anything:

```md
# Architecture Discovery Report

## Program.cs Flow
- ...

## BuildForOperation Usage
- ...

## AizenOperationServiceConfiguration
- ...

## AizenOperationApplicationConfiguration
- ...

## AddAizenAuth Location and Current Behavior
- ...

## Current Controller Protection State
- ...

## Root Cause
- ...

## Recommended Implementation Location
- ...
```
