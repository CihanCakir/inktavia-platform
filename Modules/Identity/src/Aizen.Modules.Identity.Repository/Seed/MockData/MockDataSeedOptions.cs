namespace Aizen.Modules.Identity.Repository.Seed.MockData;

/// <summary>Configuration options for the Identity admin-demo mock data seeder.</summary>
public sealed class MockDataSeedOptions
{
    public bool Enabled { get; set; }
    public bool RunOnStartup { get; set; }
    public string[] EnvironmentGuard { get; set; } = ["Local", "Development"];
    public string SeedMode { get; set; } = "InsertMissingOnly";
    public string DataSet { get; set; } = "admin-demo";
}
