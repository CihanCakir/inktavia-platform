using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;

public sealed class GetAdminKitListQuery : AizenQuery<GetAdminKitListResponse>
{
    public CargoDryKitStatus? Status    { get; init; }
    public string?            Search    { get; init; }
    public long?              VesselId  { get; init; }
    public string?            BatchCode { get; init; }
    public int                Page      { get; init; } = 1;
    public int                PageSize  { get; init; } = 25;
}

public sealed class GetAdminKitListResponse
{
    public List<CargoDryKitDto> Items    { get; init; } = [];
    public int                  Total    { get; init; }
    public int                  Page     { get; init; }
    public int                  PageSize { get; init; }
}
