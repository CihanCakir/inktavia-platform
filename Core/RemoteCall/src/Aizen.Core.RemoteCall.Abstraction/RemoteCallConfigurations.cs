namespace Aizen.Core.RemoteCall.Abstraction;

public class RemoteCallConfigurations : Dictionary<string, RemoteCallConfiguration>
{

}

public class RemoteCallConfiguration
{
    public string BaseUrl { get; set; }

    public Dictionary<string, string> DefaultHeaders { get; set; }

    public Dictionary<string, string> Parameters { get; set; }
}