namespace FabricPilot.Modules.Knowledge.Domain.Articles;

public sealed record InvalidBody(int Maximum)
    : DomainError("knowledge.invalid-body", $"An article needs a text of 1 to {Maximum} characters.");
