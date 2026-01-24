using Microsoft.AspNetCore.Hosting;

namespace Aizen.Core.Starter.Abstraction;

public interface IAizenApplicationConfiguration
{
    void Configure(AizenApplication app, IWebHostEnvironment env);
}