using System.Threading;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Services
{
    public class RealtimeIngressService : IRealtimeEventIngress
    {
        private readonly IEventSocketMapper _mapper;
        private readonly IRealtimePublisher _publisher;

        public RealtimeIngressService(IEventSocketMapper mapper, IRealtimePublisher publisher)
        {
            _mapper = mapper;
            _publisher = publisher;
        }

        public async Task PublishDomainEventAsync(EventDto evt, CancellationToken ct = default)
        {
            var msg = _mapper.Map(evt);
            if (msg == null) return;

            var (users, groups) = _mapper.GetTargets(evt);
            if (groups != null)
            {
                foreach (var g in groups)
                    await _publisher.PublishToGroupAsync(g, msg, evt.Id, ct);
            }

            if (users != null)
            {
                foreach (var u in users)
                    await _publisher.PublishToUserAsync(u, msg, evt.Id, ct);
            }

            if ((groups == null || System.Linq.Enumerable.Count(groups) == 0) && (users == null || System.Linq.Enumerable.Count(users) == 0))
            {
                var channel = msg.Stream;
                await _publisher.PublishToChannelAsync(channel, msg, evt.Id, ct);
            }
        }
    }
}