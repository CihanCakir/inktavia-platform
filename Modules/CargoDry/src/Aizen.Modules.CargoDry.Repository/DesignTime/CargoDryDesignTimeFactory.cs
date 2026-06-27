using System;
using System.IO;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.CargoDry.Repository.DesignTime;

public class CargoDryDesignTimeFactory : IDesignTimeDbContextFactory<CargoDryDbContext>
{
    private const string STARTUP_PROJECT_NAME = "Aizen.Modules.CargoDry";
    private const string DB_NAME              = "CargoDry";

    public CargoDryDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var cs            = ResolveConnectionString(configuration, DB_NAME);

        var opts = new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseNpgsql(cs, b => b.MigrationsAssembly("Aizen.Modules.CargoDry.Repository"))
            .Options;

        return new CargoDryDbContext(opts);
    }

    private static IConfiguration BuildConfiguration()
    {
        var env      = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";
        var basePath = ProbeStartupPath();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json",        optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ProbeStartupPath()
    {
        var current = Directory.GetCurrentDirectory();

        var candidate = Path.GetFullPath(Path.Combine(current, "..", STARTUP_PROJECT_NAME, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;

        candidate = Path.GetFullPath(Path.Combine(current, "..", "..", STARTUP_PROJECT_NAME, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;

        candidate = Path.GetFullPath(Path.Combine(current, "configuration"));
        if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;

        throw new InvalidOperationException(
            $"appsettings.json bulunamadı. '{STARTUP_PROJECT_NAME}/configuration' klasörünü doğrulayın.");
    }

    private static string ResolveConnectionString(IConfiguration configuration, string name)
    {
        var cs = configuration.GetSection("DatabaseSettings")?
                     .GetSection(name)?["ConnectionString"]
                 ?? configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                $"Connection string bulunamadı. `DatabaseSettings:{name}:ConnectionString` veya `ConnectionStrings:{name}` tanımlayın.");

        return cs;
    }
}
