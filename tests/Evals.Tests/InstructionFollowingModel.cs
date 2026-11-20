using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using Microsoft.Extensions.AI;

namespace FabricPilot.Evals.Tests;

/// <summary>
/// A stand-in model that does what its instruction says, as a capable model would. Given the suggested answers' own
/// instruction it drafts from the expected sources of the ticket it is asked about, or answers nothing when there are
/// none; given any other instruction for a ticket, it drafts from passage 1 whatever the ticket. Anything else gets a
/// plain sentence. No test calls a real model.
/// </summary>
public sealed partial class InstructionFollowingModel(IReadOnlyDictionary<string, IReadOnlyList<string>> expectedSources) : IChatClient
{
    private const string Draft = "Please follow the steps in the article.";

    private static readonly int[] FirstPassage = [1];

    private readonly List<IReadOnlyList<ChatMessage>> requests = [];

    /// <summary>The messages of every request, in the order they came.</summary>
    public IReadOnlyList<IReadOnlyList<ChatMessage>> Requests => requests;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> request = [.. messages];
        requests.Add(request);

        var user = request.Last(message => message.Role == ChatRole.User).Text;
        var ticket = Ticket().Match(user);
        if (!ticket.Success)
        {
            return Answer("An invoice records what was sold, to whom and for how much.");
        }

        var system = request.FirstOrDefault(message => message.Role == ChatRole.System)?.Text;
        if (system != SuggestionPrompt.Instruction)
        {
            return Answer(JsonSerializer.Serialize(new { answered = true, answer = Draft, sources = FirstPassage }));
        }

        var titles = Passage().Matches(user).ToDictionary(match => match.Groups["title"].Value, match => int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture));
        var expected = expectedSources[ticket.Groups["ticket"].Value];
        var sources = expected.Select(title => titles[title]).ToArray();
        return Answer(JsonSerializer.Serialize(new { answered = sources.Length > 0, answer = sources.Length > 0 ? Draft : string.Empty, sources }));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        Ensure.That(serviceType).IsNotNull();

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    private static ChatClientMetadata Metadata { get; } = new("fake", defaultModelId: "fake-model");

    private static Task<ChatResponse> Answer(string text) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { ModelId = Metadata.DefaultModelId });

    [GeneratedRegex(@"<ticket>\n(?<ticket>.*?)\n</ticket>", RegexOptions.Singleline)]
    private static partial Regex Ticket();

    [GeneratedRegex("<passage number=\"(?<number>[0-9]+)\">\n(?<title>[^\n]*)")]
    private static partial Regex Passage();
}
