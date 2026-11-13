using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Knowledge.Api.Endpoints;

/// <summary>
/// Staff ask for an answer to a ticket, drafted from the knowledge base. It is a read: the draft is answered, never
/// stored or sent, and staff edit it before anything reaches a customer. A POST, since a ticket's text is too long for
/// a query string.
/// </summary>
public sealed class SuggestedAnswerEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapPost("/suggested-answers", SuggestAsync)
            .WithTags("Knowledge")
            .RequireAuthorization(KnowledgePolicyNames.SuggestAnswers);
    }

    private static async Task<Results<Ok<SuggestedAnswerDto>, ProblemHttpResult>> SuggestAsync(SuggestionRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Ensure.That(request).IsNotNull();

        return (await dispatcher.QueryAsync(new SuggestAnswer(request.Question), cancellationToken)).ToHttpResult();
    }
}
