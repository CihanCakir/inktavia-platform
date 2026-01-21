using System.Data.Common;
using Aizen.Core.Data.Abstraction;
using Npgsql;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Postgresql;

public class AizenPostgresqlDbConnection : AizenDbConnection
{
    public AizenPostgresqlDbConnection(DatabaseSetting databaseSetting) : base(databaseSetting)
    {
    }

    protected override DbConnection InitDbConnection(DatabaseSetting databaseSetting)
    {
        var connection = new NpgsqlConnection(databaseSetting.ConnectionString);
        return connection;
    }
}