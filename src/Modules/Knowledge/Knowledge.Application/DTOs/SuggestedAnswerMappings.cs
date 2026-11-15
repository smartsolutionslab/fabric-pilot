using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

namespace FabricPilot.Modules.Knowledge.Application.DTOs;

public static class SuggestedAnswerMappings
{
    public static SuggestedAnswerDto ToDto(this SuggestedAnswer suggestion)
    {
        Ensure.That(suggestion).IsNotNull();
        return new SuggestedAnswerDto(
            suggestion.Drafted,
            suggestion.Text,
            [.. suggestion.Sources.Select(source => new SuggestionSourceDto(source.Article.Value, source.Title))]);
    }
}
