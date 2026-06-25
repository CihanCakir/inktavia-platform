# Admin Panel BFF — Architecture Analysis Report

## Why BFF Pattern?

The Admin Panel uses a **Backend For Frontend (BFF)** pattern to provide a single, purpose-built API surface tailored to the admin dashboard UI. Rather than having the frontend call five separate microservices, it talks to one BFF host that:

- Aggregates and shapes responses to match admin UI screens
- Forwards auth tokens uniformly to all downstream services
- Enforces `[Authorize(Roles = "Admin")]` at the gateway boundary
- Isolates the admin client from internal service topology changes

## Module Responsibilities

| Module | Port | BFF Responsibility |
|--------|------|--------------------|
| Identity | 7101 | Profile search, profile detail, organizer/venue approval and rejection |
| ReferenceData | 7104 | Lookup groups/tree/items, currencies, locations, measurement units, system parameters |
| Vessel | 7105 | Admin vessel list, vessel detail/documents, archive, restore, status update, remove document |
| FileStorage | 7106 | File metadata review, bulk pre-signed read URL generation, file deletion |
| ServiceRequest | 7107 | Admin request list, dispute list, operation detail, cancel, approve/reject completion, dispute status change, dispute resolution |

## Remote Call Patterns

All downstream calls use the **Aizen RemoteCall** architecture (`IAizenRemoteCall` + `AizenRemoteCallGet/Post/Patch/Delete` attributes), which is a Refit-backed typed HTTP client abstraction.

```csharp
public interface IVesselAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/admin/vessels")]
    Task<AizenApiResponse<GetAllVesselsAdminResponse>> GetAdminVesselList(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [Refit.Query] int pageIndex = 0, ...);
}
```

Each remote call interface passes the `Authorization` header explicitly as a method parameter — received from the incoming HTTP request in the controller layer.

## Assembly Discovery Workaround for DI

The Aizen framework auto-discovers remote call interfaces from assemblies whose name contains `Abstraction`. The BFF application assembly (`Aizen.Bff.AdminPanel.Application`) does **not** follow that naming convention, so the five remote call interfaces are **manually registered** in `DependencyInjection.cs`:

```csharp
services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    return RestService.For<IIdentityAdminBffRemoteCall>(
        factory.CreateClient(nameof(IIdentityAdminBffRemoteCall)));
});
// ... repeated for each remote call interface
```

This is an intentional workaround documented on the `DependencyInjection` class itself via `DocumentationInfo`.

## AppType.Bff Startup

The entry point uses `AizenApplicationBuilder.CreateBuilder` with `AppType.Bff`:

```csharp
var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "AdminPanelBff",
    Type = AppType.Bff
}, args);
```

`AppType.Bff` activates BFF-specific middleware: Swagger, CORS policy, JWT bearer auth middleware, `HttpClient` factory, and remote call infrastructure — without registering message bus consumers (those are for module hosts, not BFFs).

## CQRS in the BFF

Controllers delegate to `IAizenCQRSProcessor` which dispatches to `IQueryHandler<TQuery, TResult>` and `ICommandHandler<TCommand, TResult>` implementations in the Application layer. Each handler calls the appropriate remote call interface.

```
Controller → IAizenCQRSProcessor → QueryHandler/CommandHandler → IAizenRemoteCall → Microservice
```

The BFF has **no local domain model** and **no database**. It is purely an orchestration and aggregation layer.
