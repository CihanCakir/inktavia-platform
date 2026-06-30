using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;

/// <summary>
/// Registers a provider as an Iyzico sub-merchant and stores the resulting subMerchantKey
/// on their ProviderPaymentProfileEntity. Must be called once before the provider can receive
/// marketplace payouts.
/// </summary>
public sealed class RegisterSubMerchantCommand : AizenCommand<RegisterSubMerchantResult>
{
    public required long   ProviderProfileId  { get; init; }
    public required string LegalName          { get; init; }
    public required string Email              { get; init; }
    public required string Iban               { get; init; }

    /// <summary>PERSONAL | PRIVATE_COMPANY | LIMITED_OR_JOINT_STOCK_COMPANY</summary>
    public          string SubMerchantType    { get; init; } = "PRIVATE_COMPANY";

    public          string? TaxNumber        { get; init; }
    public          string? TaxOffice        { get; init; }
    public          string? GsmNumber        { get; init; }
    public          string? ContactName      { get; init; }
    public          string? ContactSurname   { get; init; }
    public          string  Address          { get; init; } = "N/A";
}
