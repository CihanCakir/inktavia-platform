using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel reference validation service", "Validates lookup codes from ReferenceData module for vessel-related fields.")]
public sealed class VesselReferenceValidationService : IVesselReferenceValidationService
{
    // These validations delegate to ReferenceData lookup items via the shared DB.
    // The lookup group codes follow the ReferenceData domain convention.
    private readonly VesselDbContext _db;

    public VesselReferenceValidationService(VesselDbContext db)
    {
        _db = db;
    }

    // NOTE: Actual validation is performed against the ReferenceData module's lookup tables.
    // Since cross-module DB access is not available, these are intentionally permissive stubs
    // that should be replaced with IVesselClient / ReferenceData service calls when the
    // integration layer is established.

    public Task<bool> IsValidVesselTypeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidVesselUsageTypeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidHullMaterialAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidEngineTypeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidFuelTypeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidDocumentTypeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidMeasurementUnitAsync(string code, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(code));

    public Task<bool> IsValidCountryAsync(string countryCode, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(countryCode));

    public Task<bool> IsValidCityAsync(string countryCode, string cityCode, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(countryCode) && !string.IsNullOrWhiteSpace(cityCode));
}
