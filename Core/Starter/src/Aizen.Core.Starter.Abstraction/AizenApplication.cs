using Elastic.Apm.AspNetCore;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Azure.CosmosDb;
using Elastic.Apm.Azure.ServiceBus;
using Elastic.Apm.Azure.Storage;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.MongoDb;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;

namespace Aizen.Core.Starter.Abstraction;

public abstract class AizenApplication : IHost, IApplicationBuilder, IEndpointRouteBuilder, IAsyncDisposable
{
    private readonly WebApplication _application;

    protected AizenApplication(WebApplication application)
    {
        _application = application;
        _application.UseElasticApm(_application.Configuration,
            new HttpDiagnosticsSubscriber(),
            new SqlClientDiagnosticSubscriber(),
            new EfCoreDiagnosticsSubscriber(),
            new ElasticsearchDiagnosticsSubscriber(),
            new GrpcClientDiagnosticSubscriber(),
            new AzureMessagingServiceBusDiagnosticsSubscriber(),
            new MicrosoftAzureServiceBusDiagnosticsSubscriber(),
            new AzureBlobStorageDiagnosticsSubscriber(),
            new AzureQueueStorageDiagnosticsSubscriber(),
            new AzureFileShareStorageDiagnosticsSubscriber(),
            new AzureCosmosDbDiagnosticsSubscriber(),
            new MongoDbDiagnosticsSubscriber(),
            new AspNetCoreDiagnosticSubscriber()
        );
    }

    public void Dispose()
    {
        ((IDisposable)_application).Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return _application.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return _application.StopAsync(cancellationToken);
    }

    public IServiceProvider Services => _application.Services;

    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        return ((IApplicationBuilder)_application).Use(middleware);
    }

    public IApplicationBuilder New()
    {
        return ((IApplicationBuilder)_application).New();
    }

    public RequestDelegate Build()
    {
        return ((IApplicationBuilder)_application).Build();
    }

    public IServiceProvider ApplicationServices
    {
        get => ((IApplicationBuilder)_application).ApplicationServices;
        set => ((IApplicationBuilder)_application).ApplicationServices = value;
    }

    public IFeatureCollection ServerFeatures => ((IApplicationBuilder)_application).ServerFeatures;

    public IDictionary<string, object?> Properties => ((IApplicationBuilder)_application).Properties;

    public IApplicationBuilder CreateApplicationBuilder()
    {
        return ((IEndpointRouteBuilder)_application).CreateApplicationBuilder();
    }

    public IServiceProvider ServiceProvider => ((IEndpointRouteBuilder)_application).ServiceProvider;

    public ICollection<EndpointDataSource> DataSources => ((IEndpointRouteBuilder)_application).DataSources;

    public IWebHostEnvironment Environment => _application.Environment;

    public ValueTask DisposeAsync()
    {
        return _application.DisposeAsync();
    }
}