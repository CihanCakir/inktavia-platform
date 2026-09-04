namespace Aizen.Core.Cache.Abstraction.Common
{
    public class AizenStringCacheItem<T>
    {
        public T Data { get; set; }
        public int Expiry { get; set; }
        public TimeSpan ExpiryDate { get; set; }
        public AizenStringCacheItem(T data, int expiry, TimeSpan expiryDate)
        {
            Data = data;
            Expiry = expiry;
            ExpiryDate = expiryDate;
        }
    }
}
