using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Messaging",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
}, args);



var app = builder.Build();



app.Run();