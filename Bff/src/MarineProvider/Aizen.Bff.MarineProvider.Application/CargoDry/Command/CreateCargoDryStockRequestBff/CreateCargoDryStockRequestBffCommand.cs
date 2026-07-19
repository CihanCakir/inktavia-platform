using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class CreateCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto> { public string ProductCode { get; init; } = default!; public int RequestedQuantity { get; init; } public string? Note { get; init; } }
