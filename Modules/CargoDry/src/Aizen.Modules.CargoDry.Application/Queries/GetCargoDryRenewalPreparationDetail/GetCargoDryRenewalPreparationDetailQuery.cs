using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationDetail;

[DocumentationInfo("Get CargoDry renewal preparation detail query",
    "Returns a single renewal preparation by Id or RenewalCode. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationDetailQuery
    : AizenQuery<CargoDryRenewalPreparationDto?>
{
    public long?   Id          { get; init; }
    public string? RenewalCode { get; init; }
}
