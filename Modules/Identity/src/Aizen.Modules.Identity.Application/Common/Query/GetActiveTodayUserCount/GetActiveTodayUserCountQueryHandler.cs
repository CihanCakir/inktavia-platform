using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Interface;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetActiveTodayUserCountQueryHandler
    : AizenQueryHandler<GetActiveTodayUserCountQuery, UserActiveTodayCountDto>
{
    private readonly IUserDeviceRepository _userDeviceRepository;

    public GetActiveTodayUserCountQueryHandler(IUserDeviceRepository userDeviceRepository)
    {
        _userDeviceRepository = userDeviceRepository;
    }

    public override async Task<UserActiveTodayCountDto> Handle(
        GetActiveTodayUserCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _userDeviceRepository.GetActiveTodayUserCountAsync(DateTime.UtcNow);
        return new UserActiveTodayCountDto { Count = count };
    }
}
