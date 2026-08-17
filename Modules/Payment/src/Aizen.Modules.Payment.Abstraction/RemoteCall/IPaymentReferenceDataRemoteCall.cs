using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall;

/// <summary>
/// payment-api → reference-data-api system-parameter READS (fee/commission parameters). Net-new (payment had no
/// reference-data remote call), following the established remote-call pattern. These reads are token-less: the
/// reference-data system-parameter read endpoints are <c>[AllowAnonymous]</c> (cluster-internal reference config,
/// exactly like the sibling Measurement/Location read controllers), so no caller token is threaded and no service
/// token is minted. Encrypted parameter values are masked server-side and never returned. Only reads — payment
/// never writes system parameters.
/// </summary>
public interface IPaymentReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters/{key}")]
    Task<AizenApiResponse<PaymentSystemParameterDto?>> GetByKey(string key);

    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters?onlyActive={onlyActive}")]
    Task<AizenApiResponse<List<PaymentSystemParameterDto>>> GetList(bool onlyActive);

    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters/by-prefix?prefix={prefix}&onlyActive={onlyActive}")]
    Task<AizenApiResponse<List<PaymentSystemParameterDto>>> GetByPrefix(string prefix, bool onlyActive);
}

/// <summary>Wire shape of a reference-data system parameter (matches <c>SystemParameterDto</c>). Local to
/// Payment.Abstraction so no ReferenceData project reference is needed on the abstraction layer.</summary>
public sealed class PaymentSystemParameterDto
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    /// <summary>ReferenceData SystemParameterValueType — serialized by reference-data-api as the ENUM NAME string
    /// (e.g. "Decimal", "String"), not an int. Parsed back to the enum by the adapter.</summary>
    public string? ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsActive { get; set; }
}
