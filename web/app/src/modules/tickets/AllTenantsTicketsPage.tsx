import { useState } from 'react';
import { Link } from 'react-router';
import { type TicketSummaryDto, useGetTicketsTicketsAllTenantsQuery } from '@/api/generated';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
const none: TicketSummaryDto[] = [];

const tickets = columnsOf<TicketSummaryDto>();
const columns = tickets.columns([
  tickets.accessor('subject', {
    header: 'Subject',
    cell: (cell) => (
      <Link to={cell.row.original.id} className="underline">
        {cell.getValue()}
      </Link>
    ),
  }),
  tickets.accessor('priority', { header: 'Priority' }),
  tickets.accessor('raisedAt', { header: 'Raised', cell: (cell) => new Date(cell.getValue()).toLocaleString() }),
  tickets.accessor('attachmentCount', { header: 'Attachments' }),
]);

/** Every tenant's tickets, newest first, for staff: read only, each opening the ticket for staff. */
export function AllTenantsTicketsPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetTicketsTicketsAllTenantsQuery({ page, pageSize });

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Tickets of every tenant</h1>
      {isError ? <p role="alert">The tickets could not be loaded.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
