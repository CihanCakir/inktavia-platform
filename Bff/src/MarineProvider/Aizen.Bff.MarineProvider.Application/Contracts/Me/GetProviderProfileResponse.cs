using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Me;

public sealed class GetProviderProfileResponse
{
    public bool HasProfileLink { get; set; }
    public ProviderProfileDto? Profile { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
