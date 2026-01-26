namespace Aizen.Core.Domain.Abstraction.Attributes;

public class AizenIgnoreMemberAttribute
{
    
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class AizenSearchableAttribute : Attribute
{
    public AizenSearchableAttribute() { }
}