import { useState } from 'react';
import { Link } from 'react-router';
import { type ArticleSummaryDto, useGetKnowledgeArticlesManageQuery } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';

const pageSize = 20;
const none: ArticleSummaryDto[] = [];

const articles = columnsOf<ArticleSummaryDto>();
const columns = articles.columns([
  articles.accessor('title', {
    header: 'Title',
    cell: (cell) => <Link to={cell.row.original.id}>{cell.getValue()}</Link>,
  }),
  articles.accessor('state', { header: 'State' }),
  articles.accessor('changedAt', { header: 'Changed', cell: (cell) => new Date(cell.getValue()).toLocaleString() }),
]);

/** Every article of the shared knowledge base, drafts included, for the staff who write it. */
export function ManageArticlesPage() {
  const [page, setPage] = useState(1);
  const { data, isError } = useGetKnowledgeArticlesManageQuery({ page, pageSize });

  return (
    <main className="flex flex-col gap-4 p-4">
      <Link to="..">Back to the knowledge base</Link>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Manage articles</h1>
        <Button asChild>
          <Link to="new">Write an article</Link>
        </Button>
      </div>
      {isError ? <p role="alert">The articles could not be loaded.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
