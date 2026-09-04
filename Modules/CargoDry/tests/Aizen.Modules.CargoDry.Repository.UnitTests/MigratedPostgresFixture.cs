using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// Boots a throwaway PostgreSQL database on the local compose stack and applies the module's
/// EF migrations to it — exactly the schema a fresh deployment builds. This is what makes the
/// commercial regression tests meaningful: the two admin endpoints (sales-attributions,
/// settlements) 500'd because the migrated schema was missing columns the entity model reads
/// (e.g. sales_attributions.TierAtSale), which an InMemory/EnsureCreated schema would silently
/// paper over. Tests that need the real migrated schema depend on this fixture.
///
/// Connection is taken from CARGODRY_TEST_PG (or the compose default). When no Postgres is
/// reachable the fixture is marked unavailable and the dependent tests skip rather than fail,
/// so the suite still runs in environments without the compose stack.
/// </summary>
public sealed class MigratedPostgresFixture : IAsyncLifetime
{
    private const string DefaultAdminConnection =
        "Host=localhost;Port=5432;Database=postgres;Username=aizen;Password=aizenpw";

    private readonly string _adminConnection =
        Environment.GetEnvironmentVariable("CARGODRY_TEST_PG") ?? DefaultAdminConnection;

    // A unique, deterministic-per-run database name. Date.Now is unavailable/undesired here;
    // a GUID keeps parallel runs isolated.
    private readonly string _dbName = $"cargodry_regr_{Guid.NewGuid():N}";

    public bool Available { get; private set; }
    public string? SkipReason { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            var adminBuilder = new NpgsqlConnectionStringBuilder(_adminConnection);
            await using (var admin = new NpgsqlConnection(adminBuilder.ConnectionString))
            {
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{_dbName}\"";
                await create.ExecuteNonQueryAsync();
            }

            ConnectionString =
                new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString) { Database = _dbName }
                    .ConnectionString;

            await using var db = NewDbContext();
            await db.Database.MigrateAsync();

            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"PostgreSQL not reachable for migration-based tests: {ex.Message}";
        }
    }

    public CargoDryDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseNpgsql(ConnectionString, o => o.MigrationsAssembly(
                typeof(CargoDryDbContext).Assembly.GetName().Name))
            .Options;
        return new CargoDryDbContext(options);
    }

    public async Task DisposeAsync()
    {
        if (!Available) return;

        NpgsqlConnection.ClearAllPools();
        var adminBuilder = new NpgsqlConnectionStringBuilder(_adminConnection);
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using var drop = admin.CreateCommand();
        drop.CommandText =
            $"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)";
        await drop.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("migrated-postgres")]
public sealed class MigratedPostgresCollection : ICollectionFixture<MigratedPostgresFixture> { }
