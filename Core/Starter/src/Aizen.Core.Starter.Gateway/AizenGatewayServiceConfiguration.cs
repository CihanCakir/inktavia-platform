using System.Reflection;
using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.IOC.Extention;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.RemoteCall.Extentions;
using Aizen.Core.Starter.Abstraction;
using Ocelot.DependencyInjection;
using Ocelot.Responder;

namespace Aizen.Core.Starter.Gateway;

// public class AizenGatewayServiceConfiguration : IAizenServiceConfiguration
// {
//     public AizenAppInfo AppInfo { get; }

//     public AizenGatewayServiceConfiguration(AizenAppInfo appInfo)
//     {
//         AppInfo = appInfo;
//     }

//     public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
//     {
//         AizenConfiguration.Configuration = configuration;

//         OcelotConfigHelper.InitializeConfiguration();
//         ConfigureOcelotConfiguration();

//         _ = services.AddCors();
//         _ = services.AddAizenIOC(configuration);
//         // services.AddAizenApi(configuration);
//         services.AddHttpContextAccessor();

//         _ = services.AddControllers();
//         _ = services.AddLogging();
//         // _ = services.AddEndpointsApiExplorer();

//         ((ConfigurationManager)configuration).SetBasePath(environment.ContentRootPath)
//             .AddJsonFile("ocelot.json", optional: false, reloadOnChange: false)
//             .AddEnvironmentVariables();
        
//         services.AddAizenInfoAccessor(configuration);
//         services.AddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
//         services.AddScoped<IAizenApiGatewayAccessor, AizenApiGatewayAccessor>();

//         services.AddAizenRemoteCall(configuration);
//         services.AddScoped<IAizenMessagePublisher>(sp => null);
//         services.AddScoped<IAuthorizationStrategyManager, AuthorizationStrategyManager>();
//         services.AddScoped<IAuthorizationStrategy, JwtCookieAuthorizationStrategy>();
//         services.AddScoped<IAuthorizationStrategy, JwtHeaderAuthorizationStrategy>();
//         services.AddScoped<IAuthorizationStrategy, BasicAuthorizationStrategy>();

//         services.AddScoped<ICryptographyFactoryManager, CryptographyFactoryManager>();
//         services.AddScoped<ICryptographyAlgorithm, RsaCryptographyAlgorithm>();

//         services.AddOcelot();
//     }

//     private void ConfigureOcelotConfiguration()
//     {
//         var path = GetLocationPath();
//         path = $"{path}ocelot/";

//         var watcher = new FileSystemWatcher(path, "*.json");
//         watcher.IncludeSubdirectories = true;
//         watcher.Changed += OnConfigurationFileChanged;
//         watcher.Created += OnConfigurationFileChanged;
//         watcher.Deleted += OnConfigurationFileChanged;
//         watcher.Renamed += OnConfigurationFileChanged;
//         watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
//         watcher.EnableRaisingEvents = true;
//     }

//     private static void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
//     {
//         OcelotConfigHelper.InitializeConfiguration();
//     }

//     private string GetLocationPath()
//     {
//         var path = string.Empty;
//         var entryAssembly = Assembly.GetEntryAssembly();
//         if (entryAssembly.Location.Contains("bin", StringComparison.CurrentCultureIgnoreCase))
//         {
//             path =
//                 $"{entryAssembly.Location.Substring(0, entryAssembly.Location.IndexOf("bin", StringComparison.CurrentCultureIgnoreCase))}";
//         }

//         if (!entryAssembly.Location.Contains("app", StringComparison.CurrentCultureIgnoreCase))
//         {
//             return path;
//         }

//         path =
//             $"{entryAssembly.Location.Substring(0, entryAssembly.Location.IndexOf("app", StringComparison.CurrentCultureIgnoreCase))}";

//         path = $"{path}/app/";

//         return path;
//     }
// }