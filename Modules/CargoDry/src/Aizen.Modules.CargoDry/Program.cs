using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Modules.CargoDry.Application;
using Aizen.Modules.CargoDry.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "CargoDry",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler },
}, args);

// ── Configuration (loaded by AizenApplicationBuilder from Configuration/{env}.json) ──────

// ── PostgreSQL ────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<CargoDryDbContext>(builder.Configuration, "CargoDry", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.CargoDry.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── MongoDB ───────────────────────────────────────────────────────────────────
builder.Services.AddAizenMongo(builder.Configuration);

// ── Repository (PostgreSQL + MongoDB) ─────────────────────────────────────────
builder.Services.AddCargoDryRepository(builder.Configuration);

// ── Application (CQRS handlers + services) ────────────────────────────────────
// Note: AddAizenRecurringJob + AddAizenBackgroundJob are registered automatically
// by AizenOperationServiceConfiguration when AppType.Scheduler is in TypeInclude.
// Scheduler storage type and options are read from appsettings.json "Scheduler" section.
builder.Services.AddCargoDryApplication();

// ── Identity + BFF Assertion (provider-scoped endpoints) ─────────────────────
builder.Services.AddAizenInfoAccessor(builder.Configuration);

// ── Redis Cache (IAizenDistributedCache — required by cacheable handlers) ─────
builder.Services.AddAizenCache(builder.Configuration);

// ── Redis Connection (for ActivationTokenService JTI store) ──────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration["DistributedCache:Configuration"]
            ?? "localhost:6379"));

// ── IP Rate Limiting ──────────────────────────────────────────────────────────
var rateLimitConfig = builder.Configuration.GetSection("CargoDry:RateLimiting:ValidateEndpoint");
builder.Services.AddRateLimiter(opts =>
{
    opts.AddSlidingWindowLimiter("validate-ip", limiter =>
    {
        limiter.PermitLimit          = rateLimitConfig.GetValue<int>("PermitLimit", 10);
        limiter.Window               = TimeSpan.FromSeconds(
            rateLimitConfig.GetValue<int>("WindowSeconds", 60));
        limiter.SegmentsPerWindow    = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = 0;
    });
    opts.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseRateLimiter();

// ── Seed + MongoDB Index Bootstrap ────────────────────────────────────────────
// Note: UseAizenRecurringJob + UseAizenBackgroundJob are called automatically
// by AizenOperationApplicationConfiguration when AppType.Scheduler is in TypeInclude.
await app.SeedCargoDryAsync();

app.Run();
