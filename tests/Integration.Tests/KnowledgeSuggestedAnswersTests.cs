using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FabricPilot.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// On a ticket, staff ask for an answer drafted from the shared knowledge base: the model is given the nearest
/// published passages, and the draft links each article it used, or says that none answers. The draft is only shown to
/// staff, who edit it; nothing is sent. The model is the fixture's stand-in, answered by a <see cref="FakeChatClient"/>.
/// </summary>
public class KnowledgeSuggestedAnswersTests(AspireFixture aspire)
{
    private const string Articles = "/knowledge/articles";

    private static readonly Uri SuggestedAnswers = new("/knowledge/suggested-answers", UriKind.Relative);

    [Fact]
    public async Task Staff_get_an_answer_drafted_from_the_knowledge_base_that_links_the_article_it_used()
    {
        var word = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAndPublishAsync(staff, $"Printer {word}", $"Restart the spooler when {word} appears.");
        using var model = new FakeChatClient("""{"answered":true,"answer":"Please restart the print spooler.","sources":[1]}""");
        using var answering = aspire.Model.Answer(word, model);

        var suggestion = await SuggestAsync(staff, $"Printer {word} Restart the spooler when {word} appears.");

        suggestion["drafted"]!.GetValue<bool>().ShouldBeTrue();
        suggestion["text"]!.GetValue<string>().ShouldBe("Please restart the print spooler.");
        var source = suggestion["sources"]!.AsArray().ShouldHaveSingleItem()!;
        source["articleId"]!.GetValue<Guid>().ShouldBe(id);
        source["title"]!.GetValue<string>().ShouldBe($"Printer {word}");
        var request = model.Requests.ShouldHaveSingleItem();
        var instructions = request.Where(message => message.Role == ChatRole.System).ShouldHaveSingleItem();
        instructions.Text.ShouldNotContain(word);
        var data = request.Where(message => message.Role == ChatRole.User).ShouldHaveSingleItem();
        data.Text.ShouldContain($"Printer {word}\n\nRestart the spooler when {word} appears.");
        data.Text.ShouldContain("<passage number=\"1\">");
    }

    [Fact]
    public async Task When_no_article_answers_the_ticket_the_suggestion_says_so_and_links_nothing()
    {
        var word = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        await WriteAndPublishAsync(staff, $"Vpn {word}", $"Install the client and sign in with {word}.");
        using var model = new FakeChatClient("""{"answered":false,"answer":"","sources":[]}""");
        using var answering = aspire.Model.Answer(word, model);

        var suggestion = await SuggestAsync(staff, $"Our invoices show the wrong address {word}.");

        suggestion["drafted"]!.GetValue<bool>().ShouldBeFalse();
        suggestion["text"]!.GetValue<string>().ShouldContain("No article in the knowledge base answers this ticket");
        suggestion["sources"]!.AsArray().ShouldBeEmpty();
        model.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_blank_question_is_refused_with_its_code()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(SuggestedAnswers, new { question = " " }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("knowledge.invalid-question");
    }

    /// <summary>developer and developer-b are customers of tenant-a and tenant-b; visitor is in tenant-a without a role.</summary>
    [Theory]
    [InlineData("developer")]
    [InlineData("developer-b")]
    [InlineData("visitor")]
    public async Task A_tenants_user_cannot_ask_for_a_suggested_answer(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.PostAsJsonAsync(SuggestedAnswers, new { question = "How do I reset my password?" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.PostAsJsonAsync(SuggestedAnswers, new { question = "How do I reset my password?" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A word no other test or run uses, so the model's stand-in answers only this test's request.</summary>
    private static string UniqueWord() => $"w{Guid.CreateVersion7():N}";

    private static async Task<Guid> WriteAndPublishAsync(HttpClient staff, string title, string body)
    {
        using var written = await staff.PostAsJsonAsync(new Uri(Articles, UriKind.Relative), new { title, body }, TestContext.Current.CancellationToken);
        written.StatusCode.ShouldBe(HttpStatusCode.Created, await written.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var id = await written.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        using var published = await staff.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);
        published.StatusCode.ShouldBe(HttpStatusCode.NoContent, await published.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return id;
    }

    private static async Task<JsonNode> SuggestAsync(HttpClient staff, string question)
    {
        using var response = await staff.PostAsJsonAsync(SuggestedAnswers, new { question }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
