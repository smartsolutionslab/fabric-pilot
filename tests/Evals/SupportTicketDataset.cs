using System.Text.Json;
using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

namespace FabricPilot.Evals;

/// <summary>
/// The checked-in dataset of the suggested answers (<c>Datasets/support-tickets.json</c>): real-looking tickets, each with
/// the articles the search finds for it and the sources a good answer cites. It is checked when it is read, so a dataset
/// too small or inconsistent to hold the use case to the threshold fails before any model is asked.
/// </summary>
public static class SupportTicketDataset
{
    /// <summary>Fewer tickets than this and one noisy judgement moves a mean too far for the threshold to mean much.</summary>
    public const int MinimumTickets = 20;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    /// <summary>The repository's dataset.</summary>
    public static IReadOnlyList<SupportTicket> Load() => Load(EvalPaths.SupportTickets);

    /// <summary>The dataset at the path; fails naming the ticket and what is wrong with it.</summary>
    public static IReadOnlyList<SupportTicket> Load(string path)
    {
        Ensure.That(path).IsNotNull();

        IReadOnlyList<SupportTicket> tickets;
        try
        {
            tickets = JsonSerializer.Deserialize<SupportTicketFile>(File.ReadAllText(path), Json)?.Tickets ?? [];
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"{path} is not a dataset of tickets, each with subject, description, articles and expectedSources (empty when no article answers): {exception.Message}", exception);
        }

        if (tickets.Count < MinimumTickets)
        {
            throw new InvalidOperationException($"{path} holds {tickets.Count} ticket{(tickets.Count == 1 ? string.Empty : "s")}, but the evals need at least {MinimumTickets}.");
        }

        foreach (var (index, ticket) in tickets.Index())
        {
            Check(path, index + 1, ticket);
        }

        return tickets;
    }

    /// <summary>The example a ticket is: its subject and description as the question, every article as the grounding context.</summary>
    public static EvalExample ToExample(SupportTicket ticket)
    {
        Ensure.That(ticket).IsNotNull();

        var (subject, description, articles, expectedSources) = ticket;
        var context = string.Join(EvalPassages.ArticleSeparator, articles.Select(article => $"{article.Title}\n\n{article.Body}"));
        return new EvalExample($"{subject}\n\n{description}", context, expectedSources);
    }

    private static void Check(string path, int number, SupportTicket ticket)
    {
        var (subject, description, articles, expectedSources) = ticket;
        var name = $"Ticket {number} ('{subject}') in {path}";
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException($"{name} needs a subject and a description.");
        }

        if (articles.Count == 0 || articles.Any(article => string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Body)))
        {
            throw new InvalidOperationException($"{name} needs articles, each with a title and a body.");
        }

        if (articles.Count > SuggestAnswerHandler.PassageCount)
        {
            throw new InvalidOperationException($"{name} has {articles.Count} articles, but the search hands the model at most {SuggestAnswerHandler.PassageCount} articles, so the rest would never be seen.");
        }

        if (articles.Any(article => article.Title.Contains('\n', StringComparison.Ordinal) || $"{article.Title}\n\n{article.Body}".Contains(EvalPassages.ArticleSeparator, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"{name} has an article with a title on more than one line or a line of --- in its body, which would split it into two passages.");
        }

        if (articles.Select(article => article.Title).Distinct(StringComparer.Ordinal).Count() != articles.Count)
        {
            throw new InvalidOperationException($"{name} has two articles with one title, so its sources cannot name them apart.");
        }

        var unknown = expectedSources.Where(source => articles.All(article => article.Title != source)).ToList();
        if (unknown.Count > 0)
        {
            throw new InvalidOperationException($"{name} expects sources that are not among its articles: {string.Join(", ", unknown.Select(source => $"'{source}'"))}.");
        }
    }
}
