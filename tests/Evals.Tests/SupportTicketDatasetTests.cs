using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

namespace FabricPilot.Evals.Tests;

/// <summary>
/// The checked-in dataset the suggested answers are evaluated on: real-looking tickets, each with the articles the search
/// would find and the sources a good answer cites. A dataset that cannot hold the use case to the threshold fails here,
/// before anything asks a model.
/// </summary>
public sealed class SupportTicketDatasetTests : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("dataset-");

    [Fact]
    public void The_checked_in_dataset_holds_at_least_20_tickets()
    {
        SupportTicketDataset.MinimumTickets.ShouldBe(20);
        SupportTicketDataset.Load().Count.ShouldBeGreaterThanOrEqualTo(SupportTicketDataset.MinimumTickets);
    }

    [Fact]
    public void Every_ticket_has_a_subject_a_description_and_articles_and_its_expected_sources_are_its_own_articles()
    {
        foreach (var ticket in SupportTicketDataset.Load())
        {
            ticket.Subject.ShouldNotBeNullOrWhiteSpace();
            ticket.Description.ShouldNotBeNullOrWhiteSpace(ticket.Subject);
            ticket.Articles.ShouldNotBeEmpty(ticket.Subject);
            ticket.Articles.Select(article => article.Title).ShouldBeUnique(ticket.Subject);
            ticket.ExpectedSources.ShouldAllBe(source => ticket.Articles.Any(article => article.Title == source), ticket.Subject);
        }
    }

    [Fact]
    public void The_dataset_covers_tickets_one_article_answers_tickets_two_articles_answer_and_tickets_no_article_answers()
    {
        var tickets = SupportTicketDataset.Load();

        tickets.Count(ticket => ticket.ExpectedSources.Count == 1).ShouldBeGreaterThanOrEqualTo(10);
        tickets.Count(ticket => ticket.ExpectedSources.Count >= 2).ShouldBeGreaterThanOrEqualTo(3);
        tickets.Count(ticket => ticket.ExpectedSources.Count == 0).ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void The_answering_article_is_not_always_the_first_passage_so_citing_passage_1_is_not_enough()
    {
        var tickets = SupportTicketDataset.Load();

        var firstIsTheOnlySource = tickets.Count(ticket => ticket.ExpectedSources.Count == 1 && ticket.ExpectedSources[0] == ticket.Articles[0].Title);

        firstIsTheOnlySource.ShouldBeLessThan(tickets.Count / 3);
    }

    [Fact]
    public void A_dataset_under_20_tickets_fails_naming_the_minimum()
    {
        var path = Write("""{ "tickets": [ { "subject": "VPN", "description": "No VPN.", "articles": [ { "title": "VPN", "body": "Reset it." } ], "expectedSources": [ "VPN" ] } ] }""");

        var message = Should.Throw<InvalidOperationException>(() => SupportTicketDataset.Load(path)).Message;

        message.ShouldContain("1 ticket");
        message.ShouldContain("at least 20");
    }

    [Fact]
    public void An_expected_source_that_is_not_one_of_the_tickets_articles_fails_naming_the_ticket_and_the_source()
    {
        var path = Write(Dataset(20, expectedSource: "Printer error 42"));

        var message = Should.Throw<InvalidOperationException>(() => SupportTicketDataset.Load(path)).Message;

        message.ShouldContain("Ticket 1");
        message.ShouldContain("'Printer error 42'");
    }

    [Fact]
    public void A_ticket_without_expected_sources_fails_naming_the_ticket()
    {
        var path = Write(Dataset(20, expectedSource: null));

        Should.Throw<InvalidOperationException>(() => SupportTicketDataset.Load(path)).Message.ShouldContain("expectedSources");
    }

    [Fact]
    public void An_article_holding_the_separator_fails_since_it_would_split_into_two_passages()
    {
        var path = Write(Dataset(20, expectedSource: "VPN", body: "Reset it.\n\n---\n\nThen sign in."));

        Should.Throw<InvalidOperationException>(() => SupportTicketDataset.Load(path)).Message.ShouldContain("---");
    }

    [Fact]
    public void A_ticket_becomes_an_example_whose_input_is_subject_and_description_and_whose_grounding_context_is_every_article()
    {
        var ticket = new SupportTicket(
            "VPN sign-in rejected",
            "The VPN rejects my password.",
            [new KnowledgeArticle("VPN sign-in fails", "Reset the password."), new KnowledgeArticle("Printer error 42", "Restart the spooler.")],
            ["VPN sign-in fails"]);

        var example = SupportTicketDataset.ToExample(ticket);

        example.Input.ShouldBe("VPN sign-in rejected\n\nThe VPN rejects my password.");
        example.GroundingContext.ShouldBe($"VPN sign-in fails\n\nReset the password.{EvalPassages.ArticleSeparator}Printer error 42\n\nRestart the spooler.");
        example.ExpectedSources.ShouldBe(["VPN sign-in fails"]);
    }

    [Fact]
    public async Task The_search_finds_every_article_of_the_example_as_its_own_passage_in_the_datasets_order()
    {
        var ticket = new SupportTicket(
            "VPN sign-in rejected",
            "The VPN rejects my password.",
            [new KnowledgeArticle("VPN sign-in fails", "Reset the password."), new KnowledgeArticle("Printer error 42", "Restart the spooler.")],
            ["VPN sign-in fails"]);

        var passages = await new EvalPassages(SupportTicketDataset.ToExample(ticket)).NearestAsync("VPN", SuggestAnswerHandler.PassageCount, TestContext.Current.CancellationToken);

        passages.Select(passage => passage.Title).ShouldBe(["VPN sign-in fails", "Printer error 42"]);
        passages[1].Text.ShouldBe("Printer error 42\n\nRestart the spooler.");
    }

    [Fact]
    public void The_suggest_answer_use_case_is_answered_on_the_dataset_every_example_grounded_with_expected_sources()
    {
        var tickets = SupportTicketDataset.Load();

        var useCase = UseCases.All.Single(useCase => useCase.Name == "suggest-answer");

        useCase.Examples.ShouldBe(tickets.Select(SupportTicketDataset.ToExample).ToList());
        useCase.Examples.ShouldAllBe(example => example.GroundingContext != null && example.ExpectedSources != null);
    }

    public void Dispose() => directory.Delete(recursive: true);

    private static string Dataset(int count, string? expectedSource, string body = "Reset it.")
    {
        var expected = expectedSource is null ? string.Empty : $", \"expectedSources\": [ \"{expectedSource}\" ]";
        var ticket = $$"""{ "subject": "VPN", "description": "No VPN.", "articles": [ { "title": "VPN", "body": {{System.Text.Json.JsonSerializer.Serialize(body)}} } ]{{expected}} }""";
        return $$"""{ "tickets": [ {{string.Join(", ", Enumerable.Repeat(ticket, count))}} ] }""";
    }

    private string Write(string content)
    {
        var path = Path.Combine(directory.FullName, "support-tickets.json");
        File.WriteAllText(path, content);
        return path;
    }
}
