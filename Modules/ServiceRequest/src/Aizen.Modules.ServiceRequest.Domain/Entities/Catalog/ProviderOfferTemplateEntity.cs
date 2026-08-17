using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;

public sealed class ProviderOfferTemplateEntity : AizenEntityWithAudit
{
    public long ProviderProfileId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<ProviderOfferTemplateItemEntity> _items = new();
    public IReadOnlyCollection<ProviderOfferTemplateItemEntity> Items => _items.AsReadOnly();

    public ProviderOfferTemplateEntity() { }

    public static ProviderOfferTemplateEntity Create(long providerProfileId, string name, string? description)
    {
        return new ProviderOfferTemplateEntity
        {
            ProviderProfileId = providerProfileId,
            Name = name.Trim(),
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description;
    }

    public void ReplaceItems(IEnumerable<ProviderOfferTemplateItemEntity> newItems)
    {
        _items.Clear();
        _items.AddRange(newItems);
    }

    public void Deactivate() => IsActive = false;
}
