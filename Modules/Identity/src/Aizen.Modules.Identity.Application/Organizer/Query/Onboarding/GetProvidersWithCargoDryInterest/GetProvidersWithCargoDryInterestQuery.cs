using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProvidersWithCargoDryInterest;

/// <summary>
/// ADDENDUM A4 — lists providers who declared CargoDry interest (onboarding CargoDryInterest step, DraftJson
/// interested == true). Admin "programme applications" queue source; enriched with CargoDry status at the BFF.
/// </summary>
public sealed class GetProvidersWithCargoDryInterestQuery : AizenQuery<CargoDryInterestApplicantsResult>
{
    public int PageIndex { get; init; } = 0;
    public int PageSize  { get; init; } = 25;
}
