using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FabricPilot.Modules.Dashboards.Application.Queries.TalkToTheDashboards;

/// <summary>
/// Runs the dashboards agent on the conversation and hands back its updates as they come. The open draft, from the run's
/// state, goes before the conversation as one message, with each declared widget's title from the catalogue; a widget
/// the caller may not see is left out. Nothing here saves: the agent's tools are queries.
/// </summary>
public sealed class TalkToTheDashboardsHandler(IChatClient model, IDispatcher dispatcher, WidgetCatalog catalog, IWidgetAccess access)
    : IQueryHandler<TalkToTheDashboards, Result<IAsyncEnumerable<ChatResponseUpdate>, ApiError>>
{
    public async Task<Result<IAsyncEnumerable<ChatResponseUpdate>, ApiError>> HandleAsync(TalkToTheDashboards query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var draft = DashboardDraft.Read(query.State);
        if (!draft.IsSuccess)
        {
            return Failure(draft.Error);
        }

        List<DraftWidget> described = [];
        foreach (var widget in draft.Value)
        {
            if (catalog.TryFind(widget.Source, out var declared) && await access.AllowsAsync(declared, cancellationToken))
            {
                described.Add(widget with { Title = declared.Title });
            }
        }

        List<ChatMessage> messages = [.. query.Messages];
        if (described.Count > 0)
        {
            messages.Insert(0, new ChatMessage(ChatRole.User, DashboardDraft.Describe(described)));
        }

        var updates = DashboardAgent.Create(model, dispatcher).RunStreamingAsync(messages, cancellationToken: cancellationToken).AsChatResponseUpdatesAsync();
        return Success(updates);
    }
}
