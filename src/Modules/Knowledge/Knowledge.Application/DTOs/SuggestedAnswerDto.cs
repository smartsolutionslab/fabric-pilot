namespace FabricPilot.Modules.Knowledge.Application.DTOs;

/// <param name="Drafted">Whether the text is an answer drafted from the knowledge base; when not, the text says why there is none.</param>
/// <param name="Text">The model's draft, untrusted: staff read and edit it as plain text before anything reaches a customer.</param>
/// <param name="Sources">The articles the draft is drafted from, each once, in the order the model cited them.</param>
public sealed record SuggestedAnswerDto(bool Drafted, string Text, IReadOnlyList<SuggestionSourceDto> Sources);
