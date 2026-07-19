using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto; using Aizen.Modules.CargoDry.Abstraction.Enum;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class GetCargoDryInventoryMovementsBffQuery : AizenQuery<CargoDryInventoryMovementPagedResultDto> { public string? ProductCode { get; init; } public string? BatchCode { get; init; } public InventoryMovementType? MovementType { get; init; } public DateTime? DateFrom { get; init; } public DateTime? DateTo { get; init; } public int Page { get; init; } = 1; public int PageSize { get; init; } = 50; }
