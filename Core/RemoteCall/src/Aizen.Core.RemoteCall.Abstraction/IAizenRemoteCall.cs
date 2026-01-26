namespace Aizen.Core.RemoteCall.Abstraction;

public interface IAizenRemoteCall
{
    
}

[AttributeUsage(AttributeTargets.Method)]
public class AizenRemoteCallGet : Refit.GetAttribute
{
    public AizenRemoteCallGet(string path) : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AizenRemoteCallPost : Refit.PostAttribute
{
    public AizenRemoteCallPost(string path) : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AizenRemoteCallPut : Refit.PutAttribute
{
    public AizenRemoteCallPut(string path) : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AizenRemoteCallDelete : Refit.DeleteAttribute
{
    public AizenRemoteCallDelete(string path) : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class AizenRemoteCallBody : Refit.BodyAttribute
{
    public AizenRemoteCallBody() : base()
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class AizenRemoteCallHeaderCollection : Refit.HeaderCollectionAttribute
{
    public AizenRemoteCallHeaderCollection() : base()
    {

    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class AizenRemoteCallHeader : Refit.HeaderAttribute
{
    public AizenRemoteCallHeader(string header) : base(header)
    {

    }
}