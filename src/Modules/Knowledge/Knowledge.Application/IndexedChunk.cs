namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>A chunk the shared index holds: its key, which names its article, and the text indexed under it.</summary>
public sealed record IndexedChunk(string Key, string Text);
