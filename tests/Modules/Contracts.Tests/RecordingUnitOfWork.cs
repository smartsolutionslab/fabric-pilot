using FabricPilot.Contracts;
using FabricPilot.Modules.Contracts.Application;

namespace FabricPilot.Modules.Contracts.Tests;

/// <summary>The module's unit of work in memory: what the store was given counts only once committed.</summary>
internal sealed class RecordingUnitOfWork(InMemoryContractStore contracts) : IContractsUnitOfWork
{
    public int Commits { get; private set; }

    public Task PublishAsync(IntegrationEvent integrationEvent) =>
        throw new InvalidOperationException("Recording a contract announces nothing.");

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        contracts.Commit();
        Commits++;
        return Task.CompletedTask;
    }
}
