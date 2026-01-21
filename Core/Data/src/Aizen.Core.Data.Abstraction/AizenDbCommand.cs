using System.Data;
using System.Data.Common;

namespace Aizen.Core.Data.Abstraction;

internal class AizenDbCommand : DbCommand
{
    private DbConnection _dbConnection;

    internal readonly DbCommand DbCommandImplementation;

    private DbTransaction? _dbTransaction;

    public AizenDbCommand(DbConnection dbConnection, DbCommand dbCommand)
    {
        this._dbConnection = dbConnection;
        this.DbCommandImplementation = dbCommand;
    }

    public override void Cancel()
    {
        this.DbCommandImplementation.Cancel();
    }

    public override int ExecuteNonQuery()
    {
        return this.DbCommandImplementation.ExecuteNonQuery();
    }

    public override object? ExecuteScalar()
    {
        return this.DbCommandImplementation.ExecuteScalar();
    }

    public override void Prepare()
    {
        this.DbCommandImplementation.Prepare();
    }

    public override string CommandText
    {
        get => this.DbCommandImplementation.CommandText;
        set => this.DbCommandImplementation.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => this.DbCommandImplementation.CommandTimeout;
        set => this.DbCommandImplementation.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => this.DbCommandImplementation.CommandType;
        set => this.DbCommandImplementation.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => this.DbCommandImplementation.UpdatedRowSource;
        set => this.DbCommandImplementation.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => this._dbConnection;
        set
        {
            this._dbConnection = value;
            this.DbCommandImplementation.Connection = this._dbConnection is AizenDbConnection connection
                ? connection.DbConnectionImplementation
                : this._dbConnection;
        }
    }

    protected override DbParameterCollection DbParameterCollection => this.DbCommandImplementation.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => this._dbTransaction;
        set
        {
            this._dbTransaction = value;
            this.DbCommandImplementation.Transaction = this._dbTransaction is AizenDbTransaction transaction
                ? transaction.DbTransactionImplementation
                : this._dbTransaction;
        }
    }

    public override bool DesignTimeVisible
    {
        get => this.DbCommandImplementation.DesignTimeVisible;
        set => this.DbCommandImplementation.DesignTimeVisible = value;
    }

    protected override DbParameter CreateDbParameter()
    {
        return this.DbCommandImplementation.CreateParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return this.DbCommandImplementation.ExecuteReader(behavior);
    }
}