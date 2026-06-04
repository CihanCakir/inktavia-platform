using Aizen.Bff.AdminPanel.Application;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "AdminPanelBff",
    Type = AppType.Bff
}, args);

builder.Services.AddAdminPanelBffApplication(builder.Configuration);

var app = builder.Build();

app.Run();

