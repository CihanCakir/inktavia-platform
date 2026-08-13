using System.Threading.RateLimiting;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Starter;
using Aizen.Modules.Content.Application;
using Aizen.Modules.Content.Repository;
using Microsoft.AspNetCore.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Content",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker },
}, args);

// ── MongoDB (only persistence — no PostgreSQL / EF Core, §1.3) ─────────────────
// Auto-discovers ContentMongoDbContext and its IAizenMongoRepositoryFactory.
builder.Services.AddAizenMongo(builder.Configuration);

// ── Redis Cache (IAizenDistributedCache — required by cacheable read handlers) ─
builder.Services.AddAizenCache(builder.Configuration);

// ── Identity + BFF assertion (surfaces app user + provider identity, §4.7) ────
builder.Services.AddAizenInfoAccessor(builder.Configuration);

// ── Repository (MongoDB context + index initializer) ──────────────────────────
builder.Services.AddContentRepository(builder.Configuration);

// ── Application (CQRS handlers auto-discovered + domain services) ──────────────
builder.Services.AddContentApplicationServices(builder.Configuration);

// ── Public read IP rate limiting ("public-read-ip" policy, per client IP) ─────
var publicReadCfg = builder.Configuration.GetSection("Content:RateLimiting:PublicRead");
var permitLimit = publicReadCfg.GetValue("PermitLimit", 120);
var windowSeconds = publicReadCfg.GetValue("WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("public-read-ip", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

var app = builder.Build();

app.UseRateLimiter();

// ── MongoDB index bootstrap (+ optional demo seed in a later phase) ───────────
await app.SeedContentAsync();

app.Run();
