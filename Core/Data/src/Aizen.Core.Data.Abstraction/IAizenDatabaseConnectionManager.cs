using System.Data.Common;

namespace Aizen.Core.Data.Abstraction;

public interface IAizenDatabaseConnectionManager
{
    Task<AizenDatabaseConnectionContainer> GetDbConnectionAsync(string dbName,
        CancellationToken cancellationToken = default);

    Task<AizenDatabaseConnectionContainer> GetDbConnectionAsync(string dbName,
        DbConnectionLifeCycle connectionLifeCycle,
        CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}