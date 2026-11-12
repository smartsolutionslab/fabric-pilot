import { useState } from 'react';
import { type ContractSummaryDto, useGetContractsContractsQuery } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
const none: ContractSummaryDto[] = [];

const contracts = columnsOf<ContractSummaryDto>();
const columns = contracts.columns([
  contracts.accessor('product', { header: 'Product' }),
  contracts.accessor('startsOn', { header: 'Starts' }),
  contracts.accessor('endsOn', { header: 'Ends' }),
  // The renewal date is the server's (Contract.RenewsOn); the screen never works one out from the end date.
  contracts.accessor('renewsOn', { header: 'Renews', cell: (cell) => cell.getValue() ?? 'Ends without renewal' }),
]);

/** The customer's tenant's contracts, the next to end first. */
export function ContractsPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetContractsContractsQuery({ page, pageSize });
  const total = Number(data?.totalCount ?? 0);

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Contracts</h1>
      {isError ? <p role="alert">The contracts could not be loaded.</p> : null}
      <DataTable columns={columns} data={data?.items ?? none} />
      {total > pageSize ? (
        <nav aria-label="Pages" className="flex gap-2">
          <Button variant="outline" disabled={page === 1} onClick={() => setPage(page - 1)}>
            Previous
          </Button>
          <Button variant="outline" disabled={page * pageSize >= total} onClick={() => setPage(page + 1)}>
            Next
          </Button>
        </nav>
      ) : null}
    </main>
  );
}
