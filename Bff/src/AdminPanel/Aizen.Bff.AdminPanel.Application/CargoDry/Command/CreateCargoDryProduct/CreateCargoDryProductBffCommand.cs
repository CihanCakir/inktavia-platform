using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.CreateCargoDryProduct;

public sealed class CreateCargoDryProductBffCommand : AizenCommand<CreateCargoDryProductBffResponse>
{
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
}

public sealed class CreateCargoDryProductBffResponse
{
    public CargoDryProductBffDto Product { get; init; } = default!;
}
