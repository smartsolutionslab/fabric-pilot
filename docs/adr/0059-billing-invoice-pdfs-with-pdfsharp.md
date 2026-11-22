# Billing: invoice PDFs with PDFsharp, rendered on request

**Status:** Accepted. It records what the pilot built by hand for charter feature 6 (#10, PR #40; #41, PR #47), as the owner's decision X2 on software-fabric#602 (2026-11-21) asks (#90). A pilot decision, not generated. PDF generation is not a catalog dimension (the owner's decision P1 on software-fabric#602), so no software-fabric ADR covers it; this is the record.

## Context

Customers see their invoices and download each as a PDF (charter feature 6, #10). The charter lists PDF generation under "Hand-written here, not generated". #10's done-when: the PDF holds the invoice number, the lines and the total, and a test reads its text back. The owner decided on #10 (2026-11-12): "PDFs are made with PDFsharp (MIT), behind one interface in the documents or billing module's Infrastructure." Why PDFsharp over other libraries is not recorded beyond its licence being named.

## What was built

- **The interface**, `src/Modules/Billing/Billing.Application/IInvoicePdfRenderer.cs`: `byte[] Render(Invoice invoice)`, "the one place that names the PDF library".
- **The renderer**, `src/Modules/Billing/Billing.Infrastructure/Pdf/PdfSharpInvoiceRenderer.cs`, registered as a singleton in `.../Wiring/BillingStorage.cs`. An A4 page with the number, the issue and due dates, a table of the lines (a long description wraps within its column, breaking inside a word too long for a line; rows move onto further pages with the table heading repeated) and the total. A heading wider than the page is drawn at the smaller size at which it fits. Amounts are `N2` and dates `yyyy-MM-dd`, both in `CultureInfo.InvariantCulture`. Each row draws its description first, so the extracted text reads description, quantity, unit price, amount.
- **The fonts**, `.../Pdf/SegoeWpFontResolver.cs`: every family resolves to the Segoe WP regular and bold faces that ship inside the PDFsharp package (`PdfSharp.WPFonts.FontDataHelper`). The renderer's static constructor sets it as PDFsharp's process-wide resolver, once (`GlobalFontSettings.FontResolver ??= ...`). The fonts are embedded as subsets.
- **The package**, `Directory.Packages.props` and `Billing.Infrastructure.csproj`: PDFsharp 6.2.4, and `System.Security.Cryptography.Pkcs` 11.0.0 referenced directly to lift the 8.0.1 PDFsharp depends on, whose advisories (NU1902, NU1903) fail the Release build. `.github/dependabot.yml` ignores every PDFsharp update.
- **Rendered on request**, `src/Modules/Billing/Billing.Application/Queries/DownloadInvoicePdf/DownloadInvoicePdfHandler.cs`: the invoice is read through the tenant's rows, so another tenant's invoice is `404 billing.invoice-not-found`, and rendered on every request from what is stored. Nothing is kept in blob storage. `GET /billing/invoices/{invoiceId}/pdf` (`src/Modules/Billing/Billing.Api/Endpoints/InvoiceEndpoints.cs`) answers `application/pdf` as `invoice-<number>.pdf`; the number is letters, digits and hyphens, safe in a file name.
- **Tests**, reading the text back with PdfPig 0.1.16 (Apache-2.0, test projects only, `tests/Modules/Billing.Tests/Billing.Tests.csproj`):
  - `tests/Modules/Billing.Tests/Pdf/PdfSharpInvoiceRendererTests.cs`: the number, dates, every line and the total are in the text; the dates are Gregorian under `th-TH`; the most lines of the longest descriptions span several pages and every line is there; a Polish description outside the Western European code page keeps every character; the widest number of the longest length fits on the page (every letter's bounding box inside it).
  - `RecordingInvoicePdfRenderer.cs` stands in for the renderer in the handler's tests.
  - Against the AppHost, `tests/Integration.Tests/BillingInvoicesTests.cs`: a customer downloads a PDF holding its number, lines and total; an invoice with the same line twice reads back both; another tenant's invoice is not found rather than forbidden.

## Why

- **PDFsharp under MIT:** the owner's decision on #10. PDFsharp 6.5.0 and the 7.0 previews are published under AGPL-3.0-only, so 6.2.4, the last MIT release, is pinned, and moving past it is a licence decision for the owner, not an update (PR #40; the comment in `.github/dependabot.yml`).
- **Billing, not documents:** the owner left the module open; billing owns invoices (PR #40).
- **Rendered, not stored:** the invoice's rows are the record, and a stored copy could drift from them; nothing in #10 needed a copy kept (PR #40).
- **The fonts from the package:** PDFsharp's Core build reads no system fonts on Linux, and the host's container has none to read (PR #40, `SegoeWpFontResolver`).
- **Invariant dates** (#41, PR #47): a finding from the cross-model review of PR #40. The amounts were already invariant; the dates used the host's culture.

## What it taught

- **Fonts are the first thing that breaks on Linux.** A PDF library that finds fonts on a developer's machine finds none in a container. Serving fonts from the package makes the output the same wherever the host runs, at the cost of one face family for every text.
- **Culture is a correctness bug, not a locale preference.** Under `th-TH`, `yyyy` uses the Buddhist calendar, so an invoice dated 2026-11-01 printed 2569-11-01 while the API and the list kept 2026-11-01. A test under `de-DE` would have passed: `yyyy-MM-dd` with literal dashes prints the same there. The test has to pick a culture that changes the calendar (PR #47).
- **Read the PDF back; do not compare bytes.** PdfPig's words, in reading order, make the assertions about content, and its letters' bounding boxes about layout. Drawing order decides the extracted order, so the renderer draws each row's description before its numbers. The review of PR #40 found a wide invoice number running off the page; the test that now holds it checks every letter's box against the page width.
- **The library switches encodings by itself.** A description outside WinAnsi kept every character with no code for it: PDFsharp switches to a Unicode-encoded font when the text needs one. The test arrived green and is kept to hold that (PR #40).
- **A library's licence can change under a minor version.** Pinning by licence needs a Dependabot ignore, or a grouped minor update would move it to AGPL. The pinned release also drags in a vulnerable dependency, lifted by a direct reference.
- **The font resolver is process-wide.** PDFsharp allows it to be set once per process, so a second module rendering PDFs would have to share this resolver, not set its own.

## Consequences

- A newer PDFsharp is the owner's licence decision.
- A second module that renders PDFs shares the process-wide font resolver and the pinned licence with billing.
