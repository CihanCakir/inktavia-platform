using System.Data;
using System.Data.Common;

namespace Aizen.Core.EFCore;

public static class DbCommandExtensions
{
    public static void AddCommandParameter(this DbCommand command, string key, object value = null, ParameterDirection direction = default, DbType dbType = default, int size = default)
    {
        var dbParameter = command.CreateParameter();
        dbParameter.Direction = direction;
        dbParameter.DbType = ConvertToDbType(dbType, value);
        dbParameter.ParameterName = $"@{key}";
        dbParameter.Value = value;
        dbParameter.Size = size;
        command.Parameters.Add(dbParameter);
    }

    private static DbType ConvertToDbType(DbType dbType, object value)
    {
        if (dbType == default)
        {
            if (value is int)
            {
                dbType = DbType.Int32;
            }
            if (value is decimal)
            {
                dbType = DbType.Decimal;
            }
            if (value is DateTime)
            {
                dbType = DbType.DateTime;
            }
            else if (value is string)
            {
                dbType = DbType.String;
            }
        }

        return dbType;
    }
}
