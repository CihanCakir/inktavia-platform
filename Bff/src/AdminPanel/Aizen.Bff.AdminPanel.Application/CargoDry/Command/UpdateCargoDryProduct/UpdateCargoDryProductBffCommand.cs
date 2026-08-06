using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.UpdateCargoDryProduct;

public sealed class UpdateCargoDryProductBffCommand : AizenCommand<UpdateCargoDryProductBffResponse>
{
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
    public bool    IsActive       { get; init; }
}

public sealed class UpdateCargoDryProductBffResponse
{
    public CargoDryProductBffDto Product { get; init; } = default!;
}
