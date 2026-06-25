using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Starter;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Hubs;
using Aizen.Modules.Messaging.Realtime;
using Aizen.Modules.Messaging.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Messaging",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

// ── Database ──────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<MessagingDbContext>(builder.Configuration, "Messaging", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.Messaging.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Infrastructure ────────────────────────────────────────────────────────
builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

// ── Module services ───────────────────────────────────────────────────────
builder.Services.AddMessagingRepository();
builder.Services.AddMessagingServices();

// ── Realtime (SignalR) ────────────────────────────────────────────────────
builder.Services.AddScoped<MessagingRealtimePublisher>();
builder.Services.AddSingleton<IRealtimeDomainRegistrar, MessagingRealtimeDomainRegistrar>();
builder.Services.AddAizenRealtime(builder.Configuration);
builder.Services.AddDomainHub<MessagingHub>("messaging");

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseAizenRealtime();

await app.SeedMessagingAsync();

app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
