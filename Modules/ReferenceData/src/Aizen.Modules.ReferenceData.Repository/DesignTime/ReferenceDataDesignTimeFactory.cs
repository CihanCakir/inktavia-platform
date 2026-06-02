using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.DesignTime;

public class ReferenceDataDesignTimeFactory : IDesignTimeDbContextFactory<ReferenceDataDbContext>
{
    private const string STARTUP_PROJECT_NAME = "Aizen.Modules.ReferenceData";
    private const string DB_NAME = "ReferenceData";

    public ReferenceDataDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var cs = ResolveConnectionString(configuration, DB_NAME);

        var opts = new DbContextOptionsBuilder<ReferenceDataDbContext>()
            .UseNpgsql(cs, b => b.MigrationsAssembly("Aizen.Modules.ReferenceData.Repository"))
            .Options;

        return new ReferenceDataDbContext(opts);
    }

    private static IConfiguration BuildConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";
        var basePath = ProbeStartupPath();

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    private static string ProbeStartupPath()
    {
        var current = Directory.GetCurrentDirectory();

        var candidate = Path.GetFullPath(Path.Combine(current, "..", STARTUP_PROJECT_NAME, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            return candidate;

        candidate = Path.GetFullPath(Path.Combine(current, "..", "..", STARTUP_PROJECT_NAME, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            return candidate;

        candidate = Path.GetFullPath(Path.Combine(current, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            return candidate;

        throw new InvalidOperationException(
            $"appsettings.json bulunamadı. '{STARTUP_PROJECT_NAME}/configuration' klasörünü doğrulayın.");
    }

    private static string ResolveConnectionString(IConfiguration configuration, string name)
    {
        var fromDatabaseSettings = configuration.GetSection("DatabaseSettings")?
            .GetSection(name)?["ConnectionString"];

        var fromConnectionStrings = configuration.GetConnectionString(name);

        var cs = fromDatabaseSettings ?? fromConnectionStrings;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                $"Connection string bulunamadı. `DatabaseSettings:{name}:ConnectionString` veya `ConnectionStrings:{name}` tanımlayın.");

        return cs;
    }
}
