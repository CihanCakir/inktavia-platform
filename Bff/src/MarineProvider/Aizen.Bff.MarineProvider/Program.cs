using Aizen.Bff.MarineProvider.Application;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Extensions;
using Aizen.Core.Starter;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineProviderBff",
    Type = AppType.Bff
}, args);

// Application services (Keycloak options, admin client, service token, provider context/resolver,
// outgoing auth handler, Refit remote calls) + inbound Keycloak authentication + provider policies.
builder.Services
    .AddMarineProviderBffApplication(builder.Configuration)
    .AddMarineProviderAuthentication(builder.Configuration)
    .AddMarineProviderAuthorization();

var app = builder.Build();

app.Run();
