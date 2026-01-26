using System.Data.Common;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Abstraction;

public interface IAizenDatabaseConnectionFactory
{
    DatabaseType DatabaseType { get; }

    Task<DbConnection> GetDbConnectionAsync(string dbName,
        CancellationToken cancellationToken = default);
}