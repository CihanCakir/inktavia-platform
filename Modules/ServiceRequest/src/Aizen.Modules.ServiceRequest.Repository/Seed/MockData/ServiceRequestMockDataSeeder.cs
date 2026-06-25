using System.Reflection;
using System.Text.Json;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ServiceRequest.Repository.Seed.MockData;

/// <summary>
/// Idempotent mock data seeder for the ServiceRequest module.
/// Seeds the full service request lifecycle for admin-demo in local and development environments.
/// </summary>
[DocumentationInfo("ServiceRequest mock data seeder", "Seeds admin-demo service request data idempotently for local and development environments.")]
public sealed class ServiceRequestMockDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ServiceRequestDbContext _db;
    private readonly MockDataSeedOptions _options;
    private readonly ILogger<ServiceRequestMockDataSeeder> _logger;
    private readonly string _environment;

    public ServiceRequestMockDataSeeder(
        ServiceRequestDbContext db,
        IOptions<MockDataSeedOptions> options,
        ILogger<ServiceRequestMockDataSeeder> logger,
        string environment)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || !_options.RunOnStartup)
        {
            _logger.LogDebug("ServiceRequest MockData seeder is disabled.");
            return;
        }

        if (!_options.EnvironmentGuard.Contains(_environment, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "ServiceRequest MockData seeder skipped: environment '{Environment}' is not in EnvironmentGuard {Guard}.",
                _environment, string.Join(", ", _options.EnvironmentGuard));
            return;
        }

        _logger.LogInformation("ServiceRequest MockData seeder starting (dataset: {DataSet}).", _options.DataSet);

        var basePath = Path.Combine(AppContext.BaseDirectory, "Seed", "Json", "MockData", _options.DataSet);

        await SeedServiceRequestsAsync(basePath, ct);
        await SeedItemsAsync(basePath, ct);
        await SeedStatusHistoriesAsync(basePath, ct);
        await SeedOffersAsync(basePath, ct);
        await SeedOfferItemsAsync(basePath, ct);
        await SeedAssignmentsAsync(basePath, ct);
        await SeedWorkPhasesAsync(basePath, ct);
        await SeedWorkLogsAsync(basePath, ct);
        await SeedCompletionsAsync(basePath, ct);
        await SeedDisputesAsync(basePath, ct);
        await SeedMessagesAsync(basePath, ct);
        await AdvanceSequencesAsync(ct);

        _logger.LogInformation("ServiceRequest MockData seeder completed.");
    }

    private async Task SeedServiceRequestsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-requests.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockServiceRequestModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequests.AnyAsync(r => r.Id == model.Id, ct)) continue;

            var entity = ServiceRequestEntity.Create(
                requestCode: model.RequestCode,
                ownerUserId: model.OwnerUserId,
                vesselId: model.VesselId,
                serviceCategoryCode: model.ServiceCategoryCode,
                serviceTypeCode: model.ServiceTypeCode,
                title: model.Title,
                description: model.Description,
                priority: (ServiceRequestPriority)model.Priority,
                requestedStartDate: model.RequestedStartDate,
                requestedEndDate: model.RequestedEndDate,
                locationCountryCode: model.LocationCountryCode,
                locationCityCode: model.LocationCityCode,
                locationMarinaName: model.LocationMarinaName,
                locationLatitude: null,
                locationLongitude: null,
                ownerNotes: null,
                expiresAt: null);

            entity.Id = model.Id;
            entity.ChangeStatus((ServiceRequestStatus)model.Status);
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;
            entity.IsActive = model.Status != 90 && model.Status != 91 && model.Status != 99;

            if (model.Status == 90 && !string.IsNullOrWhiteSpace(model.CancelReason))
                entity.Cancel(model.OwnerUserId, model.CancelReason);

            await SaveEntityAsync(entity, _db.ServiceRequests, ct, $"service request {model.Id}");
        }
    }

    private async Task SeedItemsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-items.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockServiceRequestItemModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestItems.AnyAsync(i => i.Id == model.Id, ct)) continue;

            var entity = ServiceRequestItemEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                itemType: (ServiceRequestItemType)model.ItemType,
                title: model.Title,
                description: model.Description,
                quantity: model.Quantity,
                unitCode: model.UnitCode,
                estimatedUnitPrice: model.EstimatedUnitPrice,
                sortOrder: model.SortOrder);

            entity.Id = model.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestItems, ct, $"service request item {model.Id}");
        }
    }

    private async Task SeedStatusHistoriesAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-status-history.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockStatusHistoryModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestStatusHistories.AnyAsync(h => h.Id == model.Id, ct)) continue;

            var entity = ServiceRequestStatusHistoryEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                fromStatus: (ServiceRequestStatus)model.FromStatus,
                toStatus: (ServiceRequestStatus)model.ToStatus,
                reason: model.Reason,
                actorUserId: model.ActorUserId,
                actorType: (ServiceRequestActorType)model.ActorType);

            entity.Id = model.Id;
            SetPrivateProperty(entity, "OccurredAt", model.OccurredAt);
            entity.CreateDate = model.OccurredAt;
            entity.ModifyDate = model.OccurredAt;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestStatusHistories, ct, $"status history {model.Id}");
        }
    }

    private async Task SeedOffersAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-offers.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockOfferModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestOffers.AnyAsync(o => o.Id == model.Id, ct)) continue;

            var entity = ServiceRequestOfferEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                providerProfileId: model.ProviderProfileId,
                providerUserId: model.ProviderUserId,
                totalAmount: model.TotalAmount,
                currencyCode: model.CurrencyCode,
                description: model.Description,
                providerNotes: null,
                estimatedStartDate: model.EstimatedStartDate,
                estimatedEndDate: model.EstimatedEndDate,
                estimatedDurationMinutes: model.EstimatedDurationMinutes,
                expiresAt: null);

            entity.Id = model.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            // Apply offer status
            var status = (ServiceRequestOfferStatus)model.Status;
            if (status >= ServiceRequestOfferStatus.Submitted) entity.Submit();
            if (status == ServiceRequestOfferStatus.Accepted)
                SetPrivateProperty(entity, "Status", ServiceRequestOfferStatus.Accepted);
            else if (status != ServiceRequestOfferStatus.Draft && status != ServiceRequestOfferStatus.Submitted)
                SetPrivateProperty(entity, "Status", status);

            await SaveEntityAsync(entity, _db.ServiceRequestOffers, ct, $"offer {model.Id}");
        }
    }

    private async Task SeedOfferItemsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-offer-items.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockOfferItemModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestOfferItems.AnyAsync(i => i.Id == model.Id, ct)) continue;

            var entity = ServiceRequestOfferItemEntity.Create(
                serviceRequestOfferId: model.ServiceRequestOfferId,
                itemType: (ServiceRequestOfferItemType)model.ItemType,
                title: model.Title,
                description: model.Description,
                quantity: model.Quantity,
                unitPrice: Math.Abs(model.UnitPrice),
                currencyCode: model.CurrencyCode,
                sortOrder: model.SortOrder);

            entity.Id = model.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestOfferItems, ct, $"offer item {model.Id}");
        }
    }

    private async Task SeedAssignmentsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-assignments.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockAssignmentModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestAssignments.AnyAsync(a => a.Id == model.Id, ct)) continue;

            var entity = ServiceRequestAssignmentEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                serviceRequestOfferId: model.ServiceRequestOfferId,
                providerProfileId: model.ProviderProfileId,
                providerUserId: model.ProviderUserId,
                assignedTeamMemberId: null,
                scheduledStartDate: model.ScheduledStartDate,
                scheduledEndDate: model.ScheduledEndDate);

            entity.Id = model.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            var status = (ServiceRequestAssignmentStatus)model.Status;
            SetPrivateProperty(entity, "Status", status);
            if (model.ActualStartDate.HasValue)
                SetPrivateProperty(entity, "ActualStartDate", model.ActualStartDate);
            if (model.ActualEndDate.HasValue)
                SetPrivateProperty(entity, "ActualEndDate", model.ActualEndDate);

            await SaveEntityAsync(entity, _db.ServiceRequestAssignments, ct, $"assignment {model.Id}");
        }
    }

    private async Task SeedWorkPhasesAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "work-phases.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockWorkPhaseModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.WorkPhases.AnyAsync(p => p.Id == model.Id, ct)) continue;

            var entity = WorkPhaseEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                phaseNumber: model.PhaseNumber,
                title: model.Title,
                displayOrder: model.DisplayOrder);

            entity.Id = model.Id;
            entity.UpdateProgress(model.ProgressPercent, model.Status);
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.WorkPhases, ct, $"work phase {model.Id}");
        }
    }

    private async Task SeedWorkLogsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-worklogs.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockWorkLogModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestWorkLogs.AnyAsync(w => w.Id == model.Id, ct)) continue;

            var entity = ServiceRequestWorkLogEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                serviceRequestAssignmentId: model.ServiceRequestAssignmentId,
                providerUserId: model.ProviderUserId,
                logType: (ServiceRequestWorkLogType)model.LogType,
                title: model.Title,
                description: model.Description,
                locationLatitude: null,
                locationLongitude: null,
                attachmentFileId: null);

            entity.Id = model.Id;
            SetPrivateProperty(entity, "LoggedAt", model.LoggedAt);
            entity.CreateDate = model.LoggedAt;
            entity.ModifyDate = model.LoggedAt;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestWorkLogs, ct, $"work log {model.Id}");
        }
    }

    private async Task SeedCompletionsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-completions.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockCompletionModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestCompletions.AnyAsync(c => c.Id == model.Id, ct)) continue;

            var entity = ServiceRequestCompletionEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                serviceRequestAssignmentId: model.ServiceRequestAssignmentId,
                providerUserId: model.ProviderUserId,
                completionNotes: model.CompletionNotes,
                evidenceFileId: null);

            entity.Id = model.Id;
            SetPrivateProperty(entity, "SubmittedAt", model.SubmittedAt);
            SetPrivateProperty(entity, "Status", (ServiceRequestCompletionStatus)model.Status);
            entity.CreateDate = model.SubmittedAt;
            entity.ModifyDate = model.ReviewedAt ?? model.SubmittedAt;
            entity.IsDeleted = false;

            if (model.ReviewedAt.HasValue && model.ReviewedByUserId.HasValue)
            {
                SetPrivateProperty(entity, "ReviewedAt", model.ReviewedAt);
                SetPrivateProperty(entity, "ReviewedByUserId", model.ReviewedByUserId);
                SetPrivateProperty(entity, "ReviewNotes", model.ReviewNotes);
            }

            await SaveEntityAsync(entity, _db.ServiceRequestCompletions, ct, $"completion {model.Id}");
        }
    }

    private async Task SeedDisputesAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-disputes.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockDisputeModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestDisputes.AnyAsync(d => d.Id == model.Id, ct)) continue;

            var entity = ServiceRequestDisputeEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                openedByUserId: model.OpenedByUserId,
                openedByActorType: (ServiceRequestActorType)model.OpenedByActorType,
                reason: (ServiceRequestDisputeReason)model.Reason,
                description: model.Description);

            entity.Id = model.Id;
            SetPrivateProperty(entity, "OpenedAt", model.OpenedAt);
            SetPrivateProperty(entity, "Status", (ServiceRequestDisputeStatus)model.Status);
            entity.CreateDate = model.OpenedAt;
            entity.ModifyDate = model.OpenedAt;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestDisputes, ct, $"dispute {model.Id}");
        }
    }

    private async Task SeedMessagesAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "service-request-messages.json");
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found: {Path}", filePath); return; }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockMessageModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.ServiceRequestMessages.AnyAsync(m => m.Id == model.Id, ct)) continue;

            var entity = ServiceRequestMessageEntity.Create(
                serviceRequestId: model.ServiceRequestId,
                senderUserId: model.SenderUserId,
                senderType: (ServiceRequestMessageSenderType)model.SenderType,
                messageType: (ServiceRequestMessageType)model.MessageType,
                content: model.Content,
                attachmentFileId: null);

            entity.Id = model.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ServiceRequestMessages, ct, $"message {model.Id}");
        }
    }

    private async Task SaveEntityAsync<T>(T entity, Microsoft.EntityFrameworkCore.DbSet<T> dbSet, CancellationToken ct, string label)
        where T : class
    {
        dbSet.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogDebug("Seeded {Label}.", label);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed {Label}, skipping.", label);
            _db.ChangeTracker.Clear();
        }
    }

    private async Task AdvanceSequencesAsync(CancellationToken ct)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                DO $$
                DECLARE seq_name text;
                DECLARE tbl_name text;
                BEGIN
                    FOREACH tbl_name IN ARRAY ARRAY[
                        'servicerequests', 'servicerequestitems', 'servicerequestoffers',
                        'servicerequestofferitems', 'servicerequestassignments',
                        'workphases', 'servicerequestworklogs', 'servicerequestcompletions',
                        'servicerequestdisputes', 'servicerequestmessages',
                        'servicerequeststatushistories'
                    ] LOOP
                        BEGIN
                            SELECT pg_get_serial_sequence('servicerequest.' || tbl_name, 'Id') INTO seq_name;
                            IF seq_name IS NOT NULL THEN
                                PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM servicerequest.servicerequests), 0)));
                            END IF;
                        EXCEPTION WHEN OTHERS THEN
                            NULL;
                        END;
                    END LOOP;
                END $$;", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not advance ServiceRequest sequences (non-fatal).");
        }
    }

    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var prop = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    // --- Seed models ---

    private sealed record MockServiceRequestModel(
        long Id, string RequestCode, long OwnerUserId, long VesselId,
        string ServiceCategoryCode, string? ServiceTypeCode,
        string Title, string? Description, int Status, int Priority,
        DateTime? RequestedStartDate, DateTime? RequestedEndDate,
        string? LocationCountryCode, string? LocationCityCode, string? LocationMarinaName,
        string? CancelReason = null);

    private sealed record MockServiceRequestItemModel(
        long Id, long ServiceRequestId, int ItemType,
        string Title, string? Description, int Quantity,
        string? UnitCode, decimal? EstimatedUnitPrice, int SortOrder);

    private sealed record MockStatusHistoryModel(
        long Id, long ServiceRequestId,
        int FromStatus, int ToStatus,
        string? Reason, long? ActorUserId, int ActorType,
        DateTime OccurredAt);

    private sealed record MockOfferModel(
        long Id, long ServiceRequestId,
        long ProviderProfileId, long ProviderUserId,
        int Status, decimal TotalAmount, string CurrencyCode,
        string? Description,
        DateTime? EstimatedStartDate, DateTime? EstimatedEndDate,
        int? EstimatedDurationMinutes);

    private sealed record MockOfferItemModel(
        long Id, long ServiceRequestOfferId,
        int ItemType, string Title, string? Description,
        int Quantity, decimal UnitPrice, string CurrencyCode, int SortOrder);

    private sealed record MockAssignmentModel(
        long Id, long ServiceRequestId, long ServiceRequestOfferId,
        long ProviderProfileId, long ProviderUserId,
        int Status,
        DateTime? ScheduledStartDate, DateTime? ScheduledEndDate,
        DateTime? ActualStartDate, DateTime? ActualEndDate);

    private sealed record MockWorkPhaseModel(
        long Id, long ServiceRequestId,
        int PhaseNumber, string Title,
        int ProgressPercent, string Status, int DisplayOrder);

    private sealed record MockWorkLogModel(
        long Id, long ServiceRequestId, long ServiceRequestAssignmentId,
        long ProviderUserId, int LogType,
        string Title, string? Description, DateTime LoggedAt);

    private sealed record MockCompletionModel(
        long Id, long ServiceRequestId, long ServiceRequestAssignmentId,
        long ProviderUserId, int Status, string? CompletionNotes,
        DateTime SubmittedAt, DateTime? ReviewedAt, long? ReviewedByUserId, string? ReviewNotes);

    private sealed record MockDisputeModel(
        long Id, long ServiceRequestId,
        long OpenedByUserId, int OpenedByActorType,
        int Status, int Reason, string Description, DateTime OpenedAt);

    private sealed record MockMessageModel(
        long Id, long ServiceRequestId,
        long SenderUserId, int SenderType, int MessageType, string Content);
}
