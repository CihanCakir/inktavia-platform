using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// When the wizard picks a catalog brand/model, the BFF denormalizes the display fields (name, hp, fuel) from
/// ReferenceData into the vessel's existing free-text scalars — so nothing downstream depends on the catalog. Ids
/// are passed alongside. Best-effort: a lookup failure falls back to the client-supplied text. Catalog values win
/// over client text when a model is chosen (that's the point of choosing one).
/// </summary>
internal static class MobileCatalogDenormalizer
{
    public static async Task<(string? Brand, string? Model, long? BrandId, long? ModelId)> ResolveVesselAsync(
        IReferenceDataRemoteCall rd, long? modelId, long? brandId, string? brandText, string? modelText)
    {
        var brand = NullIfBlank(brandText);
        var model = NullIfBlank(modelText);
        var vBrandId = brandId;

        if (modelId is > 0)
        {
            try
            {
                var m = (await rd.GetVesselModelById(modelId.Value))?.Body;
                if (m is not null)
                {
                    model = m.Name;
                    brand = m.BrandName ?? brand;
                    vBrandId = brandId ?? m.VesselBrandId;
                }
            }
            catch { /* best-effort — keep client text */ }
        }
        return (brand, model, vBrandId, modelId);
    }

    public static async Task<(string? Brand, string? Model, int? HorsePower, string? FuelType, long? BrandId, long? ModelId)> ResolveEngineAsync(
        IReferenceDataRemoteCall rd, long? modelId, long? brandId, int? hpText, string? fuelText)
    {
        string? brand = null, model = null;
        var hp = hpText;
        var fuel = NullIfBlank(fuelText);
        var eBrandId = brandId;

        if (modelId is > 0)
        {
            try
            {
                var m = (await rd.GetEngineModelById(modelId.Value))?.Body;
                if (m is not null)
                {
                    model = m.Name;
                    brand = m.BrandName;
                    hp = m.HorsePower ?? hp;
                    fuel = string.IsNullOrWhiteSpace(m.FuelTypeCode) ? fuel : m.FuelTypeCode;
                    eBrandId = brandId ?? m.EngineBrandId;
                }
            }
            catch { /* best-effort */ }
        }
        return (brand, model, hp, fuel, eBrandId, modelId);
    }

    private static string? NullIfBlank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
