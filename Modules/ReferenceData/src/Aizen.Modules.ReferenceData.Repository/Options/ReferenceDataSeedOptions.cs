using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Repository.Options;

/// <summary>Configuration options for the ReferenceData JSON seed system.</summary>
[DocumentationInfo(
    "Options class for configuring the ReferenceData JSON seed behavior.",
    "Bind from appsettings under the 'ReferenceDataSeed' section.")]
public sealed class ReferenceDataSeedOptions
{
    /// <summary>Enables or disables the JSON seed on startup.</summary>
    public bool Enabled { get; set; }

    /// <summary>When true, seeds EF Core entities (currencies, measurements, system parameters, lookups).</summary>
    public bool SeedEfEntities { get; set; }

    /// <summary>When true, seeds MongoDB location documents (countries, cities, districts, neighborhoods, streets).</summary>
    public bool SeedLocationDocuments { get; set; }

    /// <summary>Root path for JSON seed files. Can be absolute or relative to AppContext.BaseDirectory.</summary>
    public string JsonRootPath { get; set; } = "Seed/Json";

    /// <summary>When true, throws an exception if a required JSON seed file is missing.</summary>
    public bool FailOnMissingRequiredFile { get; set; } = true;
}
