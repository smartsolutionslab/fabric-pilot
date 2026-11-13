using PdfSharp.Fonts;
using PdfSharp.WPFonts;

namespace FabricPilot.Modules.Billing.Infrastructure.Pdf;

/// <summary>
/// PDFsharp's Core build reads no system fonts on Linux, and the host's container has none to read. Every family
/// resolves to the Segoe WP faces that ship inside the PDFsharp package itself, so the PDF looks the same wherever the
/// host runs and the fonts are embedded in it.
/// </summary>
public sealed class SegoeWpFontResolver : IFontResolver
{
    private const string Regular = "segoe-wp";

    private const string Bold = "segoe-wp-bold";

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? Bold : Regular);

    public byte[]? GetFont(string faceName) =>
        faceName == Bold ? FontDataHelper.SegoeWPBold : FontDataHelper.SegoeWP;
}
