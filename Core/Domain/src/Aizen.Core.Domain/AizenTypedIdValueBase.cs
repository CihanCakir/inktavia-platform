namespace Aizen.Core.Domain;

public class AizenTypedIdValueBase : IEquatable<AizenTypedIdValueBase>
{
    public Guid Value { get; }

    protected AizenTypedIdValueBase(Guid value)
    {
        Value = value;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is AizenTypedIdValueBase other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(AizenTypedIdValueBase other)
    {
        return this.Value == other.Value;
    }

    public static bool operator ==(AizenTypedIdValueBase obj1, AizenTypedIdValueBase obj2)
    {
        if (object.Equals(obj1, null))
        {
            if (object.Equals(obj2, null))
            {
                return true;
            }
            return false;
        }
        return obj1.Equals(obj2);
    }
    
    public static bool operator !=(AizenTypedIdValueBase x, AizenTypedIdValueBase y)
    {
        return !(x == y);
    }
}