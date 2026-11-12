using FabricPilot.Modules.Contracts.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Contracts.Infrastructure.Persistence.Configurations;

/// <summary>One row per contract; its renewal date is the domain's, stored so a list can show it and a query can select by it.</summary>
public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.ToTable("contracts");
        builder.HasKey(contract => contract.Id);
        builder.Property(contract => contract.Id).HasColumnName("id").HasConversion(id => id.Value, value => new ContractId(value)).ValueGeneratedNever();
        builder.Property(contract => contract.Product).HasColumnName("product").HasMaxLength(Contract.MaximumProductLength).IsRequired();
        builder.Property(contract => contract.StartsOn).HasColumnName("starts_on").IsRequired();
        builder.Property(contract => contract.EndsOn).HasColumnName("ends_on").IsRequired();
        builder.Property(contract => contract.RenewsOn).HasColumnName("renews_on");
        builder.HasIndex(contract => contract.EndsOn);
        builder.Ignore(contract => contract.DomainEvents);
    }
}
