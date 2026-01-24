using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using Aizen.Core.Api.Abstraction;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Messagebus.Consumers;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Core.Starter.Bff.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniUow.Paging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Aizen.Core.Starter.Api.Generic;

[AllowAnonymous]
[Route("api/[controller]")]
[GenericRestControllerNameConvention]
public class AizenGenericBffApi<TEntity> : AizenWebApiController
    where TEntity : AizenEntity
{
    private static readonly ConcurrentDictionary<string, HttpClient> _httpClients = new();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly RemoteCallConfigurations _remoteCallConfigurations;

    public AizenGenericBffApi(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IAizenMessagePublisher messagePublisher,
        IOptions<RemoteCallConfigurations> remoteCallConfigurations) : base(httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _messagePublisher = messagePublisher;
        _remoteCallConfigurations = remoteCallConfigurations.Value;
    }

    [HttpGet]
    public async Task<AizenApiResponse<TEntity>> GetEntity(
        [FromQuery] int id,
        CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();
        var response = await httpClient.GetAsync($"api/{typeof(TEntity).Name}/entity?id={id}", cancellationToken);
        var result = await GetHttpResponse<TEntity>(response, cancellationToken);

        return result;
    }

    [HttpPost]
    public async Task<AizenApiResponse<TEntity>> AddEntity(
        [FromBody] AizenGenericRequest<TEntity> request,
        CancellationToken cancellationToken)
    {
        var message = new AizenGenericMessage<TEntity>
        {
            Operation = AizenGenericMessageOperation.Create,
            Entity = request.Entity
        };
        var result =
            await _messagePublisher.SendAsync<AizenGenericMessage<TEntity>, AizenGenericMessageResult<TEntity>>(
                message, cancellationToken);

        return new AizenApiResponse<TEntity>
        {
            Body = result.Entity
        };
    }

    [HttpPut]
    public async Task<AizenApiResponse<TEntity>> UpdateEntity(
        [FromBody] AizenGenericRequest<TEntity> request,
        CancellationToken cancellationToken)
    {
        var message = new AizenGenericMessage<TEntity>
        {
            Operation = AizenGenericMessageOperation.Update,
            Entity = request.Entity
        };
        var result =
            await _messagePublisher.SendAsync<AizenGenericMessage<TEntity>, AizenGenericMessageResult<TEntity>>(
                message, cancellationToken);

        return new AizenApiResponse<TEntity>
        {
            Body = result.Entity
        };
    }

    [HttpDelete]
    public async Task<AizenApiResponse<TEntity>> DeleteEntity(
        [FromBody] AizenGenericRequest<TEntity> request,
        CancellationToken cancellationToken)
    {
        var message = new AizenGenericMessage<TEntity>
        {
            Operation = AizenGenericMessageOperation.Delete,
            Entity = request.Entity
        };
        var result =
            await _messagePublisher.SendAsync<AizenGenericMessage<TEntity>, AizenGenericMessageResult<TEntity>>(
                message, cancellationToken);

        return new AizenApiResponse<TEntity>
        {
            Body = result.Entity
        };
    }

    [HttpPost("search/list")]
    public async Task<AizenApiResponse<IList<TEntity>>> SearchEntityForList(
        [FromBody] AizenGenericRequest<TEntity> request,
        CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();
        var jsonContent = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response =
            await httpClient.PostAsync($"api/{typeof(TEntity).Name}/search/list", content, cancellationToken);
        var result = await GetHttpResponse<IList<TEntity>>(response, cancellationToken);

        return result;
    }

    [HttpPost("search/paged")]
    public async Task<AizenApiResponse<IPaginate<TEntity>>> SearchEntityForPaged(
        [FromBody] AizenGenericRequest<TEntity> request,
        [FromQuery] int index,
        [FromQuery] int size,
        CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();
        var jsonContent = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"api/{typeof(TEntity).Name}/search/paged?index={index}&size={size}",
            content, cancellationToken);
        var result = await GetHttpResponse<IPaginate<TEntity>>(response, cancellationToken);

        return result;
    }

    private HttpClient GetHttpClient()
    {
        var fullTypeName = typeof(TEntity).Assembly.FullName;
        var parts = fullTypeName.Split('.');
        var moduleName = $"I{parts[^5]}Module";
        var client = _httpClients.GetOrAdd(moduleName, s =>
        {
            var moduleNameConfiguration = _remoteCallConfigurations[moduleName];
            var httpClient = _httpClientFactory.CreateClient(moduleName);
            httpClient.BaseAddress = new Uri(moduleNameConfiguration.BaseUrl);

            return httpClient;
        });

        return client;
    }

    private async Task<AizenApiResponse<TResult>> GetHttpResponse<TResult>(HttpResponseMessage response,
        CancellationToken cancellationToken)
        where TResult : class
    {
        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<AizenApiResponse<TResult>>(jsonContent, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        return result;
    }
}