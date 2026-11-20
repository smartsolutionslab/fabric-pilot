import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { DataTable } from './data-table';
import { columnsOf } from './data-table-columns';

interface Person {
  name: string;
}

const people = columnsOf<Person>();
const columns = people.columns([people.accessor('name', { header: 'Name' })]);
const data: Person[] = [{ name: 'first' }, { name: 'second' }];

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

test('the table shows a header and one row per item', () => {
  render(<DataTable columns={columns} data={data} />);

  expect(screen.getByRole('columnheader', { name: 'Name' })).toBeInTheDocument();
  expect(screen.getAllByRole('row')).toHaveLength(3);
  expect(screen.queryByRole('navigation', { name: 'Pages' })).not.toBeInTheDocument();
});

test('a paged table shows every row it is given and offers the next page while there is one', async () => {
  const user = userEvent.setup();
  const onPageChange = vi.fn();
  render(<DataTable columns={columns} data={data} paging={{ page: 1, pageSize: 2, totalCount: 5, onPageChange }} />);

  expect(screen.getAllByRole('row')).toHaveLength(3);
  const pages = screen.getByRole('navigation', { name: 'Pages' });
  expect(pages).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(onPageChange).toHaveBeenCalledWith(2);
});

test('on the last page a paged table goes back but not further', async () => {
  const user = userEvent.setup();
  const onPageChange = vi.fn();
  render(
    <DataTable
      columns={columns}
      data={[{ name: 'fifth' }]}
      paging={{ page: 3, pageSize: 2, totalCount: 5, onPageChange }}
    />,
  );

  expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();

  await user.click(screen.getByRole('button', { name: 'Previous' }));

  expect(onPageChange).toHaveBeenCalledWith(2);
});

test('a paged table stops offering pages once its rows fit on one', () => {
  const onPageChange = vi.fn();
  const { rerender } = render(
    <DataTable columns={columns} data={data} paging={{ page: 1, pageSize: 2, totalCount: 5, onPageChange }} />,
  );
  expect(screen.getByRole('navigation', { name: 'Pages' })).toBeInTheDocument();

  rerender(<DataTable columns={columns} data={data} paging={{ page: 1, pageSize: 2, totalCount: 2, onPageChange }} />);

  expect(screen.queryByRole('navigation', { name: 'Pages' })).not.toBeInTheDocument();
});

test('a paged table has no accessibility violations axe can find', async () => {
  const { container } = render(
    <DataTable columns={columns} data={data} paging={{ page: 2, pageSize: 2, totalCount: 5, onPageChange: vi.fn() }} />,
  );
  expect(screen.getByRole('navigation', { name: 'Pages' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
