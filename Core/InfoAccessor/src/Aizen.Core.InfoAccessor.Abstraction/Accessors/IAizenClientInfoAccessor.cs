using Aizen.Core.Common.Abstraction.Enums;
using System.Globalization;

namespace Aizen.Core.InfoAccessor.Abstraction
{
    public interface IAizenClientInfoAccessor
    {
        public AizenClientInfo ClientInfo { get; }
    }

    public class AizenClientInfo : IAizenInfo
    {
        public static AizenClientInfo Unknown { get; } = new AizenClientInfo();
        public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;
        public long ApplicationId { get; set; }

        public string RequestId { get; set; }

        public string AppName { get; set; }

        public string AppVersion { get; set; }

        public ChannelType ChannelType { get; set; }

        public string ChannelName { get; set; }

        private string _language;

        public string Language
        {
            get
            {
                if (string.IsNullOrEmpty(this._language))
                {
                    this._language = CultureInfo.CurrentUICulture.Name;
                }

                return this._language;
            }

            set => this._language = value;
        }

        public string TenantCode { get; set; } // TODO: Tenant-Based FeatureToggle geliştirmesi sonrası enum'a çevir.

        public string AuthToken { get; set; }
    }
}
