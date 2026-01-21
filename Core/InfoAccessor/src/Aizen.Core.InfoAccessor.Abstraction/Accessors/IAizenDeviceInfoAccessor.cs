using Aizen.Core.Common.Abstraction.Enums;

namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenDeviceInfoAccessor
{
    public AizenDeviceInfo DeviceInfo { get; }
}

public class AizenDeviceInfo : IAizenInfo
{
    public static AizenDeviceInfo Unknown { get; } = new AizenDeviceInfo();

    public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;
    public DeviceOsType OsType { get; set; }

    public string DeviceId { get; set; }
    public string Name { get; set; }

    public string Brand { get; set; }

    public string Model { get; set; }

    public string OsVersion { get; set; }

    public string IpAddress { get; set; }

    public string MacAddress { get; set; }
}