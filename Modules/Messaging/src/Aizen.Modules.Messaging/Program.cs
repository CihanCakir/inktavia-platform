using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Starter;
using Aizen.Modules.Messaging.Application;
using Aizen.Modules.Messaging.Application.Configuration;
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
builder.Services.AddMessagingApplicationServices();

// ── BE_WC0 write-cutover flags (scaffolding; all OFF). The SR host reads SystemMessages to gate its writes; the
//    Messaging lifecycle consumers below always run (parallel-then-flip). ──
builder.Services.Configure<MessagingWriteCutoverOptions>(
    builder.Configuration.GetSection(MessagingWriteCutoverOptions.SectionName));

// ── BE_WC1 — shared writer for the Messaging-generated System/lifecycle messages (used by the auto-discovered
//    lifecycle consumers). Scoped: one MessagingDbContext per message, matching the sync consumer. ──
builder.Services.AddScoped<Aizen.Modules.Messaging.Consumers.ServiceRequest.Lifecycle.ServiceRequestLifecycleMessageWriter>();

// ── HTTP clients ──────────────────────────────────────────────────────────
builder.Services.AddHttpClient("anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── Realtime (publish-only; ADR: modules are publish-only, the BFF is the browser edge) ──────────
// The module no longer hosts a browser-facing SignalR hub. Admin observes messaging via the AdminPanel BFF
// (/hubs/admin-messaging) and providers via the MarineProvider BFF (/hubs/provider); both consume the module's
// bus events. Nothing connects to the module's former /hubs/messaging (verified by grep across all web repos +
// services). We keep AddAizenRealtime + the event registry (MessagingRealtimeDomainRegistrar) and the bus
// publishing (SendMessageCommandHandler → MessagingMessageSentMessage). We DROP the browser hub registration
// (AddDomainHub<MessagingHub>) and its MapHub below. MessagingRealtimePublisher's SignalR broadcasts now no-op
// (no "messaging" domain hub resolves) and are left inert for a later cleanup — this deploy only retires the edge.
builder.Services.AddScoped<MessagingRealtimePublisher>();
builder.Services.AddSingleton<IRealtimeDomainRegistrar, MessagingRealtimeDomainRegistrar>();
builder.Services.AddAizenRealtime(builder.Configuration);

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseAizenRealtime();

await app.SeedMessagingAsync();

// Browser-facing hub retired (ADR: modules publish-only). No app.MapHub<MessagingHub>("/hubs/messaging").

app.Run();
