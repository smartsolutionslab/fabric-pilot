namespace FabricPilot.Modules.Knowledge.Domain.Articles;

public sealed record ArticleId(Guid Value)
{
    public static ArticleId New() => new(Guid.CreateVersion7());
}
