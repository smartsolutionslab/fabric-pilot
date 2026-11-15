using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

/// <summary>
/// An answer to a ticket's <see cref="Question"/> (its subject and description), drafted from the published articles
/// by the model, for staff to edit. Nothing is sent: the draft is only shown.
/// </summary>
public sealed record SuggestAnswer(string Question) : IQuery<Result<SuggestedAnswerDto, ApiError>>
{
    /// <summary>A ticket's subject and description at their longest, and room for the line between them.</summary>
    public const int MaximumQuestionLength = 5000;
}
