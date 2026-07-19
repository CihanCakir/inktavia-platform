using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto; using Aizen.Modules.CargoDry.Abstraction.Enum;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class GetCargoDryInventoryBffQuery : AizenQuery<CargoDryProviderInventoryPagedResultDto> { public string? ProductCode { get; init; } public CargoDryCommercialModel? CommercialModel { get; init; } public SalesChannel? SalesChannel { get; init; } public bool? HasAvailableStock { get; init; } public string? Search { get; init; } public int Page { get; init; } = 1; public int PageSize { get; init; } = 25; }
