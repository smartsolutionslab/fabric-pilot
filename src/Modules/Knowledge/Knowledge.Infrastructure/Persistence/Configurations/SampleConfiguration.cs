using FabricPilot.Modules.Knowledge.Domain.Samples;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Knowledge.Infrastructure.Persistence.Configurations;

public sealed class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.ToTable("samples");
        builder.HasKey(sample => sample.Id);
        builder.Property(sample => sample.Id).HasColumnName("id").ValueGeneratedNever()
            .HasConversion(id => id.Value, value => SampleId.From(value));
        builder.Property(sample => sample.Name).HasColumnName("name").HasMaxLength(SampleName.MaxLength).IsRequired()
            .HasConversion(name => name.Value, value => SampleName.From(value));
    }
}
