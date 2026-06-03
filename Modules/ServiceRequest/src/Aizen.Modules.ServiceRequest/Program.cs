using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Hubs;
using Aizen.Modules.ServiceRequest.Realtime;
using Aizen.Modules.ServiceRequest.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "ServiceRequest",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

builder.Services.AddAizenUnitOfWork<ServiceRequestDbContext>(builder.Configuration, "ServiceRequest", options =>
{
    options.UseMigration = true;
    options.MigrationAssembly = "Aizen.Modules.ServiceRequest.Repository";
    options.UseLazyLoadingProxies = false;
});

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

builder.Services.AddServiceRequestRepository(builder.Configuration);
builder.Services.AddServiceRequestServices();

builder.Services.AddScoped<ServiceRequestRealtimePublisher>();

builder.Services.AddSingleton<IRealtimeDomainRegistrar, ServiceRequestRealtimeDomainRegistrar>();

builder.Services.AddAizenRealtime(builder.Configuration);
builder.Services.AddDomainHub<ServiceRequestHub>("servicerequest");

var app = builder.Build();

app.UseAizenRealtime();

await app.SeedServiceRequestAsync();

app.MapHub<ServiceRequestHub>("/hubs/servicerequest");

app.Run();