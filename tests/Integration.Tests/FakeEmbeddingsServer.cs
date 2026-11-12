using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Tests.Fakes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// An OpenAI-compatible embeddings endpoint on the loopback interface, answering with <see cref="FakeEmbeddingGenerator"/>'s
/// vectors, so the host can index and search while the integration tests run, and no test calls a real model. The host
/// reaches it as its <c>azure-openai-endpoint</c>, which ends in <c>/v1</c> and is therefore used as is. Anything but
/// <c>POST /v1/embeddings</c>, a chat request included, is a 404: the chat model stays unreachable, as before.
/// </summary>
public sealed class FakeEmbeddingsServer : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly FakeEmbeddingGenerator generator = new(VectorTables.Dimensions);
    private readonly CancellationTokenSource stopping = new();
    private readonly ConcurrentDictionary<string, HeldEmbeddings> held = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task serving;

    public FakeEmbeddingsServer()
    {
        var port = FreePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        Endpoint = $"http://127.0.0.1:{port}/v1";
        serving = Task.Run(ServeAsync);
    }

    /// <summary>The value for the AppHost's parameter <c>azure-openai-endpoint</c>.</summary>
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
            return new JsonArray([.. vector.ToArray().Select(value => (JsonNode)value)]);
        }

        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector.ToArray(), 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

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
}
