import { cleanup, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetKnowledgeArticlesManageQuery } from '@/api/generated';
import { ManageArticlesPage } from './ManageArticlesPage';

vi.mock('@/api/generated', () => ({ useGetKnowledgeArticlesManageQuery: vi.fn() }));

const page = {
  items: [
    { id: 'd1', title: 'Printer drivers', state: 'Draft', changedAt: '2026-11-12T10:00:00+00:00', publishedAt: null },
    {
      id: 'b1',
      title: 'Printer offline',
      state: 'Published',
      changedAt: '2026-11-12T09:30:00+00:00',
      publishedAt: '2026-11-12T09:30:00+00:00',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

beforeEach(() => {
  vi.mocked(useGetKnowledgeArticlesManageQuery).mockReturnValue({
    data: page,
    isLoading: false,
    isError: false,
  } as never);
});

afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <ManageArticlesPage />
    </MemoryRouter>,
  );
}

test('staff see every article with its state, each linking to its editor', { timeout: 20_000 }, () => {
  renderPage();

  expect(screen.getByRole('heading', { name: 'Manage articles' })).toBeInTheDocument();
  const draft = screen.getByRole('row', { name: /Printer drivers/ });
  expect(within(draft).getByRole('link', { name: 'Printer drivers' })).toHaveAttribute('href', '/d1');
  expect(within(draft).getByText('Draft')).toBeInTheDocument();
  expect(within(screen.getByRole('row', { name: /Printer offline/ })).getByText('Published')).toBeInTheDocument();
  expect(useGetKnowledgeArticlesManageQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
});

test('staff are offered to write an article', () => {
  renderPage();

  expect(screen.getByRole('link', { name: 'Write an article' })).toHaveAttribute('href', '/new');
});

test('the Manage articles page has no accessibility violations axe can find', async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Manage articles' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
