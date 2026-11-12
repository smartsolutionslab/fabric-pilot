import { useState } from 'react';
import { type ContractSummaryDto, useGetContractsContractsUpForRenewalQuery } from '@/api/generated';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
const none: ContractSummaryDto[] = [];

const contracts = columnsOf<ContractSummaryDto>();
const columns = contracts.columns([
  contracts.accessor('product', { header: 'Product' }),
  contracts.accessor('endsOn', { header: 'Ends' }),
  // The renewal date and the window are the server's (Contract.RenewsOn, Contract.RenewalWindowDays).
  contracts.accessor('renewsOn', { header: 'Renews' }),
]);

/** Every tenant's contracts up for renewal, the soonest first; for staff. */
export function RenewalsPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetContractsContractsUpForRenewalQuery({ page, pageSize });

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Contracts up for renewal</h1>
      {isError ? <p role="alert">The contracts up for renewal could not be loaded.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
