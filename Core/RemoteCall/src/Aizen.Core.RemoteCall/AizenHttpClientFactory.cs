using System.Net.Http.Headers;
using System.Text;
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
        var response = await base.SendAsync(request, cancellationToken);

        // Read the response body once; then restore the content so downstream (Refit) can read it again.
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        var charset  = response.Content.Headers.ContentType?.CharSet ?? "utf-8";
        var encoding = Encoding.GetEncoding(charset);

        response.Content = new StringContent(responseBody, encoding, mediaType);

        // Legacy compatibility: promote non-2xx responses that carry an Aizen envelope body
        // to 200 so Refit can deserialize them as typed AizenApiResponse<T>.
        if (responseBody.Contains("errors") || responseBody.Contains("Message"))
        {
            response.StatusCode = System.Net.HttpStatusCode.OK;
        }

        return response;
    }
}
