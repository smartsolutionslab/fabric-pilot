using FabricPilot.Modules.Knowledge.Domain.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FabricPilot.Modules.Knowledge.Infrastructure.Persistence.Configurations;

/// <summary>
/// One row per article in <c>knowledge.articles</c>, shared by every tenant: no tenant column and no tenant filter, and
/// written only on an endpoint mapped with <c>WritesSharedRecords()</c>.
/// </summary>
public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        Ensure.That(builder).IsNotNull();

        builder.SharedByAllTenants();
        builder.ToTable("articles");
        builder.HasKey(article => article.Id);
        builder.Property(article => article.Id).HasColumnName("id").HasConversion(id => id.Value, value => new ArticleId(value)).ValueGeneratedNever();
        builder.Property(article => article.Title).HasColumnName("title").HasMaxLength(Article.MaximumTitleLength).IsRequired();
        builder.Property(article => article.Body).HasColumnName("body").HasMaxLength(Article.MaximumBodyLength).IsRequired();
        builder.Property(article => article.State).HasColumnName("state").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(article => article.ChangedAt).HasColumnName("changed_at").IsRequired();
        builder.Property(article => article.PublishedAt).HasColumnName("published_at");
        builder.Property(article => article.NeedsIndexing).HasColumnName("needs_indexing").IsRequired();
        builder.HasIndex(article => new { article.State, article.ChangedAt });
        builder.Ignore(article => article.DomainEvents);
    }
}
