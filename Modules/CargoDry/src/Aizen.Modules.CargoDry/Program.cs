using Aizen.Core.InfoAccessor.Abstraction;
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

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<CargoDryDbContext>(builder.Configuration, "CargoDry", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.CargoDry.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Repository + Application ──────────────────────────────────────────────────
builder.Services.AddCargoDryRepository();
builder.Services.AddCargoDryApplication();

// ── Redis (IConnectionMultiplexer for ActivationTokenService JTI store) ───────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration["DistributedCache:Configuration"]
            ?? "localhost:6379"));

// ── Rate Limiting (10 req/min/IP on /validate) ────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddSlidingWindowLimiter("validate-ip", limiter =>
    {
        limiter.PermitLimit          = 10;
        limiter.Window               = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow    = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = 0;
    });
    opts.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseRateLimiter();

// ── Seed ──────────────────────────────────────────────────────────────────────
await app.SeedCargoDryAsync();

app.Run();
