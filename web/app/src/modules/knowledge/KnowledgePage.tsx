import { type FormEvent, useState } from 'react';
import { Link } from 'react-router';
import { type ArticleSummaryDto, useGetKnowledgeArticlesQuery } from '@/api/generated';
import { RequirePermission } from '@/auth/RequirePermission';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

const pageSize = 20;
const none: ArticleSummaryDto[] = [];

const articles = columnsOf<ArticleSummaryDto>();
const columns = articles.columns([
  articles.accessor('title', {
    header: 'Title',
    cell: (cell) => <Link to={cell.row.original.id}>{cell.getValue()}</Link>,
  }),
  articles.accessor('changedAt', { header: 'Updated', cell: (cell) => new Date(cell.getValue()).toLocaleDateString() }),
]);

/** The knowledge base every tenant shares: the published articles, searched by title and text. */
export function KnowledgePage() {
  const [draft, setDraft] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const { data, isError } = useGetKnowledgeArticlesQuery({ search, page, pageSize });

  const onSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSearch(draft.trim());
    setPage(1);
  };

  return (
    <main className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Knowledge</h1>
        <RequirePermission role="staff">
          <Button asChild variant="outline">
            <Link to="manage">Manage articles</Link>
          </Button>
        </RequirePermission>
      </div>
      <form role="search" onSubmit={onSearch} className="flex items-end gap-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="article-search">Search articles</Label>
          <Input
            id="article-search"
            type="search"
            value={draft}
            onChange={(event) => {
              setDraft(event.target.value);
            }}
          />
        </div>
        <Button type="submit">Search</Button>
      </form>
      {isError ? <p role="alert">The articles could not be loaded.</p> : null}
      {data?.items.length === 0 ? <p>No published article matches.</p> : null}
      <DataTable
        columns={columns}
        data={data?.items ?? none}
        paging={{ page, pageSize, totalCount: Number(data?.totalCount ?? 0), onPageChange: setPage }}
      />
    </main>
  );
}
