using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class KnowledgeStorage
{
    /// <summary>
    /// The shared articles table, their chunks in the shared vector index the host registers for every module, and a
    /// lock per article on a connection pool of the locks' own.
    /// </summary>
    public static IServiceCollection AddKnowledgeStorage(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        return services
            .AddScoped<IArticleStore, ArticleStore>()
            .AddScoped<IArticleIndex, SharedArticleIndex>()
            .AddSingleton(_ => new ArticleLockConnections(configuration))
            .AddScoped<IArticleLocks, ArticleLocks>();
    }
}
