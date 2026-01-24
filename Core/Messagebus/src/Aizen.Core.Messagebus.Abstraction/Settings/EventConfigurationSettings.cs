namespace Aizen.Core.Messagebus.Abstraction.Settings;

public class EventConfigurationSettings
{
    public bool AddConsumer { get; set; }

    public bool AddRequestClient { get; set; }

    public EventConfigurationSettings()
    {
        AddConsumer = true;
        AddRequestClient = true;
    }
}