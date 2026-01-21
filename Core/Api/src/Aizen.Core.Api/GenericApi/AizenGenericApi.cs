using Aizen.Core.Api.Abstraction;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniUow.Paging;

namespace Aizen.Core.Infrastructure.Api.GenericApi;

[AllowAnonymous]
[Route("api/[controller]")]
[GenericRestControllerNameConvention]
public class AizenGenericApi<TEntity> : AizenWebApiController, IAizenGenericApi<TEntity>
    where TEntity : AizenEntity
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public AizenGenericApi(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
    {
        _cqrsProcessor = cqrsProcessor;
    }

    [HttpGet("entity")]
    public async Task<AizenApiResponse<TEntity>> GetEntity(
        [FromQuery] int id,
        CancellationToken cancellationToken)
    {
        var query = new AizenGetEntityByIdQuery<TEntity>
        {
            Id = id
        };
        var result = await _cqrsProcessor.ProcessAsync(query, cancellationToken);

        return new AizenApiResponse<TEntity>
        {
            Body = result
        };
    }

    [HttpPost("search/list")]
    public async Task<AizenApiResponse<IList<TEntity>>> SearchEntityForList(
        [FromBody] AizenGenericRequest<TEntity> search,
        CancellationToken cancellationToken)
    {
        var query = new AizenSearchEntityForListQuery<TEntity>
        {
            Search = search.Entity,
            Filters = search.Filters
        };
        var result = await _cqrsProcessor.ProcessAsync(query, cancellationToken);

        return new AizenApiResponse<IList<TEntity>>
        {
            Body = result
        };
    }

    [HttpPost("search/paged")]
    public async Task<AizenApiResponse<IPaginate<TEntity>>> SearchEntityForPaged(
        [FromBody] AizenGenericRequest<TEntity> search,
        [FromQuery] int index,
        [FromQuery] int size,
        CancellationToken cancellationToken)
    {
        var query = new AizenSearchEntityForPagedQuery<TEntity>
        {
            Search = search.Entity,
            Filters = search.Filters,
            Index = index,
            Size = size
        };
        var result = await _cqrsProcessor.ProcessAsync(query, cancellationToken);

        return new AizenApiResponse<IPaginate<TEntity>>
        {
            Body = result
        };
    }
}