namespace Aizen.Core.Cache.Abstraction.Common;

public sealed class AizenCacheItem<T> : IEquatable<AizenCacheItem<T>>
{

    public string Type { get; set; }

    public T Value { get; set; }

    public int Hash { get; set; }
    
    public AizenCacheItem(T item)
    {
        this.Type = typeof(T).Name;
        this.Value = item;
        this.Hash = this.Value?.GetHashCode() ?? 0;
    }
    
    public bool Equals(AizenCacheItem<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.Type == other.Type && this.Hash == other.Hash;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return this.Equals((AizenCacheItem<T>)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((this.Type.GetHashCode(StringComparison.CurrentCulture)) * 397) ^ (this.Hash > 0 ? this.Hash.GetHashCode() : 0);
        }
    }
}