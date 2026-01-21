using System.Data;
using System.Data.Common;

namespace Aizen.Core.Data.Abstraction;

internal class AizenDbTransaction : DbTransaction
{
    protected override DbConnection? DbConnection { get; }
    
    public override IsolationLevel IsolationLevel => this.DbTransactionImplementation.IsolationLevel;
    
    internal readonly DbTransaction DbTransactionImplementation;

    public AizenDbTransaction(DbConnection dbConnection, DbTransaction dbTransaction)
    {
        this.DbConnection = dbConnection;
        this.DbTransactionImplementation = dbTransaction;
    }

    public override void Commit()
    {
        this.DbTransactionImplementation.Commit();
    }

    public override void Rollback()
    {
        this.DbTransactionImplementation.Rollback();
    }
}