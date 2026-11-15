using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Tests.Fakes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// An OpenAI-compatible embeddings endpoint on the loopback interface, answering <c>POST /v1/embeddings</c> with
/// <see cref="FakeEmbeddingGenerator"/>'s vectors, so the host can index and search while the integration tests run,
/// and no test calls a real model or needs a key. The fixture gives it to the host as its embeddings endpoint, which
/// ends in <c>/v1</c>. The chat model shares that endpoint: a chat request, <c>POST /v1/chat/completions</c>, is
/// answered by the <see cref="FakeChatClient"/> a test gave <see cref="Answer"/> for a word its messages contain, plainly
/// or streamed. A chat request no test answers, like anything else, is a 404, as a model the host cannot reach.
/// </summary>
public sealed class FakeEmbeddingsServer : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly FakeEmbeddingGenerator generator = new(VectorTables.Dimensions);
    private readonly CancellationTokenSource stopping = new();
    private readonly ConcurrentDictionary<string, HeldEmbeddings> held = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, FakeChatClient> chats = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task serving;

    public FakeEmbeddingsServer()
    {
        var port = FreePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        Endpoint = $"http://127.0.0.1:{port}/v1";
        serving = Task.Run(ServeAsync);
    }

    /// <summary>The address the host calls for embeddings.</summary>
    public string Endpoint { get; }

    /// <summary>
    /// Holds back the answer to every request whose text contains the word until released, as a slow provider would, so a
    /// test can act while the host waits in the middle of indexing.
    /// </summary>
    public HeldEmbeddings Hold(string word)
    {
        var holding = new HeldEmbeddings();
        held[word] = holding;
        return holding;
    }

    /// <summary>
    /// Answers every chat request whose messages contain the word with the client, which keeps what it was asked, until
    /// the returned registration is disposed. A word no other test uses keeps tests that run at the same time apart.
    /// </summary>
    public IDisposable Answer(string word, FakeChatClient model)
    {
        chats[word] = model;
        return new Registration(() => chats.TryRemove(word, out _));
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        listener.Stop();
        await serving;
        listener.Close();
        generator.Dispose();
        stopping.Dispose();
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    private static IReadOnlyList<string> Inputs(JsonNode request) =>
        request["input"] switch
        {
            JsonArray inputs => [.. inputs.Select(input => input!.GetValue<string>())],
            JsonValue input => [input.GetValue<string>()],
            _ => [],
        };

    private static JsonNode Encoded(ReadOnlyMemory<float> vector, bool base64)
    {
        if (!base64)
        {
            return Vector(vector);
        }

        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector.ToArray(), 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>A message's text: its content as a string, or the text of its content parts.</summary>
    private static string Text(JsonNode? content) =>
        content switch
        {
            JsonValue text => text.GetValue<string>(),
            JsonArray parts => string.Concat(parts.Select(part => part?["text"]?.GetValue<string>() ?? string.Empty)),
            _ => string.Empty,
        };

    private static ChatRole Role(string? role) =>
        role switch
        {
            "system" or "developer" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User,
        };

    private static JsonObject Completion(string kind, string model, JsonObject choice) =>
        new()
        {
            ["id"] = "chatcmpl-fake",
            ["object"] = kind,
            ["created"] = 0,
            ["model"] = model,
            ["choices"] = new JsonArray(choice),
        };

    private static JsonArray Vector(ReadOnlyMemory<float> vector) =>
        [.. vector.ToArray().Select(value => (JsonNode)value)];

    private async Task ServeAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception stopped) when (stopping.IsCancellationRequested && stopped is HttpListenerException or ObjectDisposedException)
            {
                // Stopping the listener ends the request it was waiting for: that is how serving ends.
                return;
            }

            _ = Task.Run(() => AnswerAsync(context));
        }
    }

    private async Task AnswerAsync(HttpListenerContext context)
    {
        using var response = context.Response;
        if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath.TrimEnd('/') == "/v1/chat/completions")
        {
            await ChatAsync(context.Request, response);
            return;
        }

        if (context.Request.HttpMethod != "POST" || context.Request.Url?.AbsolutePath.TrimEnd('/') != "/v1/embeddings")
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var request = await JsonNode.ParseAsync(context.Request.InputStream) ?? new JsonObject();
        var inputs = Inputs(request);
        var base64 = request["encoding_format"]?.GetValue<string>() == "base64";
        foreach (var (word, holding) in held)
        {
            if (inputs.Any(input => input.Contains(word, StringComparison.OrdinalIgnoreCase)))
            {
                await holding.WaitAsync();
            }
        }

        var embeddings = await generator.GenerateAsync(inputs);
        var answer = new JsonObject
        {
            ["object"] = "list",
            ["data"] = new JsonArray([.. embeddings.Select((embedding, index) => (JsonNode)new JsonObject
            {
                ["object"] = "embedding",
                ["index"] = index,
                ["embedding"] = Encoded(embedding.Vector, base64),
            })]),
            ["model"] = request["model"]?.GetValue<string>() ?? "fake-embeddings",
            ["usage"] = new JsonObject { ["prompt_tokens"] = 0, ["total_tokens"] = 0 },
        };

        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(response.OutputStream, answer);
    }

    private async Task ChatAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var body = await JsonNode.ParseAsync(request.InputStream) ?? new JsonObject();
        List<ChatMessage> messages = [.. (body["messages"] as JsonArray ?? []).Select(message =>
            new ChatMessage(Role(message?["role"]?.GetValue<string>()), Text(message?["content"])))];
        var model = chats.FirstOrDefault(chat => messages.Any(message => message.Text.Contains(chat.Key, StringComparison.OrdinalIgnoreCase))).Value;
        if (model is null)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var answer = (await model.GetResponseAsync(messages)).Text;
        var name = body["model"]?.GetValue<string>() ?? "fake-model";
        response.StatusCode = (int)HttpStatusCode.OK;
        if (body["stream"]?.GetValue<bool>() != true)
        {
            response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(response.OutputStream, Completion("chat.completion", name, new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = answer },
                ["finish_reason"] = "stop",
            }));
            return;
        }

        // Streamed, the answer comes as server-sent events: one chunk with the text, one that finishes, then [DONE].
        response.ContentType = "text/event-stream";
        JsonObject[] chunks =
        [
            Completion("chat.completion.chunk", name, new JsonObject
            {
                ["index"] = 0,
                ["delta"] = new JsonObject { ["role"] = "assistant", ["content"] = answer },
                ["finish_reason"] = null,
            }),
            Completion("chat.completion.chunk", name, new JsonObject
            {
                ["index"] = 0,
                ["delta"] = new JsonObject(),
                ["finish_reason"] = "stop",
            }),
        ];
        var events = string.Concat(chunks.Select(chunk => $"data: {chunk.ToJsonString()}\n\n")) + "data: [DONE]\n\n";
        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(events));
    }

    /// <summary>An answer held back: <see cref="Arrived"/> completes once a request for it came in, and disposing releases it.</summary>
    public sealed class HeldEmbeddings : IDisposable
    {
        private readonly TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int waiting;

        public Task Arrived => arrived.Task;

        /// <summary>How many requests are being held back now.</summary>
        public int Waiting => Volatile.Read(ref waiting);

        public void Dispose() => released.TrySetResult();

        internal async Task WaitAsync()
        {
            Interlocked.Increment(ref waiting);
            arrived.TrySetResult();
            await released.Task;
            Interlocked.Decrement(ref waiting);
        }
    }

    /// <summary>A registration that ends when it is disposed.</summary>
    private sealed class Registration(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
