using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotByProfile;

[DocumentationInfo("GetPerformanceSnapshotByProfileQueryHandler",
    "Returns the current performance snapshot + per-dimension score components for a (ProfileId, ProfileType) pair. " +
    "Returns Found=false when no snapshot exists yet (cold-start or never calculated).")]
public sealed class GetPerformanceSnapshotByProfileQueryHandler
    : AizenQueryHandler<GetPerformanceSnapshotByProfileQuery, GetPerformanceSnapshotByProfileResponse>
{
    private readonly IProfilePerformanceSnapshotRepository _snapshots;
    private readonly IProfileScoreComponentRepository      _components;

    public GetPerformanceSnapshotByProfileQueryHandler(
        IProfilePerformanceSnapshotRepository snapshots,
        IProfileScoreComponentRepository      components)
    {
        _snapshots  = snapshots;
        _components = components;
    }

    public override async Task<GetPerformanceSnapshotByProfileResponse> Handle(
        GetPerformanceSnapshotByProfileQuery request, CancellationToken ct)
    {
        var snapshot = await _snapshots.GetByProfileAsync(request.ProfileId, request.ProfileType, ct);

        if (snapshot is null)
            return new GetPerformanceSnapshotByProfileResponse { Found = false };

        var componentEntities = await _components.GetBySnapshotIdAsync(snapshot.Id, ct);

        var snapshotDto = MapSnapshot(snapshot);
        var componentDtos = componentEntities.Select(c => new ProfileScoreComponentDto
        {
            SnapshotId           = c.SnapshotId,
            Category             = c.Category,
            RawScore             = c.RawScore,
            Weight               = c.Weight,
            WeightedContribution = c.WeightedContribution,
            MetricCount          = c.MetricCount,
            MetricsJson          = c.MetricsJson,
            CalculatedAtUtc      = c.CalculatedAtUtc,
        }).ToList();

        return new GetPerformanceSnapshotByProfileResponse
        {
            Snapshot   = snapshotDto,
            Components = componentDtos,
            Found      = true,
        };
    }

    internal static ProfilePerformanceSnapshotDto MapSnapshot(
        Aizen.Modules.Profile.Domain.Entities.Performance.ProfilePerformanceSnapshotEntity e)
        => new()
        {
            ProfileId                  = e.ProfileId,
            ProfileType                = e.ProfileType,
            PriorityTier               = e.PriorityTier,
            OverallScore               = e.OverallScore,
            ServiceRequestScore        = e.ServiceRequestScore,
            CargoDryScore              = e.CargoDryScore,
            OperationalDisciplineScore = e.OperationalDisciplineScore,
            FinancialReliabilityScore  = e.FinancialReliabilityScore,
            PlatformComplianceScore    = e.PlatformComplianceScore,
            RiskPenaltyScore           = e.RiskPenaltyScore,
            ConfidenceScore            = e.ConfidenceScore,
            ConfidenceLevel            = e.ConfidenceLevel,
            SampleSize                 = e.SampleSize,
            IsColdStart                = e.MetadataJson?.Contains("\"coldStart\":true") ?? false,
            HasActiveRiskSignal        = e.HasActiveRiskSignal,
            ActiveRiskSignalMaxSeverity = e.ActiveRiskSignalMaxSeverity,
            LastCalculatedAtUtc        = e.LastCalculatedAtUtc,
            ValidFromUtc               = e.ValidFromUtc,
            MetadataJson               = e.MetadataJson,
        };
}
