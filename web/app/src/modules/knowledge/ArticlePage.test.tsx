import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetKnowledgeArticlesByArticleIdQuery } from '@/api/generated';
import { ArticlePage } from './ArticlePage';

vi.mock('@/api/generated', () => ({ useGetKnowledgeArticlesByArticleIdQuery: vi.fn() }));

const article = {
  id: 'b1',
  title: 'Printer offline',
  body: 'Check the cable.\n\nThen restart the printer.',
  state: 'Published',
  changedAt: '2026-11-12T09:30:00+00:00',
  publishedAt: '2026-11-12T09:30:00+00:00',
};

beforeEach(() => {
  vi.mocked(useGetKnowledgeArticlesByArticleIdQuery).mockReturnValue({
    data: article,
    isLoading: false,
    isError: false,
  } as never);
});

afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/knowledge/b1']}>
      <Routes>
        <Route path="/knowledge/:articleId" element={<ArticlePage />} />
      </Routes>
    </MemoryRouter>,
  );
}

test('the article shows its title and each paragraph of its text', { timeout: 20_000 }, () => {
  renderPage();

  expect(screen.getByRole('heading', { name: 'Printer offline' })).toBeInTheDocument();
  expect(screen.getByText('Check the cable.')).toBeInTheDocument();
  expect(screen.getByText('Then restart the printer.')).toBeInTheDocument();
  expect(useGetKnowledgeArticlesByArticleIdQuery).toHaveBeenCalledWith({ articleId: 'b1' });
});

test('an article that cannot be read says so', () => {
  vi.mocked(useGetKnowledgeArticlesByArticleIdQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);
  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('This article could not be found.');
});

test('the Article page has no accessibility violations axe can find', async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Printer offline' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
