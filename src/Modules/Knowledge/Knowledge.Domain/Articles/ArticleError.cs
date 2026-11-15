namespace FabricPilot.Modules.Knowledge.Domain.Articles;

/// <summary>The ways writing, editing or publishing an article fails: a new one fails the build until the Application maps it.</summary>
public union ArticleError(InvalidTitle, InvalidBody, ArticleAlreadyPublished);
