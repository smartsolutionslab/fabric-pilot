namespace FabricPilot.Modules.Knowledge.Api.Endpoints;

/// <summary>What staff ask an answer to: a ticket's subject and description.</summary>
public sealed record SuggestionRequest(string Question);
