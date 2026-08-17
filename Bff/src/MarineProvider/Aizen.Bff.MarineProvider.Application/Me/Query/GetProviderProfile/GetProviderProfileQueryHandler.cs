using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Me;

public sealed class GetProviderProfileQueryHandler
    : AizenQueryHandler<GetProviderProfileQuery, GetProviderProfileResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly ILogger<GetProviderProfileQueryHandler> _logger;

    public GetProviderProfileQueryHandler(
        IProviderProfileResolver resolver,
        ILogger<GetProviderProfileQueryHandler> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public override async Task<GetProviderProfileResponse?> Handle(
        GetProviderProfileQuery request, CancellationToken cancellationToken)
    {
        var response = new GetProviderProfileResponse();

        try
        {
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            response.HasProfileLink = resolution.ProfileId is > 0;

            var dto = resolution.Profile;
            if (dto is null)
            {
                response.Message = "No provider profile is linked to this account yet.";
                return response;
            }

            response.Profile = new ProviderProfileDto
            {
                ProviderProfileId = dto.Id,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.OrganizationName,
                OwnerFirstName = dto.FirstName,
                OwnerLastName = dto.LastName,
                City = dto.City,
                Country = dto.Country,
                TaxpayerType = dto.TaxpayerType,
                Bio = dto.Bio,
                ProfilePhotoUrl = dto.ProfilePhotoUrl,
                ApprovalStatus = dto.ApprovalStatus,
                ProfileStatus = dto.Status,
                DocumentCount = dto.Documents?.Count ?? 0,
                ApprovedAt = dto.ApprovedAt,
                RejectedAt = dto.RejectedAt,
                RejectReason = dto.RejectReason
            };
            response.Message = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider profile resolution failed.");
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.GetOrganizerProfile", ex.GetType().Name));
            response.Message = "Provider profile is temporarily unavailable.";
        }

        return response;
    }
}
