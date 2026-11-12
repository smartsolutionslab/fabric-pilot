using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// The use cases the evals score, each with its examples. A module's use case references its Application project and
/// answers through its handler, with inputs that cover what it is for. The suggested answers cover a ticket an article
/// answers, in two subjects, and one no article answers, whose answer must say so rather than invent one.
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
            [
                new(
                    "Printer shows error 42 since this morning\n\nOur office printer stopped printing and shows error 42. What can we do?",
                    "Printer error 42\n\nError 42 means the print spooler stopped. Open Services on the computer that shares the printer, restart the Print Spooler service, then print a test page. If the error comes back, reinstall the printer driver."),
                new(
                    "VPN sign-in rejected\n\nSince today the VPN client rejects my sign-in, although I type the right password.",
                    "VPN sign-in fails\n\nWhen the VPN client rejects a sign-in with a correct password, the password has usually expired. Reset it in the self-service portal, wait five minutes for it to reach the VPN, then sign in again."),
                new(
                    "Wrong billing address\n\nPlease change the billing address on our invoices to our new office.",
                    "Printer error 42\n\nError 42 means the print spooler stopped. Open Services on the computer that shares the printer, restart the Print Spooler service, then print a test page."),
            ]),
    ];
}
