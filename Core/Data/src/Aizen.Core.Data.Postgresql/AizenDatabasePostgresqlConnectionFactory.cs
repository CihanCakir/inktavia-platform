using System.Data;
using System.Data.Common;
using Aizen.Core.Data.Abstraction;
using Microsoft.Extensions.Options;
using Npgsql;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Postgresql;

internal class AizenDatabasePostgresqlConnectionFactory : IAizenDatabaseConnectionFactory
{
    private readonly IOptions<DatabaseSettings> _databaseOptions;
    
    public DatabaseType DatabaseType => DatabaseType.PostgreSQL;

    public AizenDatabasePostgresqlConnectionFactory(IOptions<DatabaseSettings> databaseOptions)
    {
        _databaseOptions = databaseOptions;
    }
    
    public async Task<DbConnection> GetDbConnectionAsync(string dbName, CancellationToken cancellationToken = default)
    {
        if (_databaseOptions.Value.TryGetValue(dbName, out var databaseOption))
        {
            if (databaseOption.Type == DatabaseType.PostgreSQL)
            {
                var connection = new AizenPostgresqlDbConnection(databaseOption);
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                
                return connection;
            }
            
            throw new NotSupportedException($"{dbName} is not supported for {databaseOption.Type}.");
        }

        throw new NotSupportedException($"{dbName} is not supported.");
    }
}