using System.Net.Http;
using System.Text;
using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.RemoteCall.Extensions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// Regression guard for the BFF↔CargoDry-module JSON enum contract.
///
/// History: the CargoDry commercial pages (sales-attributions, settlements) 500'd because the
/// module (Core/Api Newtonsoft + StringEnumConverter) emits enum fields as STRINGS
/// (e.g. "salesChannel":"DirectSale"), while the AdminPanel BFF response DTOs declared those
/// fields as plain <c>int</c>. Refit reads the response with System.Text.Json; its
/// JsonStringEnumConverter only applies to enum-TYPED properties, so an <c>int</c> property hit
/// a string token and Utf8JsonReader.GetInt32() threw. Earlier DB-level 42703 500s masked this;
/// once the schema was repaired the deserialization layer became the failing point instead.
///
/// These tests reproduce the exact wire path: serialize the module DTO with the module-side
/// Newtonsoft settings, then deserialize with the exact Refit content serializer used by every
/// ICargoDryRemoteCall response. They must round-trip without throwing and preserve enum values.
/// </summary>
public class CargoDryCommercialEnumContractTests
{
    // Mirrors Core/Api BuilderExtensions module-side Newtonsoft settings:
    // CamelCase property names + StringEnumConverter + NullValueHandling.Ignore.
    private static readonly JsonSerializerSettings ModuleNewtonsoftSettings = new()
    {
        ContractResolver  = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Converters        = { new StringEnumConverter() },
    };

    // Deserialize using the EXACT Refit content serializer (System.Text.Json +
    // JsonStringEnumConverter, PropertyNameCaseInsensitive) from Core/RemoteCall BuilderExtensions —
    // the same instance every ICargoDryRemoteCall call uses.
    private static async Task<T> DeserializeWithRefitSerializerAsync<T>(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return (await BuilderExtensions.AizenRefitSettings.ContentSerializer
            .FromHttpContentAsync<T>(content))!;
    }

    [Fact]
    public async Task SalesAttribution_moduleStringEnumJson_deserializes_intoBffDto_withEnumValuesPreserved()
    {
        var module = new CargoDrySalesAttributionDto
        {
            Id                  = 42,
            KitId               = 7,
            SerialNumber        = "SN-1",
            KitCode             = "KIT-1",
            ProductCode         = "PRD-1",
            SalesChannel        = SalesChannel.DirectSale,
            SalesChannelName    = "DirectSale",
            CommercialModel     = CargoDryCommercialModel.PrincipalSale,
            CommercialModelName = "PrincipalSale",
            Status              = CargoDrySalesAttributionStatus.Attributed,
            StatusName          = "Attributed",
            CreatedAtUtc        = DateTime.UnixEpoch,
        };

        var json = JsonConvert.SerializeObject(module, ModuleNewtonsoftSettings);

        // Sanity: the module really emits string enum tokens (the shape that used to 500 the BFF).
        json.Should().Contain("\"salesChannel\":\"DirectSale\"");
        json.Should().Contain("\"status\":\"Attributed\"");

        Func<Task> act = () => DeserializeWithRefitSerializerAsync<CargoDrySalesAttributionBffDto>(json);
        await act.Should().NotThrowAsync();

        var bff = await DeserializeWithRefitSerializerAsync<CargoDrySalesAttributionBffDto>(json);
        bff.Id.Should().Be(42);
        bff.SalesChannel.Should().Be(SalesChannel.DirectSale);
        bff.CommercialModel.Should().Be(CargoDryCommercialModel.PrincipalSale);
        bff.Status.Should().Be(CargoDrySalesAttributionStatus.Attributed);
    }

    [Fact]
    public async Task SellThroughSettlement_moduleStringEnumJson_deserializes_intoBffDto_withEnumValuesPreserved()
    {
        var module = new CargoDrySellThroughSettlementDto
        {
            Id                     = 100,
            SettlementCode         = "STL-1",
            ConsignmentAgreementId = 5,
            ProviderProfileId      = 9,
            ProductCode            = "PRD-1",
            CurrencyCode           = "TRY",
            Status                 = CargoDrySellThroughSettlementStatus.ReadyForSettlement,
            StatusName             = "ReadyForSettlement",
            CreatedAtUtc           = DateTime.UnixEpoch,
        };

        var json = JsonConvert.SerializeObject(module, ModuleNewtonsoftSettings);

        json.Should().Contain("\"status\":\"ReadyForSettlement\"");

        Func<Task> act = () => DeserializeWithRefitSerializerAsync<CargoDrySellThroughSettlementBffDto>(json);
        await act.Should().NotThrowAsync();

        var bff = await DeserializeWithRefitSerializerAsync<CargoDrySellThroughSettlementBffDto>(json);
        bff.SettlementCode.Should().Be("STL-1");
        bff.Status.Should().Be(CargoDrySellThroughSettlementStatus.ReadyForSettlement);
    }
}
