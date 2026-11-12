using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Knowledge.Infrastructure;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;
using FabricPilot.Tests.Fakes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// One knowledge base, shared by every tenant and written by the operator's staff: staff write, edit and publish
/// articles; every tenant's users search and read the published ones and never a draft; nobody else writes.
/// </summary>
public class KnowledgeArticlesTests(AspireFixture aspire)
{
    private const string Articles = "/knowledge/articles";

    private const string ManagedArticles = "/knowledge/articles/manage";

    /// <summary>An article id for the anonymous calls, which are refused before anything looks it up.</summary>
    private const string AnyArticle = "0193c0de-0000-7000-8000-000000000011";

    [Fact]
    public async Task A_draft_staff_write_is_listed_for_staff_and_invisible_to_every_tenants_users()
    {
        var word = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");

        var id = await WriteAsync(staff, $"Draft {word}", $"Not ready {word}.");

        var managed = await JsonAsync(staff, $"{ManagedArticles}?page=1&pageSize=100");
        managed["items"]!.AsArray().Single(item => item!["id"]!.GetValue<Guid>() == id)!["state"]!.GetValue<string>().ShouldBe("Draft");
        (await JsonAsync(staff, $"{ManagedArticles}/{id}"))["body"]!.GetValue<string>().ShouldBe($"Not ready {word}.");
        foreach (var customer in new[] { "visitor", "developer-b" })
        {
            using var client = await aspire.CreateHostClientAsync(customer);
            (await SearchAsync(client, word)).ShouldBeEmpty();
            using var read = await client.GetAsync(new Uri($"{Articles}/{id}", UriKind.Relative), TestContext.Current.CancellationToken);
            read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task A_published_article_is_found_by_its_title_and_its_text_and_read_by_every_tenants_users()
    {
        var titleWord = UniqueWord();
        var textWord = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Printer {titleWord}", $"Restart the printer, then {textWord}.");

        await PublishAsync(staff, id);

        foreach (var customer in new[] { "visitor", "developer-b", "staff" })
        {
            using var client = await aspire.CreateHostClientAsync(customer);
            (await SearchAsync(client, titleWord.ToUpperInvariant())).ShouldBe([id]);
            (await SearchAsync(client, textWord)).ShouldBe([id]);
            var article = await JsonAsync(client, $"{Articles}/{id}");
            article["title"]!.GetValue<string>().ShouldBe($"Printer {titleWord}");
            article["body"]!.GetValue<string>().ShouldBe($"Restart the printer, then {textWord}.");
            article["state"]!.GetValue<string>().ShouldBe("Published");
        }
    }

    [Fact]
    public async Task Publishing_chunks_and_embeds_the_article_into_the_shared_index_every_tenant_searches()
    {
        var word = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Vpn {word}", $"Install the client and sign in with {word}.");

        await PublishAsync(staff, id);

        foreach (var tenant in new[] { "tenant-a", "tenant-b" })
        {
            var passages = await SharedPassagesAsync(tenant, $"Vpn {word} Install the client and sign in with {word}.");
            var passage = passages.ShouldHaveSingleItem();
            passage.Key.ShouldBe($"knowledge/articles/{id}/0");
            passage.Text.ShouldBe($"Vpn {word}\n\nInstall the client and sign in with {word}.");
        }
    }

    [Fact]
    public async Task Editing_a_published_article_shows_and_indexes_the_new_text()
    {
        var before = UniqueWord();
        var after = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Mail {before}", $"Old text {before}.");
        await PublishAsync(staff, id);

        using var edited = await staff.PutAsJsonAsync(new Uri($"{Articles}/{id}", UriKind.Relative), new { title = $"Mail {after}", body = $"New text {after}." }, TestContext.Current.CancellationToken);

        edited.StatusCode.ShouldBe(HttpStatusCode.NoContent, await edited.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var customer = await aspire.CreateHostClientAsync("visitor");
        (await SearchAsync(customer, after)).ShouldBe([id]);
        (await SearchAsync(customer, before)).ShouldBeEmpty();
        var passage = (await SharedPassagesAsync("tenant-a", $"Mail {after} New text {after}.")).ShouldHaveSingleItem();
        passage.Text.ShouldBe($"Mail {after}\n\nNew text {after}.");
    }

    [Fact]
    public async Task Publishing_twice_is_a_conflict()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Twice {UniqueWord()}", "Text.");
        await PublishAsync(staff, id);

        using var again = await staff.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await again.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("knowledge.article-already-published");
    }

    [Fact]
    public async Task An_article_without_a_title_is_refused_with_its_code()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(new Uri(Articles, UriKind.Relative), new { title = " ", body = "Text." }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("knowledge.invalid-title");
    }

    [Theory]
    [InlineData("visitor")]
    [InlineData("developer")]
    [InlineData("developer-b")]
    public async Task A_tenants_user_writes_edits_publishes_and_manages_nothing(string user)
    {
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Staff only {UniqueWord()}", "Text.");
        using var client = await aspire.CreateHostClientAsync(user);

        using var write = await client.PostAsJsonAsync(new Uri(Articles, UriKind.Relative), new { title = "Mine", body = "Text." }, TestContext.Current.CancellationToken);
        using var edit = await client.PutAsJsonAsync(new Uri($"{Articles}/{id}", UriKind.Relative), new { title = "Mine", body = "Text." }, TestContext.Current.CancellationToken);
        using var publish = await client.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);
        using var list = await client.GetAsync(new Uri(ManagedArticles, UriKind.Relative), TestContext.Current.CancellationToken);
        using var read = await client.GetAsync(new Uri($"{ManagedArticles}/{id}", UriKind.Relative), TestContext.Current.CancellationToken);

        new[] { write.StatusCode, edit.StatusCode, publish.StatusCode, list.StatusCode, read.StatusCode }.ShouldAllBe(status => status == HttpStatusCode.Forbidden);
        (await JsonAsync(staff, $"{ManagedArticles}/{id}"))["state"]!.GetValue<string>().ShouldBe("Draft");
    }

    [Fact]
    public async Task A_change_to_an_article_a_publish_is_still_indexing_answers_409_at_once()
    {
        var word = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var id = await WriteAsync(staff, $"Busy {word}", $"Text {word}.");
        using var slowEmbeddings = aspire.Embeddings.Hold(word);
        var publishing = PublishAsync(staff, id);
        await slowEmbeddings.Arrived.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        using var edit = await staff.PutAsJsonAsync(new Uri($"{Articles}/{id}", UriKind.Relative), new { title = $"Busy {word}", body = "Changed meanwhile." }, TestContext.Current.CancellationToken);
        using var publishAgain = await staff.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);
        slowEmbeddings.Dispose();
        await publishing;

        edit.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await edit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("knowledge.article-being-changed");
        publishAgain.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await JsonAsync(staff, $"{ManagedArticles}/{id}"))["body"]!.GetValue<string>().ShouldBe($"Text {word}.");
    }

    [Fact]
    public async Task While_the_lock_pool_is_full_another_article_answers_409_the_shared_index_stays_searchable_and_the_held_changes_complete()
    {
        var word = UniqueWord();
        var anchorWord = UniqueWord();
        using var staff = await aspire.CreateHostClientAsync("staff");
        var anchor = await WriteAsync(staff, $"Anchor {anchorWord}", $"Searchable {anchorWord}.");
        await PublishAsync(staff, anchor);
        List<Guid> articles = [];
        for (var position = 0; position <= ArticleLocks.Capacity; position++)
        {
            articles.Add(await WriteAsync(staff, $"Held {word} {position}", $"Text {word} {position}."));
        }

        using var slowEmbeddings = aspire.Embeddings.Hold(word);
        List<Task<HttpResponseMessage>> held = [.. articles.Take(ArticleLocks.Capacity).Select(id => staff.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken))];
        using (var allHeld = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            while (slowEmbeddings.Waiting < ArticleLocks.Capacity)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), allHeld.Token);
            }
        }

        var extra = staff.PostAsync(new Uri($"{Articles}/{articles[^1]}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);
        await Task.WhenAny(extra, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        var searchedMeanwhile = await SharedPassagesAsync("tenant-a", $"Anchor {anchorWord} Searchable {anchorWord}.");
        slowEmbeddings.Dispose();
        using var extraResponse = await extra;
        var heldResponses = await Task.WhenAll(held);

        extraResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await extraResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("knowledge.article-being-changed");
        searchedMeanwhile.ShouldHaveSingleItem().Key.ShouldBe($"knowledge/articles/{anchor}/0");
        heldResponses.Select(response => response.StatusCode).ShouldAllBe(status => status == HttpStatusCode.NoContent);
        foreach (var response in heldResponses)
        {
            response.Dispose();
        }

        foreach (var (position, id) in articles.Take(ArticleLocks.Capacity).Index())
        {
            (await SharedPassagesAsync("tenant-b", $"Held {word} {position} Text {word} {position}.")).ShouldHaveSingleItem().Key.ShouldBe($"knowledge/articles/{id}/0");
        }
    }

    [Theory]
    [InlineData("GET", Articles)]
    [InlineData("POST", Articles)]
    [InlineData("GET", ManagedArticles)]
    [InlineData("PUT", $"{Articles}/{AnyArticle}")]
    [InlineData("POST", $"{Articles}/{AnyArticle}/publish")]
    [InlineData("GET", $"{ManagedArticles}/{AnyArticle}")]
    public async Task An_anonymous_caller_is_unauthorized(string method, string path)
    {
        using var client = aspire.CreateHostClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A word no other test or run uses, so a search finds only this test's articles.</summary>
    private static string UniqueWord() => $"w{Guid.CreateVersion7():N}";

    private static async Task<Guid> WriteAsync(HttpClient staff, string title, string body)
    {
        using var response = await staff.PostAsJsonAsync(new Uri(Articles, UriKind.Relative), new { title, body }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
    }

    private static async Task PublishAsync(HttpClient staff, Guid id)
    {
        using var response = await staff.PostAsync(new Uri($"{Articles}/{id}/publish", UriKind.Relative), null, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<IReadOnlyList<Guid>> SearchAsync(HttpClient client, string term)
    {
        var page = await JsonAsync(client, $"{Articles}?search={Uri.EscapeDataString(term)}&page=1&pageSize=100");
        return [.. page["items"]!.AsArray().Select(item => item!["id"]!.GetValue<Guid>())];
    }

    private static async Task<JsonNode> JsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }

    /// <summary>
    /// The shared index as a use case of a tenant searches it (the drafted answers of #12 will): with the same fake
    /// embeddings the host was given, the nearest passage to an article's own text is that article's.
    /// </summary>
    private async Task<IReadOnlyList<RetrievedPassage>> SharedPassagesAsync(string tenant, string query)
    {
        var connectionString = await aspire.App.GetConnectionStringAsync("database", TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The AppHost's database has no connection string.");
        await using var dataSource = VectorTables.DataSource(connectionString);
        using var generator = new FakeEmbeddingGenerator(VectorTables.Dimensions);
        var index = new SharedVectorIndex<KnowledgeDbContext>(dataSource, generator, new OneTenant(tenant));
        var passages = await index.SearchAsync(query, 1, TestContext.Current.CancellationToken);
        return [.. passages.Where(passage => passage.Distance < 0.0001)];
    }

    private sealed class OneTenant(string tenantId) : ITenantContext
    {
        public string? TenantId => tenantId;
    }
}
