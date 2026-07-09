# MarineProvider BFF — CQRS Folder Cleanup (Phase 31B/C)

> Build note: no .NET SDK in the authoring environment — verify with `dotnet build` locally.

## Final structure (folder-per-feature, one type per file)

```
Application/
  Auth/RegisterProvider/{RegisterProviderCommand, RegisterProviderCommandHandler, RegisterProviderCommandValidator}.cs
  Auth/EnsureProviderProfile/{EnsureProviderProfileCommand, EnsureProviderProfileCommandHandler}.cs
  Me/GetProviderMe/{GetProviderMeQuery, GetProviderMeQueryHandler}.cs
  Me/GetProviderProfile/{GetProviderProfileQuery, GetProviderProfileQueryHandler}.cs
  Me/GetProviderStatus/{GetProviderStatusQuery, GetProviderStatusQueryHandler}.cs
  Phone/SendProviderPhoneOtp/{Command, CommandHandler, CommandValidator}.cs
  Phone/VerifyProviderPhoneOtp/{Command, CommandHandler, CommandValidator}.cs
  Contracts/Auth/{RegisterProviderResponse, EnsureProviderProfileResponse}.cs
  Contracts/Me/{GetProviderMeResponse, ProviderProfileDto, GetProviderProfileResponse, GetProviderStatusResponse}.cs
  Contracts/Phone/{SendProviderPhoneOtpResponse, VerifyProviderPhoneOtpResponse}.cs
  Common/{Authorization, Http, Options, RemoteClients, Services, Warnings}/...
  DependencyInjection.cs
```

## What changed
- Split every combined `*Command.cs`/`*Query.cs` (which previously held command+response+handler) into
  one type per file: command/query, handler, and (where meaningful) validator.
- Moved all response DTOs + `ProviderProfileDto` into `Contracts/{Auth,Me,Phone}`.
- Deleted stale files/folders: `Auth/Command/`, `Me/Query/`, `Phone/Command/` (old combined files).
- Added FluentValidation validators (`AizenValidator<T>`) for Register / SendPhoneOtp / VerifyPhoneOtp;
  added a `Aizen.Core.Validation` project reference to the Application csproj (BFF starter already calls
  `AddAizenValidation`). Handlers keep defensive inline checks so behavior degrades to controlled responses.
- Controllers updated to the new namespaces; they remain thin (dispatch + envelope only — no Keycloak/Identity/provisioning logic).

## Rules satisfied
No inner handler/validator classes · no command/handler/validator in one file · no duplicate CQRS types
(verified: each response type defined exactly once) · no stale files · no controller-side business logic.

## Program.cs (final, slim)
```csharp
var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo { Name = "MarineProviderBff", Type = AppType.Bff }, args);

builder.Services
    .AddMarineProviderBffApplication(builder.Configuration)   // options, admin client, service token, context/resolver, Refit remote calls
    .AddMarineProviderAuthentication(builder.Configuration)   // inbound Keycloak JWT (Extensions/AuthenticationExtensions.cs)
    .AddMarineProviderAuthorization();                        // provider policies (runtime Identity status)

var app = builder.Build();
app.Run();
```
~20 lines; no inline JWT/Keycloak/Refit/HttpClient/business logic; no nested helpers. Swagger + remote-call
config binding + middleware pipeline are provided by the Aizen BFF starter and the Application DI.
