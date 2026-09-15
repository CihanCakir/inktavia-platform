using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProgramApplications;

[DocumentationInfo("Get CargoDry programme applications BFF query handler",
    "Joins the Identity CargoDry-interest list (declared wish) with CargoDry agreement/inventory status (participation). " +
    "Per-provider CargoDry read failures degrade gracefully — the row still shows with default enrichment.")]
public sealed class GetCargoDryProgramApplicationsBffQueryHandler
    : AizenQueryHandler<GetCargoDryProgramApplicationsBffQuery, GetCargoDryProgramApplicationsBffResponse>
{
    private const int ConsignmentAgreementStatusActive = 2; // ConsignmentAgreementStatus.Active

    private readonly IIdentityRemoteCall _identity;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<GetCargoDryProgramApplicationsBffQueryHandler> _logger;

    public GetCargoDryProgramApplicationsBffQueryHandler(
        IIdentityRemoteCall identity, ICargoDryRemoteCall cargoDry,
        ILogger<GetCargoDryProgramApplicationsBffQueryHandler> logger)
    {
        _identity = identity;
        _cargoDry = cargoDry;
        _logger = logger;
    }

    public override async Task<GetCargoDryProgramApplicationsBffResponse?> Handle(
        GetCargoDryProgramApplicationsBffQuery request, CancellationToken ct)
    {
        var interest = await _identity.GetCargoDryInterestApplicants(request.PageIndex, request.PageSize);
        var page = interest?.Body;
        if (page is null || page.Items.Count == 0)
        {
            return new GetCargoDryProgramApplicationsBffResponse
            { PageIndex = request.PageIndex, PageSize = request.PageSize, Total = page?.Total ?? 0 };
        }

        var items = new List<CargoDryProgramApplicationBffDto>(page.Items.Count);
        foreach (var a in page.Items)
        {
            var activeAgreements = 0;
            var hasInventory = false;
            try
            {
                var agreements = await _cargoDry.GetConsignmentAgreementsPagedAsync(
                    a.ProfileId, null, ConsignmentAgreementStatusActive, null, null, null, 1, 1, ct);
                activeAgreements = agreements?.Total ?? 0;

                var inv = await _cargoDry.GetInventoryDetailAsync(a.ProfileId, ct);
                hasInventory = inv?.InventoryRows.Count > 0;
            }
            catch (Exception ex)
            {
                // Degrade gracefully — the applicant still shows; enrichment defaults to none.
                _logger.LogWarning(ex, "CargoDry status enrichment failed for provider {ProfileId} in programme-applications.", a.ProfileId);
            }

            items.Add(new CargoDryProgramApplicationBffDto
            {
                ProfileId                 = a.ProfileId,
                UserId                    = a.UserId,
                OnboardingStatus          = a.OnboardingStatus,
                InterestDeclaredAtUtc     = a.InterestDeclaredAtUtc,
                CommercialModelPreference = a.CommercialModelPreference,
                ActiveAgreementCount      = activeAgreements,
                IsParticipant             = activeAgreements > 0,
                HasInventory              = hasInventory,
            });
        }

        return new GetCargoDryProgramApplicationsBffResponse
        {
            Items = items, Total = page.Total, PageIndex = page.PageIndex, PageSize = page.PageSize,
        };
    }
}
