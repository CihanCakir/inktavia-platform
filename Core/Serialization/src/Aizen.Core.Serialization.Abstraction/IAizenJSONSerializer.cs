using System;
using Newtonsoft.Json;

namespace Aizen.Core.Serialization.Abstraction
{
    public interface IAizenJSONSerializer : IAizenSerializer
    {
        string Serialize<T>(T value, JsonSerializerSettings settings);

        T Deserialize<T>(string value, JsonSerializerSettings settings);

        object? DeserializeObject(string value, Type type);

        bool ValidateJson(string json);
    }
}
