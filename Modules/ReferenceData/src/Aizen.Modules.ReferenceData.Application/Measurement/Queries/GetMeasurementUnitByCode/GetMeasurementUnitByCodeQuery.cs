using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

/// <summary>
/// R3 — resolves a single measurement unit by its code (e.g. LITER, KILOMETER, SQUARE_METER,
/// HOUR, DAY). Lets the ServiceRequest PricingMethod map to a concrete unit at offer build time.
/// </summary>
public sealed class GetMeasurementUnitByCodeQuery : AizenQuery<MeasurementUnitDto?>
{
    public string Code { get; }

    public GetMeasurementUnitByCodeQuery(string code)
    {
        Code = code;
    }
}
