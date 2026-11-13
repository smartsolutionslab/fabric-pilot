using System.Globalization;
using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Domain.Invoices;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FabricPilot.Modules.Billing.Infrastructure.Pdf;

/// <summary>
/// An A4 page per screenful of lines: the number and dates, a table of the lines (a long description wraps within its
/// column) and the total. Amounts are written with two decimals and a comma between thousands, whatever the host's
/// culture, so the same invoice gives the same text everywhere.
/// </summary>
public sealed class PdfSharpInvoiceRenderer : IInvoicePdfRenderer
{
    private const double Margin = 50;

    private const double LineHeight = 14;

    private const double DescriptionWidth = 260;

    private const double QuantityRight = 360;

    private const double UnitPriceRight = 445;

    private const string FontFamily = "Invoice";

    private static readonly CultureInfo Numbers = CultureInfo.InvariantCulture;

    static PdfSharpInvoiceRenderer()
    {
        // PDFsharp's font resolver is process-wide and may be set only once; this module is the only one that renders PDFs.
        GlobalFontSettings.FontResolver ??= new SegoeWpFontResolver();
    }

    private static XFont Title => Fonts.Title;

    private static XFont Heading => Fonts.Heading;

    private static XFont Body => Fonts.Body;

    public byte[] Render(Invoice invoice)
    {
        Ensure.That(invoice).IsNotNull();

        using var document = new PdfDocument();
        document.Info.Title = $"Invoice {invoice.Number}";
        var page = new InvoicePage(document);
        page.WriteFitted(Title, $"Invoice {invoice.Number}", 24);
        page.Write(Body, $"Issued on {invoice.IssuedOn:yyyy-MM-dd}", LineHeight);
        page.Write(Body, $"Due on {invoice.DueOn:yyyy-MM-dd}", LineHeight * 2);
        page.TableHeading(invoice.Currency);

        foreach (var line in invoice.Lines)
        {
            var description = page.Wrap(line.Description, Body, DescriptionWidth);
            if (!page.Fits(description.Count * LineHeight))
            {
                page = page.Next(document);
                page.TableHeading(invoice.Currency);
            }

            page.Row(
                description,
                line.Quantity.ToString(Numbers),
                line.UnitPrice.ToString("N2", Numbers),
                line.Amount.ToString("N2", Numbers));
        }

        if (!page.Fits(LineHeight * 2))
        {
            page = page.Next(document);
        }

        page.Total($"{invoice.Total.ToString("N2", Numbers)} {invoice.Currency}");
        page.Dispose();

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    /// <summary>Created on first use, after the static constructor has given PDFsharp its font resolver.</summary>
    private static class Fonts
    {
        public static readonly XFont Title = new(FontFamily, 18, XFontStyleEx.Bold);

        public static readonly XFont Heading = new(FontFamily, 10, XFontStyleEx.Bold);

        public static readonly XFont Body = new(FontFamily, 10);
    }

    /// <summary>One page being written top to bottom.</summary>
    private sealed class InvoicePage : IDisposable
    {
        private readonly PdfPage page;

        private readonly XGraphics graphics;

        private double top = Margin;

        public InvoicePage(PdfDocument document)
        {
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            graphics = XGraphics.FromPdfPage(page);
        }

        private double Right => page.Width.Point - Margin;

        private double Bottom => page.Height.Point - Margin;

        public InvoicePage Next(PdfDocument document)
        {
            Dispose();
            return new InvoicePage(document);
        }

        public bool Fits(double height) => top + height <= Bottom;

        public void Write(XFont font, string text, double advance)
        {
            graphics.DrawString(text, font, XBrushes.Black, new XPoint(Margin, top + font.Size));
            top += advance;
        }

        /// <summary>At the font's size if it fits between the margins, or at the smaller size at which it does.</summary>
        public void WriteFitted(XFont font, string text, double advance)
        {
            var width = graphics.MeasureString(text, font).Width;
            var available = Right - Margin;
            Write(width <= available ? font : new XFont(FontFamily, font.Size * available / width, font.Style), text, advance);
        }

        public void TableHeading(string currency)
        {
            Left(Heading, "Description");
            RightAligned(Heading, "Quantity", QuantityRight);
            RightAligned(Heading, "Unit price", UnitPriceRight);
            RightAligned(Heading, $"Amount ({currency})", Right);
            top += LineHeight;
            graphics.DrawLine(XPens.Black, Margin, top, Right, top);
            top += LineHeight / 2;
        }

        /// <summary>The description first, so the text reads description, quantity, unit price, amount.</summary>
        public void Row(IReadOnlyList<string> description, string quantity, string unitPrice, string amount)
        {
            var first = top;
            foreach (var text in description)
            {
                Left(Body, text);
                top += LineHeight;
            }

            var row = top;
            top = first;
            RightAligned(Body, quantity, QuantityRight);
            RightAligned(Body, unitPrice, UnitPriceRight);
            RightAligned(Body, amount, Right);
            top = row + (LineHeight / 2);
        }

        public void Total(string total)
        {
            graphics.DrawLine(XPens.Black, Margin, top, Right, top);
            top += LineHeight / 2;
            Left(Heading, "Total");
            RightAligned(Heading, total, Right);
            top += LineHeight;
        }

        /// <summary>The text in lines no wider than <paramref name="width"/>, broken between words, or inside a word too long for a line.</summary>
        public List<string> Wrap(string text, XFont font, double width)
        {
            List<string> lines = [];
            var current = string.Empty;
            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (graphics.MeasureString(candidate, font).Width <= width)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                }

                current = word;
                while (graphics.MeasureString(current, font).Width > width)
                {
                    var fits = 1;
                    while (graphics.MeasureString(current[..(fits + 1)], font).Width <= width)
                    {
                        fits++;
                    }

                    lines.Add(current[..fits]);
                    current = current[fits..];
                }
            }

            lines.Add(current);
            return lines;
        }

        public void Dispose() => graphics.Dispose();

        private void Left(XFont font, string text) =>
            graphics.DrawString(text, font, XBrushes.Black, new XPoint(Margin, top + font.Size));

        private void RightAligned(XFont font, string text, double right) =>
            graphics.DrawString(text, font, XBrushes.Black, new XPoint(right - graphics.MeasureString(text, font).Width, top + font.Size));
    }
}
