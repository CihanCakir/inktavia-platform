using MassTransit;
using Aizen.Core.Messagebus.Abstraction.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Aizen.Core.Messagebus.Abstraction.Consumers
{
    public interface IAizenMessageConsumer {}
    
    public interface IAizenMessageConsumer<TMessage> :
        IConsumer<AizenPrepareMessage<TMessage>>,
        IConsumer<AizenCommitMessage<TMessage>>,
        IConsumer<AizenRollbackMessage<TMessage>>,
        IAizenMessageConsumer
        where TMessage : AizenBaseMessage
    {
    }
}
