using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall;

/// <summary>
/// CargoDry → ReferenceData: read an admin-editable numeric config value (system parameter). Mirrors the SR-side
/// <c>IServiceRequestReferenceDataRemoteCall.GetSystemParameter</c>. Returns a null Body for an unknown/inactive key;
/// the caller falls back to a hardcoded default. Auto-registered by the assembly-scanning AddAizenRemoteCall; base URL
/// from <c>RemoteCalls:ICargoDryReferenceDataRemoteCall:BaseUrl</c>.
/// </summary>
public interface ICargoDryReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters/{key}")]
    Task<AizenApiResponse<CargoDrySystemParameterDto?>> GetSystemParameter(string key);
}

/// <summary>Minimal projection of a ReferenceData system parameter (CargoDry config read).</summary>
public sealed class CargoDrySystemParameterDto
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public bool IsActive { get; set; }
}
