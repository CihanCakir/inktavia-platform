using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Aizen.Core.Serialization.Abstraction.Converters;

namespace Aizen.Core.Serialization.Abstraction
{
    public sealed class AizenSerializerSettings : JsonSerializerSettings
    {
        public AizenSerializerSettings()
        {
            this.ContractResolver = new CamelCasePropertyNamesContractResolver();
            this.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
            this.Formatting = Formatting.Indented;
            this.Converters.Add(new TimespanConverter());
            this.Converters.Add(new PagedListConverter());
        }
    }
}
