using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Options;

namespace Aizen.Core.Infrastructure.RemoteCall;

public class AizenHttpClientFactory : IHttpClientFactory
{
    private readonly IOptions<RemoteCallConfigurations> _options;

    public AizenHttpClientFactory(IOptions<RemoteCallConfigurations> options)
    {
        _options = options;
    }

    public HttpClient CreateClient(string name)
    {
        this._options.Value.TryGetValue(name, out var configuration);
        if (configuration == null)
        {
            var tempClient = new HttpClient();
            return tempClient;
        }

        HttpClient httpClient = new AizenHttpClient(configuration);
        httpClient.BaseAddress = new Uri(configuration.BaseUrl);

        return httpClient;
    }
}

internal class AizenHttpClient : HttpClient
{
    private readonly RemoteCallConfiguration _remoteCallConfiguration;

    public AizenHttpClient(RemoteCallConfiguration remoteCallConfiguration) : base(new AizenHttpClientHandler())
    {
        _remoteCallConfiguration = remoteCallConfiguration;
        if (_remoteCallConfiguration.DefaultHeaders != null)
        {
            foreach (var header in _remoteCallConfiguration.DefaultHeaders)
            {
                this.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}

internal class AizenHttpClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var resStr = request.Content != null ? await request.Content.ReadAsStringAsync() : "";
        var response = await base.SendAsync(request, cancellationToken);
        var reqStr = await response.Content.ReadAsStringAsync();

        if (reqStr.Contains("errors") || reqStr.Contains("Message"))
        {
            response.StatusCode = System.Net.HttpStatusCode.OK;
        }

        return response;
    }
}
