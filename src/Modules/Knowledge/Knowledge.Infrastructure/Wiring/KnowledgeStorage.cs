using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class KnowledgeStorage
{
    /// <summary>The shared articles table, their chunks in the shared vector index the host registers for every module, and a lock per article.</summary>
    public static IServiceCollection AddKnowledgeStorage(this IServiceCollection services) =>
        services
            .AddScoped<IArticleStore, ArticleStore>()
            .AddScoped<IArticleIndex, SharedArticleIndex>()
            .AddScoped<IArticleLocks, ArticleLocks>();
}
