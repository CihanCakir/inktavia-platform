# 06 - Wire Operation Service Configuration

This step wires the centralized auth behavior through the existing operation service configuration.

## Target

Inspect and update if required:

```text
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
```

## Goal

Ensure all operation-based services call the centralized auth registration from:

```text
Core/Auth/src/Aizen.Core.Auth
```

Specifically, ensure the flow used by `BuildForOperation` results in:

```csharp
AddAizenAuth(...)
```

being called once and in the correct service registration stage.

## Requirements

### 1. Use Existing Starter Architecture

If `AizenOperationServiceConfiguration` is responsible for service registration, this is likely where the auth extension should be included or verified.

### 2. Do Not Duplicate

If `AddAizenAuth` is already called from another shared service configuration invoked by `BuildForOperation`, do not add it again.

Instead, confirm that all operation services go through that path.

### 3. Respect Existing Configuration Source

If `AddAizenAuth` requires configuration, options, environment or builder references, pass them using the existing framework pattern.

### 4. Avoid Direct Service Program.cs Changes

Do not modify every service `Program.cs`.

Only modify service `Program.cs` if the architecture itself requires a missing starter call and there is no central way to apply it.

## Required Output

Produce:

```md
# Operation Service Configuration Result

## AddAizenAuth Call Location
- ...

## Is It Already Called?
- Yes / No

## Change Made
- ...

## Why This Covers All API Services
- ...

## Files Changed
- ...
```
