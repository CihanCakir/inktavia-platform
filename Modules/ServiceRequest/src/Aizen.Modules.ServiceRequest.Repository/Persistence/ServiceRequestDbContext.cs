using Aizen.Core.EFCore;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence;

[DocumentationInfo("ServiceRequest EF DbContext", "EF Core context for the service request PostgreSQL schema.")]
public sealed class ServiceRequestDbContext : AizenDbContext
{
    public ServiceRequestDbContext(DbContextOptions<ServiceRequestDbContext> options)
        : base(options) { }

    public DbSet<ServiceRequestEntity> ServiceRequests => Set<ServiceRequestEntity>();
    public DbSet<ServiceRequestItemEntity> ServiceRequestItems => Set<ServiceRequestItemEntity>();
    public DbSet<ServiceRequestStatusHistoryEntity> ServiceRequestStatusHistories => Set<ServiceRequestStatusHistoryEntity>();
    public DbSet<ServiceRequestAttachmentEntity> ServiceRequestAttachments => Set<ServiceRequestAttachmentEntity>();
    public DbSet<ServiceRequestMessageEntity> ServiceRequestMessages => Set<ServiceRequestMessageEntity>();
    public DbSet<ServiceRequestOfferEntity> ServiceRequestOffers => Set<ServiceRequestOfferEntity>();
    public DbSet<ServiceRequestOfferItemEntity> ServiceRequestOfferItems => Set<ServiceRequestOfferItemEntity>();
    public DbSet<OfferFxSnapshotEntity> OfferFxSnapshots => Set<OfferFxSnapshotEntity>();   // BE-S3b
    public DbSet<ServiceRequestAssignmentEntity> ServiceRequestAssignments => Set<ServiceRequestAssignmentEntity>();
    public DbSet<ServiceRequestWorkLogEntity> ServiceRequestWorkLogs => Set<ServiceRequestWorkLogEntity>();
    public DbSet<ServiceRequestCompletionEntity> ServiceRequestCompletions => Set<ServiceRequestCompletionEntity>();
    public DbSet<ServiceRequestDisputeEntity> ServiceRequestDisputes => Set<ServiceRequestDisputeEntity>();
    public DbSet<WorkPhaseEntity> WorkPhases => Set<WorkPhaseEntity>();
    public DbSet<ServiceRequestConversationEntity> Conversations => Set<ServiceRequestConversationEntity>();
    public DbSet<ConversationMessageEntity> ConversationMessages => Set<ConversationMessageEntity>();
    public DbSet<MessageAttachmentEntity> MessageAttachments => Set<MessageAttachmentEntity>();
    public DbSet<ProviderCatalogItemEntity> ProviderCatalogItems => Set<ProviderCatalogItemEntity>();
    public DbSet<ProviderOfferTemplateEntity> ProviderOfferTemplates => Set<ProviderOfferTemplateEntity>();
    public DbSet<ProviderOfferTemplateItemEntity> ProviderOfferTemplateItems => Set<ProviderOfferTemplateItemEntity>();

    // ── Pricing attributes (BE-S2, §20.6) — descriptive metadata, not part of the line money math ──
    public DbSet<PricingAttributeDefinitionEntity> PricingAttributeDefinitions => Set<PricingAttributeDefinitionEntity>();
    public DbSet<PricingAttributeDefinitionCategoryEntity> PricingAttributeDefinitionCategories => Set<PricingAttributeDefinitionCategoryEntity>();
    public DbSet<PricingAttributeValueEntity> PricingAttributeValues => Set<PricingAttributeValueEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("servicerequest");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceRequestDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimeProperties();
        return base.SaveChanges();
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}
