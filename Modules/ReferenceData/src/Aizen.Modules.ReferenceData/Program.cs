using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Modules.ReferenceData.Repository;
using Aizen.Modules.ReferenceData.Repository.Context;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "ReferenceData",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);



builder.Services.AddAizenUnitOfWork<ReferenceDataDbContext>(builder.Configuration, "ReferenceData", options =>
{
    options.UseMigration = true;
    options.MigrationAssembly = "Aizen.Modules.ReferenceData.Repository";
});


builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

builder.Services.AddReferenceDataRepository(builder.Configuration).AddReferenceDataServices();

builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(ReferenceDataDbContext).Assembly);

var app = builder.Build();

app.Run();