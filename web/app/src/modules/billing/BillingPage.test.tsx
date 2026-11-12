import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetBillingInvoicesQuery, useLazyGetBillingInvoicesByInvoiceIdPdfQuery } from '@/api/generated';
import { BillingPage } from './BillingPage';

vi.mock('@/api/generated', () => ({
  useGetBillingInvoicesQuery: vi.fn(),
  useLazyGetBillingInvoicesByInvoiceIdPdfQuery: vi.fn(),
}));

const page = {
  items: [
    {
      id: 'i2',
      number: '2026-0042',
      issuedOn: '2026-11-01',
      dueOn: '2026-11-30',
      currency: 'EUR',
      total: 2618.7,
    },
    {
      id: 'i1',
      number: '2026-0041',
      issuedOn: '2026-10-01',
      dueOn: '2026-10-31',
      currency: 'EUR',
      total: 25,
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

const pdf = new Blob(['%PDF'], { type: 'application/pdf' });
const download = vi.fn();

beforeEach(() => {
  vi.mocked(useGetBillingInvoicesQuery).mockReturnValue({ data: page, isLoading: false, isError: false } as never);
  download.mockReset();
  download.mockReturnValue({ unwrap: () => Promise.resolve(pdf) });
  vi.mocked(useLazyGetBillingInvoicesByInvoiceIdPdfQuery).mockReturnValue([download] as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <BillingPage />
    </MemoryRouter>,
  );
}

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test(
  'the list shows each invoice with its number, its dates and its total in its currency, the newest first',
  { timeout: 20_000 },
  () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Invoices' })).toBeInTheDocument();
    const newest = screen.getByRole('row', { name: /2026-0042/ });
    expect(within(newest).getByText('2026-11-01')).toBeInTheDocument();
    expect(within(newest).getByText('2026-11-30')).toBeInTheDocument();
    expect(within(newest).getByText('2,618.70 EUR')).toBeInTheDocument();
    expect(within(screen.getByRole('row', { name: /2026-0041/ })).getByText('25.00 EUR')).toBeInTheDocument();
    expect(useGetBillingInvoicesQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
  },
);

test('downloading an invoice saves its PDF under the invoice number', async () => {
  const user = userEvent.setup();
  const createObjectURL = vi.fn(() => 'blob:the-invoice');
  const revokeObjectURL = vi.fn();
  vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }));
  const clicked: { href: string; download: string }[] = [];
  const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
    clicked.push({ href: this.href, download: this.download });
  });
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Download invoice 2026-0042 as PDF' }));

  expect(download).toHaveBeenCalledWith({ invoiceId: 'i2' });
  expect(createObjectURL).toHaveBeenCalledWith(pdf);
  expect(clicked).toEqual([{ href: 'blob:the-invoice', download: 'invoice-2026-0042.pdf' }]);
  expect(revokeObjectURL).toHaveBeenCalledWith('blob:the-invoice');
  click.mockRestore();
});

test('a PDF that cannot be downloaded says so', async () => {
  const user = userEvent.setup();
  download.mockReturnValue({ unwrap: () => Promise.reject(new Error('404')) });
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Download invoice 2026-0041 as PDF' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Invoice 2026-0041 could not be downloaded.');
});

test('a list that cannot be loaded says so', () => {
  vi.mocked(useGetBillingInvoicesQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);

  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The invoices could not be loaded.');
});

test('above twenty invoices the list pages through them twenty at a time', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetBillingInvoicesQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(useGetBillingInvoicesQuery).toHaveBeenLastCalledWith({ page: 2, pageSize: 20 });
});

test('the Billing page has no accessibility violations axe can find', async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});

test('a download that failed has no accessibility violations axe can find', async () => {
  const user = userEvent.setup();
  download.mockReturnValue({ unwrap: () => Promise.reject(new Error('404')) });
  const { container } = renderPage();
  await user.click(screen.getByRole('button', { name: 'Download invoice 2026-0042 as PDF' }));
  expect(await screen.findByRole('alert')).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
