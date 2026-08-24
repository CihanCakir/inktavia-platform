using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderParticipation;

/// <summary>
/// Answers "is this provider in the CargoDry programme?" from the only record that can answer it: an Active
/// consignment agreement inside its date window. Read-only, and cheap — a single indexed COUNT.
/// </summary>
public sealed class GetCargoDryProviderParticipationQueryHandler
    : AizenQueryHandler<GetCargoDryProviderParticipationQuery, CargoDryProviderParticipationDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public GetCargoDryProviderParticipationQueryHandler(ICargoDryConsignmentAgreementRepository agreements)
        => _agreements = agreements;

    public override async Task<CargoDryProviderParticipationDto?> Handle(
        GetCargoDryProviderParticipationQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = await _agreements.CountActiveForProviderAsync(request.ProviderProfileId, now, ct);

        return new CargoDryProviderParticipationDto
        {
            ProviderProfileId    = request.ProviderProfileId,
            IsParticipant        = count > 0,
            ActiveAgreementCount = count,
            EvaluatedAtUtc       = now,
        };
    }
}
