using MassTransit;
using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.Messagebus.Middleware;

internal class SendMessageMiddleware : IFilter<SendContext>
{
    private readonly IAizenInfoAccessor _infoAccessor;

    public SendMessageMiddleware(IAizenInfoAccessor infoAccessor)
    {
        _infoAccessor = infoAccessor;
    }

    public async Task Send(SendContext context, IPipe<SendContext> next)
    {
        context.Headers.Set(nameof(_infoAccessor.ClientInfoAccessor.ClientInfo), _infoAccessor.ClientInfoAccessor.ClientInfo);
        context.Headers.Set(nameof(_infoAccessor.DeviceInfoAccessor.DeviceInfo), _infoAccessor.DeviceInfoAccessor.DeviceInfo);
        context.Headers.Set(nameof(_infoAccessor.ChannelInfoAccessor.ChannelInfo), _infoAccessor.ChannelInfoAccessor.ChannelInfo);
        context.Headers.Set(nameof(_infoAccessor.NetworkInfoAccessor.NetworkInfo), _infoAccessor.NetworkInfoAccessor.NetworkInfo);
        context.Headers.Set(nameof(_infoAccessor.RequestInfoAccessor.RequestInfo), _infoAccessor.RequestInfoAccessor.RequestInfo);
        context.Headers.Set(nameof(_infoAccessor.UserInfoAccessor.UserInfo), _infoAccessor.UserInfoAccessor.UserInfo);
        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

internal class ReceiveMessageMiddleware : IFilter<ConsumeContext>
{
    private readonly IAizenInfoContainer _infoContainer;

    public ReceiveMessageMiddleware(IAizenInfoContainer infoContainer)
    {
        _infoContainer = infoContainer;
    }

    public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        _infoContainer.Set(context.Headers.Get<AizenClientInfo>("ClientInfo"));
        _infoContainer.Set(context.Headers.Get<AizenDeviceInfo>("DeviceInfo"));
        _infoContainer.Set(context.Headers.Get<AizenChannelInfo>("ChannelInfo"));
        _infoContainer.Set(context.Headers.Get<AizenNetworkInfo>("NetworkInfo"));
        _infoContainer.Set(context.Headers.Get<AizenRequestInfo>("RequestInfo"));
        _infoContainer.Set(context.Headers.Get<AizenUserInfo>("UserInfo"));
        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}