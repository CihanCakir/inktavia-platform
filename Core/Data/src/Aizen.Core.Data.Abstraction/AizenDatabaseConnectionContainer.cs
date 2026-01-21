using System.Data;
using System.Data.Common;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Abstraction;

public class AizenDatabaseConnectionContainer
{
    public DatabaseSetting DbOption { get; }
    public DbConnection DbConnection { get; }
    
    private DbTransaction _transaction;

    public DbTransaction DbTransaction
    {
        get => this._transaction;

        set
        {
            this._transaction = value;
            TransactionChanged?.Invoke(this._transaction);
        }
    }

    public delegate void TransactionChangedEventHandler(DbTransaction dbTransaction);

    public event TransactionChangedEventHandler? TransactionChanged;

    public AizenDatabaseConnectionContainer(DatabaseSetting dbOption, DbConnection dbConnection)
    {
        DbOption = dbOption;
        DbConnection = dbConnection;
    }
}