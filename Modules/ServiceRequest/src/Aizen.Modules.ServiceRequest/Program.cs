using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Starter;
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

builder.Services.AddServiceRequestRepository();
builder.Services.AddServiceRequestServices();

// BE_WC4b — the WriteCutover flag machinery is gone (Phase-4 complete): the SR module no longer writes sr.Messages
// chat/System rows; the Messaging store is the sole producer, unconditionally.
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.OfferCalculationService>();
// FIX_ASSIGNMENT_ON_ACCEPT — shared create-assignment path (owner auto-accept + manual endpoint; idempotent).
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.ServiceRequestAssignmentCreator>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.UnitCodeValidator>();

// ── Pricing attributes (BE-S2) ──
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Pricing.ReferenceDataLookupClient>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Pricing.PricingAttributeDefinitionValidator>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Pricing.PricingAttributeValidator>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Pricing.PricingAttributeSnapshotResolver>();

// ── Travel / mobilization pricing (BE-S4) ──
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Travel.TravelPricingValidator>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Travel.TravelPricingSnapshotResolver>();

// ── Offer FX (BE-S3) — submit-time foreign→TRY conversion + point-in-time rate snapshot ──
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Fx.IExchangeRateSource,
    Aizen.Modules.ServiceRequest.Application.Services.Fx.ExchangeRateSource>();
builder.Services.AddScoped<Aizen.Modules.ServiceRequest.Application.Services.Fx.OfferFxResolver>();

builder.Services.AddServiceRequestMockData(builder.Configuration);

builder.Services.AddScoped<ServiceRequestRealtimePublisher>();

builder.Services.AddSingleton<IRealtimeDomainRegistrar, ServiceRequestRealtimeDomainRegistrar>();

builder.Services.AddAizenRealtime(builder.Configuration);
builder.Services.AddDomainHub<ServiceRequestHub>("servicerequest");

var app = builder.Build();

app.UseAizenRealtime();

await app.SeedServiceRequestAsync();

app.MapHub<ServiceRequestHub>("/hubs/servicerequest");

app.Run();