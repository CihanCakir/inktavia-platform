using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class ContactMessageEntityConfiguration : IEntityTypeConfiguration<ContactMessageEntity>
{
    public void Configure(EntityTypeBuilder<ContactMessageEntity> builder)
    {
        builder.ToTable("contact_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.SourcePage).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.SpamScore).IsRequired();
        builder.Property(x => x.TicketRef).HasMaxLength(40).IsRequired();
        builder.Property(x => x.IpHash).HasMaxLength(64);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.TicketRef).IsUnique();
        builder.HasIndex(x => new { x.IpHash, x.CreatedAt });  // rate/abuse correlation
        builder.HasIndex(x => new { x.Status, x.CreatedAt });  // admin triage queue
    }
}
