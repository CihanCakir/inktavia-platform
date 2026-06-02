using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
// using Aizen.Modules.ReferenceData.Repository;
// using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Cache.Extension;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Vessel",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);



// builder.Services.AddAizenUnitOfWork<ReferenceDataDbContext>(builder.Configuration, "ReferenceData", options =>
// {
//     options.UseMigration = true;
//     options.MigrationAssembly = "Aizen.Modules.ReferenceData.Repository";
//     options.UseLazyLoadingProxies = false;
// });


builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

// builder.Services.AddReferenceDataRepository(builder.Configuration).AddReferenceDataServices();

builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(ReferenceDataDbContext).Assembly);

var app = builder.Build();

// await app.SeedReferenceDataAsync();

app.Run();