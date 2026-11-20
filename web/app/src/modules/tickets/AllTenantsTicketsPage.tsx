import { useState } from 'react';
import { Link } from 'react-router';
import {
  type GetTicketsTicketsAllTenantsApiArg,
  type QueuedTicketDto,
  type TicketPriority,
  type TicketStatus,
  useGetTenantsQuery,
  useGetTicketsTicketsAllTenantsQuery,
} from '@/api/generated';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';
import { Label } from '@/components/ui/label';
import { NativeSelect } from '@/components/ui/native-select';
import { priorities } from './raiseTicketSchema';
import { statusLabel, ticketStatuses } from './ticketStatuses';

const pageSize = 20;
const none: QueuedTicketDto[] = [];

type Filters = Pick<GetTicketsTicketsAllTenantsApiArg, 'tenant' | 'status' | 'priority'>;
type Age = Pick<GetTicketsTicketsAllTenantsApiArg, 'sortBy' | 'sortDirection'>;

const tickets = columnsOf<QueuedTicketDto>();
const columns = tickets.columns([
  tickets.accessor('tenant', { header: 'Tenant' }),
  tickets.accessor('subject', {
    header: 'Subject',
    cell: (cell) => (
      <Link to={cell.row.original.id} className="underline">
        {cell.getValue()}
      </Link>
    ),
  }),
  tickets.accessor('priority', { header: 'Priority' }),
  tickets.accessor('status', { header: 'Status', cell: (cell) => statusLabel(cell.getValue()) }),
  tickets.accessor('assignee', { header: 'Assignee', cell: (cell) => cell.getValue() ?? 'Unassigned' }),
  tickets.accessor('raisedAt', { header: 'Raised', cell: (cell) => new Date(cell.getValue()).toLocaleString() }),
  tickets.accessor('attachmentCount', { header: 'Attachments' }),
]);

/** A filter's value, or none when it shows every ticket. */
const chosen = <T extends string>(value: string) => (value === '' ? undefined : (value as T));

/**
 * The staff triage queue: every tenant's tickets, filtered by tenant, status and priority and sorted by age, each
 * opening the ticket for staff to triage.
 */
export function AllTenantsTicketsPage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<Filters>({});
  const [age, setAge] = useState<Age>({});
  // The host lists at most 100 tenants a page; the pilot serves far fewer.
  const { data: tenants } = useGetTenantsQuery({ page: 1, pageSize: 100 });
  const { data, isError } = useGetTicketsTicketsAllTenantsQuery({ page, pageSize, ...filters, ...age });
  // Newest first until staff ask otherwise, as the API answers.
  const oldestFirst = age.sortDirection === 'Ascending';

  const sortByAge = () => {
    setAge({ sortBy: 'RaisedAt', sortDirection: oldestFirst ? 'Descending' : 'Ascending' });
    setPage(1);
  };

  const filterBy = (next: Filters) => {
    setFilters({ ...filters, ...next });
    setPage(1);
  };

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Triage queue</h1>
      <div className="flex flex-wrap gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="tenant-filter">Tenant</Label>
          <NativeSelect
            id="tenant-filter"
            value={filters.tenant ?? ''}
            onChange={(event) => filterBy({ tenant: chosen(event.target.value) })}
          >
            <option value="">All tenants</option>
            {tenants?.items.map((tenant) => (
              <option key={tenant} value={tenant}>
                {tenant}
              </option>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="status-filter">Status</Label>
          <NativeSelect
            id="status-filter"
            value={filters.status ?? ''}
            onChange={(event) => filterBy({ status: chosen<TicketStatus>(event.target.value) })}
          >
            <option value="">All statuses</option>
            {ticketStatuses.map(({ value, label }) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="priority-filter">Priority</Label>
          <NativeSelect
            id="priority-filter"
            value={filters.priority ?? ''}
            onChange={(event) => filterBy({ priority: chosen<TicketPriority>(event.target.value) })}
          >
            <option value="">All priorities</option>
            {priorities.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col justify-end">
          <Button variant="outline" onClick={sortByAge}>
            {oldestFirst ? 'Newest first' : 'Oldest first'}
          </Button>
        </div>
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
