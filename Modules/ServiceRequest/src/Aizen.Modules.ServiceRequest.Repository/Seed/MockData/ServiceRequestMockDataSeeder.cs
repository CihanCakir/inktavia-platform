using System.Reflection;
using System.Text.Json;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;
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
        await SeedAttachmentsAsync(ct);
        await SeedCatalogItemsAsync(ct);
        await SeedTemplatesAsync(ct);
        await SeedConversationAsync(ct);
        await SeedAcceptedJobAsync(ct);
        await ResetJob91001Async(ct);
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
                locationLatitude: model.LocationLatitude,
                locationLongitude: model.LocationLongitude,
                ownerNotes: null,
                expiresAt: null);

            entity.Id = model.Id;
            entity.ChangeStatus((ServiceRequestStatus)model.Status);
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;
            entity.IsActive = model.Status != 90 && model.Status != 91 && model.Status != 99;

            // Set PublishedAt for biddable statuses so they appear in provider discovery
            var status = (ServiceRequestStatus)model.Status;
            if (status is ServiceRequestStatus.Open or ServiceRequestStatus.WaitingForOffer or ServiceRequestStatus.OfferReceived)
                SetPrivateProperty(entity, "PublishedAt", (DateTime?)DateTime.UtcNow);

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

            // Line economics snapshots are normally set by the offer pricing-calc service (10c), which the mock seeder
            // bypasses — so they defaulted to 0, leaving accepted-offer economics degenerate (LineSubtotal 0 ⇒ the P8
            // combiner sees a ~₺0 base and rejects on the min-contribution gate). Compute them here from qty × unitPrice so
            // a seeded offer has a coherent, acceptable economics base (Exempt lines contribute 0 to the commission base).
            var lineSubtotal = decimal.Round(entity.Quantity * entity.UnitPrice, 2);
            var taxAmount    = decimal.Round(lineSubtotal * entity.TaxRate, 2);
            SetPrivateProperty(entity, "LineSubtotal", lineSubtotal);
            SetPrivateProperty(entity, "TaxAmount", taxAmount);
            SetPrivateProperty(entity, "LineTotal", lineSubtotal + taxAmount);
            SetPrivateProperty(entity, "DiscountAmount", 0m);
            SetPrivateProperty(entity, "CommissionBaseAmount",
                entity.CommissionEligibility == LineCommissionEligibility.Exempt ? 0m : lineSubtotal);

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

    /// <summary>
    private async Task SeedCatalogItemsAsync(CancellationToken ct)
    {
        const long provider2ProfileId = 100011;
        var seeds = new[]
        {
            (Id: 60001L, Type: ServiceRequestOfferItemType.Service, Title: "Gövde Basınçlı Yıkama", Unit: "PIECE", Price: 1250m, Tax: 0.20m),
            (Id: 60002L, Type: ServiceRequestOfferItemType.Product, Title: "Antifouling Boya — Jotun", Unit: "LITER", Price: 480m, Tax: 0.20m),
            (Id: 60003L, Type: ServiceRequestOfferItemType.Service, Title: "Pasta Cila", Unit: (string?)null, Price: 900m, Tax: 0.20m),
            (Id: 60004L, Type: ServiceRequestOfferItemType.Labor, Title: "Tekne Altı İşçilik (saat)", Unit: "HOUR", Price: 350m, Tax: 0.20m),
        };

        foreach (var s in seeds)
        {
            if (await _db.ProviderCatalogItems.AnyAsync(c => c.Id == s.Id, ct)) continue;

            var entity = ProviderCatalogItemEntity.Create(
                provider2ProfileId, s.Type, s.Title, null,
                1, s.Unit, s.Price, "TRY", s.Tax);
            entity.Id = s.Id;
            entity.CreateDate = DateTime.UtcNow;
            entity.ModifyDate = DateTime.UtcNow;
            entity.IsDeleted = false;

            await SaveEntityAsync(entity, _db.ProviderCatalogItems, ct, $"catalog item {s.Id}");
        }
    }

    private async Task SeedTemplatesAsync(CancellationToken ct)
    {
        const long templateId = 70001;
        const long provider2ProfileId = 100011;

        if (await _db.ProviderOfferTemplates.AnyAsync(t => t.Id == templateId, ct)) return;

        var template = ProviderOfferTemplateEntity.Create(provider2ProfileId, "Standart Karina Bakımı",
            "Gövde yıkama + antifouling + cila — standart tekne altı bakım paketi");
        template.Id = templateId;
        template.CreateDate = DateTime.UtcNow;
        template.ModifyDate = DateTime.UtcNow;
        template.IsDeleted = false;

        var items = new[]
        {
            ProviderOfferTemplateItemEntity.Create(ServiceRequestOfferItemType.Service, "Gövde Basınçlı Yıkama", null, 1, "PIECE", 1250, "TRY", 0.20m, OfferDiscountType.None, null, 0),
            ProviderOfferTemplateItemEntity.Create(ServiceRequestOfferItemType.Product, "Antifouling Boya — Jotun", null, 2, "LITER", 480, "TRY", 0.20m, OfferDiscountType.None, null, 1),
            ProviderOfferTemplateItemEntity.Create(ServiceRequestOfferItemType.Service, "Pasta Cila", null, 1, null, 900, "TRY", 0.20m, OfferDiscountType.None, null, 2),
        };
        template.ReplaceItems(items);

        _db.ProviderOfferTemplates.Add(template);
        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogDebug("Seeded template {TemplateId}.", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed template {TemplateId}, skipping.", templateId);
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>Seeds a realistic conversation on SR 9011 for the Messages page.</summary>
    private async Task SeedConversationAsync(CancellationToken ct)
    {
        const long srId = 9011;
        const long firstMsgId = 80001;

        if (await _db.ServiceRequestMessages.AnyAsync(m => m.Id == firstMsgId, ct)) return;
        if (!await _db.ServiceRequests.AnyAsync(r => r.Id == srId, ct)) return;

        var fileId = new Guid("a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4");
        var baseTime = DateTime.UtcNow.AddHours(-3);
        var msgs = new (long Id, long Sender, ServiceRequestMessageSenderType SType, ServiceRequestMessageType MType, string Content, Guid? File, decimal? Lat, decimal? Lng, string? Label, bool Unread)[]
        {
            (80001, 10008, ServiceRequestMessageSenderType.Owner, ServiceRequestMessageType.Text, "Merhaba, teklifinizi aldım. Dümen arızası acil mi yoksa planlı bakımda mı yapılabilir?", null, null, null, null, true),
            (80002, 100011, ServiceRequestMessageSenderType.Provider, ServiceRequestMessageType.Text, "Merhaba, acil müdahale gerekiyor. Hidrolik hortum sızıntısı varsa tekne hareket ettirilemez.", null, null, null, null, false),
            (80003, 10008, ServiceRequestMessageSenderType.Owner, ServiceRequestMessageType.Image, "Hasar fotoğrafı", fileId, null, null, null, true),
            (80004, 10008, ServiceRequestMessageSenderType.Owner, ServiceRequestMessageType.Location, "Çeşme Marina", null, 38.3235m, 26.3050m, "Çeşme Marina", true),
            (80005, 0, ServiceRequestMessageSenderType.System, ServiceRequestMessageType.StatusChange, "OFFER_ACCEPTED", null, null, null, null, false),
        };

        foreach (var m in msgs)
        {
            ServiceRequestMessageEntity entity;
            if (m.Lat.HasValue && m.Lng.HasValue)
                entity = ServiceRequestMessageEntity.CreateLocation(srId, m.Sender, m.SType, m.Lat.Value, m.Lng.Value, m.Label);
            else
                entity = ServiceRequestMessageEntity.Create(srId, m.Sender, m.SType, m.MType, m.Content, m.File);

            entity.Id = m.Id;
            entity.CreateDate = baseTime.AddMinutes((m.Id - firstMsgId) * 10);
            entity.ModifyDate = entity.CreateDate;
            entity.IsDeleted = false;
            if (!m.Unread) entity.MarkAsRead();

            await SaveEntityAsync(entity, _db.ServiceRequestMessages, ct, $"conversation msg {m.Id}");
        }
    }

    /// <summary>
    /// Seeds one accepted offer + assignment (job) for provider2 on SR 9011.
    /// Uses domain entities directly. Idempotent on offer/assignment ids.
    /// </summary>
    private async Task SeedAcceptedJobAsync(CancellationToken ct)
    {
        const long srId = 9011;
        const long offerId = 90001;
        const long assignmentId = 91001;
        const long provider2ProfileId = 100011;
        const long provider2UserId = 100011;
        const long ownerUserId = 10008;

        // Guard: already seeded
        if (await _db.ServiceRequestAssignments.AnyAsync(a => a.Id == assignmentId, ct)) return;
        if (!await _db.ServiceRequests.AnyAsync(r => r.Id == srId, ct)) return;

        // 1. Create and accept an offer (if not already present)
        if (!await _db.ServiceRequestOffers.AnyAsync(o => o.Id == offerId, ct))
        {
            var offer = ServiceRequestOfferEntity.Create(
                srId, provider2ProfileId, provider2UserId,
                5000m, "TRY", "Dümen sistemi komple onarım", null,
                DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(5), 960, null);
            offer.Id = offerId;
            offer.CreateDate = DateTime.UtcNow;
            offer.ModifyDate = DateTime.UtcNow;
            offer.IsDeleted = false;

            // Submit then accept
            SetPrivateProperty(offer, "Status", ServiceRequestOfferStatus.Accepted);
            SetPrivateProperty(offer, "AcceptedAt", (DateTime?)DateTime.UtcNow);
            SetPrivateProperty(offer, "SubmittedAt", (DateTime?)DateTime.UtcNow.AddMinutes(-30));

            _db.ServiceRequestOffers.Add(offer);
            try { await _db.SaveChangesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to seed offer {OfferId}.", offerId); _db.ChangeTracker.Clear(); return; }
        }

        // 2. Set SR status to Assigned
        var sr = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == srId, ct);
        if (sr is not null && sr.Status != ServiceRequestStatus.Assigned)
        {
            sr.ChangeStatus(ServiceRequestStatus.Assigned);
            sr.Assign(provider2ProfileId, "Marine Teknik Çeşme");
            await _db.SaveChangesAsync(ct);
        }

        // 3. Create assignment
        var assignment = ServiceRequestAssignmentEntity.Create(
            srId, offerId, provider2ProfileId, provider2UserId,
            null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3));
        assignment.Id = assignmentId;
        assignment.CreateDate = DateTime.UtcNow;
        assignment.ModifyDate = DateTime.UtcNow;
        assignment.IsDeleted = false;

        _db.ServiceRequestAssignments.Add(assignment);
        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded accepted job: assignment {AssignmentId} on SR {SrId}.", assignmentId, srId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed assignment {AssignmentId}.", assignmentId);
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Dev/Local reset: clears test work logs, completion, lifecycle messages on job 91001 / SR 9011,
    /// resets to Assigned, and seeds offer line items if missing. Idempotent.
    /// </summary>
    private async Task ResetJob91001Async(CancellationToken ct)
    {
        try
        {
        const long srId = 9011;
        const long assignmentId = 91001;
        const long offerId = 90001;

        var sr = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == srId, ct);
        if (sr is null) return;

        var assignment = await _db.ServiceRequestAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct);
        if (assignment is null) return;

        // Idempotent guard: if already Assigned and no work logs, nothing to do (except maybe offer items)
        var hasWorkLogs = await _db.ServiceRequestWorkLogs.AnyAsync(w => w.ServiceRequestAssignmentId == assignmentId, ct);
        var needsReset = sr.Status != ServiceRequestStatus.Assigned || hasWorkLogs;

        if (needsReset)
        {
            // 1. Delete work logs
            var workLogs = await _db.ServiceRequestWorkLogs.Where(w => w.ServiceRequestAssignmentId == assignmentId).ToListAsync(ct);
            _db.ServiceRequestWorkLogs.RemoveRange(workLogs);

            // 2. Delete completion
            var completion = await _db.ServiceRequestCompletions.FirstOrDefaultAsync(c => c.ServiceRequestId == srId, ct);
            if (completion is not null) _db.ServiceRequestCompletions.Remove(completion);

            // 3. Remove lifecycle system messages (keep OFFER_ACCEPTED)
            var sysMessages = await _db.ServiceRequestMessages
                .Where(m => m.ServiceRequestId == srId && m.SenderType == ServiceRequestMessageSenderType.System
                    && m.MessageType == ServiceRequestMessageType.StatusChange && m.Content != "OFFER_ACCEPTED")
                .ToListAsync(ct);
            _db.ServiceRequestMessages.RemoveRange(sysMessages);

            // 4. Trim status history after Assigned
            var historyToRemove = await _db.ServiceRequestStatusHistories
                .Where(h => h.ServiceRequestId == srId
                    && h.ToStatus != ServiceRequestStatus.Assigned
                    && h.ToStatus != ServiceRequestStatus.OfferAccepted
                    && h.ToStatus != ServiceRequestStatus.Open
                    && h.ToStatus != ServiceRequestStatus.WaitingForOffer
                    && h.ToStatus != ServiceRequestStatus.OfferReceived)
                .ToListAsync(ct);
            _db.ServiceRequestStatusHistories.RemoveRange(historyToRemove);

            // 5. Reset assignment + SR
            SetPrivateProperty(assignment, "Status", ServiceRequestAssignmentStatus.Accepted);
            SetPrivateProperty(assignment, "ActualStartDate", (DateTime?)null);
            SetPrivateProperty(assignment, "ActualEndDate", (DateTime?)null);
            sr.ChangeStatus(ServiceRequestStatus.Assigned);

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Reset job 91001 / SR 9011 to Assigned.");
        }

        // 7. Seed offer line items directly via DbContext (bypass ReplaceItems guard on Accepted offer)
        if (!await _db.ServiceRequestOfferItems.AnyAsync(i => i.ServiceRequestOfferId == offerId, ct))
        {
            var offer = await _db.ServiceRequestOffers.FirstOrDefaultAsync(o => o.Id == offerId, ct);
            if (offer is not null)
            {
                var currency = offer.CurrencyCode;
                var itemDefs = new (ServiceRequestOfferItemType Type, string Title, decimal Qty, string Unit, decimal Price, int Sort)[]
                {
                    (ServiceRequestOfferItemType.Labor, "Gövde Temizliği İşçiliği", 24, "HOUR", 45, 0),
                    (ServiceRequestOfferItemType.Product, "International Ultra 300 Antifouling", 15, "LITER", 120, 1),
                    (ServiceRequestOfferItemType.Service, "Sarf Malzeme Paketi", 1, "PIECE", 250, 2),
                };

                foreach (var d in itemDefs)
                {
                    var item = ServiceRequestOfferItemEntity.Create(
                        offerId, d.Type, d.Title, null, d.Qty, d.Price, currency, d.Sort, d.Unit, 0.20m);
                    var lineSub = Math.Round(d.Qty * d.Price, 2, MidpointRounding.AwayFromZero);
                    var lineTax = Math.Round(lineSub * 0.20m, 2, MidpointRounding.AwayFromZero);
                    item.SetComputedTotals(lineSub, 0, lineTax, lineSub + lineTax);
                    _db.ServiceRequestOfferItems.Add(item);
                }

                // subtotal=3130, taxTotal=626, grandTotal=3756
                offer.SetComputedTotals(3130m, 0, 626m, 3756m, 250m, 1800m, 1080m, 0, 0, 0, 0, 0);

                if (string.IsNullOrWhiteSpace(offer.Description))
                    SetPrivateProperty(offer, "Description",
                        "Teknenin su altı kısmındaki kekamozların temizlenmesi, zımparalanması ve seçilen yüksek kaliteli antifouling boyanın iki kat uygulanması işidir.");

                _db.ServiceRequestOffers.Update(offer);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded offer 90001 line items (3 items, GrandTotal=3756).");
            }
        }

        // Optional: seed demo conversation messages
        var hasConvoMessages = await _db.ServiceRequestMessages.AnyAsync(
            m => m.ServiceRequestId == srId && m.SenderType == ServiceRequestMessageSenderType.Owner
                 && m.MessageType == ServiceRequestMessageType.Text && !m.IsDeleted, ct);
        if (!hasConvoMessages)
        {
            var baseTime = DateTime.UtcNow.AddHours(-2);
            var msg1 = ServiceRequestMessageEntity.Create(srId, 10008, ServiceRequestMessageSenderType.Owner,
                ServiceRequestMessageType.Text,
                "Selamlar, boya uygulaması öncesi gövde zımpara bittikten sonra bir fotoğraf paylaşabilir misiniz? Son durumu görmek isterim.", null);
            msg1.CreateDate = baseTime;
            msg1.ModifyDate = baseTime;
            msg1.IsDeleted = false;

            var msg2 = ServiceRequestMessageEntity.Create(srId, 100011, ServiceRequestMessageSenderType.Provider,
                ServiceRequestMessageType.Text,
                "Tabii ki, zımpara işlemi şu an devam ediyor. Öğleden sonra temizlik bitince detaylı fotoğraf göndereceğim.", null);
            msg2.CreateDate = baseTime.AddMinutes(15);
            msg2.ModifyDate = baseTime.AddMinutes(15);
            msg2.IsDeleted = false;

            _db.ServiceRequestMessages.AddRange(msg1, msg2);
            await _db.SaveChangesAsync(ct);
        }

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResetJob91001Async skipped (dev seed, non-fatal).");
            _db.ChangeTracker.Clear();
        }
    }

    /// Seeds a single attachment on the emergency request (SR 9011) for end-to-end gallery/read-url testing.
    /// Uses a deterministic FileId so it can be linked to a real MinIO object later.
    /// </summary>
    private async Task SeedAttachmentsAsync(CancellationToken ct)
    {
        const long attachmentId = 50001;
        const long serviceRequestId = 9011;
        var fileId = new Guid("a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4");

        if (await _db.ServiceRequestAttachments.AnyAsync(a => a.Id == attachmentId, ct))
            return;

        // Only seed if the parent SR exists
        if (!await _db.ServiceRequests.AnyAsync(r => r.Id == serviceRequestId, ct))
            return;

        var entity = ServiceRequestAttachmentEntity.Create(
            serviceRequestId: serviceRequestId,
            fileId: fileId,
            attachmentType: ServiceRequestAttachmentType.Photo,
            title: "Dümen sistemi hasar fotoğrafı",
            description: "Hidrolik hortum sızıntısı gösteren fotoğraf",
            uploaderUserId: 10008,
            uploaderActorType: ServiceRequestActorType.Owner);

        entity.Id = attachmentId;
        entity.CreateDate = DateTime.UtcNow;
        entity.ModifyDate = DateTime.UtcNow;
        entity.IsDeleted = false;

        await SaveEntityAsync(entity, _db.ServiceRequestAttachments, ct, $"attachment {attachmentId}");
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
        string? CancelReason = null,
        decimal? LocationLatitude = null, decimal? LocationLongitude = null);

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
