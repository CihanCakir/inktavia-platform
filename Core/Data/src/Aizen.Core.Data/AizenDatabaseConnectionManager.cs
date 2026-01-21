using Aizen.Core.Data.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data;

internal class AizenDatabaseConnectionManager
    : IAizenDatabaseConnectionManager
{
    private readonly IOptions<DatabaseSettings> _databaseOptions;
    private readonly IEnumerable<IAizenDatabaseConnectionFactory> _connectionFactories;
    private readonly Dictionary<string, AizenDatabaseConnectionContainer> _dbConnections;
    private readonly IHttpContextAccessor _contextAccessor;

    public AizenDatabaseConnectionManager(
        IOptions<DatabaseSettings> databaseOptions,
        IEnumerable<IAizenDatabaseConnectionFactory> connectionFactories,
        IHttpContextAccessor contextAccessor)
    {
        _databaseOptions = databaseOptions;
        _connectionFactories = connectionFactories;
        _dbConnections = new Dictionary<string, AizenDatabaseConnectionContainer>();
        _contextAccessor = contextAccessor;
    }

    public async Task<AizenDatabaseConnectionContainer> GetDbConnectionAsync(string dbName,
        CancellationToken cancellationToken = default)
    {
        if (!_dbConnections.ContainsKey(dbName))
        {
            var connection = await CreateDbConnectionAsync(dbName, cancellationToken);
            _dbConnections.Add(dbName, connection);
        }

        return _dbConnections[dbName];
    }

    public Task<AizenDatabaseConnectionContainer> GetDbConnectionAsync(string dbName,
        DbConnectionLifeCycle connectionLifeCycle,
        CancellationToken cancellationToken = default)
    {
        if (connectionLifeCycle == DbConnectionLifeCycle.Managed)
        {
            return GetDbConnectionAsync(dbName, cancellationToken);
        }

        return CreateDbConnectionAsync(dbName, cancellationToken);
    }

    private async Task<AizenDatabaseConnectionContainer> CreateDbConnectionAsync(string dbName,
        CancellationToken cancellationToken)
    {
        var useReplicaSet = false;
        if (_contextAccessor.HttpContext.Request.Headers.TryGetValue("X-Use-Replica-Set", out var value))
        {
            _ = bool.TryParse(value, out useReplicaSet);
        }

        var databaseName = useReplicaSet ? "ReplicaSet" : dbName;
        if (_databaseOptions.Value.TryGetValue(databaseName, out var databaseOption))
        {
            var connectionFactory = _connectionFactories.FirstOrDefault(x => x.DatabaseType == databaseOption.Type);
            if (connectionFactory == null)
            {
                throw new NotSupportedException($"Database {databaseName} is not supported for {databaseOption.Type}");
            }

            var connection = await connectionFactory.GetDbConnectionAsync(databaseName, cancellationToken);
            return new AizenDatabaseConnectionContainer(databaseOption, connection);
        }

        throw new NotSupportedException($"Database {databaseName} is not supported.");
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        foreach (var dbConnection in _dbConnections)
        {
            var transaction = await dbConnection.Value.DbConnection.BeginTransactionAsync(cancellationToken);
            dbConnection.Value.DbTransaction = transaction;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var dbConnection in _dbConnections)
        {
            await dbConnection.Value.DbTransaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        foreach (var dbConnection in _dbConnections)
        {
            await dbConnection.Value.DbTransaction.RollbackAsync(cancellationToken);
        }
    }
}