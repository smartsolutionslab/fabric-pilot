import { useState } from 'react';
import { type InvoiceSummaryDto, useLazyGetBillingInvoicesByInvoiceIdPdfQuery } from '@/api/generated';
import { Button } from '@/components/ui/button';

/** Fetches the PDF with the user's token, then hands it to the browser to save under the invoice's number. */
export function InvoicePdfDownloadButton({ invoice }: { invoice: InvoiceSummaryDto }) {
  const [download] = useLazyGetBillingInvoicesByInvoiceIdPdfQuery();
  const [saving, setSaving] = useState(false);
  const [failed, setFailed] = useState(false);

  async function save() {
    setFailed(false);
    setSaving(true);
    try {
      const content = await download({ invoiceId: invoice.id }).unwrap();
      const url = URL.createObjectURL(content as Blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `invoice-${invoice.number}.pdf`;
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch {
      setFailed(true);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      {/* The visible words stay in the button's name, so a speech user can say what they see (WCAG 2.5.3). */}
      <Button variant="outline" size="sm" disabled={saving} onClick={() => void save()}>
        Download <span className="sr-only">invoice {invoice.number} as</span> PDF
      </Button>
      {failed ? <span role="alert">Invoice {invoice.number} could not be downloaded.</span> : null}
    </>
  );
}
