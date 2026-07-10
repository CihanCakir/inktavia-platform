using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Jobs;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Jobs.GetProviderJobs;

/// <summary>
/// Resolves the provider profile first (this populates the identity holder so the outgoing auth handler attaches
/// the trusted-BFF identity assertion), then calls the ServiceRequest module which scopes the result by the
/// asserted provider profile id. Downstream failures degrade to a warning rather than an error.
/// </summary>
public sealed class GetProviderJobsQueryHandler
    : AizenQueryHandler<GetProviderJobsQuery, GetProviderJobsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetProviderJobsQueryHandler> _logger;

    public GetProviderJobsQueryHandler(
        IProviderProfileResolver resolver,
        IProviderServiceRequestRemoteCall serviceRequest,
        ILogger<GetProviderJobsQueryHandler> logger)
    {
        _resolver = resolver;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<GetProviderJobsResponse?> Handle(
        GetProviderJobsQuery request, CancellationToken cancellationToken)
    {
        var response = new GetProviderJobsResponse
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        response.HasProfileLink = resolution.ProfileId is { } id && id > 0;

        if (!response.HasProfileLink)
        {
            response.Message = "Your account is not linked to a provider profile yet.";
            return response;
        }

        try
        {
            var result = await _serviceRequest.GetProviderJobs(request.PageIndex, request.PageSize);
            var body = result?.Body;

            if (body is not null)
            {
                response.Items = body.Items
                    .Select(j => new ProviderJobDto
                    {
                        AssignmentId = j.AssignmentId,
                        ServiceRequestId = j.ServiceRequestId,
                        ServiceRequestOfferId = j.ServiceRequestOfferId,
                        Status = j.Status,
                        ScheduledStartDate = j.ScheduledStartDate,
                        ScheduledEndDate = j.ScheduledEndDate,
                        ActualStartDate = j.ActualStartDate,
                        ActualEndDate = j.ActualEndDate,
                        ProviderNotes = j.ProviderNotes
                    })
                    .ToList();
            }

            response.TotalReturned = response.Items.Count;
            response.Message = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ServiceRequest provider jobs call failed.");
            response.Message = "Jobs are temporarily unavailable.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("ServiceRequest.GetProviderJobs", ex.GetType().Name));
        }

        return response;
    }
}
