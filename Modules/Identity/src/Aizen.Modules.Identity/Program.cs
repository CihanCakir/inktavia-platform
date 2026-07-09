using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Common.Extension;
using Microsoft.AspNetCore.Identity;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Core.Domain.Abstraction.Extension;
using Aizen.Modules.Identity.Repository;
using Aizen.Modules.Identity.Extensions;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Identity",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args)
.AddAizenAuth<
    UserEntity, RoleEntity,
    IdentityUserClaim<long>, UserRoleEntity, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>,
    IdentityDbContext>();


builder.Services.AddAizenUnitOfWork<IdentityDbContext>(builder.Configuration, "Identity", options =>
{
    options.UseMigration = true;
    options.MigrationAssembly = "Aizen.Modules.Identity.Repository";
});


builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(nameof(ApplicationSettings)));

builder.Services.AddInktaviaService(builder.Configuration).AddInktaviaRepository();

builder.Services.AddIdentityMockData(builder.Configuration);

builder.Services.AddInktaviaAuthorizationPolicies();

builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(IdentityDbContext).Assembly);

var app = builder.Build();
await app.SeedIdentityAsync();


app.Run();