namespace Aizen.Core.Serialization.Abstraction
{
    public interface IAizenSerializer
    {
        string Serialize<T>(T value);

        T Deserialize<T>(string value);
    }
}
