# Aizen Keycloak API Protection Prompt Pack

This prompt pack is designed for GitHub Copilot Agent or a similar coding agent.

It guides the agent to protect all API controller endpoints with Keycloak authentication while preserving the existing Aizen framework startup architecture.

## Main Architectural Assumption

The solution already uses Keycloak.

The startup flow is not a simple direct `Program.cs` setup. The project uses framework-level starter configuration under:

```text
Core/Starter/src
```

The service/application startup implementation is connected through:

```text
BuildForOperation
```

The relevant starter implementation is located under:

```text
Aizen.Core.Starter.Operation
```

The main classes to inspect are:

```text
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
```

The authentication extension is located under:

```text
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

The method to inspect and extend is:

```csharp
AddAizenAuth
```

## Execution Order

Run these files one by one:

1. `00-context-and-objective.md`
2. `01-discovery-architecture-map.md`
3. `02-starter-operation-flow.md`
4. `03-auth-package-analysis.md`
5. `04-central-policy-design.md`
6. `05-implement-auth-options-and-policies.md`
7. `06-wire-operation-service-configuration.md`
8. `07-wire-operation-application-pipeline.md`
9. `08-protect-all-controllers.md`
10. `09-allow-anonymous-public-endpoints.md`
11. `10-swagger-health-check-and-dev-exceptions.md`
12. `11-verification-tests-and-final-report.md`

For a single prompt execution, use:

```text
all-in-one-agent-prompt.md
```
