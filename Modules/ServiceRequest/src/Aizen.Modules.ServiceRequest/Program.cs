using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Core.Data.Mongo.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Cache.Extension;
using Aizen.Modules.ServiceRequest.Repository;
// using Aizen.Modules.ServiceRequest.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "ServiceRequest",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

// builder.Services.AddAizenUnitOfWork<ServiceRequestDbContext>(builder.Configuration, "ServiceRequest", options =>
// {
//     options.UseMigration = true;
//     options.MigrationAssembly = "Aizen.Modules.ServiceRequest.Repository";
//     options.UseLazyLoadingProxies = false;
// });

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);
builder.Services.AddAizenInfoAccessor(builder.Configuration);

// builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

// builder.Services.AddServiceRequestRepository(builder.Configuration).AddServiceRequestServices();

// builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(ServiceRequestDbContext).Assembly);

var app = builder.Build();

// await app.SeedServiceRequestAsync();

app.Run();