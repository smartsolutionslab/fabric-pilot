import { useState } from 'react';
import { Link } from 'react-router';
import { type TicketSummaryDto, useGetTicketsTicketsQuery } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
const none: TicketSummaryDto[] = [];

const tickets = columnsOf<TicketSummaryDto>();
const columns = tickets.columns([
  tickets.accessor('subject', {
    header: 'Subject',
    cell: (cell) => <Link to={cell.row.original.id}>{cell.getValue()}</Link>,
  }),
  tickets.accessor('priority', { header: 'Priority' }),
  tickets.accessor('raisedAt', { header: 'Raised', cell: (cell) => new Date(cell.getValue()).toLocaleString() }),
  tickets.accessor('attachmentCount', { header: 'Attachments' }),
]);

/** The customer's tenant's tickets, newest first. */
export function TicketsPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetTicketsTicketsQuery({ page, pageSize });

  return (
    <main className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Tickets</h1>
        <Button asChild>
          <Link to="new">Raise a ticket</Link>
        </Button>
      </div>
      {isError ? <p role="alert">The tickets could not be loaded.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
