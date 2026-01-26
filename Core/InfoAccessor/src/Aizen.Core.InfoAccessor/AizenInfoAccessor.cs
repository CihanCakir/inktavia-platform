using System.Reflection;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction.Attributes;
using Aizen.Core.InfoAccessor.Extensions;
using Microsoft.Extensions.Configuration;

namespace Aizen.Core.InfoAccessor;

internal class AizenInfoAccessor : IAizenInfoAccessor, IAizenServerInfoAccessor, IAizenAppInfoAccessor,
    IAizenUserInfoAccessor, IAizenChannelInfoAccessor, IAizenDeviceInfoAccessor, IAizenRequestInfoAccessor, IAizenNetworkInfoAccessor, IAizenClientInfoAccessor,
    IAizenExecutionInfoAccessor
{
    private readonly IAizenInfoContainer _infoContainer;

    private readonly IConfiguration _configuration;

    public IAizenServerInfoAccessor ServerInfoAccessor => this;

    public IAizenNetworkInfoAccessor NetworkInfoAccessor => this;

    public IAizenAppInfoAccessor AppInfoAccessor => this;

    public IAizenUserInfoAccessor UserInfoAccessor => this;

    public IAizenChannelInfoAccessor ChannelInfoAccessor => this;

    public IAizenClientInfoAccessor ClientInfoAccessor => this;

    public IAizenExecutionInfoAccessor ExecutionInfoAccessor => this;


    public IAizenDeviceInfoAccessor DeviceInfoAccessor => this;

    public IAizenRequestInfoAccessor RequestInfoAccessor => this;

    public AizenServerInfo ServerInfo => _infoContainer.Get<AizenServerInfo>();

    public AizenNetworkInfo NetworkInfo => _infoContainer.Get<AizenNetworkInfo>();

    public AizenAppInfo AppInfo => _infoContainer.Get<AizenAppInfo>();

    public AizenUserInfo UserInfo => _infoContainer.Get<AizenUserInfo>();

    public AizenChannelInfo ChannelInfo => _infoContainer.Get<AizenChannelInfo>();
    public AizenClientInfo ClientInfo => _infoContainer.Get<AizenClientInfo>();
    public AizenDeviceInfo DeviceInfo => _infoContainer.Get<AizenDeviceInfo>();
    public AizenRequestInfo RequestInfo => _infoContainer.Get<AizenRequestInfo>();
    public AizenExecutionInfo ExecutionInfo => _infoContainer.Get<AizenExecutionInfo>();


    public AizenInfoAccessor(IAizenInfoContainer infoContainer, IConfiguration configuration)
    {
        _infoContainer = infoContainer;
        _configuration = configuration;
    }

    public TInfo GetInfo<TInfo>() where TInfo : IAizenInfo
    {
        var infoType = typeof(TInfo);
        var info = Activator.CreateInstance(infoType);
        foreach (var property in info.GetType().GetProperties()
                     .Where(x => x.CustomAttributes.Any(a => a.AttributeType.IsSubclassOf(typeof(InfoBaseAttribute)))))
        {
            object value;
            var baseAttribute = property.GetCustomAttribute<InfoBaseAttribute>();
            switch (baseAttribute)
            {
                case InfoFromConfigurationAttribute infoBaseAttribute:
                    {
                        value = _configuration[infoBaseAttribute.Key];
                        break;
                    }
                case InfoFromEnvironmentAttribute infoBaseAttribute:
                    {
                        value = Environment.GetEnvironmentVariable(infoBaseAttribute.Key);
                        break;
                    }
                default:
                    throw new NotSupportedException($"{baseAttribute.GetType().FullName} not supported");
            }

            property.SetValue(info, property.ConvertToPropertyType(value));
        }

        return (TInfo)info;
    }
}