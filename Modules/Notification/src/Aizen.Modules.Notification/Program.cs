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
    options.UseMigration           = true;
    options.MigrationAssembly      = "Aizen.Modules.Notification.Repository";
    options.UseLazyLoadingProxies  = false;
});

// ── Repository / Application ───────────────────────────────────────────────────
builder.Services.AddNotificationRepository();
builder.Services.AddNotificationApplicationServices(builder.Configuration);

// ── SignalR ────────────────────────────────────────────────────────────────────
var signalRBuilder = builder.Services.AddSignalR();
var signalRRedisConn = builder.Configuration["Realtime:SignalR:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(signalRRedisConn))
    signalRBuilder.AddStackExchangeRedis(signalRRedisConn);
builder.Services.AddScoped<IInAppNotificationPusher, NotificationHubPusher>();

var app = builder.Build();

app.MapHub<NotificationHub>("/hubs/notification");

await app.SeedNotificationAsync();

app.Run();
