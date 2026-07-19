using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class CancelCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto> { public long Id { get; init; } }
