using FabricPilot.Contracts;
using FabricPilot.Modules.Knowledge.Application;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>The module's unit of work in memory: published events count only once committed, with the store's changes.</summary>
internal sealed class RecordingUnitOfWork(InMemoryArticleStore articles) : IKnowledgeUnitOfWork
{
    private readonly List<IntegrationEvent> queued = [];

    public List<IntegrationEvent> Published { get; } = [];

    public int Commits { get; private set; }

    public Task PublishAsync(IntegrationEvent integrationEvent)
    {
        queued.Add(integrationEvent);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        articles.Commit();
        Published.AddRange(queued);
        queued.Clear();
        Commits++;
        return Task.CompletedTask;
    }
}
