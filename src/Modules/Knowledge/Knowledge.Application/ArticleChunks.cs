using System.Globalization;
using System.Text.RegularExpressions;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// How an article is cut for retrieval: its paragraphs packed into chunks of at most <see cref="MaximumLength"/>
/// characters, each led by the article's title, so a passage a search finds says which article it is from.
/// </summary>
public static partial class ArticleChunks
{
    public const int MaximumLength = 1000;

    /// <summary>
    /// The most chunks the longest article can have: two neighbouring chunks hold more than <see cref="MaximumLength"/>
    /// minus a paragraph break between them, or they would have been packed into one.
    /// </summary>
    public const int MaximumCount = (2 * ((Article.MaximumBodyLength / (MaximumLength - 2)) + 1)) + 1;

    private const string ParagraphBreak = "\n\n";

    private static readonly char[] WhiteSpace = [' ', '\t', '\n'];

    /// <summary>The chunk's key in the index: <c>knowledge/articles/&lt;article&gt;/&lt;position&gt;</c>, counted from 0.</summary>
    public static string Key(ArticleId article, int position)
    {
        Ensure.That(article).IsNotNull();

        return string.Create(CultureInfo.InvariantCulture, $"knowledge/articles/{article.Value}/{position}");
    }

    /// <summary>The article a chunk's key names, or none for a key that is not a chunk's.</summary>
    public static Option<ArticleId> ArticleOf(string key)
    {
        Ensure.That(key).IsNotNull();

        var parts = key.Split('/');
        if (parts is not ["knowledge", "articles", var article, var position]
            || !Guid.TryParse(article, CultureInfo.InvariantCulture, out var id)
            || !int.TryParse(position, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return Option<ArticleId>.None;
        }

        return Option<ArticleId>.Some(new ArticleId(id));
    }

    public static IReadOnlyList<string> Of(string title, string body)
    {
        Ensure.That(title).IsNotNull();
        Ensure.That(body).IsNotNull();

        List<string> chunks = [];
        var current = string.Empty;
        foreach (var piece in Paragraphs(body).SelectMany(Pieces))
        {
            if (current.Length > 0 && current.Length + ParagraphBreak.Length + piece.Length <= MaximumLength)
            {
                current += ParagraphBreak + piece;
                continue;
            }

            if (current.Length > 0)
            {
                chunks.Add(current);
            }

            current = piece;
        }

        if (current.Length > 0)
        {
            chunks.Add(current);
        }

        return [.. chunks.Select(chunk => title + ParagraphBreak + chunk)];
    }

    private static IEnumerable<string> Paragraphs(string body) =>
        BlankLines().Split(body.ReplaceLineEndings("\n"))
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0);

    /// <summary>A paragraph as it is, or, longer than the maximum, cut at the last white space before it (or at it, in a word that long).</summary>
    private static IEnumerable<string> Pieces(string paragraph)
    {
        var rest = paragraph;
        while (rest.Length > MaximumLength)
        {
            var cut = rest.LastIndexOfAny(WhiteSpace, MaximumLength);
            if (cut <= 0)
            {
                yield return rest[..MaximumLength];
                rest = rest[MaximumLength..];
                continue;
            }

            yield return rest[..cut];
            rest = rest[(cut + 1)..];
        }

        yield return rest;
    }

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLines();
}
