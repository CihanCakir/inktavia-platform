using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.UpdateCargoDrySupplyConfigBff;

public sealed class UpdateCargoDrySupplyConfigBffCommand : AizenCommand<CargoDrySupplyConfigBffDto>
{
    public string Key   { get; init; } = default!;
    public string Value { get; init; } = default!;
}

[DocumentationInfo("Update CargoDry supply config (BFF)",
    "Edits a CargoDry.* system parameter (timeouts). Key is scoped to the 'CargoDry.' prefix; preserves description/active.")]
public sealed class UpdateCargoDrySupplyConfigBffCommandHandler
    : AizenCommandHandler<UpdateCargoDrySupplyConfigBffCommand, CargoDrySupplyConfigBffDto>
{
    private const string Prefix = "CargoDry.";
    private readonly IReferenceDataRemoteCall _refData;
    public UpdateCargoDrySupplyConfigBffCommandHandler(IReferenceDataRemoteCall refData) => _refData = refData;

    public override async Task<CargoDrySupplyConfigBffDto?> Handle(
        UpdateCargoDrySupplyConfigBffCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || !request.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            throw new AizenBusinessException("Only CargoDry.* configuration keys can be edited here.");
        if (!int.TryParse(request.Value, out var v) || v <= 0)
            throw new AizenBusinessException("Value must be a positive integer.");

        // Preserve the existing description + active flag.
        var all = await _refData.GetSystemParameters();
        var current = (all.Body ?? new List<Modules.ReferenceData.Abstraction.Dto.SystemParameter.SystemParameterDto>())
            .FirstOrDefault(p => string.Equals(p.Key, request.Key, StringComparison.OrdinalIgnoreCase))
            ?? throw new AizenBusinessException($"Unknown configuration key '{request.Key}'.");

        var updated = await _refData.UpdateSystemParameter(request.Key,
            new Modules.ReferenceData.Abstraction.Request.SystemParameter.UpdateSystemParameterRequest
            {
                Key = request.Key, Value = request.Value, Description = current.Description, IsActive = true,
            });
        var dto = updated.Body ?? current;
        return new CargoDrySupplyConfigBffDto { Key = dto.Key, Value = dto.Value, Description = dto.Description, IsActive = dto.IsActive };
    }
}
