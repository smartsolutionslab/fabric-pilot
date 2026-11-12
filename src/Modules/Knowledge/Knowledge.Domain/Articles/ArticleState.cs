namespace FabricPilot.Modules.Knowledge.Domain.Articles;

/// <summary>Only staff see a draft; every tenant's users read a published article.</summary>
public enum ArticleState
{
    Draft,
    Published,
}
