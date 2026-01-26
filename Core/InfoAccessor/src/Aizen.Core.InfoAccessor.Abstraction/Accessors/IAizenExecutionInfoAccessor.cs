using Aizen.Core.Common.Abstraction.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aizen.Core.InfoAccessor.Abstraction
{
    public interface IAizenExecutionInfoAccessor
    {
    }
    public class AizenExecutionInfo : IAizenInfo
    {
        public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;

        public string ExecutionId { get; set; }

        public ExecutionType ExecutionType { get; set; }

        public string[] DatabaseList => this._databaseList.ToArray();

        public SchedulerInfo SchedulerInfo { get; set; }


        private readonly List<string> _databaseList;

        public void AddResolvedDatabaseName(string dbName)
        {
            if (!this._databaseList.Contains(dbName))
            {
                this._databaseList.Add((dbName));
            }
        }

        public AizenExecutionInfo()
        {
            this._databaseList = new List<string>();
        }
    }
}
