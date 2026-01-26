using System.Data;
using System.Data.Common;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Abstraction;

public abstract class AizenDbConnection : DbConnection
{
    protected internal readonly DbConnection DbConnectionImplementation;

    protected AizenDbConnection(DatabaseSetting databaseSetting)
    {
        this.DbConnectionImplementation = InitDbConnection(databaseSetting);
    }

    protected abstract DbConnection InitDbConnection(DatabaseSetting databaseSetting);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return new AizenDbTransaction(this, this.DbConnectionImplementation.BeginTransaction(isolationLevel));
    }

    public override void ChangeDatabase(string databaseName)
    {
        this.DbConnectionImplementation.ChangeDatabase(databaseName);
    }

    public override void Close()
    {
        this.DbConnectionImplementation.Close();
    }

    public override void Open()
    {
        this.DbConnectionImplementation.Open();
    }

    public override string ConnectionString
    {
        get => this.DbConnectionImplementation.ConnectionString;
        set => this.DbConnectionImplementation.ConnectionString = value;
    }

    public override string Database => this.DbConnectionImplementation.Database;

    public override ConnectionState State => this.DbConnectionImplementation.State;

    public override string DataSource => this.DbConnectionImplementation.DataSource;

    public override string ServerVersion => this.DbConnectionImplementation.ServerVersion;

    protected override DbCommand CreateDbCommand()
    {
        return new AizenDbCommand(this, this.DbConnectionImplementation.CreateCommand());
    }
}