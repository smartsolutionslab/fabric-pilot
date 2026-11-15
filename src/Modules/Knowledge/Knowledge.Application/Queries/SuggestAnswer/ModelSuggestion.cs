namespace FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

/// <summary>
/// The shape the model is asked to answer in. It is untrusted: <see cref="Sources"/> are checked against the passages
/// the model was given, and nothing else of it is used but <see cref="Answer"/>, as plain text.
/// </summary>
/// <param name="Answered">Whether the passages answer the ticket.</param>
/// <param name="Answer">The drafted answer; empty when they do not.</param>
/// <param name="Sources">The numbers of the passages the answer uses, counted from 1.</param>
public sealed record ModelSuggestion(bool Answered, string Answer, IReadOnlyList<int> Sources);
