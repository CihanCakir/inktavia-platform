using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RecalculateProfilePerformance;

public sealed class RecalculateProfilePerformanceBffCommand
    : AizenCommand<RecalculateProfilePerformanceBffCommandResponse>
{
    public long    ProfileId   { get; init; }
    public string  ProfileType { get; init; } = "Provider";
    public string? Reason      { get; init; }
}

public sealed class RecalculateProfilePerformanceBffCommandResponse
{
    public RecalculateProfilePerformanceBffResult Result { get; init; } = default!;
}
