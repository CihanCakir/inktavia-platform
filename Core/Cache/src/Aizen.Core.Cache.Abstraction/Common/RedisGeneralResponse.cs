using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Core.Cache.Abstraction.Common
{
public class RedisGeneralResponse<T> 
{
    public string Type { get; set; }
    public T Value { get; set; }
    public int Hash { get; set; }
}
}