# AdminPanel BFF Fix Guide

This guide defines the corrective pass for AdminPanel BFF.

## Problem summary

The previous generation produced partial results:

- Some Identity endpoints were missing.
- Authentication and Authorization endpoints were missing or incomplete.
- Several ReferenceData endpoints were missing.
- Endpoint discovery was not exhaustive.
- Some Query and QueryHandler classes were generated in the same file.
- Some Command and CommandHandler classes were generated in the same file.
- Controller names were over-prefixed with `Admin` although the BFF already represents AdminPanel.

## Expected correction

The agent must scan all `*Controller.cs` files under active modules and generate a complete AdminPanel BFF representation.

## Controller scan roots

Search all of the following patterns:

```text
Modules/Identity/src/**/*Controller.cs
Modules/ReferenceData/src/**/*Controller.cs
Modules/Vessel/src/**/*Controller.cs
Modules/FileStorage/src/**/*Controller.cs
Modules/ServiceRequest/src/**/*Controller.cs
Modules/**/src/**/Controller/**/*.cs
Modules/**/src/**/Controllers/**/*.cs
```

Do not include Payment/Profile as active modules.

## Required Application folder structure

Use one folder per feature/action:

```text
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Query.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>QueryHandler.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Response.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Validator.cs

Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Command.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>CommandHandler.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Response.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Validator.cs
```

Example:

```text
Application/Profiles/Query/GetProfileDetail/GetProfileDetailQuery.cs
Application/Profiles/Query/GetProfileDetail/GetProfileDetailQueryHandler.cs
Application/Profiles/Command/ApproveOrganizerProfile/ApproveOrganizerProfileCommand.cs
Application/Profiles/Command/ApproveOrganizerProfile/ApproveOrganizerProfileCommandHandler.cs
```
