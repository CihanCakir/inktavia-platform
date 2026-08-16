using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebLocationBySlug;

public sealed class GetWebLocationBySlugQueryHandler
    : AizenQueryHandler<GetWebLocationBySlugQuery, WebLocationDetailDto>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebLocationBySlugQueryHandler> _logger;

    public GetWebLocationBySlugQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebLocationBySlugQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<WebLocationDetailDto?> Handle(
        GetWebLocationBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = (request.Slug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(slug))
            throw new AizenBusinessException("A location slug is required.");

        try
        {
            var resolved = (await _reference.GetLocationBySlug(slug))?.Body
                ?? throw new AizenBusinessException($"Unknown location '{slug}'.");
            return WebLocationMapper.FromBySlug(resolved);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web location by-slug '{Slug}' failed (status {Status}).", slug, ex.StatusCode);
            throw new AizenBusinessException("Location reference data is currently unavailable.");
        }
    }
}
