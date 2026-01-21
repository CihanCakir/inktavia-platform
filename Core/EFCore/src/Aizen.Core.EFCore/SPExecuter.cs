using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aizen.Core.EFCore;

public static class SPExecuter
{
    public static async Task SPExecuteAsync(this DbContext context,
        string spName,
        Func<DbCommand, Task> inputMapper,
        Func<DbDataReader, Task> outputMapper,
        bool isUseCurrentConnection = false)
    {
        var cmd = isUseCurrentConnection
            ? await CreateDbCommandForCurrentConnection(context, spName)
            : await CreateDbCommandForNewConnection(context, spName);

        await inputMapper(cmd);

        var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await outputMapper(reader);
        }

        await reader.CloseAsync();

        if (!isUseCurrentConnection)
        {
            if (cmd.Connection.State != ConnectionState.Closed)
            {
                await cmd.Connection.CloseAsync();
            }
        }
    }

    private static async Task<DbCommand> CreateDbCommandForCurrentConnection(DbContext context, string spName)
    {
        var con = context.Database.GetDbConnection();
        if (con.State != ConnectionState.Open)
        {
            await con.OpenAsync();
        }

        var cmd = con.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = spName;

        return cmd;
    }

    private static async Task<DbCommand> CreateDbCommandForNewConnection(DbContext context, string spName)
    {
        var conStr = context.Database.GetConnectionString();
        var con = new SqlConnection(conStr);
        if (con.State != ConnectionState.Open)
        {
            await con.OpenAsync();
        }

        var cmd = con.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = spName;

        return cmd;
    }
}