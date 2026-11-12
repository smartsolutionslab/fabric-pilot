using FabricPilot.Modules.Tickets.Application;

namespace FabricPilot.Modules.Tickets.Tests;

/// <summary>Blobs by name, in memory: what was uploaded is what is read back.</summary>
internal sealed class InMemoryBlobStore : IBlobStore
{
    public Dictionary<string, byte[]> Blobs { get; } = new(StringComparer.Ordinal);

    public async Task UploadAsync(string name, Stream content, CancellationToken cancellationToken)
    {
        Ensure.That(content).IsNotNull();

        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        Blobs[name] = copy.ToArray();
    }

    public Task<Option<Stream>> OpenReadAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Blobs.TryGetValue(name, out var content) ? Option<Stream>.Some(new MemoryStream(content, writable: false)) : Option<Stream>.None);
}
