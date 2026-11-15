using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using FabricPilot.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// An OpenAI-compatible chat model on the loopback interface, so no test calls a real one. A chat request is answered by
/// the <see cref="FakeChatClient"/> a test registered for a word its messages contain, and that client keeps what it was
/// asked; a chat request no test answers is a 404, as a model the host cannot reach. Every other request, embeddings
/// included, is passed on unchanged to the generated <see cref="FakeEmbeddingsServer"/>, since the chat model and the
/// embeddings share the host's <c>azure-openai-endpoint</c>. That endpoint ends in <c>/v1</c> and is therefore used as is.
/// </summary>
public sealed class FakeModelServer : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly HttpClient embeddings;
    private readonly CancellationTokenSource stopping = new();
    private readonly ConcurrentDictionary<string, FakeChatClient> chats = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task serving;

    /// <param name="embeddingsEndpoint">The <see cref="FakeEmbeddingsServer.Endpoint"/> every request but a chat goes on to.</param>
    public FakeModelServer(string embeddingsEndpoint)
    {
        embeddings = new HttpClient { BaseAddress = new Uri(embeddingsEndpoint) };
        var port = FreePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        Endpoint = $"http://127.0.0.1:{port}/v1";
        serving = Task.Run(ServeAsync);
    }

    /// <summary>The value for the AppHost's parameter <c>azure-openai-endpoint</c>.</summary>
    public string Endpoint { get; }

    /// <summary>
    /// Answers every chat request whose messages contain the word with the client, until the returned registration is
    /// disposed. A word no other test uses keeps tests that run at the same time apart.
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
        embeddings.Dispose();
        stopping.Dispose();
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
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

    /// <summary>A message's text: its content as a string, or the text of its content parts.</summary>
    private static string Text(JsonNode? content) =>
        content switch
        {
            JsonValue text => text.GetValue<string>(),
            JsonArray parts => string.Concat(parts.Select(part => part?["text"]?.GetValue<string>() ?? string.Empty)),
            _ => string.Empty,
        };

    private static ChatRole Role(string role) =>
        role switch
        {
            "system" or "developer" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User,
        };

    private async Task AnswerAsync(HttpListenerContext context)
    {
        using var response = context.Response;
        if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath.TrimEnd('/') == "/v1/chat/completions")
        {
            await ChatAsync(context.Request, response);
            return;
        }

        await PassOnAsync(context.Request, response);
    }

    /// <summary>Sends the request on to the embeddings stand-in, both endpoints ending in <c>/v1</c>, and copies its answer back.</summary>
    private async Task PassOnAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var passedOn = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Url?.PathAndQuery);
        if (request.HasEntityBody)
        {
            passedOn.Content = new StreamContent(request.InputStream);
            if (request.ContentType is not null)
            {
                passedOn.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
            }
        }

        using var answer = await embeddings.SendAsync(passedOn);
        response.StatusCode = (int)answer.StatusCode;
        response.ContentType = answer.Content.Headers.ContentType?.ToString();
        await answer.Content.CopyToAsync(response.OutputStream);
    }

    private async Task ChatAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var body = await JsonNode.ParseAsync(request.InputStream) ?? new JsonObject();
        List<ChatMessage> messages = [.. (body["messages"] as JsonArray ?? []).Select(message =>
            new ChatMessage(Role(message?["role"]?.GetValue<string>() ?? "user"), Text(message?["content"])))];
        var model = chats.FirstOrDefault(chat => messages.Any(message => message.Text.Contains(chat.Key, StringComparison.OrdinalIgnoreCase))).Value;
        if (model is null)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var answer = await model.GetResponseAsync(messages);
        var completion = new JsonObject
        {
            ["id"] = "chatcmpl-fake",
            ["object"] = "chat.completion",
            ["created"] = 0,
            ["model"] = body["model"]?.GetValue<string>() ?? "fake-model",
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = answer.Text },
                ["finish_reason"] = "stop",
            }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0 },
        };

        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(response.OutputStream, completion);
    }

    /// <summary>A registration that ends when it is disposed.</summary>
    private sealed class Registration(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
