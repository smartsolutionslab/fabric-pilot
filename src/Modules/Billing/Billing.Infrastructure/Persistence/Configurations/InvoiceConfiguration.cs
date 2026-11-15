using FabricPilot.Modules.Billing.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Billing.Infrastructure.Persistence.Configurations;

/// <summary>One row per invoice, with the total the domain worked out, and one row per line in <c>invoice_lines</c>, saved and read with it.</summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.ToTable("invoices");
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).HasColumnName("id").HasConversion(id => id.Value, value => new InvoiceId(value)).ValueGeneratedNever();
        builder.Property(invoice => invoice.Number).HasColumnName("number").HasMaxLength(Invoice.MaximumNumberLength).IsRequired();
        builder.Property(invoice => invoice.IssuedOn).HasColumnName("issued_on").IsRequired();
        builder.Property(invoice => invoice.DueOn).HasColumnName("due_on").IsRequired();
        builder.Property(invoice => invoice.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(invoice => invoice.Total).HasColumnName("total").HasPrecision(18, 2).IsRequired();
        builder.HasIndex(invoice => invoice.IssuedOn);
        builder.Ignore(invoice => invoice.DomainEvents);
        builder.OwnsMany(invoice => invoice.Lines, lines =>
        {
            lines.ToTable("invoice_lines");
            lines.WithOwner().HasForeignKey("invoice_id");
            lines.Property(line => line.Position).HasColumnName("position").ValueGeneratedNever().IsRequired();
            lines.HasKey("invoice_id", nameof(InvoiceLine.Position));
            lines.Property(line => line.Description).HasColumnName("description").HasMaxLength(InvoiceLine.MaximumDescriptionLength).IsRequired();
            lines.Property(line => line.Quantity).HasColumnName("quantity").IsRequired();
            lines.Property(line => line.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2).IsRequired();
            lines.Ignore(line => line.Amount);
        });
        builder.Navigation(invoice => invoice.Lines).HasField("lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
