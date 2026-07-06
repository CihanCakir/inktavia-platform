using Aizen.Core.Cache.Extension;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Modules.Profile.Application;
using Aizen.Modules.Profile.Repository;
using Aizen.Modules.Profile.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Profile",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api },
}, args);

// ── PostgreSQL ────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<ProfileDbContext>(builder.Configuration, "Profile", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.Profile.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Repository ────────────────────────────────────────────────────────────────
builder.Services.AddProfileRepository(builder.Configuration);

// ── Application (CQRS handlers + services) ────────────────────────────────────
builder.Services.AddProfileApplication();

// ── Redis Cache ───────────────────────────────────────────────────────────────
builder.Services.AddAizenCache(builder.Configuration);

var app = builder.Build();

// ── Seed + Migration ──────────────────────────────────────────────────────────
await app.SeedProfileAsync();

app.Run();
