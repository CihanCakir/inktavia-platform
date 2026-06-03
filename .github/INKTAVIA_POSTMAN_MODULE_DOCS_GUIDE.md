# Inktavia Postman Module Docs Guide

## Goal

Create importable Postman collections, environments, scripts, endpoint inventories, testing guides, and reports for active Inktavia Marine OS modules.

## Required module output

For every active module:

```text
Modules/<ModuleName>/docs/postman/
  endpoint-inventory.md
  <ModuleName>.ControllerApiTests.postman_collection.json
  <ModuleName>.postman-testing-guide.md
  <ModuleName>.postman-validation-report.md
```

For ServiceRequest:

```text
Modules/ServiceRequest/docs/postman/
  ServiceRequest.BusinessScenarios.postman_collection.json
  ServiceRequest.scenario-matrix.md
  ServiceRequest.realtime-testing-guide.md
```

Root output:

```text
docs/postman/
  InktaviaMarineOS.Local.postman_environment.json
  InktaviaMarineOS.Dev.postman_environment.json
  InktaviaMarineOS.ActiveModules.ControllerApiTests.postman_collection.json
  InktaviaMarineOS.ServiceRequestScenarios.postman_collection.json
  postman-generation-final-report.md
```

## Mandatory checks

- Every endpoint has a meaningful sample request if it has a request model.
- Every endpoint has a meaningful response expectation if a response model can be inferred.
- Auth scripts are included and tested.
- Identity user token header is generated and used.
- Payment/Profile are skipped.
- Routes are normalized.
