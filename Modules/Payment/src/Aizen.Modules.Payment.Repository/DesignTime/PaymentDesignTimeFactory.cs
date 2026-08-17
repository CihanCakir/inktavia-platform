using System;
using System.IO;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.Payment.Repository.DesignTime;

/// <summary>
/// Design-time factory for EF Core migration tooling.
///
/// Usage (from the Aizen.Modules.Payment host project directory):
///   dotnet ef migrations add &lt;MigrationName&gt; --project ../Aizen.Modules.Payment.Repository
///   dotnet ef database update             --project ../Aizen.Modules.Payment.Repository
///
/// Connection string resolved from DatabaseSettings:Payment:ConnectionString
/// in Aizen.Modules.Payment/configuration/appsettings[.Local].json
/// </summary>
public class PaymentDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    private const string STARTUP_PROJECT_NAME = "Aizen.Modules.Payment";
    private const string DB_NAME              = "Payment";

    public PaymentDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var cs            = ResolveConnectionString(configuration, DB_NAME);

        var opts = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(cs, b => b.MigrationsAssembly("Aizen.Modules.Payment.Repository"))
            .Options;

        return new PaymentDbContext(opts);
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
                $"Connection string bulunamadı. `DatabaseSettings:{name}:ConnectionString` veya " +
                $"`ConnectionStrings:{name}` değerini appsettings.Local.json içinde tanımlayın.");

        return cs;
    }
}
