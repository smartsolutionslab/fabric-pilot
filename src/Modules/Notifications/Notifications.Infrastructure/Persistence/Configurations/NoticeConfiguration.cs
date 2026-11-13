using FabricPilot.Modules.Notifications.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>One row per notice; one notice per causing event, which the unique index holds even under a race.</summary>
public sealed class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    /// <summary>Room for a ticket's subject of 200 characters and the sentence around it.</summary>
    private const int MaximumTextLength = 400;

    /// <summary>A Keycloak user id is a UUID; room for an identity provider whose ids are longer.</summary>
    private const int MaximumRecipientLength = 255;

    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.ToTable("notices");
        builder.HasKey(notice => notice.Id);
        builder.Property(notice => notice.Id).HasColumnName("id").HasConversion(id => id.Value, value => new NoticeId(value)).ValueGeneratedNever();
        builder.Property(notice => notice.CausingEvent).HasColumnName("causing_event").IsRequired();
        builder.Property(notice => notice.Recipient).HasColumnName("recipient").HasMaxLength(MaximumRecipientLength).IsRequired();
        builder.Property(notice => notice.TicketId).HasColumnName("ticket_id").IsRequired();
        builder.Property(notice => notice.Text).HasColumnName("text").HasMaxLength(MaximumTextLength).IsRequired();
        builder.Property(notice => notice.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(notice => notice.EmailedAt).HasColumnName("emailed_at");
        builder.Ignore(notice => notice.IsEmailed);
        builder.Ignore(notice => notice.DomainEvents);
        builder.HasIndex(notice => notice.CausingEvent).IsUnique();
        builder.HasIndex(notice => new { notice.Recipient, notice.CreatedAt });
    }
}
