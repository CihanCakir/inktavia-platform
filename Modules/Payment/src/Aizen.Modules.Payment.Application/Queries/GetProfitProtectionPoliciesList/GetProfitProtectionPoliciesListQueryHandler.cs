using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPoliciesList;

[DocumentationInfo("GetProfitProtectionPoliciesListQueryHandler",
    "Returns the profit-protection policy version history (no paging — policies are few: one active per currency plus " +
    "historical versions). Optional currency / status / active filters. Sorted by currency then EffectiveFrom desc so " +
    "the newest version of each currency reads first. Used by the admin profit-protection editor's version history.")]
public sealed class GetProfitProtectionPoliciesListQueryHandler
    : AizenQueryHandler<GetProfitProtectionPoliciesListQuery, ProfitProtectionPolicyListResult>
{
    private readonly IProfitProtectionPolicyRepository _policies;

    public GetProfitProtectionPoliciesListQueryHandler(IProfitProtectionPolicyRepository policies)
        => _policies = policies;

    public override async Task<ProfitProtectionPolicyListResult?> Handle(
        GetProfitProtectionPoliciesListQuery request, CancellationToken ct)
    {
        var all = await _policies.GetAllAsync(ct);

        IEnumerable<Domain.Entities.ProfitProtection.ProfitProtectionPolicyEntity> filtered = all;

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var cur = request.CurrencyCode.ToUpperInvariant();
            filtered = filtered.Where(p => p.CurrencyCode == cur);
        }
        if (request.Status.HasValue)
            filtered = filtered.Where(p => p.Status == request.Status.Value);
        if (request.IsActive.HasValue)
            filtered = filtered.Where(p => p.IsActive == request.IsActive.Value);

        var items = filtered
            .OrderBy(p => p.CurrencyCode)
            .ThenByDescending(p => p.EffectiveFrom)
            .Select(ProfitProtectionPolicyDtoMapper.ToDto)
            .ToList();

        return new ProfitProtectionPolicyListResult(items, items.Count);
    }
}
