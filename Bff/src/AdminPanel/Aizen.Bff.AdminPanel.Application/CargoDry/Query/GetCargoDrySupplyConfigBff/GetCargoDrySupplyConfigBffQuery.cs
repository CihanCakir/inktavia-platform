using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySupplyConfigBff;

public sealed class GetCargoDrySupplyConfigBffQuery : AizenQuery<GetCargoDrySupplyConfigBffResponse> { }

public sealed class GetCargoDrySupplyConfigBffResponse
{
    public List<CargoDrySupplyConfigBffDto> Items { get; init; } = new();
}

[DocumentationInfo("Get CargoDry supply config (BFF)",
    "Returns the admin-editable CargoDry supply timeouts (system parameters prefixed 'CargoDry.').")]
public sealed class GetCargoDrySupplyConfigBffQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyConfigBffQuery, GetCargoDrySupplyConfigBffResponse>
{
    private const string Prefix = "CargoDry.";
    private readonly IReferenceDataRemoteCall _refData;
    public GetCargoDrySupplyConfigBffQueryHandler(IReferenceDataRemoteCall refData) => _refData = refData;

    public override async Task<GetCargoDrySupplyConfigBffResponse?> Handle(
        GetCargoDrySupplyConfigBffQuery request, CancellationToken ct)
    {
        var all = await _refData.GetSystemParameters();
        var items = (all.Body ?? new List<Modules.ReferenceData.Abstraction.Dto.SystemParameter.SystemParameterDto>())
            .Where(p => p.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => new CargoDrySupplyConfigBffDto
            {
                Key = p.Key, Value = p.Value, Description = p.Description, IsActive = p.IsActive,
            })
            .OrderBy(p => p.Key)
            .ToList();
        return new GetCargoDrySupplyConfigBffResponse { Items = items };
    }
}
