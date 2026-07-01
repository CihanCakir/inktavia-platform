using Aizen.Core.Cache.Extension;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Modules.Payment.Application;
using Aizen.Modules.Payment.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Core.Common.Extension;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Payment",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler },
}, args);

builder.Services.AddAizenUnitOfWork<PaymentDbContext>(builder.Configuration, "Payment", options =>
{
    options.UseMigration          = true;
    options.MigrationAssembly     = "Aizen.Modules.Payment.Repository";
    options.UseLazyLoadingProxies = false;
});

// ── Repository ─────────────────────────────────────────────────────────────────
builder.Services.AddPaymentRepository(builder.Configuration);

builder.Services.AddPaymentApplication(builder.Configuration);

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenErrorLocalization(builder.Configuration, typeof(PaymentDbContext).Assembly);

var app = builder.Build();
await app.SeedPaymentAsync();

app.Run();
