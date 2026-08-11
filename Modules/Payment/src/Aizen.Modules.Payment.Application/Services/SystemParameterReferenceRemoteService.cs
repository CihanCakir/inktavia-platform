using System.Globalization;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// payment-api's remote-call adapter for <see cref="ISystemParameterReferenceService"/>. The concrete DB-backed impl
/// lives in the ReferenceData module (needs its DbContext) and is unusable in-process, so payment-api had an
/// unsatisfied dependency and economics 500'd once actually reached. This adapter delegates the READS to
/// reference-data-api via <see cref="IPaymentReferenceDataRemoteCall"/> and mirrors the ReferenceData parsing exactly
/// (IsActive-gated value, invariant-culture decimal, bool/int TryParse, null-on-miss). WRITES throw
/// <see cref="NotSupportedException"/> — payment never mutates system parameters (that is the reference-data admin API).
/// The reads are token-less: the reference-data system-parameter read endpoints are <c>[AllowAnonymous]</c>
/// cluster-internal reference config (like the sibling Measurement/Location controllers), with encrypted values
/// masked server-side. Economics reads only non-encrypted params (fee/VAT rates).
/// </summary>
public sealed class SystemParameterReferenceRemoteService : ISystemParameterReferenceService
{
    private readonly IPaymentReferenceDataRemoteCall _remote;

    public SystemParameterReferenceRemoteService(IPaymentReferenceDataRemoteCall remote)
    {
        _remote = remote;
    }

    private static SystemParameterDto Map(PaymentSystemParameterDto d) => new()
    {
        Id = d.Id, Key = d.Key, Value = d.Value,
        // reference-data-api serializes the enum as its NAME ("Decimal", "String", …); parse it back (default String).
        ValueType = Enum.TryParse<SystemParameterValueType>(d.ValueType, ignoreCase: true, out var vt)
            ? vt : SystemParameterValueType.String,
        Description = d.Description, IsEncrypted = d.IsEncrypted, IsActive = d.IsActive,
    };

    // ── Reads (delegate to reference-data-api) ─────────────────────────────────────────────────────────
    public async Task<SystemParameterDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var res = await _remote.GetByKey(key);
        var body = res?.Body;
        return body is null ? null : Map(body);
    }

    public async Task<IReadOnlyList<SystemParameterDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var res = await _remote.GetList(onlyActive);
        return (res?.Body ?? new List<PaymentSystemParameterDto>()).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SystemParameterDto>> GetByPrefixAsync(string prefix, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var res = await _remote.GetByPrefix(prefix, onlyActive);
        return (res?.Body ?? new List<PaymentSystemParameterDto>()).Select(Map).ToList();
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        // Mirror ReferenceData: the value is returned only for an ACTIVE parameter; else null.
        var dto = await GetByKeyAsync(key, cancellationToken);
        return dto?.IsActive == true ? dto.Value : null;
    }

    public async Task<bool?> GetBooleanAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val is null) return null;
        return bool.TryParse(val, out var result) ? result : null;
    }

    public async Task<int?> GetIntAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val is null) return null;
        return int.TryParse(val, out var result) ? result : null;
    }

    public async Task<decimal?> GetDecimalAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val is null) return null;
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    // ── Writes: not supported in payment-api (reference-data admin controller owns parameter mutations). ──
    private static NotSupportedException WriteNotSupported() =>
        new("payment-api reads system parameters remotely; writes go through the reference-data admin API.");

    public Task<SystemParameterDto> CreateAsync(string key, string value, SystemParameterValueType valueType, string? description, bool isEncrypted, CancellationToken cancellationToken = default)
        => throw WriteNotSupported();
    public Task<SystemParameterDto> UpdateAsync(string key, string value, string? description, bool isActive, CancellationToken cancellationToken = default)
        => throw WriteNotSupported();
    public Task ActivateAsync(string key, CancellationToken cancellationToken = default)
        => throw WriteNotSupported();
    public Task DeactivateAsync(string key, CancellationToken cancellationToken = default)
        => throw WriteNotSupported();
}
