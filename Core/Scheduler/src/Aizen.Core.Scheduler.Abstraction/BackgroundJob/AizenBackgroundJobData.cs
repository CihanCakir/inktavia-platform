using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.Scheduler.Abstraction.BackgroundJob
{
    public class AizenBackgroundJobData
    {
        public AizenClientInfo ClientInfo { get; set; }

        public AizenUserInfo CustomerInfo { get; set; }

        public AizenDeviceInfo DeviceInfo { get; set; }
    }
}
