using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// The use cases the evals score, each with its examples. A module's use case references its Application project and
/// answers through its handler, with inputs that cover what it is for. The suggested answers are evaluated on the
/// checked-in dataset (<see cref="SupportTicketDataset"/>): tickets one article answers, tickets two answer together, and
/// tickets no article answers, whose answer must say so rather than invent one.
/// </summary>
public static class UseCases
{
    public static IReadOnlyList<EvalUseCase> All { get; } =
    [
        new(
            "ask-the-model",
            (model, example, cancellationToken) => model.GetResponseAsync(EvalPrompt.Messages(example), cancellationToken: cancellationToken),
            [
                new("In one sentence: what does an invoice record?"),
                new(
                    "When is invoice 2026-001 due?",
                    "Invoice 2026-001 was issued on 1 March 2026 for 1,200 EUR and is due 30 days after it was issued, on 31 March 2026."),
            ]),
        new(
            "suggest-answer",
            SuggestedAnswers.AnswerAsync,
            [.. SupportTicketDataset.Load().Select(SupportTicketDataset.ToExample)]),
    ];
}
