using FabricPilot.Modules.Tickets.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Tickets.Infrastructure.Persistence.Configurations;

/// <summary>One row per ticket, and one row per attachment in <c>ticket_attachments</c>, saved and read with it.</summary>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.ToTable("tickets");
        builder.HasKey(ticket => ticket.Id);
        builder.Property(ticket => ticket.Id).HasColumnName("id").HasConversion(id => id.Value, value => new TicketId(value)).ValueGeneratedNever();
        builder.Property(ticket => ticket.Subject).HasColumnName("subject").HasMaxLength(Ticket.MaximumSubjectLength).IsRequired();
        builder.Property(ticket => ticket.Description).HasColumnName("description").HasMaxLength(Ticket.MaximumDescriptionLength).IsRequired();
        builder.Property(ticket => ticket.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(ticket => ticket.RaisedAt).HasColumnName("raised_at").IsRequired();
        builder.HasIndex(ticket => ticket.RaisedAt);
        builder.Ignore(ticket => ticket.DomainEvents);
        builder.OwnsMany(ticket => ticket.Attachments, attachments =>
        {
            attachments.ToTable("ticket_attachments");
            attachments.WithOwner().HasForeignKey("ticket_id");
            attachments.HasKey(attachment => attachment.Id);
            attachments.Property(attachment => attachment.Id).HasColumnName("id").HasConversion(id => id.Value, value => new AttachmentId(value)).ValueGeneratedNever();
            attachments.Property(attachment => attachment.Position).HasColumnName("position").IsRequired();
            attachments.Property(attachment => attachment.FileName).HasColumnName("file_name").HasMaxLength(Attachment.MaximumFileNameLength).IsRequired();
            attachments.Property(attachment => attachment.ContentType).HasColumnName("content_type").HasMaxLength(Attachment.MaximumContentTypeLength).IsRequired();
            attachments.Property(attachment => attachment.Size).HasColumnName("size").IsRequired();
        });
        builder.Navigation(ticket => ticket.Attachments).HasField("attachments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
