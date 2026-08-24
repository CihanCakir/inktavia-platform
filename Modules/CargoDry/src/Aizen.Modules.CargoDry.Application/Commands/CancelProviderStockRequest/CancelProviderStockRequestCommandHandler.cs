using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.CancelProviderStockRequest;

public sealed class CancelProviderStockRequestCommandHandler : AizenCommandHandler<CancelProviderStockRequestCommand, bool>
{
    private readonly ICargoDryStockRequestRepository _requests;
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public CancelProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests,
        ICargoDryConsignmentAgreementRepository agreements)
    { _requests = requests; _agreements = agreements; }


    /// <summary>
    /// PROV-MVP-002 — participation is checked HERE, not only at the BFF.
    ///
    /// Verified 2026-08-24: neither this handler nor the BFF checked whether the caller is in the CargoDry
    /// programme. The BFF's only gate was `ProviderActive` ("any approved, active provider on the platform") and
    /// this handler validated product code, quantity, product existence and duplicate-pending — all questions
    /// about the REQUEST, none about the CALLER. A provider whose onboarding said
    /// `CargoDryInterest.interested = false` could create a real stock-request row against any product.
    ///
    /// A BFF policy alone is not sufficient: `CargoDryProviderController` carries a bare [Authorize] and resolves
    /// the provider from the `provider_profile_id` token CLAIM, so the module is reachable by anything holding a
    /// valid token — not only by the BFF. The write model has to defend itself.
    /// </summary>
    public override async Task<bool> Handle(CancelProviderStockRequestCommand request, CancellationToken ct)
    {
        var activeAgreements = await _agreements.CountActiveForProviderAsync(
            request.ProviderProfileId, DateTime.UtcNow, ct);
        if (activeAgreements == 0)
            throw new AizenBusinessException("CD_PROVIDER_NOT_IN_PROGRAMME");

        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException("Stock request not found.");

        if (entity.ProviderProfileId != request.ProviderProfileId)
            throw new AizenBusinessException("Stock request not found.");

        entity.Cancel(request.Reason);
        await _requests.SaveChangesAsync(ct);
        return true;
    }
}
