using Aizen.Core.Cache.Extension;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Modules.Payment.Application;
using Aizen.Modules.Payment.Consumers.CargoDry;
using Aizen.Modules.Payment.Consumers.ServiceRequest;
using Aizen.Modules.Payment.Consumers.Subscription;
using Aizen.Modules.Payment.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Core.Common.Extension;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Payment",
    Type        = AppType.Operation,

    // Worker    → enables AizenBaseMessageConsumer auto-registration + RabbitMQ consumer wiring
    // Scheduler → enables AizenRecurringJob auto-discovery via Hangfire; reads "Scheduler" config section
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler },
}, args);

// ── Configuration ──────────────────────────────────────────────────────────────
// Loaded automatically by AizenApplicationBuilder from Configuration/{env}.json.
// Environments: local | development | production (set via ASPNETCORE_ENVIRONMENT)

// ── PostgreSQL ─────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<PaymentDbContext>(builder.Configuration, "Payment", options =>
{
    options.UseMigration          = true;
    options.MigrationAssembly     = "Aizen.Modules.Payment.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Repository ─────────────────────────────────────────────────────────────────
builder.Services.AddPaymentRepository(builder.Configuration);

// ── Application (CQRS + Gateway + Commission) ─────────────────────────────────
// AddAizenRecurringJob + AddAizenBackgroundJob are auto-registered by
// AizenOperationServiceConfiguration when AppType.Scheduler is in TypeInclude.
// Scheduler storage type + schema are read from the "Scheduler" appsettings section.
builder.Services.AddPaymentApplication(builder.Configuration);


// ── Redis Cache (IAizenDistributedCache — required by cacheable query handlers) ──
builder.Services.AddAizenCache(builder.Configuration);

// ── Error Localization (reads Resource/aizen_error_messages.json via ErrorLocalization config) ──
builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(PaymentDbContext).Assembly);

var app = builder.Build();

// ── Seed: subscription plans + commission rules ────────────────────────────────
// UseAizenRecurringJob + UseAizenBackgroundJob are called automatically by
// AizenOperationApplicationConfiguration when AppType.Scheduler is in TypeInclude.
await app.SeedPaymentAsync();

app.Run();
