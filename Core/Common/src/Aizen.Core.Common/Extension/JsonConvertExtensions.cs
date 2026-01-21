using Newtonsoft.Json;

namespace Aizen.Core.Common.Extension;

public static class JsonConvertExtensions
{
    public static bool IsSerializable(object obj)
    {
        try
        {
            JsonConvert.SerializeObject(obj);
            return true;
        }
        catch (JsonSerializationException)
        {
            return false;
        }
        catch (JsonWriterException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}