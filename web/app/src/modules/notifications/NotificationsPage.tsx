import { useState } from 'react';
import { Link } from 'react-router';
import { type NoticeDto, useGetNotificationsNoticesQuery } from '@/api/generated';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
// The profile chose no realtime transport (frontend.realtime: none), so a new notice arrives by asking again.
const pollingInterval = 15_000;
const none: NoticeDto[] = [];

const notices = columnsOf<NoticeDto>();
const columns = notices.columns([
  notices.accessor('text', {
    header: 'Notice',
    cell: (cell) => <Link to={`/tickets/${cell.row.original.ticketId}`}>{cell.getValue()}</Link>,
  }),
  notices.accessor('createdAt', { header: 'Received', cell: (cell) => new Date(cell.getValue()).toLocaleString() }),
]);

/** The signed-in customer's notices, newest first; a new one shows up without a reload. */
export function NotificationsPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetNotificationsNoticesQuery({ page, pageSize }, { pollingInterval });

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Notifications</h1>
      {isError ? <p role="alert">The notifications could not be loaded.</p> : null}
      {data?.items.length === 0 ? <p className="text-muted-foreground">No notifications yet.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
