using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Cache.Extension;
using Aizen.Modules.FileStorage.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "FileStorage",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

builder.Services.AddAizenUnitOfWork<FileStorageDbContext>(builder.Configuration, "FileStorage", options =>
{
    options.UseMigration = true;
    options.MigrationAssembly = "Aizen.Modules.FileStorage.Repository";
    options.UseLazyLoadingProxies = false;
});

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

builder.Services.AddFileStorageRepository(builder.Configuration).AddFileStorageServices();

builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(FileStorageDbContext).Assembly);

var app = builder.Build();

await app.SeedFileStorageAsync();

app.Run();