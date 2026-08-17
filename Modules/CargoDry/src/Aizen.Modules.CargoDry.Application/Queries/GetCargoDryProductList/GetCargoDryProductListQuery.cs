using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;

/// <summary>Returns the active product catalog. Used by batch generation UI and mobile onboarding.</summary>
public sealed class GetCargoDryProductListQuery : AizenQuery<List<CargoDryProductDto>>;
