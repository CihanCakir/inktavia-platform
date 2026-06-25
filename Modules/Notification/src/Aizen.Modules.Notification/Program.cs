using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.UnitOfWork.Extension;
using Aizen.Core.Starter;
using Aizen.Modules.Notification.Application;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Hubs;
using Aizen.Modules.Notification.Repository;
using Aizen.Modules.Notification.Repository.Persistence;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "Notification",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddAizenUnitOfWork<NotificationDbContext>(builder.Configuration, "Notification", options =>
{
    options.UseMigration      = true;
    options.MigrationAssembly = "Aizen.Modules.Notification.Repository";
});

// ── Repository / Application ───────────────────────────────────────────────────
builder.Services.AddNotificationRepository();
builder.Services.AddNotificationApplicationServices();

// ── SignalR ────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddScoped<IInAppNotificationPusher, NotificationHubPusher>();

var app = builder.Build();

app.MapHub<NotificationHub>("/hubs/notification");

await app.SeedNotificationAsync();

app.Run();
