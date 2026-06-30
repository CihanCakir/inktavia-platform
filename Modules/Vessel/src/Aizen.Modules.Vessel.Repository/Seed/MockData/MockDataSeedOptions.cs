
namespace Aizen.Modules.Vessel.Repository.Seed.MockData;

/// <summary>Configuration options for the Vessel admin-demo mock data seeder.</summary>
[DocumentationInfo("MockData seed options", "Configuration options for the admin demo mock data seeder for the Vessel module.")]
public sealed class MockDataSeedOptions
{
    public bool Enabled { get; set; }
    public bool RunOnStartup { get; set; }
    public string[] EnvironmentGuard { get; set; } = ["Local", "Development"];
    public string SeedMode { get; set; } = "InsertMissingOnly";
    public string DataSet { get; set; } = "admin-demo";
}
