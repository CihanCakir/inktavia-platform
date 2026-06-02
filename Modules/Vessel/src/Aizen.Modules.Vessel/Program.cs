using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Cache.Extension;
using Aizen.Modules.Vessel.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Vessel",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

builder.Services.AddAizenUnitOfWork<VesselDbContext>(builder.Configuration, "Vessel", options =>
{
    options.UseMigration = true;
    options.MigrationAssembly = "Aizen.Modules.Vessel.Repository";
    options.UseLazyLoadingProxies = false;
});

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

builder.Services.AddVesselRepository(builder.Configuration).AddVesselServices();

builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(VesselDbContext).Assembly);

var app = builder.Build();

await app.SeedVesselAsync();

app.Run();