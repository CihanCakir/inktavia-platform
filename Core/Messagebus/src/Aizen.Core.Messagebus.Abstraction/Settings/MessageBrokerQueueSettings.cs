namespace Aizen.Core.Messagebus.Abstraction.Settings;

public class MessageBrokerQueueSettings
{
    public bool Debug { get; set; }
    
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int Port { get; set; }
    public string VirtualHost { get; set; }
}