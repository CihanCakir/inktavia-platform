using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Starter.Abstraction;

public interface IAizenServiceConfiguration
{
    void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment);
}