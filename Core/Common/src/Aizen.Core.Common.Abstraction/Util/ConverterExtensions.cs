using Newtonsoft.Json;

namespace Aizen.Core.Common.Abstraction.Util;

public static class ConverterExtensions
{
    /// <summary>
    ///  Deserialize NewtonSoft
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="convertableData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T Deserialize<T>(this string convertableData)
    {
        return JsonConvert.DeserializeObject<T>(convertableData) ?? throw new ArgumentException();
    }

    /// <summary>
    ///  Deserialize NewtonSoft
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="convertableData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<T> DeserializeList<T>(this string convertableData)
    {
        return JsonConvert.DeserializeObject<List<T>>(convertableData) ?? throw new ArgumentException();
    }

    /// <summary>
    ///  Serialize NewtonSoft
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="convertableData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Serialize<T>(this T convertableData)
    {
        return JsonConvert.SerializeObject(convertableData) ?? throw new ArgumentException();
    }
}