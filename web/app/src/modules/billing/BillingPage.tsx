import { useState } from 'react';
import { type InvoiceSummaryDto, useGetBillingInvoicesQuery } from '@/api/generated';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';
import { InvoicePdfDownloadButton } from './InvoicePdfDownloadButton';
import { useCanManage } from './useCanManage';

const pageSize = 20;
const none: InvoiceSummaryDto[] = [];

/** Two decimals and a comma between thousands, as the PDF writes them. */
const amount = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const invoices = columnsOf<InvoiceSummaryDto>();
const columns = invoices.columns([
  invoices.accessor('number', { header: 'Number' }),
  invoices.accessor('issuedOn', { header: 'Issued' }),
  invoices.accessor('dueOn', { header: 'Due' }),
  invoices.accessor('total', {
    header: 'Total',
    cell: (cell) => `${amount.format(Number(cell.getValue()))} ${cell.row.original.currency}`,
  }),
  invoices.display({
    id: 'pdf',
    header: 'PDF',
    cell: (cell) => <InvoicePdfDownloadButton invoice={cell.row.original} />,
  }),
]);

/** The customer's tenant's invoices, the newest first, each downloadable as a PDF. */
export function BillingPage() {
  const permission = useCanManage();
  const [page, setPage] = useState(1);
  const { data, isError } = useGetBillingInvoicesQuery({ page, pageSize });

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Billing</h1>
      {permission === 'granted' && <h2>Manage</h2>}
      {permission === 'failed' && <p role="alert">Permissions for billing could not be loaded.</p>}
      <h2 className="text-xl font-semibold">Invoices</h2>
      {isError ? <p role="alert">The invoices could not be loaded.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
