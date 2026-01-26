using MassTransit;

namespace Aizen.Core.Messagebus.Middleware;

public class ElasticApmMiddleware<T> : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageType = context.Message.GetType();
        var messageName = messageType.Name;
        if (messageType.IsGenericType)
        {
            var genericTypeName = messageType.GenericTypeArguments[0].Name;
            messageName = $"{messageType.Name}<{genericTypeName}>";
        }
        
        var transaction = Elastic.Apm.Agent.Tracer.StartTransaction(messageName, "MassTransit");

        try
        {
            await next.Send(context);
        }
        catch (System.Exception ex)
        {
            transaction.CaptureException(ex);
            throw;
        }
        finally
        {
            transaction.End();
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("ElasticApmMiddleware");
    }
}