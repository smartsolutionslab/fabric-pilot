using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

/// <summary>The project's dispatcher, keeping every request sent through it, so a test sees what went through it.</summary>
public sealed class RecordingDispatcher(IDispatcher inner) : IDispatcher
{
    private readonly List<object> requests = [];

    public IReadOnlyList<object> Requests => requests;

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        requests.Add(command);
        return inner.SendAsync(command, cancellationToken);
    }

    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        requests.Add(query);
        return inner.QueryAsync(query, cancellationToken);
    }
}
