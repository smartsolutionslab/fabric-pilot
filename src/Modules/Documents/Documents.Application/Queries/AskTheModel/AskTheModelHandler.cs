using FabricPilot.BuildingBlocks.Messaging;
using Microsoft.Extensions.AI;

namespace FabricPilot.Modules.Documents.Application.Queries.AskTheModel;

/// <summary>
/// Asks the model through <see cref="IChatClient"/>, whichever provider the project chose. The answer is the model's
/// output: untrusted input, returned as text and never executed or put into SQL or markup.
/// </summary>
public sealed class AskTheModelHandler(IChatClient model) : IQueryHandler<AskTheModel, Result<string, ApiError>>
{
    public async Task<Result<string, ApiError>> HandleAsync(AskTheModel query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var response = await model.GetResponseAsync(query.Question, cancellationToken: cancellationToken);
        return Success(response.Text);
    }
}
