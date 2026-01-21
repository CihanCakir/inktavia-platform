using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aizen.Core.Common.Abstraction.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AizenLinePropertyAttribute : Attribute
    {
        public AizenLinePropertyAttribute(
            int capacity,
            int order)
        {
            this.Capacity = capacity;
            this.Order = order;
        }

        public int Capacity { get; }
        public int Order { get; }
    }
}
