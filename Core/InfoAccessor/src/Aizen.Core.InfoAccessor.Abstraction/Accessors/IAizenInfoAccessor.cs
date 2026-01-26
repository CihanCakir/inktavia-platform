namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenInfoAccessor
{
    public IAizenUserInfoAccessor UserInfoAccessor { get; }

    public IAizenChannelInfoAccessor ChannelInfoAccessor { get; }

    public IAizenClientInfoAccessor ClientInfoAccessor { get; }
    
    public IAizenDeviceInfoAccessor DeviceInfoAccessor { get; }

    public IAizenExecutionInfoAccessor ExecutionInfoAccessor { get; }

    public IAizenAppInfoAccessor AppInfoAccessor { get; }

    public IAizenServerInfoAccessor ServerInfoAccessor { get; }

    public IAizenNetworkInfoAccessor NetworkInfoAccessor { get; }

    public IAizenRequestInfoAccessor RequestInfoAccessor { get; }

    public TInfo GetInfo<TInfo>() where TInfo : IAizenInfo;
}