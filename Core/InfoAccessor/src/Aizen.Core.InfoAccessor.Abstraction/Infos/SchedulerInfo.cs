using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aizen.Core.InfoAccessor.Abstraction
{
    public class SchedulerInfo
    {
        public string Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string TypeName { get; set; }

        public string MethodName { get; set; }
    }
}
