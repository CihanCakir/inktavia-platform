using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

/// <summary>Orchestrates all ReferenceData JSON seed operations.</summary>
[DocumentationInfo(
    "Service interface for running JSON-based seed operations for the ReferenceData module.",
    "Implementations read JSON files from Seed/Json and idempotently insert or update EF entities and Mongo documents.")]
public interface IReferenceDataJsonSeedService
{
    /// <summary>Runs all seed operations: EF entities, lookup tree, and location documents.</summary>
    Task SeedAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds all EF Core entities: currencies, measurements, system definitions, and lookup groups/items.</summary>
    Task SeedEfEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds all MongoDB location documents: countries, cities, districts, neighborhoods, and streets.</summary>
    Task SeedLocationDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds the lookup group tree and all lookup items.</summary>
    Task SeedLookupTreeAsync(CancellationToken cancellationToken = default);
}
