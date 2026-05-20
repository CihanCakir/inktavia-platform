
using Aizen.Core.InfoAccessor;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Api;
using Aizen.Core.Starter.Scheduler;
using Aizen.Core.Starter.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Aizen.Core.Infrastructure.Auth.Extension;
using Aizen.Core.Starter.Operation;
using Aizen.Core.IOC.Extension;

namespace Aizen.Core.Starter;

public class AizenApplicationBuilder
{
    private readonly WebApplicationBuilder _builder;

    public IWebHostEnvironment Environment => _builder.Environment;

    public IServiceCollection Services => _builder.Services;

    public ConfigurationManager Configuration => _builder.Configuration;

    public ILoggingBuilder Logging => _builder.Logging;

    public ConfigureWebHostBuilder WebHost => _builder.WebHost;

    public ConfigureHostBuilder Host => _builder.Host;

    public AizenAppInfo AppInfo { get; set; }

    public AizenServerInfo ServerInfo { get; set; }

    private AizenApplicationBuilder(AizenAppInfo appInfo, string[] args)
    {
        AppInfo = appInfo;
        _builder = WebApplication.CreateBuilder(args);

        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configuration");
        var configBuilder = _builder.Configuration
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{_builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

        if (!IsRunningInDocker())
            configBuilder.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

        configBuilder.AddEnvironmentVariables();

        _builder.Host.UseAizenServiceProviderFactory();
    }

    private static bool IsRunningInDocker() =>
        System.Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
        || File.Exists("/.dockerenv");



    public AizenApplicationBuilder AddAizenAuth<TUser, TRole, TContext>()
           where TUser : IdentityUser<long>
           where TRole : IdentityRole<long>
           where TContext : IdentityDbContext<TUser, TRole, long>
    {
        _builder.Services.AddAizenAuth<TUser, TRole, TContext>(_builder);
        return this;
    }

    // 2) 8 parametreli tam generic sürüm (istersen bunu da kullan)
    public AizenApplicationBuilder AddAizenAuth<
        TUser, TRole,
        TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken,
        TContext>()
        where TUser : IdentityUser<long>
        where TRole : IdentityRole<long>
        where TUserClaim : IdentityUserClaim<long>
        where TUserRole : IdentityUserRole<long>
        where TUserLogin : IdentityUserLogin<long>
        where TRoleClaim : IdentityRoleClaim<long>
        where TUserToken : IdentityUserToken<long>
        where TContext : IdentityDbContext<TUser, TRole, long, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
    {
        _builder.Services.AddAizenAuth<
            TUser, TRole,
            TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken,
            TContext>(_builder);
        return this;
    }


    public static AizenApplicationBuilder CreateBuilder(AizenAppInfo appInfo, string[] args = null)
    {
        return new AizenApplicationBuilder(appInfo, args);
    }

    public AizenApplication Build()
    {
        switch (AppInfo.Type)
        {
            case AppType.Api:
                return BuildForApi();
            // case AppType.Gateway:
            //     return BuildForGateway();
            case AppType.Worker:
                return BuildForWorker();
            case AppType.Scheduler:
                return BuildForScheduler();
            case AppType.Bff:
                return BuildForBff();
            case AppType.Operation:
                return BuildForOperation();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private AizenApplication BuildForApi()
    {
        var serviceConfiguration = new AizenApiServiceConfiguration(AppInfo);
        serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

        var application = new AizenApiApplication(_builder.Build());

        var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
        infoContainer.Set(AppInfo);

        var applicationConfiguration = new AizenApiApplicationConfiguration();
        applicationConfiguration.Configure(application, application.Environment);

        return application;
    }

    // private AizenApplication BuildForGateway()
    // {
    //     var serviceConfiguration = new AizenGatewayServiceConfiguration(AppInfo);
    //     serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

    //     var application = new AizenGatewayApplication(_builder.Build());

    //     var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
    //     infoContainer.Set(AppInfo);

    //     var applicationConfiguration = new AizenGatewayApplicationConfiguration();
    //     applicationConfiguration.Configure(application, application.Environment);

    //     return application;
    // }


    private AizenApplication BuildForBff()
    {
        var serviceConfiguration = new AizenBffServiceConfiguration(AppInfo);
        serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

        var application = new AizenBffApplication(_builder.Build());

        var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
        infoContainer.Set(AppInfo);

        var applicationConfiguration = new AizenBffApplicationConfiguration();
        applicationConfiguration.Configure(application, application.Environment);

        return application;
    }

    private AizenApplication BuildForWorker()
    {
        var serviceConfiguration = new AizenWorkerServiceConfiguration(AppInfo);
        serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

        var application = new AizenWorkerApplication(_builder.Build());

        var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
        infoContainer.Set(AppInfo);

        var applicationConfiguration = new AizenWorkerApplicationConfiguration();
        applicationConfiguration.Configure(application, application.Environment);

        return application;
    }

    private AizenApplication BuildForScheduler()
    {
        var serviceConfiguration = new AizenSchedulerServiceConfiguration(AppInfo);
        serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

        var application = new AizenSchedulerApplication(_builder.Build());

        var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
        infoContainer.Set(AppInfo);

        var applicationConfiguration = new AizenSchedulerApplicationConfiguration();
        applicationConfiguration.Configure(application, application.Environment);

        return application;
    }

    private AizenApplication BuildForOperation()
    {
        var serviceConfiguration = new AizenOperationServiceConfiguration(AppInfo);
        serviceConfiguration.Configure(_builder.Services, _builder.Configuration, _builder.Environment);

        var application = new AizenOperationApplication(_builder.Build());

        var infoContainer = application.Services.GetRequiredService<AizenInfoContainerForSigleton>();
        infoContainer.Set(AppInfo);

        var applicationConfiguration = new AizenOperationApplicationConfiguration(AppInfo);
        applicationConfiguration.Configure(application, application.Environment);
        return application;
    }
}