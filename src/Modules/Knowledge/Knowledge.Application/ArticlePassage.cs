using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// A chunk of a published article the shared index found: the article it is from, that article's title, and the chunk's
/// text. The text is untrusted input to a model, whoever wrote the article: it goes into a delimited user message only.
/// </summary>
public sealed record ArticlePassage(ArticleId Article, string Title, string Text);
