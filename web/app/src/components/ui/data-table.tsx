import { type ColumnDef, type PaginationState, type RowData, type Updater, useTable } from '@tanstack/react-table';
import { Button } from './button';
import { features, type Features } from './data-table-columns';

/** One page of a list the server pages: `page` counts from 1, as the API's does. */
export interface DataTablePaging {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

interface DataTableProps<TRow extends RowData> {
  columns: ColumnDef<Features, TRow, unknown>[];
  data: TRow[];
  paging?: DataTablePaging;
}

/** A plain table over TanStack Table; keep columns and data stable (module scope or memoised). */
export function DataTable<TRow extends RowData>({ columns, data, paging }: DataTableProps<TRow>) {
  const pagination: PaginationState = paging
    ? { pageIndex: paging.page - 1, pageSize: paging.pageSize }
    : { pageIndex: 0, pageSize: Number.POSITIVE_INFINITY };
  const table = useTable({
    features,
    columns,
    data,
    manualPagination: true,
    rowCount: paging?.totalCount,
    state: { pagination },
    onPaginationChange: (updater: Updater<PaginationState>) => {
      const next = typeof updater === 'function' ? updater(pagination) : updater;
      paging?.onPageChange(next.pageIndex + 1);
    },
  });

  return (
    <>
      <table className="w-full text-sm">
        <thead>
          {table.getHeaderGroups().map((group) => (
            <tr key={group.id}>
              {group.headers.map((header) => (
                <th key={header.id} className="border-b p-2 text-left font-medium">
                  {header.isPlaceholder ? null : <table.FlexRender header={header} />}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row) => (
            <tr key={row.id}>
              {row.getAllCells().map((cell) => (
                <td key={cell.id} className="border-b p-2">
                  <table.FlexRender cell={cell} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
      {paging && table.getPageCount() > 1 ? (
        <nav aria-label="Pages" className="flex gap-2">
          <Button variant="outline" disabled={!table.getCanPreviousPage()} onClick={() => table.previousPage()}>
            Previous
          </Button>
          <Button variant="outline" disabled={!table.getCanNextPage()} onClick={() => table.nextPage()}>
            Next
          </Button>
        </nav>
      ) : null}
    </>
  );
}
