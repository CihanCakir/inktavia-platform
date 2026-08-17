
namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel reference validation service interface", "Validates lookup codes from ReferenceData module for vessel-related fields.")]
public interface IVesselReferenceValidationService
{
    Task<bool> IsValidVesselTypeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidVesselUsageTypeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidHullMaterialAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidEngineTypeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidFuelTypeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidDocumentTypeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidMeasurementUnitAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsValidCountryAsync(string countryCode, CancellationToken cancellationToken = default);
    Task<bool> IsValidCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default);
}
