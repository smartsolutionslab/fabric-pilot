namespace FabricPilot.Modules.Knowledge.Domain.Articles;

public sealed record InvalidTitle(int Maximum)
    : DomainError("knowledge.invalid-title", $"An article needs a title of 1 to {Maximum} characters.");
